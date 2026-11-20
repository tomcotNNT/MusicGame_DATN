using System;
using System.Collections.Generic;
using UnityEngine;

public class SubmarineRhythmSpawner : MonoBehaviour
{
    public event Action SongEnded;

    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private SubmarineController submarine;
    [Tooltip("Mỗi lần thả sẽ random 1 prefab trong danh sách. Mỗi prefab phải có FallingItem")]
    [SerializeField] private List<FallingItem> itemPrefabs = new List<FallingItem>();
    [Tooltip("Bật: không để 2 lần liền nhau ra cùng 1 loại item (khi có từ 2 loại trở lên)")]
    [SerializeField] private bool avoidRepeat = true;

    private int lastItemIndex = -1;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointLeft;
    [SerializeField] private Transform hitPointRight;

    [Header("Hit Point Animations")]
    [SerializeField] private Animator hitPointLeftAnimator;
    [SerializeField] private Animator hitPointRightAnimator;

    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string missTrigger = "Miss";

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float leadIn = 3f;

    [Header("MIDI Settings")]
    [Tooltip("Item được thả trước thời điểm nốt bao nhiêu giây (= thời gian rơi)")]
    [SerializeField] private float fallDuration = 1.5f;
    [SerializeField] private float simultaneousThreshold = 0.03f;
    [SerializeField] private float minSpawnInterval = 0.15f;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Timing")]
    [Tooltip("Chạm trong khoảng +-giây quanh thời điểm nốt thì tính trúng")]
    [SerializeField] private float hitWindow = 0.15f;
    [Tooltip("Quá thời điểm nốt + giây này mà chưa chạm thì tính Miss")]
    [SerializeField] private float missWindow = 0.2f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [Header("Boss HP")]
    [Tooltip("Máu boss = số nốt x tỉ lệ này. 1 = phải trúng hết mọi nốt mới hạ được")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bossHpRatio = 1f;

    private class MidiSpawnEvent
    {
        public float time;
        public List<int> lanes = new List<int>();
    }

    private readonly List<MidiSpawnEvent> spawnEvents = new List<MidiSpawnEvent>();
    private readonly List<FallingItem> activeItems = new List<FallingItem>();
    private readonly float[] lastSpawnTimeOnLane = { -999f, -999f };

    private int nextEventIndex;
    private double songStartDsp;
    private bool isPlaying;
    private bool songEnded;

    public float SongTime => isPlaying ? (float)(AudioSettings.dspTime - songStartDsp) : 0f;
    public bool IsPlaying => isPlaying;

    private void Start()
    {
        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        if (spawnOnStart)
            StartSpawner(leadIn);
    }

    public void StartSpawner(float countdown = 3f)
    {
        if (isPlaying) return;

        if (midiReader == null || musicSource == null || musicSource.clip == null ||
            submarine == null || GetRandomItemPrefab() == null ||
            hitPointLeft == null || hitPointRight == null)
        {
            Debug.LogError("SubmarineRhythmSpawner: Missing references!");
            return;
        }

        BuildMidiEvents();

        if (spawnEvents.Count == 0)
        {
            Debug.LogError("SubmarineRhythmSpawner: No MIDI events found!");
            return;
        }

        int noteCount = CountDroppableNotes();
        submarine.InitHp(Mathf.CeilToInt(noteCount * bossHpRatio));

        nextEventIndex = 0;
        songEnded = false;
        lastSpawnTimeOnLane[0] = -999f;
        lastSpawnTimeOnLane[1] = -999f;
        activeItems.Clear();

        songStartDsp = AudioSettings.dspTime + countdown;
        musicSource.Stop();
        musicSource.PlayScheduled(songStartDsp);

        isPlaying = true;
    }

    // =========================================
    // BUILD MIDI EVENTS (giống EnemySpawnerMan2)
    // =========================================

    private void BuildMidiEvents()
    {
        spawnEvents.Clear();

        var notes = new List<MidiReader.RhythmNote>(midiReader.GetNotesWithSpeed(1));
        notes.Sort((a, b) => a.time.CompareTo(b.time));

        foreach (var note in notes)
        {
            if (note.lane != 0 && note.lane != 1)
                continue;

            MidiSpawnEvent midiEvent = null;

            if (spawnEvents.Count > 0)
            {
                MidiSpawnEvent previous = spawnEvents[spawnEvents.Count - 1];
                if (Mathf.Abs(previous.time - note.time) <= simultaneousThreshold)
                    midiEvent = previous;
            }

            if (midiEvent == null)
            {
                midiEvent = new MidiSpawnEvent { time = note.time };
                spawnEvents.Add(midiEvent);
            }

            if (!midiEvent.lanes.Contains(note.lane))
                midiEvent.lanes.Add(note.lane);
        }
    }

    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        if (!isPlaying) return;

        DropUpcomingItems();
        CheckMissedItems();
        CheckSongEnd();
    }

    private void DropUpcomingItems()
    {
        float now = SongTime;

        while (nextEventIndex < spawnEvents.Count)
        {
            MidiSpawnEvent ev = spawnEvents[nextEventIndex];
            float dropTime = ev.time - fallDuration;

            if (now < dropTime)
                break;

            nextEventIndex++;

            bool dropped = false;
            foreach (int lane in ev.lanes)
            {
                if (DropItem(lane, ev.time, dropTime))
                    dropped = true;
            }

            // Mỗi lần thả thì rung màn (hợp âm chỉ rung một lần)
            if (dropped)
                submarine.Shake();
        }
    }

