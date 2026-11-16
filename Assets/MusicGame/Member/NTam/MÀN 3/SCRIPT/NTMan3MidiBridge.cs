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

    [Header("4. Tự Động Sinh Buff, Bom & Chuỗi Xu (Fever Coins)")]
    [SerializeField] private bool autoInjectBuffAndBomb = true;
    [SerializeField] private int buffEveryNNotes = 16;
    [SerializeField] private int bombEveryNNotes = 12;

    [Space(5)]
    [SerializeField] private bool autoInjectFeverCoins = true;
    [Tooltip("Khoảng nghỉ tối thiểu giữa 2 nốt nhạc để rải chuỗi xu (giây)")]
    [SerializeField] private float minGapForCoins = 2.0f;
    [Tooltip("Khoảng đệm an toàn trước và sau chuỗi xu để không vướng nốt chính (giây)")]
    [SerializeField] private float coinSafeBuffer = 0.65f;
    [SerializeField] private int minCoinsPerChain = 3;
    [SerializeField] private int maxCoinsPerChain = 5;

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

    private List<NoteDataMan3> ConvertMidiReaderToMan3Chart()
    {
        List<NoteDataMan3> result = new List<NoteDataMan3>();

        if (midiReader == null || midiReader.Notes == null || midiReader.Notes.Count == 0)
        {
            Debug.LogWarning("[NTMan3MidiBridge] MidiReader chưa có dữ liệu nốt!");
            return result;
        }

        foreach (var n in midiReader.Notes)
        {
            bool leftLane = (n.lane == 0);
            Man3NoteType type = (n.duration > 0.25f) ? Man3NoteType.Hold : Man3NoteType.Normal;
            float holdDur = (type == Man3NoteType.Hold) ? n.duration : 0f;

            if (n.pitch == 64)
            {
                result.Add(new NoteDataMan3
                {
                    isLeftLane = true,
                    noteType = Man3NoteType.Double,
                    hitTime = n.time,
                    holdDuration = 0f,
                    flightDuration = defaultFlightDuration
                });
                result.Add(new NoteDataMan3
                {
                    isLeftLane = false,
                    noteType = Man3NoteType.Double,
                    hitTime = n.time,
                    holdDuration = 0f,
                    flightDuration = defaultFlightDuration
                });
                continue;
            }
            else if (n.pitch == 65 || n.pitch == 67)
            {
                type = Man3NoteType.Bomb;
                leftLane = (n.pitch == 65);
                holdDur = 0f;
            }
            else if (n.pitch == 69 || n.pitch == 71)
            {
                type = Man3NoteType.Buff;
                leftLane = (n.pitch == 69);
                holdDur = 0f;
            }

            result.Add(new NoteDataMan3
            {
                isLeftLane = leftLane,
                noteType = type,
                hitTime = n.time,
                holdDuration = holdDur,
                flightDuration = defaultFlightDuration
            });
        }

        result.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));

        // 1. Gộp cặp nốt đánh đồng thời thành Nốt Đôi (Double)
        for (int i = 0; i < result.Count - 1; i++)
        {
            if (Mathf.Abs(result[i + 1].hitTime - result[i].hitTime) <= 0.03f &&
                result[i + 1].isLeftLane != result[i].isLeftLane)
            {
                NoteDataMan3 n1 = result[i];
                NoteDataMan3 n2 = result[i + 1];
                n1.noteType = Man3NoteType.Double;
                n2.noteType = Man3NoteType.Double;
                n2.hitTime = n1.hitTime;
                result[i] = n1;
                result[i + 1] = n2;
                i++;
            }
        }

        List<NoteDataMan3> extraItems = new List<NoteDataMan3>();
        float bpm = (midiReader.Bpm > 0f) ? midiReader.Bpm : 120f;
        float beatInterval = 60f / bpm;

        // 2. Rải Chuỗi Nốt Xu (Fever Coins) vào các khoảng nghỉ dài (>= 2.0s)
        if (autoInjectFeverCoins && result.Count > 1)
        {
            for (int i = 0; i < result.Count - 1; i++)
            {
                float currentEndTime = result[i].hitTime + result[i].holdDuration;
                float nextStartTime = result[i + 1].hitTime;
                float gap = nextStartTime - currentEndTime;

                if (gap >= minGapForCoins)
                {
                    float safeStart = currentEndTime + coinSafeBuffer;
                    float safeEnd = nextStartTime - coinSafeBuffer;
                    float usableWindow = safeEnd - safeStart;

                    if (usableWindow >= 0.4f)
                    {
                        float step = beatInterval;
                        if (usableWindow / step < (minCoinsPerChain - 1))
                        {
                            step *= 0.5f;
                        }

                        int coinCount = Mathf.Clamp(Mathf.FloorToInt(usableWindow / step) + 1, minCoinsPerChain, maxCoinsPerChain);
                        float actualSpacing = (coinCount > 1) ? (usableWindow / (coinCount - 1)) : 0f;

                        bool coinLaneLeft = !result[i].isLeftLane;

                        for (int c = 0; c < coinCount; c++)
                        {
                            extraItems.Add(new NoteDataMan3
                            {
                                isLeftLane = coinLaneLeft,
                                noteType = Man3NoteType.Coin,
                                hitTime = safeStart + (c * actualSpacing),
                                holdDuration = 0f,
                                flightDuration = defaultFlightDuration
                            });

                            coinLaneLeft = !coinLaneLeft;
                        }
                    }
                }
            }
        }

        // 3. Chèn Buff x2 và Bẫy Bom vào các khoảng nghỉ vừa (0.75s -> dưới 2.0s)
        if (autoInjectBuffAndBomb && result.Count > 0)
        {
            for (int i = 0; i < result.Count; i++)
            {
                if (buffEveryNNotes > 0 && i > 0 && i % buffEveryNNotes == 0 && result[i].noteType == Man3NoteType.Normal)
                {
                    NoteDataMan3 buffNote = result[i];
                    buffNote.noteType = Man3NoteType.Buff;
                    result[i] = buffNote;
                }

                if (bombEveryNNotes > 0 && i > 0 && i % bombEveryNNotes == 0 && i < result.Count - 1)
                {
                    float gap = result[i + 1].hitTime - (result[i].hitTime + result[i].holdDuration);
                    if (gap >= 0.75f && gap < minGapForCoins && result[i].noteType == Man3NoteType.Normal)
                    {
                        extraItems.Add(new NoteDataMan3
                        {
                            isLeftLane = result[i].isLeftLane,
                            noteType = Man3NoteType.Bomb,
                            hitTime = result[i].hitTime + (gap * 0.5f),
                            holdDuration = 0f,
                            flightDuration = defaultFlightDuration
                        });
                    }
                }
            }
        }

        if (extraItems.Count > 0)
        {
            result.AddRange(extraItems);
            result.Sort((a, b) => a.hitTime.CompareTo(b.hitTime));
        }

        return result;
    }
}