using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > BeatRun > MIDI Chart Tool
///
/// Xem và lọc nốt của file MIDI trong StreamingAssets/MIDI để tinh chỉnh chart từng màn:
///   - Danh sách track: tên, channel, số nốt, khoảng cao độ.
///   - Lọc theo track, channel, cao độ, khoảng thời gian, khoảng cách tối thiểu, số nốt tối đa mỗi giây.
///   - Xem mật độ nốt theo từng đoạn 10 giây.
///   - Xuất file MIDI sạch (giữ nguyên tempo) để LevelData dùng ngay, hoặc xuất CSV để xem bằng Excel.
/// File xuất có track tên "Main Melody (clean)" -> MidiReader (Melody) tự chọn track này.
/// </summary>
public class MidiChartTool : EditorWindow
{
    // ================= DỮ LIỆU MIDI =================

    private class Note
    {
        public int track, channel, pitch, velocity;
        public long startTick, endTick;
        public double time, duration;
    }

    private class TrackInfo
    {
        public int index;
        public string name = "";
        public int noteCount;
        public HashSet<int> channels = new HashSet<int>();
        public int minPitch = 127, maxPitch = 0;
        public bool include = true;
    }

    private struct TempoEvent { public long tick; public int usPerQuarter; }

    private string[] files = new string[0];
    private int fileIndex;
    private string loadedPath;
    private int tpq = 480;
    private List<Note> allNotes = new List<Note>();
    private List<TrackInfo> tracks = new List<TrackInfo>();
    private List<TempoEvent> tempos = new List<TempoEvent>();
    private List<byte[]> metaTrackEvents = new List<byte[]>();   // tempo / time signature để xuất lại

    // ================= BỘ LỌC =================

    private int channelFilter = -1;                 // -1 = mọi channel
    private int minPitch = 0, maxPitch = 127;
    private float fromSec = 0f, toSec = 0f;         // toSec = 0 -> đến hết bài
    private float minGap = 0.15f;                   // giây giữa 2 nốt giữ lại
    private int maxPerSecond = 0;                   // 0 = không giới hạn
    private bool mergeChords = true;                // nốt cùng lúc (< 30 ms) -> giữ 1
    private int minVelocity = 1;

    private List<Note> filtered = new List<Note>();
    private Vector2 scroll;

    [MenuItem("Tools/BeatRun/MIDI Chart Tool")]
    private static void Open() => GetWindow<MidiChartTool>("MIDI Chart Tool");

    private static string MidiFolder => Path.Combine(Application.streamingAssetsPath, "MIDI");

    private void OnEnable() => RefreshFileList();

    private void RefreshFileList()
    {
        files = Directory.Exists(MidiFolder)
            ? Directory.GetFiles(MidiFolder, "*.mid").Select(Path.GetFileName).OrderBy(n => n).ToArray()
            : new string[0];
        fileIndex = Mathf.Clamp(fileIndex, 0, Mathf.Max(0, files.Length - 1));
    }

    // ================= GIAO DIỆN =================

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("File MIDI (StreamingAssets/MIDI)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        fileIndex = EditorGUILayout.Popup(fileIndex, files);
        if (GUILayout.Button("↻", GUILayout.Width(26))) RefreshFileList();
        GUI.enabled = files.Length > 0;
        if (GUILayout.Button("Đọc", GUILayout.Width(60))) Load(Path.Combine(MidiFolder, files[fileIndex]));
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        if (allNotes.Count == 0)
        {
            EditorGUILayout.HelpBox("Chọn file rồi bấm 'Đọc'.", MessageType.Info);
            EditorGUILayout.EndScrollView();
            return;
        }

        double length = allNotes.Max(n => n.time + n.duration);
        EditorGUILayout.LabelField($"TPQ {tpq} · {tempos.Count} lần đổi tempo · dài ~{length:F1}s · tổng {allNotes.Count} nốt");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Track (bỏ tick để loại)", EditorStyles.boldLabel);
        foreach (TrackInfo t in tracks)
        {
            if (t.noteCount == 0) continue;
            t.include = EditorGUILayout.ToggleLeft(
                $"#{t.index} \"{t.name}\" · {t.noteCount} nốt · ch {string.Join(",", t.channels.Select(c => c + 1))} · pitch {t.minPitch}-{t.maxPitch}",
                t.include);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Bộ lọc", EditorStyles.boldLabel);
        channelFilter = EditorGUILayout.IntSlider("Channel (0 = tất cả)", channelFilter + 1, 0, 16) - 1;
        EditorGUILayout.BeginHorizontal();
        minPitch = EditorGUILayout.IntField("Pitch từ", minPitch);
        maxPitch = EditorGUILayout.IntField("đến", maxPitch);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        fromSec = EditorGUILayout.FloatField("Thời gian từ (s)", fromSec);
        toSec = EditorGUILayout.FloatField("đến (0 = hết)", toSec);
        EditorGUILayout.EndHorizontal();
        minVelocity = EditorGUILayout.IntSlider("Velocity tối thiểu", minVelocity, 1, 127);
        mergeChords = EditorGUILayout.Toggle("Gộp hợp âm (cùng lúc → 1 nốt)", mergeChords);
        minGap = EditorGUILayout.Slider("Khoảng cách tối thiểu (s)", minGap, 0f, 1f);
        maxPerSecond = EditorGUILayout.IntSlider("Tối đa nốt / giây (0 = không)", maxPerSecond, 0, 12);

        if (GUILayout.Button("Áp dụng lọc", GUILayout.Height(24))) ApplyFilter();

        if (filtered.Count > 0)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Sau lọc: {filtered.Count} nốt", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(DensityReport(filtered, length), MessageType.None);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Xuất MIDI sạch", GUILayout.Height(26))) ExportMidi();
            if (GUILayout.Button("Xuất CSV", GUILayout.Height(26))) ExportCsv();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    // ================= ĐỌC MIDI =================

