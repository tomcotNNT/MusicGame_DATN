using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

public class MidiReader : MonoBehaviour
{
    // =====================================================
    // DATA
    // =====================================================

    [Serializable]
    public class RhythmNote
    {
        public float time;      // giây (đã cộng timeOffset)
        public int pitch;
        public int velocity;
        public int lane;
        public float duration;  // giây (dùng sau này cho key dài)

        public RhythmNote(float time, int pitch, int velocity, int lane, float duration = 0f)
        {
            this.time = time;
            this.pitch = pitch;
            this.velocity = velocity;
            this.lane = lane;
            this.duration = duration;
        }
    }

    public enum LaneMode
    {
        Contour,            // nốt cao hơn nốt trước -> lane 1, thấp hơn -> lane 0
        SplitByMedian,      // chia theo cao độ trung vị của bài (tự cân bằng)
        SplitByFixedPitch   // chia theo splitPitch cố định
    }

    private class RawNote
    {
        public long startTick;
        public long endTick;
        public int channel;
        public int pitch;
        public int velocity;
    }

    private class TrackData
    {
        public int index;
        public string name = "";
        public List<RawNote> notes = new List<RawNote>();
    }

    private struct TempoPoint
    {
        public long tick;
        public double seconds;
        public int tempo; // microseconds / quarter
    }

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Chọn track")]
    [Tooltip("Bỏ qua channel 10 (trống)")]
    [SerializeField] private bool ignoreDrums = true;

    // Giá trị theo từng level, được nạp từ LevelData trong LoadMidi()
    private int forcedTrackIndex = -1;
    private float minimumNoteDistance = 0.15f;
    private float timeOffset = 0f;

    [Header("Lane")]
    [SerializeField] private int laneCount = 2;
    [SerializeField] private LaneMode laneMode = LaneMode.Contour;
    [SerializeField] private int splitPitch = 60;
    [Tooltip("Tối đa bao nhiêu key liên tiếp cùng lane (0 = không giới hạn)")]
    [SerializeField] private int maxSameLane = 3;

    public List<RhythmNote> Notes { get; private set; } = new List<RhythmNote>();
    public float Bpm { get; private set; } = 120f;

    private int ticksPerQuarterNote;
    private readonly List<TempoPoint> tempoMap = new List<TempoPoint>();

    // =====================================================
    // LOAD
    // =====================================================

    public void LoadMidi(LevelData level)
    {
        Notes.Clear();

        if (level == null)
        {
            Debug.LogError("LevelData chưa được gán!");
            return;
        }

        if (string.IsNullOrEmpty(level.midiFileName))
        {
            Debug.LogError("Level chưa có tên MIDI!");
            return;
        }

        forcedTrackIndex = level.forcedTrackIndex;
        timeOffset = level.timeOffset;
        minimumNoteDistance = level.GetMinNoteDistance();

        string path = Path.Combine(
            Application.streamingAssetsPath,
            "MIDI",
            level.midiFileName
        );

        byte[] data = ReadAllBytesCompat(path);

        if (data == null)
            return;

        Debug.Log("Loading MIDI: " + path);

        try
        {
            ParseMidi(data);
        }
        catch (Exception e)
        {
            Notes.Clear();
            Debug.LogError("Lỗi đọc MIDI (file hỏng hoặc không hỗ trợ): " + e.Message);
        }
    }

    private byte[] ReadAllBytesCompat(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // StreamingAssets trên Android nằm trong APK, phải đọc qua UnityWebRequest
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) { }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Không tải được MIDI: " + req.error);
                return null;
            }

            return req.downloadHandler.data;
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError("Không tìm thấy MIDI: " + path);
            return null;
        }

        return File.ReadAllBytes(path);
