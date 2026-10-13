using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawn coin theo thời gian bài hát (SongTime). Tốc độ, hướng, vị trí Player
/// đều lấy từ PianoKeySpawner nên luôn khớp với key.
///
/// 2 nguồn coin:
///  1) Tự sinh từ MIDI: vòng cung coin ở khoảng trống giữa 2 nốt, hàng coin dọc theo nốt dài.
///  2) Pattern đặt tay trong Inspector (tuỳ chọn).
///
/// Coin được xếp lịch sẵn từ đầu bài và chỉ xuất hiện đúng lúc nó vào tầm nhìn
/// (SpawnAheadTime trước khi chạm Player) nên không bị "bật" ra giữa màn hình.
/// </summary>
public class CoinSpawner : MonoBehaviour
{
    public enum Shape
    {
        Line,   // hàng ngang
        Arc     // vòng cung
    }

    [Serializable]
    public class CoinPattern
    {
        [Tooltip("Giây trong bài hát mà coin ĐẦU TIÊN chạm tới player")]
        public float startTime;
        [Min(1)] public int count = 5;
        [Tooltip("Khoảng cách thời gian giữa 2 coin (giây)")]
        public float interval = 0.15f;
        [Tooltip("Độ cao Y (world) của hàng coin")]
        public float y;
        public Shape shape = Shape.Line;
        public float arcHeight = 1.5f;
    }

    private struct Group
    {
        public float spawnTime;
        public float startTime;
        public int count;
        public float interval;
        public float y;
        public Shape shape;
        public float arcHeight;
    }

    [Header("References")]
    [SerializeField] private Coin coinPrefab;
    [SerializeField] private Transform container;

    [Header("Auto Generate từ MIDI")]
    [SerializeField] private bool autoGenerate = true;

    [Tooltip("Khoảng cách thời gian giữa 2 coin liên tiếp (giây)")]
    [SerializeField] private float coinInterval = 0.15f;

    [Header("Coin ở khoảng trống giữa 2 nốt")]
    [Tooltip("Khoảng trống tối thiểu (giây) giữa 2 nốt thì mới có coin")]
    [SerializeField] private float gapMinDuration = 1.2f;

    [Tooltip("Chừa lại sát nốt trước/sau (giây) để coin không đè lên key")]
    [SerializeField] private float gapMargin = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Xác suất 1 khoảng trống có coin (1 = khoảng trống nào cũng có)")]
    [SerializeField] private float gapCoinChance = 0.6f;

    [SerializeField] private int maxCoinsPerGap = 8;

    [Tooltip("Độ cao của vòng cung so với Y của key (đỉnh cung = Y + giá trị này)")]
    [SerializeField] private float gapArcHeight = 1.5f;

    [Tooltip("Nâng cả vòng cung lên so với Y của key")]
    [SerializeField] private float gapYOffset = 0.3f;

    [Header("Coin dọc theo nốt dài")]
    [SerializeField] private bool longNoteCoins = true;

    [Tooltip("Nâng coin lên so với đường đi của nốt dài")]
    [SerializeField] private float longNoteCoinYOffset = 0.5f;

    [Tooltip("Chừa đầu/đuôi nốt dài (giây)")]
    [SerializeField] private float longNoteInset = 0.15f;

    [SerializeField] private int maxCoinsPerLongNote = 30;

    [Header("Pattern đặt tay (tuỳ chọn)")]
    [SerializeField] private List<CoinPattern> manualPatterns = new List<CoinPattern>();

    private readonly List<Group> pending = new List<Group>();
    private readonly List<GameObject> spawned = new List<GameObject>();
    private int nextGroup;

    // =====================================================
    // BUILD (PianoKeySpawner gọi trong StartSong)
    // =====================================================