    private void Load(string path)
    {
        allNotes.Clear(); tracks.Clear(); tempos.Clear(); metaTrackEvents.Clear(); filtered.Clear();

        try
        {
            byte[] d = File.ReadAllBytes(path);
            int p = 0;
            if (ReadStr(d, ref p, 4) != "MThd") throw new Exception("Không phải file MIDI");
            int hlen = (int)ReadU32(d, ref p);
            int hstart = p;
            ReadU16(d, ref p); // format
            int ntrk = ReadU16(d, ref p);
            tpq = ReadU16(d, ref p);
            if ((tpq & 0x8000) != 0) throw new Exception("Chưa hỗ trợ MIDI chia thời gian SMPTE");
            p = hstart + hlen;

            for (int ti = 0; ti < ntrk && p + 8 <= d.Length; ti++)
            {
                string id = ReadStr(d, ref p, 4);
                int len = (int)ReadU32(d, ref p);
                int end = p + len;
                if (id != "MTrk") { p = end; ti--; continue; }

                TrackInfo info = new TrackInfo { index = ti };
                ParseTrack(d, p, end, info);
                tracks.Add(info);
                p = end;
            }

            tempos.Sort((a, b) => a.tick.CompareTo(b.tick));
            if (tempos.Count == 0 || tempos[0].tick > 0)
                tempos.Insert(0, new TempoEvent { tick = 0, usPerQuarter = 500000 });

            foreach (Note n in allNotes)
            {
                n.time = TickToSec(n.startTick);
                n.duration = TickToSec(n.endTick) - n.time;
            }
            allNotes.Sort((a, b) => a.time.CompareTo(b.time));

            loadedPath = path;
            toSec = 0f;
            ApplyFilter();
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("MIDI Chart Tool", "Lỗi đọc MIDI: " + e.Message, "OK");
        }
    }

    private void ParseTrack(byte[] d, int p, int end, TrackInfo info)
    {
        long tick = 0;
        int status = 0;
        var open = new Dictionary<int, Note>();   // key = channel*128 + pitch

        while (p < end)
        {
            tick += ReadVar(d, ref p);
            int b = d[p];

            if (b == 0xFF)
            {
                int type = d[p + 1];
                p += 2;
                int len = (int)ReadVar(d, ref p);

                if (type == 0x03) info.name = Encoding.UTF8.GetString(d, p, len);
                if (type == 0x51 && len == 3)
                    tempos.Add(new TempoEvent { tick = tick, usPerQuarter = (d[p] << 16) | (d[p + 1] << 8) | d[p + 2] });
                if (type == 0x51 || type == 0x58)
                {
                    // Lưu lại để xuất: tick(8 byte) + type + data
                    byte[] ev = new byte[9 + len];
                    BitConverter.GetBytes(tick).CopyTo(ev, 0);
                    ev[8] = (byte)type;
                    Array.Copy(d, p, ev, 9, len);
                    metaTrackEvents.Add(ev);
                }
                p += len;
                continue;
            }

            if (b == 0xF0 || b == 0xF7)
            {
                p++;
                int len = (int)ReadVar(d, ref p);
                p += len;
                continue;
            }

            if ((b & 0x80) != 0) { status = b; p++; }

            int hi = status & 0xF0, ch = status & 0x0F;

            if (hi == 0xC0 || hi == 0xD0) { p += 1; continue; }

            int a1 = d[p], a2 = d[p + 1];
            p += 2;

            int key = ch * 128 + a1;
            if (hi == 0x90 && a2 > 0)
            {
                if (open.TryGetValue(key, out Note prev)) prev.endTick = tick;
                Note n = new Note { track = info.index, channel = ch, pitch = a1, velocity = a2, startTick = tick, endTick = tick };
                open[key] = n;
                allNotes.Add(n);

                info.noteCount++;
                info.channels.Add(ch);
                info.minPitch = Mathf.Min(info.minPitch, a1);
                info.maxPitch = Mathf.Max(info.maxPitch, a1);
            }
            else if (hi == 0x80 || (hi == 0x90 && a2 == 0))
            {
                if (open.TryGetValue(key, out Note n)) { n.endTick = tick; open.Remove(key); }
            }
        }

        foreach (Note n in open.Values)
            if (n.endTick <= n.startTick) n.endTick = n.startTick + tpq / 4;
    }