#endif
    }

    // =====================================================
    // PARSE
    // =====================================================

    private void ParseMidi(byte[] data)
    {
        int pos = 0;

        if (ReadString(data, ref pos, 4) != "MThd")
        {
            Debug.LogError("File không phải MIDI hợp lệ.");
            return;
        }

        int headerLength = ReadInt32(data, ref pos);
        int format = ReadUInt16(data, ref pos);
        int trackCount = ReadUInt16(data, ref pos);
        int division = ReadUInt16(data, ref pos);

        if ((division & 0x8000) != 0)
        {
            Debug.LogError("MIDI dùng SMPTE time division, script này chỉ hỗ trợ PPQ.");
            return;
        }

        ticksPerQuarterNote = division;
        pos = 8 + headerLength;

        Debug.Log($"MIDI Format: {format} | Tracks: {trackCount} | Ticks/Quarter: {ticksPerQuarterNote}");

        var tracks = new List<TrackData>();
        var tempoRaw = new List<KeyValuePair<long, int>>();
        int trackIndex = 0;

        // Đọc từng chunk, bỏ qua chunk lạ
        while (pos + 8 <= data.Length)
        {
            string id = ReadString(data, ref pos, 4);
            int length = ReadInt32(data, ref pos);

            if (length < 0)
                break;

            int end = Math.Min(pos + length, data.Length);

            if (id == "MTrk")
            {
                tracks.Add(ParseTrack(data, pos, end, trackIndex, tempoRaw));
                trackIndex++;
            }

            pos = end;
        }

        BuildTempoMap(tempoRaw);

        TrackData chosen = ChooseTrack(tracks);

        if (chosen == null)
        {
            Debug.LogError("MIDI không có track nào chứa nốt.");
            return;
        }

        BuildGameplayNotes(chosen);
    }

    private TrackData ParseTrack(
        byte[] data, int start, int end, int trackIndex,
        List<KeyValuePair<long, int>> tempoRaw)
    {
        var track = new TrackData { index = trackIndex };
        var open = new Dictionary<int, Stack<RawNote>>();

        int pos = start;
        long tick = 0;
        byte running = 0;

        while (pos < end)
        {
            tick += ReadVariableLength(data, ref pos);

            byte status = data[pos];

            if ((status & 0x80) != 0)
            {
                pos++;
                // Meta/SysEx xóa running status
                running = status < 0xF0 ? status : (byte)0;
            }
            else
            {
                status = running;
                if (status == 0)
                    break; // dữ liệu hỏng
            }

            if (status == 0xFF)
            {
                byte metaType = data[pos++];
                int metaLength = (int)ReadVariableLength(data, ref pos);

                if (metaType == 0x51 && metaLength == 3)
                {
                    int tempo = (data[pos] << 16) | (data[pos + 1] << 8) | data[pos + 2];
                    tempoRaw.Add(new KeyValuePair<long, int>(tick, tempo));
                }
                else if (metaType == 0x03)
                {
                    track.name = Encoding.ASCII.GetString(data, pos, metaLength);
                }

                pos += metaLength;

                if (metaType == 0x2F) // End of track
                    break;
            }
            else if (status == 0xF0 || status == 0xF7)
            {
                int length = (int)ReadVariableLength(data, ref pos);
                pos += length;
            }
            else
            {
                int command = status & 0xF0;
                int channel = status & 0x0F;

                int d1 = data[pos++];
                int d2 = 0;

                if (command != 0xC0 && command != 0xD0)
                    d2 = data[pos++];

                int key = channel * 128 + d1;

                if (command == 0x90 && d2 > 0)
                {
                    var note = new RawNote
                    {
                        startTick = tick,
                        endTick = tick,
                        channel = channel,
                        pitch = d1,
                        velocity = d2
                    };

                    track.notes.Add(note);

                    if (!open.TryGetValue(key, out Stack<RawNote> stack))
                    {
                        stack = new Stack<RawNote>();
                        open[key] = stack;
                    }

                    stack.Push(note);
                }
                else if (command == 0x80 || (command == 0x90 && d2 == 0))
                {
                    if (open.TryGetValue(key, out Stack<RawNote> stack) && stack.Count > 0)
                        stack.Pop().endTick = tick;
                }
            }
        }

        return track;
    }

    // =====================================================
    // TEMPO MAP
    // =====================================================

    private void BuildTempoMap(List<KeyValuePair<long, int>> raw)
    {
        tempoMap.Clear();

        var sorted = raw.OrderBy(x => x.Key).ToList();

        if (sorted.Count == 0 || sorted[0].Key > 0)
            sorted.Insert(0, new KeyValuePair<long, int>(0, 500000));

        for (int i = 0; i < sorted.Count; i++)
        {
            double seconds = 0;

            if (i > 0)
            {
                TempoPoint prev = tempoMap[i - 1];
                seconds = prev.seconds +
                          (sorted[i].Key - prev.tick) * prev.tempo / 1000000.0 / ticksPerQuarterNote;
            }

            tempoMap.Add(new TempoPoint
            {
                tick = sorted[i].Key,
                seconds = seconds,
                tempo = sorted[i].Value
            });
        }

        Bpm = 60000000f / tempoMap[0].tempo;
    }

    private float TickToSeconds(long tick)
    {
        int idx = 0;

        for (int i = tempoMap.Count - 1; i >= 0; i--)
        {
            if (tempoMap[i].tick <= tick)
            {
                idx = i;
                break;
            }
        }

        TempoPoint p = tempoMap[idx];

        return (float)(p.seconds +
                       (tick - p.tick) * p.tempo / 1000000.0 / ticksPerQuarterNote);
    }

    // =====================================================
    // CHỌN TRACK GIAI ĐIỆU
    // =====================================================

    private List<RawNote> GetCandidates(TrackData t)
    {
        return ignoreDrums
            ? t.notes.Where(n => n.channel != 9).ToList()
            : t.notes;
    }

    private TrackData ChooseTrack(List<TrackData> tracks)
    {
        // In danh sách để dễ chọn tay nếu cần
        foreach (TrackData t in tracks)
        {
            List<RawNote> c = GetCandidates(t);
            if (c.Count == 0) continue;

            Debug.Log(
                $"[MIDI] Track {t.index} \"{t.name}\": {c.Count} nốt, " +
                $"pitch TB {c.Average(n => n.pitch):F0}, score {ScoreTrack(c, t.name):F1}");
        }

        if (forcedTrackIndex >= 0)
        {
            TrackData forced = tracks.FirstOrDefault(t => t.index == forcedTrackIndex);

            if (forced != null)
                return forced;

            Debug.LogWarning($"Không có track {forcedTrackIndex}, chuyển sang tự chọn.");
        }

        TrackData best = null;
        double bestScore = -1;

        foreach (TrackData t in tracks)
        {
            List<RawNote> c = GetCandidates(t);

            if (c.Count < 8)
                continue;

            double score = ScoreTrack(c, t.name);

            if (score > bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        // Fallback: track nhiều nốt nhất
        if (best == null)
        {
            best = tracks
                .OrderByDescending(t => GetCandidates(t).Count)
                .FirstOrDefault(t => GetCandidates(t).Count > 0);
        }

        if (best != null)
            Debug.Log($"[MIDI] Tự chọn track {best.index} \"{best.name}\"");

        return best;
    }

    // Điểm càng cao càng giống giai điệu: nhiều nốt, ít chồng nốt, cao độ cao
    private double ScoreTrack(List<RawNote> notes, string name)
    {
        int count = notes.Count;
        int groups = notes.Select(n => n.startTick).Distinct().Count();
        double monoRatio = (double)groups / count;   // 1 = đơn âm hoàn toàn
        double avgPitch = notes.Average(n => n.pitch);

        double pitchFactor = Mathf.Clamp((float)((avgPitch - 36.0) / 36.0), 0.2f, 1.3f);
        double score = Math.Sqrt(count) * (0.3 + monoRatio) * pitchFactor;

        string lower = (name ?? "").ToLowerInvariant();

        if (lower.Contains("melody") || lower.Contains("lead") ||
            lower.Contains("vocal") || lower.Contains("voice") ||
            lower.Contains("sing") || lower.Contains("main"))
            score *= 1.5;

        if (lower.Contains("bass") || lower.Contains("drum") ||
            lower.Contains("perc") || lower.Contains("chord") ||
            lower.Contains("pad") || lower.Contains("accomp"))
            score *= 0.5;

        return score;
    }

    // =====================================================
    // TẠO NOTE GAMEPLAY
    // =====================================================

    private void BuildGameplayNotes(TrackData track)
    {
        // Mỗi thời điểm chỉ giữ nốt cao nhất (skyline) -> bỏ hợp âm
        List<RawNote> melody = GetCandidates(track)
            .GroupBy(n => n.startTick)
            .Select(g => g.OrderByDescending(n => n.pitch).First())
            .OrderBy(n => n.startTick)
            .ToList();

        // Lọc theo khoảng cách tối thiểu (toàn cục, không theo lane)
        var filtered = new List<RhythmNote>();
        float lastTime = -999f;

        foreach (RawNote n in melody)
        {
            float start = TickToSeconds(n.startTick) + timeOffset;

            if (start < 0f)
                continue;

            if (start - lastTime < minimumNoteDistance)
                continue;

            float end = TickToSeconds(n.endTick) + timeOffset;

            filtered.Add(new RhythmNote(start, n.pitch, n.velocity, 0, Mathf.Max(0f, end - start)));
            lastTime = start;
        }

        AssignLanes(filtered);

        Notes.AddRange(filtered);

        Debug.Log(
            $"MIDI: track {track.index} | BPM {Bpm:F0} | " +
            $"Nốt gốc {track.notes.Count} -> Gameplay {Notes.Count}");
    }

    private void AssignLanes(List<RhythmNote> notes)
    {
        if (notes.Count == 0)
            return;

        if (laneCount <= 1)
        {
            foreach (RhythmNote n in notes) n.lane = 0;
            return;
        }

        int median = notes.OrderBy(n => n.pitch).ElementAt(notes.Count / 2).pitch;

        int lastPitch = -1;
        int lastLane = -1;
        int sameCount = 0;

        foreach (RhythmNote n in notes)
        {
            int lane;

            switch (laneMode)
            {
                case LaneMode.SplitByFixedPitch:
                    lane = n.pitch < splitPitch ? 0 : 1;
                    break;

                case LaneMode.SplitByMedian:
                    lane = n.pitch < median ? 0 : 1;
                    break;

                default: // Contour
                    if (lastPitch < 0)
                        lane = n.pitch >= median ? 1 : 0;
                    else if (n.pitch > lastPitch)
                        lane = 1;
                    else if (n.pitch < lastPitch)
                        lane = 0;
                    else
                        lane = lastLane;
                    break;
            }

            // Không cho quá nhiều key liên tiếp cùng lane
            if (maxSameLane > 0 && lane == lastLane && sameCount >= maxSameLane)
                lane = 1 - lane;

            sameCount = (lane == lastLane) ? sameCount + 1 : 1;
            lastLane = lane;
            lastPitch = n.pitch;

            n.lane = lane;
        }
    }

    // =====================================================
    // MIDI UTILITIES
    // =====================================================

    private string ReadString(byte[] data, ref int position, int length)
    {
        string result = Encoding.ASCII.GetString(data, position, length);
        position += length;
        return result;
    }

    private int ReadUInt16(byte[] data, ref int position)
    {
        int value = (data[position] << 8) | data[position + 1];
        position += 2;
        return value;
    }

    private int ReadInt32(byte[] data, ref int position)
    {
        int value =
            (data[position] << 24) |
            (data[position + 1] << 16) |
            (data[position + 2] << 8) |
            data[position + 3];

        position += 4;
        return value;
    }

    private long ReadVariableLength(byte[] data, ref int position)
    {
        long value = 0;

        while (true)
        {
            byte current = data[position++];
            value = (value << 7) | (long)(current & 0x7F);

            if ((current & 0x80) == 0)
                break;
        }

        return value;
    }
}