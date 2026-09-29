using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class MidiReader : MonoBehaviour
{
    [Serializable]
    public class RhythmNote
    {
        public float time;
        public int pitch;
        public int velocity;
        public int lane;

        public RhythmNote(float time, int pitch, int velocity, int lane)
        {
            this.time = time;
            this.pitch = pitch;
            this.velocity = velocity;
            this.lane = lane;
        }
    }

    private class MidiEvent
    {
        public long tick;
        public byte status;
        public byte data1;
        public byte data2;
    }

    private class TempoEvent
    {
        public long tick;
        public int microsecondsPerQuarter;

        public TempoEvent(long tick, int tempo)
        {
            this.tick = tick;
            this.microsecondsPerQuarter = tempo;
        }
    }

    // [Header("MIDI")]
    // [SerializeField] private TextAsset midiFile;

    [Header("Settings")]
    [SerializeField] private int laneCount = 2;
    [SerializeField] private float minimumNoteDistance = 0.08f;
    [SerializeField] private int splitPitch = 60;

    public List<RhythmNote> Notes { get; private set; } = new List<RhythmNote>();

    private int ticksPerQuarterNote;


   
    public void LoadMidi(LevelData level)
    {
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

        string path = Path.Combine(
            Application.streamingAssetsPath,
            "MIDI",
            level.midiFileName
        );

        if (!File.Exists(path))
        {
            Debug.LogError("Không tìm thấy MIDI: " + path);
            return;
        }

        byte[] data = File.ReadAllBytes(path);

        Debug.Log("Loading MIDI: " + path);

        ParseMidi(data);
    }

    private void ParseMidi(byte[] data)
    {
        int position = 0;

        // =========================
        // MIDI HEADER
        // =========================

        string header = ReadString(data, ref position, 4);

        if (header != "MThd")
        {
            Debug.LogError("File không phải MIDI hợp lệ.");
            return;
        }

        int headerLength = ReadInt32(data, ref position);

        short format = ReadInt16(data, ref position);
        short trackCount = ReadInt16(data, ref position);
        short division = ReadInt16(data, ref position);

        if (division < 0)
        {
            Debug.LogError("MIDI dùng SMPTE time division, script này hiện dùng PPQ.");
            return;
        }

        ticksPerQuarterNote = division;

        Debug.Log(
            $"MIDI Format: {format} | " +
            $"Tracks: {trackCount} | " +
            $"Ticks/Quarter: {ticksPerQuarterNote}"
        );

        // bỏ phần header dư nếu có
        position = 8 + headerLength;

        List<MidiEvent> allEvents = new List<MidiEvent>();
        List<TempoEvent> tempoEvents = new List<TempoEvent>();

        // =========================
        // READ TRACKS
        // =========================

        for (int track = 0; track < trackCount; track++)
        {
            string trackHeader = ReadString(data, ref position, 4);

            if (trackHeader != "MTrk")
            {
                Debug.LogError("Không tìm thấy MTrk.");
                return;
            }

            int trackLength = ReadInt32(data, ref position);

            int trackEnd = position + trackLength;

            long currentTick = 0;

            byte runningStatus = 0;

            while (position < trackEnd)
            {
                long deltaTime = ReadVariableLength(data, ref position);

                currentTick += deltaTime;

                byte status = data[position];

                // Running status
                if ((status & 0x80) != 0)
                {
                    position++;
                    runningStatus = status;
                }
                else
                {
                    status = runningStatus;
                }

                // =========================
                // META EVENT
                // =========================

                if (status == 0xFF)
                {
                    byte metaType = data[position++];
                    long metaLength = ReadVariableLength(data, ref position);

                    // Tempo
                    if (metaType == 0x51 && metaLength == 3)
                    {
                        int tempo =
                            (data[position] << 16) |
                            (data[position + 1] << 8) |
                            data[position + 2];

                        tempoEvents.Add(
                            new TempoEvent(currentTick, tempo)
                        );
                    }

                    position += (int)metaLength;
                }

                // =========================
                // SYSEX
                // =========================

                else if (status == 0xF0 || status == 0xF7)
                {
                    long length = ReadVariableLength(data, ref position);

                    position += (int)length;
                }

                // =========================
                // NOTE / MIDI EVENTS
                // =========================

                else
                {
                    byte command = (byte)(status & 0xF0);

                    byte data1 = data[position++];

                    byte data2 = 0;

                    if (command != 0xC0 && command != 0xD0)
                    {
                        data2 = data[position++];
                    }

                    // NOTE ON
                    if (command == 0x90)
                    {
                        // Velocity > 0 = Note On
                        if (data2 > 0)
                        {
                            allEvents.Add(
                                new MidiEvent
                                {
                                    tick = currentTick,
                                    status = status,
                                    data1 = data1,
                                    data2 = data2
                                }
                            );
                        }
                    }
                }
            }

            position = trackEnd;
        }

        // Nếu không có tempo event
        if (tempoEvents.Count == 0)
        {
            tempoEvents.Add(
                new TempoEvent(
                    0,
                    500000
                )
            );
        }

        tempoEvents = tempoEvents
            .OrderBy(x => x.tick)
            .ToList();

        // =========================
        // CONVERT TICK -> SECONDS
        // =========================

        List<RhythmNote> rawNotes = new List<RhythmNote>();

        foreach (MidiEvent midiEvent in allEvents)
        {
            float time = TickToSeconds(
                midiEvent.tick,
                tempoEvents
            );

            int pitch = midiEvent.data1;
            int velocity = midiEvent.data2;

            int lane = GetLane(pitch);

            rawNotes.Add(
                new RhythmNote(
                    time,
                    pitch,
                    velocity,
                    lane
                )
            );
        }

        rawNotes = rawNotes
            .OrderBy(x => x.time)
            .ToList();

        // =========================
        // FILTER NOTE
        // =========================

        Notes.Clear();

        float lastTimeLane0 = -999f;
        float lastTimeLane1 = -999f;

        foreach (RhythmNote note in rawNotes)
        {
            if (note.lane == 0)
            {
                if (note.time - lastTimeLane0 < minimumNoteDistance)
                    continue;

                lastTimeLane0 = note.time;
            }
            else
            {
                if (note.time - lastTimeLane1 < minimumNoteDistance)
                    continue;

                lastTimeLane1 = note.time;
            }

            Notes.Add(note);
        }

        Debug.Log(
            $"MIDI Raw Notes: {rawNotes.Count} | " +
            $"Gameplay Notes: {Notes.Count}"
        );
    }

    // ============================================================
    // TICK -> SECOND
    // ============================================================

    private float TickToSeconds(
        long targetTick,
        List<TempoEvent> tempoEvents)
    {
        double seconds = 0;

        long previousTick = 0;

        int currentTempo = 500000;

        foreach (TempoEvent tempo in tempoEvents)
        {
            if (tempo.tick > targetTick)
                break;

            long deltaTick = tempo.tick - previousTick;

            seconds +=
                deltaTick *
                currentTempo /
                1000000.0 /
                ticksPerQuarterNote;

            previousTick = tempo.tick;

            currentTempo = tempo.microsecondsPerQuarter;
        }

        long remainingTick = targetTick - previousTick;

        seconds +=
            remainingTick *
            currentTempo /
            1000000.0 /
            ticksPerQuarterNote;

        return (float)seconds;
    }

    // ============================================================
    // CHIA LANE
    // ============================================================

    private int GetLane(int pitch)
    {
        if (laneCount <= 1)
            return 0;

        // Note thấp -> Lane 0
        // Note cao -> Lane 1

        if (pitch < splitPitch)
            return 0;

        return 1;
    }

    // ============================================================
    // MIDI UTILITIES
    // ============================================================

    private string ReadString(
        byte[] data,
        ref int position,
        int length)
    {
        string result = "";

        for (int i = 0; i < length; i++)
        {
            result += (char)data[position++];
        }

        return result;
    }

    private short ReadInt16(
        byte[] data,
        ref int position)
    {
        short value =
            (short)(
                (data[position] << 8) |
                data[position + 1]
            );

        position += 2;

        return value;
    }

    private int ReadInt32(
        byte[] data,
        ref int position)
    {
        int value =
            (data[position] << 24) |
            (data[position + 1] << 16) |
            (data[position + 2] << 8) |
            data[position + 3];

        position += 4;

        return value;
    }

    private long ReadVariableLength(
        byte[] data,
        ref int position)
    {
        long value = 0;

        while (true)
        {
            byte current = data[position++];

            value =
                (value << 7) |
                (current & 0x7F);

            if ((current & 0x80) == 0)
                break;
        }

        return value;
    }
}