    private double TickToSec(long tick)
    {
        double sec = 0;
        long prevTick = 0;
        int us = 500000;
        foreach (TempoEvent t in tempos)
        {
            if (t.tick >= tick) break;
            sec += (t.tick - prevTick) * (double)us / tpq / 1e6;
            prevTick = t.tick;
            us = t.usPerQuarter;
        }
        return sec + (tick - prevTick) * (double)us / tpq / 1e6;
    }

    // ================= LỌC =================

    private void ApplyFilter()
    {
        HashSet<int> keepTracks = new HashSet<int>(tracks.Where(t => t.include).Select(t => t.index));
        double to = toSec > 0 ? toSec : double.MaxValue;

        List<Note> list = allNotes.Where(n =>
            keepTracks.Contains(n.track) &&
            (channelFilter < 0 || n.channel == channelFilter) &&
            n.pitch >= minPitch && n.pitch <= maxPitch &&
            n.velocity >= minVelocity &&
            n.time >= fromSec && n.time <= to).ToList();

        // Hợp âm: giữ nốt cao nhất trong nhóm cách nhau < 30 ms
        if (mergeChords)
        {
            List<Note> merged = new List<Note>();
            foreach (Note n in list)
            {
                if (merged.Count > 0 && n.time - merged[merged.Count - 1].time < 0.03)
                {
                    if (n.pitch > merged[merged.Count - 1].pitch) merged[merged.Count - 1] = n;
                    continue;
                }
                merged.Add(n);
            }
            list = merged;
        }

        // Khoảng cách tối thiểu
        if (minGap > 0f)
        {
            List<Note> spaced = new List<Note>();
            double last = double.MinValue;
            foreach (Note n in list)
            {
                if (n.time - last < minGap) continue;
                spaced.Add(n);
                last = n.time;
            }
            list = spaced;
        }

        // Tối đa N nốt trong cửa sổ 1 giây trượt
        if (maxPerSecond > 0)
        {
            List<Note> capped = new List<Note>();
            Queue<double> window = new Queue<double>();
            foreach (Note n in list)
            {
                while (window.Count > 0 && n.time - window.Peek() >= 1.0) window.Dequeue();
                if (window.Count >= maxPerSecond) continue;
                window.Enqueue(n.time);
                capped.Add(n);
            }
            list = capped;
        }

        filtered = list;
        Repaint();
    }

    private static string DensityReport(List<Note> notes, double length)
    {
        StringBuilder sb = new StringBuilder("Mật độ (nốt / 10 giây):\n");
        int buckets = Mathf.Max(1, Mathf.CeilToInt((float)length / 10f));
        int[] count = new int[buckets];
        foreach (Note n in notes)
            count[Mathf.Clamp((int)(n.time / 10.0), 0, buckets - 1)]++;

        int max = Mathf.Max(1, count.Max());
        for (int i = 0; i < buckets; i++)
            sb.AppendLine($"{i * 10,4}-{i * 10 + 10,-4}s {new string('█', Mathf.CeilToInt(count[i] * 30f / max))} {count[i]}");

        sb.Append($"Trung bình {notes.Count / Math.Max(1.0, length):F2} nốt/giây");
        return sb.ToString();
    }

    // ================= XUẤT =================

    private void ExportCsv()
    {
        string path = EditorUtility.SaveFilePanel("Xuất CSV", MidiFolder, Path.GetFileNameWithoutExtension(loadedPath) + "_notes", "csv");
        if (string.IsNullOrEmpty(path)) return;

        StringBuilder sb = new StringBuilder("time_sec,duration_sec,pitch,velocity,track,channel\n");
        foreach (Note n in filtered)
            sb.AppendLine($"{n.time:F4},{n.duration:F4},{n.pitch},{n.velocity},{n.track},{n.channel + 1}");

        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        EditorUtility.RevealInFinder(path);
    }