    private bool DropItem(int lane, float hitTime, float dropTime)
    {
        bool isLeft = lane == 0;

        if (hitTime - lastSpawnTimeOnLane[lane] < minSpawnInterval)
            return false;

        Transform hitPoint = isLeft ? hitPointLeft : hitPointRight;

        // Bỏ nốt nếu đã trễ quá (không còn thời gian rơi)
        if (hitTime - SongTime <= 0f)
            return false;

        lastSpawnTimeOnLane[lane] = hitTime;

       Vector3 start = submarine.GetDropPosition();

        FallingItem prefab = GetRandomItemPrefab();
        if (prefab == null)
            return false;

        FallingItem item = Instantiate(prefab, start, Quaternion.identity);

        item.Init(isLeft, start, hitPoint.position, dropTime, hitTime, () => SongTime);

        activeItems.Add(item);

        if (showDebug)
            Debug.Log($"Drop {(isLeft ? "LEFT" : "RIGHT")} | HitTime: {hitTime:F3}");

        return true;
    }

    // Item quá hạn mà chưa chạm -> Miss, combo về 0
    private void CheckMissedItems()
    {
        float now = SongTime;

        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            FallingItem item = activeItems[i];

            if (item == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            if (now > item.HitTime + missWindow)
            {
                // Chạy animation Miss của đúng HitPoint
                PlayHitPointAnimation(item.IsLeft, false);

                if (comboSystem != null)
                    comboSystem.MissNote();

                activeItems.RemoveAt(i);
                Destroy(item.gameObject);
            }
        }
    }

    private void CheckSongEnd()
    {
        if (songEnded || musicSource == null || musicSource.clip == null)
            return;

        bool finished =
            nextEventIndex >= spawnEvents.Count &&
            SongTime >= musicSource.clip.length &&
            activeItems.Count == 0;

        if (!finished) return;

        songEnded = true;
        isPlaying = false;
        SongEnded?.Invoke();
    }
    private FallingItem GetRandomItemPrefab()
    {
        if (itemPrefabs == null || itemPrefabs.Count == 0)
            return null;

        // Gom các prefab hợp lệ (bỏ ô trống trong Inspector)
        List<int> valid = new List<int>();
        for (int i = 0; i < itemPrefabs.Count; i++)
        {
            if (itemPrefabs[i] != null)
                valid.Add(i);
        }

        if (valid.Count == 0)
            return null;

        int pick = valid[UnityEngine.Random.Range(0, valid.Count)];

        // Tránh trùng loại với lần trước
        if (avoidRepeat && valid.Count > 1)
        {
            int guard = 0;
            while (pick == lastItemIndex && guard++ < 10)
                pick = valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        lastItemIndex = pick;
        return itemPrefabs[pick];
    }

    // =========================================
    // PLAYER INPUT (PlayerController gọi hàm này)
    // =========================================

    // Giữ bản cũ để code khác gọi TryHit(isLeft) không lỗi
    public bool TryHit(bool isLeft)
    {
        return TryHit(isLeft, out _);
    }

    public bool TryHit(bool isLeft, out Vector3 hitPosition)
    {
        hitPosition = Vector3.zero;

        FallingItem target = null;
        float bestError = float.MaxValue;

        foreach (FallingItem item in activeItems)
        {
            if (item == null || item.IsLeft != isLeft)
                continue;

            float error = item.TimingError();

            if (error > hitWindow)
                continue;

            if (error < bestError)
            {
                bestError = error;
                target = item;
            }
        }

        if (target == null)
            return false;

        hitPosition = target.transform.position;

        // Chạy animation Hit của đúng HitPoint
        PlayHitPointAnimation(target.IsLeft, true);

        activeItems.Remove(target);
        Destroy(target.gameObject);

        submarine.TakeHit();

        // Cộng combo + điểm (100/200/300 theo combo liên tiếp)
        if (comboSystem != null)
            comboSystem.HitNote();

        return true;
    }

    public void StopSpawner()
    {
        isPlaying = false;

        if (musicSource != null)
            musicSource.Stop();

        foreach (FallingItem item in activeItems)
        {
            if (item != null)
                Destroy(item.gameObject);
        }

        activeItems.Clear();
        spawnEvents.Clear();
        nextEventIndex = 0;
    }

    private int CountDroppableNotes()
    {
        float[] last = { -999f, -999f };
        int count = 0;

        foreach (MidiSpawnEvent ev in spawnEvents)
        {
            foreach (int lane in ev.lanes)
            {
                if (ev.time - last[lane] < minSpawnInterval)
                    continue;

                last[lane] = ev.time;
                count++;
            }
        }

        return count;
    }

    private void PlayHitPointAnimation(bool isLeft, bool isHit)
    {
        Animator animator = isLeft
            ? hitPointLeftAnimator
            : hitPointRightAnimator;

        if (animator == null)
            return;

        string trigger = isHit ? hitTrigger : missTrigger;

        // Reset để có thể kích hoạt lại animation liên tục
        animator.ResetTrigger(hitTrigger);
        animator.ResetTrigger(missTrigger);

        animator.SetTrigger(trigger);
    }
}