    public void Build(
        List<MidiReader.RhythmNote> notes,
        float minKeyGap,
        float keySpeed,
        float longNoteMinDuration,
        float keyY,
        float longNoteY)
    {
        ClearAll();

        if (coinPrefab == null || PianoKeySpawner.Instance == null)
            return;

        foreach (CoinPattern p in manualPatterns)
            Enqueue(p.startTime, p.count, p.interval, p.y, p.shape, p.arcHeight);

        if (autoGenerate && notes != null)
        {
            // Lặp lại đúng bộ lọc của PianoKeySpawner để biết nốt nào thật sự xuất hiện
            float lastEnd = -999f;
            float minGapTime = minKeyGap / Mathf.Max(0.01f, keySpeed);

            foreach (MidiReader.RhythmNote note in notes)
            {
                if (note.time - lastEnd < minGapTime)
                    continue;

                bool isLong = note.duration >= longNoteMinDuration;

                if (lastEnd > -900f)
                    AddGapCoins(lastEnd, note.time, keyY);

                if (isLong && longNoteCoins)
                    AddLongNoteCoins(note.time, note.duration, longNoteY);

                lastEnd = note.time + (isLong ? note.duration : 0f);
            }
        }

        pending.Sort((a, b) => a.spawnTime.CompareTo(b.spawnTime));
        nextGroup = 0;
    }

    public void ClearAll()
    {
        pending.Clear();
        nextGroup = 0;

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }

        spawned.Clear();
    }

    // =====================================================
    // GENERATE
    // =====================================================

    private void AddGapCoins(float prevEnd, float nextTime, float keyY)
    {
        if (nextTime - prevEnd < gapMinDuration)
            return;

        float start = prevEnd + gapMargin;
        float end = nextTime - gapMargin;
        float available = end - start;

        if (available <= 0f)
            return;

        if (UnityEngine.Random.value > gapCoinChance)
            return;

        int count = Mathf.Clamp(
            Mathf.FloorToInt(available / coinInterval) + 1, 1, maxCoinsPerGap);

        // Căn giữa khoảng trống
        float span = (count - 1) * coinInterval;
        float first = start + (available - span) * 0.5f;

        Enqueue(first, count, coinInterval, keyY + gapYOffset, Shape.Arc, gapArcHeight);
    }

    private void AddLongNoteCoins(float noteTime, float duration, float longNoteY)
    {
        float start = noteTime + longNoteInset;
        float end = noteTime + duration - longNoteInset;
        float available = end - start;

        if (available < 0f)
            return;

        int count = Mathf.Clamp(
            Mathf.FloorToInt(available / coinInterval) + 1, 1, maxCoinsPerLongNote);

        Enqueue(start, count, coinInterval, longNoteY + longNoteCoinYOffset, Shape.Line, 0f);
    }

    private void Enqueue(float startTime, int count, float interval, float y, Shape shape, float arcHeight)
    {
        pending.Add(new Group
        {
            spawnTime = startTime - PianoKeySpawner.Instance.SpawnAheadTime,
            startTime = startTime,
            count = count,
            interval = interval,
            y = y,
            shape = shape,
            arcHeight = arcHeight
        });
    }

    // =====================================================
    // UPDATE / SPAWN
    // =====================================================

    private void Update()
    {
        if (coinPrefab == null || PianoKeySpawner.Instance == null)
            return;

        if (nextGroup >= pending.Count)
            return;

        float songTime = PianoKeySpawner.Instance.SongTime;

        while (nextGroup < pending.Count && songTime >= pending[nextGroup].spawnTime)
        {
            SpawnGroup(pending[nextGroup], songTime);
            nextGroup++;
        }
    }

    private void SpawnGroup(Group g, float songTime)
    {
        PianoKeySpawner sp = PianoKeySpawner.Instance;

        float speed = sp.KeySpeed;
        float dir = sp.SpawnDirection;
        float playerX = sp.PlayerX;

        spawned.RemoveAll(c => c == null);

        for (int i = 0; i < g.count; i++)
        {
            float targetTime = g.startTime + i * g.interval;

            // Coin đã quá giờ thì không thể nhặt được nữa
            if (targetTime <= songTime)
                continue;

            float y = g.y;
            if (g.shape == Shape.Arc && g.count > 1)
            {
                float t = (float)i / (g.count - 1);
                y += Mathf.Sin(t * Mathf.PI) * g.arcHeight;
            }

            float x = playerX + dir * speed * (targetTime - songTime);

            Coin coin = Instantiate(
                coinPrefab,
                new Vector3(x, y, coinPrefab.transform.position.z),
                Quaternion.identity,
                container);

            coin.Setup(targetTime, speed, playerX, dir);
            spawned.Add(coin.gameObject);
        }
    }
}