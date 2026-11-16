using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Networking;
#endif

public class NTMidiReader : MonoBehaviour
{
    [Serializable]
    public class RhythmNote
    {
        public float time;
        public int pitch;
        public int velocity;
        public int lane;
        public float duration;

        public RhythmNote(float time, int pitch, int velocity, int lane, float duration = 0f)
        {
            this.time = time;
            this.pitch = pitch;
            this.velocity = velocity;
            this.lane = lane;
            this.duration = duration;
        }
    }

    public enum ContentMode
    {
        AllNotes,       // Đọc tất cả nốt nhạc
        Melody,         // Ưu tiên track giai điệu
        Drums,          // Chỉ lấy nốt trên MIDI drum channel 10 (index 9)
        Bass,           // Lọc theo pitch bass gần đúng
        CustomPitch     // Lấy các pitch được chọn thủ công
    }

    public enum LaneMode
    {
        Contour,
        SplitByMedian,
        SplitByFixedPitch,
        DrumType        // Gán lane theo loại trống
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
        public int tempo;
    }

    [Header("Drum Filter Mặc Định")]
    [SerializeField] private bool requireDrumChannel = true;
    [SerializeField] private List<int> allowedDrumPitches = new List<int>();
    [SerializeField] private bool filterDrumPitches;
    [SerializeField] private bool mapDrumsByType = true;
    [SerializeField] private int hiHatLane = 1;
    [SerializeField] private int otherDrumLane = 0;

    [Header("Custom Pitch Filter Mặc Định")]
    [SerializeField] private List<int> allowedPitches = new List<int>();
    [SerializeField] private bool filterCustomPitches = true;

    [Header("Bass Filter Mặc Định")]
    [SerializeField] private int bassMinPitch = 28;
    [SerializeField] private int bassMaxPitch = 55;

    [Header("Lane & Timing Cấu Hình Chung")]
    [SerializeField] private int laneCount = 2;
    [SerializeField] private LaneMode laneMode = LaneMode.Contour;
    [SerializeField] private int splitPitch = 60;
    [SerializeField] private int maxSameLane = 3;

    [Header("Long Note")]
    [SerializeField] private float longNoteMinDuration = 0.5f;
    [SerializeField] private float longNoteTailGap = 0.25f;

    public List<RhythmNote> Notes { get; private set; } = new List<RhythmNote>();
    public float Bpm { get; private set; } = 120f;

    // Các biến cấu hình sẽ được nạp trực tiếp từ NTLevelData
    private ContentMode contentMode = ContentMode.Melody;
    private int forcedTrackIndex = -1;
    private float timeOffset = 0f;
    private float minimumNoteDistance = 0.15f;

    private int ticksPerQuarterNote;
    private readonly List<TempoPoint> tempoMap = new List<TempoPoint>();

    /// <summary>
    /// Hàm chính: Đọc file MIDI hoàn toàn dựa vào cấu hình tường minh của NTLevelData
    /// </summary>
    public void LoadMidi(NTLevelData level)
    {
        Notes.Clear();

        if (level == null)
        {
            Debug.LogError("NTMidiReader: NTLevelData chưa được gán.");
            return;
        }

        if (string.IsNullOrWhiteSpace(level.midiFileName))
        {
            Debug.LogError("NTMidiReader: NTLevelData chưa có tên file MIDI.");
            return;
        }

        // --- ĐỒNG BỘ CẤU HÌNH TƯỜNG MINH TỪ NT LEVEL DATA ---
        contentMode = level.levelContentMode;       // Quyết định đọc theo thể loại nào (Drums, Melody, Bass...)
        forcedTrackIndex = level.forcedTrackIndex;   // Ép buộc đọc chính xác track số mấy (-1 nếu để code tự chọn)
        timeOffset = level.timeOffset;               // Độ trễ thời gian khớp nhạc
        minimumNoteDistance = level.GetMinNoteDistance(); // Khoảng cách tối thiểu giữa các nốt theo độ khó
        // ----------------------------------------------------

        string path = Path.Combine(Application.streamingAssetsPath, "MIDI", level.midiFileName);
        byte[] data = ReadAllBytesCompat(path);

        if (data == null || data.Length == 0)
            return;

        try
        {
            ParseMidi(data);
        }
        catch (Exception e)
        {
            Notes.Clear();
            Debug.LogError("NTMidiReader: Lỗi đọc file MIDI: " + e.Message);
        }
    }

