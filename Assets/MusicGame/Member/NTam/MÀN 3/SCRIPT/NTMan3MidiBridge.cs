using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NTMan3MidiBridge : MonoBehaviour
{
    [Header("1. Chế Độ Nguồn Dữ Liệu")]
    [Tooltip("Tích = Chạy chuỗi nốt mẫu của NTam | Bỏ tích = Lấy dữ liệu từ MIDI System (Màn 1/2)")]
    [SerializeField] private bool useDummyTestChart = false;

    [Header("2. Tham Chiếu Tới Object MIDI System")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private AudioSource midiAudioSource;
    [SerializeField] private NTMan3Director man3Director;

    [Header("3. Thông Số Quy Đổi Sang Gameplay Màn 3")]
    [Tooltip("Thời gian viên nốt rơi từ đỉnh xuống vòng tròn (giây)")]
    [SerializeField] private float defaultFlightDuration = 1.25f;
    [Tooltip("Độ trễ bắt đầu phát nhạc khớp với thời gian ẩn chữ READY (1.2s)")]
    [SerializeField] private float startMusicDelay = 1.2f;
    [SerializeField] private float audioOffset = 0f;

    [Header("4. Tự Động Sinh Thêm Buff & Bom Từ MIDI 2 Làn")]
    [SerializeField] private bool autoInjectBuffAndBomb = true;
    [Tooltip("Cứ sau bao nhiêu nốt thường sẽ biến 1 nốt thành Buff x2")]
    [SerializeField] private int buffEveryNNotes = 16;
    [Tooltip("Cứ sau bao nhiêu nốt sẽ chèn 1 bẫy Bom vào khoảng trống an toàn")]
    [SerializeField] private int bombEveryNNotes = 12;

    private void Start()
    {
        if (man3Director == null) man3Director = GetComponent<NTMan3Director>();

        if (!useDummyTestChart)
        {
            StartCoroutine(LoadFromMidiSystemRoutine());
        }
    }

    private IEnumerator LoadFromMidiSystemRoutine()
    {
        // Đợi 1 frame để LevelManager.Awake() gọi midiReader.LoadMidi() hoàn tất
        yield return null;

        if (midiReader == null || man3Director == null)
        {
            Debug.LogError("[NTMan3MidiBridge] Thiếu tham chiếu MidiReader hoặc NTMan3Director!");
            yield break;
        }

        List<NoteDataMan3> convertedNotes = ConvertMidiReaderToMan3Chart();

        if (convertedNotes.Count == 0)
        {
            Debug.LogWarning("[NTMan3MidiBridge] MidiReader chưa có dữ liệu nốt. Kiểm tra lại LevelData!");
            yield break;
        }

        // Chờ hết hiệu ứng chữ READY rồi mới phát nhạc và thả nốt đồng bộ
        if (startMusicDelay > 0f)
        {
            yield return new WaitForSeconds(startMusicDelay);
        }

        if (midiAudioSource != null && midiAudioSource.clip != null)
        {
            midiAudioSource.Play();
        }

        man3Director.LoadExternalChart(convertedNotes, null, midiAudioSource, audioOffset);
    }

    /// <summary>
    /// Chuyển đổi dữ liệu từ MidiReader (Lane 0/1) sang 5 loại nốt của Màn 3
    /// </summary>
    private List<NoteDataMan3> ConvertMidiReaderToMan3Chart()
    {
        List<NoteDataMan3> result = new List<NoteDataMan3>();

        // =====================================================================
        // LƯU Ý KẾT NỐI: Mở file MidiReader.cs của TTAM để xem tên biến List chứa nốt
        // (Ví dụ: midiReader.notes hoặc midiReader.parsedNotes) và thay vào vòng lặp dưới:
        // =====================================================================
        /*
        foreach (var rawNote in midiReader.notes) // <-- Thay đúng tên List trong MidiReader.cs
        {
            bool isLeft = (rawNote.lane == 0); // Lane 0 = Trái, Lane 1 = Phải (theo Inspector Màn 1)
            bool isLong = (rawNote.duration >= 0.5f); // Khớp thông số Long Note Min Duration = 0.5s

            result.Add(new NoteDataMan3
            {
                isLeftLane = isLeft,
                noteType = isLong ? Man3NoteType.Hold : Man3NoteType.Normal,
                hitTime = rawNote.time,
                holdDuration = isLong ? rawNote.duration : 0f,
                flightDuration = defaultFlightDuration
            });
        }
        */

        // Sắp xếp nốt tăng dần theo thời gian chạm
        result.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));

        // 1. Tự động gộp 2 nốt Trái + Phải đánh cùng lúc (lệch <= 0.03s) thành Nốt Đôi (SwingTHand)
        for (int i = 0; i < result.Count - 1; i++)
        {
            if (Mathf.Abs(result[i + 1].hitTime - result[i].hitTime) <= 0.03f &&
                result[i + 1].isLeftLane != result[i].isLeftLane)
            {
                NoteDataMan3 n1 = result[i];
                NoteDataMan3 n2 = result[i + 1];
                n1.noteType = Man3NoteType.Double;
                n2.noteType = Man3NoteType.Double;
                n2.hitTime = n1.hitTime; // Đồng bộ chuẩn xác 100% thời điểm chạm
                result[i] = n1;
                result[i + 1] = n2;
                i++;
            }
        }

        // 2. Tự động chèn Buff x2 và Bẫy Bom vào các khoảng nghỉ giữa nhịp
        if (autoInjectBuffAndBomb && result.Count > 0)
        {
            List<NoteDataMan3> extraBombs = new List<NoteDataMan3>();

            for (int i = 0; i < result.Count; i++)
            {
                // Biến nốt đơn định kỳ thành nốt Buff vàng x2 điểm
                if (buffEveryNNotes > 0 && i > 0 && i % buffEveryNNotes == 0 && result[i].noteType == Man3NoteType.Normal)
                {
                    NoteDataMan3 buffNote = result[i];
                    buffNote.noteType = Man3NoteType.Buff;
                    result[i] = buffNote;
                }

                // Nếu giữa 2 nốt có khoảng trống >= 0.75s, chèn 1 bẫy Bom vào giữa để chống spam nút
                if (bombEveryNNotes > 0 && i > 0 && i % bombEveryNNotes == 0 && i < result.Count - 1)
                {
                    float gap = result[i + 1].hitTime - result[i].hitTime;
                    if (gap >= 0.75f && result[i].noteType == Man3NoteType.Normal)
                    {
                        extraBombs.Add(new NoteDataMan3
                        {
                            isLeftLane = result[i].isLeftLane, // Đặt cùng làn vừa bấm để bẫy người chơi spam
                            noteType = Man3NoteType.Bomb,
                            hitTime = result[i].hitTime + (gap * 0.5f),
                            holdDuration = 0f,
                            flightDuration = defaultFlightDuration
                        });
                    }
                }
            }

            result.AddRange(extraBombs);
            result.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }

        return result;
    }
}