    private void ExportMidi()
    {
        string defName = Path.GetFileNameWithoutExtension(loadedPath) + "_clean";
        string path = EditorUtility.SaveFilePanel("Xuất MIDI sạch", MidiFolder, defName, "mid");
        if (string.IsNullOrEmpty(path)) return;

        // Track 0: tempo + nhịp (giữ nguyên thời gian của bài)
        List<(long tick, byte[] data)> tempoTrack = metaTrackEvents
            .Select(ev => (BitConverter.ToInt64(ev, 0), MetaEvent(ev[8], ev.Skip(9).ToArray())))
            .OrderBy(e => e.Item1).ToList();

        // Track 1: nốt đã lọc
        List<(long tick, byte[] data)> noteTrack = new List<(long, byte[])>
        {
            (0, MetaEvent(0x03, Encoding.UTF8.GetBytes("Main Melody (clean)")))
        };
        foreach (Note n in filtered)
        {
            long endTick = Math.Max(n.startTick + 1, n.endTick);
            noteTrack.Add((n.startTick, new[] { (byte)(0x90 | n.channel), (byte)n.pitch, (byte)Mathf.Max(1, n.velocity) }));
            noteTrack.Add((endTick, new[] { (byte)(0x80 | n.channel), (byte)n.pitch, (byte)0 }));
        }
        // note-off trước note-on khi cùng tick
        noteTrack = noteTrack.OrderBy(e => e.Item1).ThenBy(e => (e.Item2[0] & 0xF0) == 0x80 ? 0 : 1).ToList();

        using (MemoryStream ms = new MemoryStream())
        {
            WriteStr(ms, "MThd"); WriteU32(ms, 6); WriteU16(ms, 1); WriteU16(ms, 2); WriteU16(ms, tpq);
            WriteTrack(ms, tempoTrack);
            WriteTrack(ms, noteTrack);
            File.WriteAllBytes(path, ms.ToArray());
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("MIDI Chart Tool",
            $"Đã xuất {filtered.Count} nốt:\n{Path.GetFileName(path)}\n\n" +
            "Trong LevelData: đặt MIDI File Name = tên file này, Forced Track Index = 1.", "OK");
        RefreshFileList();
    }

    private static byte[] MetaEvent(int type, byte[] data)
    {
        List<byte> b = new List<byte> { 0xFF, (byte)type };
        b.AddRange(VarLen(data.Length));
        b.AddRange(data);
        return b.ToArray();
    }

    private static void WriteTrack(Stream s, List<(long tick, byte[] data)> events)
    {
        using (MemoryStream body = new MemoryStream())
        {
            long last = 0;
            foreach (var e in events)
            {
                byte[] dt = VarLen((int)(e.tick - last));
                body.Write(dt, 0, dt.Length);
                body.Write(e.data, 0, e.data.Length);
                last = e.tick;
            }
            byte[] eot = { 0x00, 0xFF, 0x2F, 0x00 };
            body.Write(eot, 0, eot.Length);

            WriteStr(s, "MTrk");
            WriteU32(s, (uint)body.Length);
            body.WriteTo(s);
        }
    }

    // ================= BYTE HELPERS =================

    private static string ReadStr(byte[] d, ref int p, int n) { string s = Encoding.ASCII.GetString(d, p, n); p += n; return s; }
    private static uint ReadU32(byte[] d, ref int p) { uint v = (uint)(d[p] << 24 | d[p + 1] << 16 | d[p + 2] << 8 | d[p + 3]); p += 4; return v; }
    private static int ReadU16(byte[] d, ref int p) { int v = d[p] << 8 | d[p + 1]; p += 2; return v; }
    private static long ReadVar(byte[] d, ref int p)
    {
        long v = 0;
        while (true)
        {
            byte b = d[p++];
            v = (v << 7) | (uint)(b & 0x7F);
            if ((b & 0x80) == 0) return v;
        }
    }

    private static byte[] VarLen(int value)
    {
        List<byte> bytes = new List<byte> { (byte)(value & 0x7F) };
        value >>= 7;
        while (value > 0)
        {
            bytes.Insert(0, (byte)((value & 0x7F) | 0x80));
            value >>= 7;
        }
        return bytes.ToArray();
    }

    private static void WriteStr(Stream s, string t) { byte[] b = Encoding.ASCII.GetBytes(t); s.Write(b, 0, b.Length); }
    private static void WriteU32(Stream s, uint v) { s.WriteByte((byte)(v >> 24)); s.WriteByte((byte)(v >> 16)); s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
    private static void WriteU16(Stream s, int v) { s.WriteByte((byte)(v >> 8)); s.WriteByte((byte)v); }
}