    private byte[] ReadAllBytesCompat(string path)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            var op = req.SendWebRequest();
            while (!op.isDone) { }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("NTMidiReader: Không tải được MIDI trên Android: " + req.error);
                return null;
            }

            return req.downloadHandler.data;
        }
#else
        if (!File.Exists(path))
        {
            Debug.LogError("NTMidiReader: Không tìm thấy file MIDI tại đường dẫn: " + path);
            return null;
        }

        return File.ReadAllBytes(path);
#endif
    }

    private void ParseMidi(byte[] data)
    {
        int pos = 0;
        if (ReadString(data, ref pos, 4) != "MThd")
            throw new InvalidDataException("Header không phải MThd.");

        int headerLength = ReadInt32(data, ref pos);
        int format = ReadUInt16(data, ref pos);
        int trackCount = ReadUInt16(data, ref pos);
        int division = ReadUInt16(data, ref pos);

        if ((division & 0x8000) != 0)
            throw new InvalidDataException("Chỉ hỗ trợ MIDI PPQ, không hỗ trợ SMPTE.");

        ticksPerQuarterNote = division;
        if (ticksPerQuarterNote <= 0)
            throw new InvalidDataException("Ticks per quarter note không hợp lệ.");

        pos = 8 + headerLength;

        var tracks = new List<TrackData>();
        var tempoRaw = new List<KeyValuePair<long, int>>();
        int trackIndex = 0;

        while (pos + 8 <= data.Length && tracks.Count < trackCount)
        {
            string id = ReadString(data, ref pos, 4);
            int length = ReadInt32(data, ref pos);
            if (length < 0 || pos + (long)length > data.Length)
                throw new InvalidDataException("Track chunk có độ dài không hợp lệ.");

            int end = pos + length;
            if (id == "MTrk")
            {
                tracks.Add(ParseTrack(data, pos, end, trackIndex, tempoRaw));
                trackIndex++;
            }
            pos = end;
        }

        BuildTempoMap(tempoRaw);
        Debug.Log($"[NTMidiReader] Format {format}, Tổng số tracks: {tracks.Count}, TPQ: {ticksPerQuarterNote}");

        foreach (TrackData t in tracks)
            Debug.Log($"[MIDI Track Info] Index: {t.index} | Name: \"{t.name}\" | Tổng nốt thô: {t.notes.Count}");

        TrackData chosen = ChooseTrack(tracks);
        if (chosen == null)
        {
            Debug.LogError("NTMidiReader: Không tìm thấy track phù hợp với Content Mode: " + contentMode + " hoặc Forced Track Index: " + forcedTrackIndex);
            return;
        }

        BuildGameplayNotes(chosen);
    }

    private TrackData ParseTrack(byte[] data, int start, int end, int trackIndex,
        List<KeyValuePair<long, int>> tempoRaw)
    {
        var track = new TrackData { index = trackIndex };
        var open = new Dictionary<int, Stack<RawNote>>();
        int pos = start;
        long tick = 0;
        byte running = 0;

        while (pos < end)
        {
            tick += ReadVariableLength(data, ref pos, end);
            if (pos >= end) break;

            byte status = data[pos];
            if ((status & 0x80) != 0)
            {
                pos++;
                running = status < 0xF0 ? status : (byte)0;
            }
            else
            {
                status = running;
                if (status == 0) throw new InvalidDataException("Running status không hợp lệ.");
            }

            if (status == 0xFF)
            {
                if (pos >= end) break;
                byte metaType = data[pos++];
                int metaLength = checked((int)ReadVariableLength(data, ref pos, end));
                if (pos + (long)metaLength > end)
                    throw new InvalidDataException("Meta event vượt khỏi track.");

                if (metaType == 0x51 && metaLength == 3)
                {
                    int tempo = (data[pos] << 16) | (data[pos + 1] << 8) | data[pos + 2];
                    if (tempo > 0) tempoRaw.Add(new KeyValuePair<long, int>(tick, tempo));
                }
                else if (metaType == 0x03)
                {
                    track.name = Encoding.ASCII.GetString(data, pos, metaLength);
                }

                pos += metaLength;
                if (metaType == 0x2F) break;
            }
            else if (status == 0xF0 || status == 0xF7)
            {
                int length = checked((int)ReadVariableLength(data, ref pos, end));
                if (pos + (long)length > end)
                    throw new InvalidDataException("SysEx vượt khỏi track.");
                pos += length;
            }
            else
            {
                int command = status & 0xF0;
                int channel = status & 0x0F;
                int dataBytes = (command == 0xC0 || command == 0xD0) ? 1 : 2;
                if (pos + dataBytes > end)
                    throw new InvalidDataException("MIDI event thiếu dữ liệu.");

                int d1 = data[pos++];
                int d2 = dataBytes == 2 ? data[pos++] : 0;

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

    private bool IsDrum(RawNote n) => n.channel == 9;

    private List<RawNote> FilterNotesForMode(TrackData track)
    {
        IEnumerable<RawNote> notes = track.notes;

        switch (contentMode)
        {
            case ContentMode.Drums:
                if (requireDrumChannel) notes = notes.Where(IsDrum);
                if (filterDrumPitches && allowedDrumPitches.Count > 0)
                    notes = notes.Where(n => allowedDrumPitches.Contains(n.pitch));
                break;

            case ContentMode.Bass:
                notes = notes.Where(n => n.pitch >= bassMinPitch && n.pitch <= bassMaxPitch && !IsDrum(n));
                break;

            case ContentMode.CustomPitch:
                if (filterCustomPitches && allowedPitches.Count > 0)
                    notes = notes.Where(n => allowedPitches.Contains(n.pitch));
                break;

            case ContentMode.Melody:
                notes = notes.Where(n => !IsDrum(n));
                break;

            case ContentMode.AllNotes:
                break;
        }

        return notes.ToList();
    }

    private TrackData ChooseTrack(List<TrackData> tracks)
    {
        // 1. ƯU TIÊN TUYỆT ĐỐI: Nếu Coder/Designer đã chỉ định rõ Forced Track Index trong NTLevelData
        if (forcedTrackIndex >= 0)
        {
            TrackData forced = tracks.FirstOrDefault(t => t.index == forcedTrackIndex);
            if (forced != null)
            {
                Debug.Log($"[NTMidiReader] Ép buộc sử dụng chính xác Track {forced.index} \"{forced.name}\" theo cấu hình NTLevelData.");
                return forced;
            }
            else
            {
                Debug.LogWarning($"[NTMidiReader] Không tìm thấy Track index {forcedTrackIndex} như đã ép buộc; hệ thống sẽ tự động chọn track thay thế.");
            }
        }

        // 2. TỰ ĐỘNG CHỌN TRACK DỰA TRÊN CONTENT MODE VÀ TÊN TRACK
        var candidates = tracks
            .Select(t => new { Track = t, Notes = FilterNotesForMode(t) })
            .Where(x => x.Notes.Count > 0)
            .ToList();

        if (candidates.Count == 0) return null;

        string[] keywords = contentMode == ContentMode.Drums
            ? new[] { "drum", "perc" }
            : contentMode == ContentMode.Bass
                ? new[] { "bass" }
                : contentMode == ContentMode.Melody
                    ? new[] { "melody", "lead", "vocal", "voice", "main", "piano" }
                    : Array.Empty<string>();

        var named = candidates.Where(x =>
            keywords.Any(k => (x.Track.name ?? "").ToLowerInvariant().Contains(k))).ToList();

        if (named.Count > 0)
            candidates = named;

        var chosen = candidates
            .OrderByDescending(x =>
            {
                double countScore = Math.Sqrt(x.Notes.Count);
                if (contentMode == ContentMode.Melody && x.Notes.Count > 0)
                    return countScore * Mathf.Clamp((float)((x.Notes.Average(n => n.pitch) - 36) / 36), 0.2f, 1.3f);
                return countScore;
            })
            .First();

        Debug.Log($"[NTMidiReader] Tự động chọn track {chosen.Track.index} \"{chosen.Track.name}\" ({chosen.Notes.Count} nốt phù hợp).");
        return chosen.Track;
    }

    private void BuildGameplayNotes(TrackData track)
    {
        List<RawNote> source = FilterNotesForMode(track)
            .OrderBy(n => n.startTick)
            .ThenBy(n => n.pitch)
            .ToList();

        if (source.Count == 0) return;

        if (contentMode == ContentMode.Melody)
        {
            source = source
                .GroupBy(n => n.startTick)
                .Select(g => g.OrderByDescending(n => n.pitch).First())
                .OrderBy(n => n.startTick)
                .ToList();
        }

        var filtered = new List<RhythmNote>();
        float lastAcceptedTime = -999f;
        var lastTimeByPitchLane = new Dictionary<int, float>();

        foreach (RawNote n in source)
        {
            float start = TickToSeconds(n.startTick) + timeOffset;
            if (start < 0f) continue;

            int lane = contentMode == ContentMode.Drums && mapDrumsByType
                ? GetDrumLane(n.pitch)
                : 0;

            float previousTime;
            bool hasPrevious = contentMode == ContentMode.Drums && mapDrumsByType
                ? lastTimeByPitchLane.TryGetValue(lane, out previousTime)
                : (previousTime = lastAcceptedTime) > -999f;

            if (hasPrevious && start - previousTime < minimumNoteDistance)
                continue;

            float end = TickToSeconds(n.endTick) + timeOffset;
            float duration = Mathf.Max(0f, end - start);

            filtered.Add(new RhythmNote(start, n.pitch, n.velocity, lane, duration));
            if (contentMode == ContentMode.Drums && mapDrumsByType)
                lastTimeByPitchLane[lane] = start;
            else
                lastAcceptedTime = start;
        }

        for (int i = 0; i < filtered.Count; i++)
        {
            RhythmNote n = filtered[i];
            float nextTime = float.PositiveInfinity;

            for (int j = i + 1; j < filtered.Count; j++)
            {
                if (filtered[j].lane == n.lane && filtered[j].time > n.time)
                {
                    nextTime = filtered[j].time;
                    break;
                }
            }

            if (!float.IsPositiveInfinity(nextTime))
                n.duration = Mathf.Min(n.duration, nextTime - n.time - longNoteTailGap);

            if (n.duration < longNoteMinDuration)
                n.duration = 0f;
        }

        if (contentMode != ContentMode.Drums || !mapDrumsByType)
            AssignLanes(filtered);

        Notes.AddRange(filtered.OrderBy(n => n.time).ThenBy(n => n.lane));

        Debug.Log($"[NTMidiReader Hoàn Tất] Mode={contentMode}, Track={track.index} \"{track.name}\", BPM={Bpm:F1}, Raw={source.Count}, Gameplay Notes={Notes.Count}");
    }

    private int GetDrumLane(int pitch)
    {
        if (pitch == 35 || pitch == 36) return 0;
        if (pitch == 38 || pitch == 40) return 1;
        if (pitch == 42 || pitch == 44 || pitch == 46) return Mathf.Clamp(hiHatLane, 0, 1);
        return Mathf.Clamp(otherDrumLane, 0, 1);
    }

    private void AssignLanes(List<RhythmNote> notes)
    {
        if (notes.Count == 0) return;
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
                default:
                    if (lastPitch < 0) lane = n.pitch >= median ? 1 : 0;
                    else if (n.pitch > lastPitch) lane = 1;
                    else if (n.pitch < lastPitch) lane = 0;
                    else lane = lastLane;
                    break;
            }

            if (maxSameLane > 0 && lane == lastLane && sameCount >= maxSameLane)
                lane = 1 - lane;

            sameCount = lane == lastLane ? sameCount + 1 : 1;
            lastLane = lane;
            lastPitch = n.pitch;
            n.lane = lane;
        }
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

    private string ReadString(byte[] data, ref int position, int length)
    {
        if (position < 0 || length < 0 || position + (long)length > data.Length)
            throw new InvalidDataException("Không đủ dữ liệu để đọc string.");
        string result = Encoding.ASCII.GetString(data, position, length);
        position += length;
        return result;
    }

    private int ReadUInt16(byte[] data, ref int position)
    {
        if (position + 2 > data.Length) throw new InvalidDataException("Thiếu UInt16.");
        int value = (data[position] << 8) | data[position + 1];
        position += 2;
        return value;
    }

    private int ReadInt32(byte[] data, ref int position)
    {
        if (position + 4 > data.Length) throw new InvalidDataException("Thiếu Int32.");
        int value = (data[position] << 24) | (data[position + 1] << 16) |
                    (data[position + 2] << 8) | data[position + 3];
        position += 4;
        return value;
    }

    private long ReadVariableLength(byte[] data, ref int position, int end)
    {
        long value = 0;
        int count = 0;
        while (true)
        {
            if (position >= end || count++ >= 4)
                throw new InvalidDataException("Variable-length quantity không hợp lệ.");
            byte current = data[position++];
            value = (value << 7) | (long)(current & 0x7F);
            if ((current & 0x80) == 0) return value;
        }
    }

    public List<RhythmNote> GetNotesWithSpeed(float speed)
    {
        speed = Mathf.Max(0.1f, speed);
        return Notes.Select(n => new RhythmNote(
            n.time / speed, n.pitch, n.velocity, n.lane, n.duration / speed)).ToList();
    }
}