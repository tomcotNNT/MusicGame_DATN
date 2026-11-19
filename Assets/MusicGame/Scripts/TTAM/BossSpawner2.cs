using System;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner2 : MonoBehaviour
{
    public event Action SongEnded;

    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [Tooltip("Script Boss giống SubmarineController (InitHp, TakeHit, GetDropPosition)")]
    [SerializeField] private BossController2 boss;
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private string attackTrigger = "Attack1";
    [SerializeField] private Transform player;
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [Tooltip("Không để 2 lần liền nhau ra cùng 1 loại enemy")]
    [SerializeField] private bool avoidRepeat = true;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointLeft;
    [SerializeField] private Transform hitPointRight;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float leadIn = 3f;

    [Header("MIDI Settings")]
    [Tooltip("Enemy spawn trước thời điểm nốt bao nhiêu giây (= thời gian bay)")]
    [SerializeField] private float travelDuration = 1.5f;
    [SerializeField] private float simultaneousThreshold = 0.03f;
    [SerializeField] private float minSpawnInterval = 0.15f;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Timing")]
    [SerializeField] private float missWindow = 0.2f;

    [Header("Boss HP")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bossHpRatio = 1f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private class MidiSpawnEvent
    {
        public float time;
        public List<int> lanes = new List<int>();
    }

    private class ActiveEnemy
    {
        public EnemyRhythmController rhythm;
        public EnemyMan2 movement;
        public float hitTime;
    }

    private readonly List<MidiSpawnEvent> spawnEvents = new List<MidiSpawnEvent>();
    private readonly List<ActiveEnemy> activeEnemies = new List<ActiveEnemy>();
    private readonly float[] lastSpawnTimeOnLane = { -999f, -999f };

    private int attackHash;
    private int lastPrefabIndex = -1;
    private int nextEventIndex;
    private double songStartDsp;
    private bool isPlaying;
    private bool songEnded;

    public float SongTime => isPlaying ? (float)(AudioSettings.dspTime - songStartDsp) : 0f;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        attackHash = Animator.StringToHash(attackTrigger);
    }

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
            boss == null || player == null || GetRandomPrefab() == null ||
            hitPointLeft == null || hitPointRight == null)
        {
            Debug.LogError("BossSpawner: Missing references!");
            return;
        }

        BuildMidiEvents();

        if (spawnEvents.Count == 0)
        {
            Debug.LogError("BossSpawner: No MIDI events found!");
            return;
        }

        boss.InitHp(Mathf.CeilToInt(CountDroppableNotes() * bossHpRatio));

        nextEventIndex = 0;
        songEnded = false;
        lastSpawnTimeOnLane[0] = -999f;
        lastSpawnTimeOnLane[1] = -999f;
        activeEnemies.Clear();

        songStartDsp = AudioSettings.dspTime + countdown;
        musicSource.Stop();
        musicSource.PlayScheduled(songStartDsp);

        isPlaying = true;
    }

    // =========================================
    // BUILD MIDI EVENTS
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

        SpawnUpcomingEnemies();
        CheckMissedEnemies();
        CheckSongEnd();
    }

    private void SpawnUpcomingEnemies()
    {
        float now = SongTime;

        while (nextEventIndex < spawnEvents.Count)
        {
            MidiSpawnEvent ev = spawnEvents[nextEventIndex];
            float spawnTime = ev.time - travelDuration;

            if (now < spawnTime)
                break;

            nextEventIndex++;

            bool spawned = false;
            foreach (int lane in ev.lanes)
            {
                if (SpawnEnemy(lane, ev.time, spawnTime))
                    spawned = true;
            }

            // Mỗi lần spawn thì Boss tung Attack1 (hợp âm chỉ trigger một lần)
            if (spawned)
                boss.PlayAttack();
        }
    }

    private void PlayBossAttack()
    {
        if (bossAnimator == null) return;

        bossAnimator.ResetTrigger(attackHash);
        bossAnimator.SetTrigger(attackHash);
    }

    private bool SpawnEnemy(int lane, float hitTime, float spawnTime)
    {
        bool isLeft = lane == 0;

        if (hitTime - lastSpawnTimeOnLane[lane] < minSpawnInterval)
            return false;

        // Bỏ nốt nếu đã trễ quá
        if (hitTime - SongTime <= 0f)
            return false;

        GameObject prefab = GetRandomPrefab();
        if (prefab == null)
            return false;

        Transform hitPoint = isLeft ? hitPointLeft : hitPointRight;
        Vector3 start = boss.GetDropPosition();

        GameObject obj = Instantiate(prefab, start, Quaternion.identity);

        EnemyMan2 movement = obj.GetComponent<EnemyMan2>();
        EnemyRhythmController rhythm = obj.GetComponent<EnemyRhythmController>();

        if (movement == null || rhythm == null)
        {
            Debug.LogError("Enemy prefab requires EnemyMan2 and EnemyRhythmController!");
            Destroy(obj);
            return false;
        }

        lastSpawnTimeOnLane[lane] = hitTime;

        movement.Init(player, hitPoint, spawnTime, hitTime, () => SongTime);
        rhythm.Initialize(new float[] { hitTime }, isLeft, () => SongTime);

        activeEnemies.Add(new ActiveEnemy
        {
            rhythm = rhythm,
            movement = movement,
            hitTime = hitTime
        });

        if (showDebug)
            Debug.Log($"Spawn {(isLeft ? "LEFT" : "RIGHT")} | HitTime: {hitTime:F3}");

        return true;
    }

    // Enemy quá hạn mà chưa đánh trúng -> Miss, combo về 0
    private void CheckMissedEnemies()
    {
        float now = SongTime;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            ActiveEnemy e = activeEnemies[i];

            if (e.rhythm == null)
            {
                activeEnemies.RemoveAt(i);
                continue;
            }

            if (now > e.hitTime + missWindow && !e.rhythm.IsCompleted)
            {
                if (comboSystem != null)
                    comboSystem.MissNote();

                activeEnemies.RemoveAt(i);
                Destroy(e.rhythm.gameObject);
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
            activeEnemies.Count == 0;

        if (!finished) return;

        songEnded = true;
        isPlaying = false;
        SongEnded?.Invoke();
    }

    private GameObject GetRandomPrefab()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
            return null;

        List<int> valid = new List<int>();
        for (int i = 0; i < enemyPrefabs.Count; i++)
        {
            if (enemyPrefabs[i] != null)
                valid.Add(i);
        }

        if (valid.Count == 0)
            return null;

        int pick = valid[UnityEngine.Random.Range(0, valid.Count)];

        if (avoidRepeat && valid.Count > 1)
        {
            int guard = 0;
            while (pick == lastPrefabIndex && guard++ < 10)
                pick = valid[UnityEngine.Random.Range(0, valid.Count)];
        }

        lastPrefabIndex = pick;
        return enemyPrefabs[pick];
    }

    // =========================================
    // PLAYER INPUT
    // =========================================

    public bool TryHit(bool isLeft)
    {
        return TryHit(isLeft, out _);
    }

    public bool TryHit(bool isLeft, out Vector3 hitPosition)
    {
        hitPosition = Vector3.zero;

        ActiveEnemy target = null;
        float bestError = float.MaxValue;

        foreach (ActiveEnemy e in activeEnemies)
        {
            if (e.rhythm == null || e.rhythm.IsCompleted)
                continue;

            if (e.rhythm.IsLeft != isLeft)
                continue;

            if (!e.rhythm.CanHitNow())
                continue;

            if (e.rhythm.TimingError < bestError)
            {
                bestError = e.rhythm.TimingError;
                target = e;
            }
        }

        if (target == null)
            return false;

        hitPosition = target.rhythm.transform.position;

        EnemyRhythmController.HitGrade grade;
        if (!target.rhythm.TryHit(out grade))
            return false;

        if (target.movement != null)
            target.movement.MarkHit();

        activeEnemies.Remove(target);

        boss.TakeHit();

        if (comboSystem != null)
            comboSystem.HitNote();

        return true;
    }

    // =========================================
    // STOP / RESET
    // =========================================

    public void StopSpawner()
    {
        isPlaying = false;

        if (musicSource != null)
            musicSource.Stop();

        foreach (ActiveEnemy e in activeEnemies)
        {
            if (e.rhythm != null)
                Destroy(e.rhythm.gameObject);
        }

        activeEnemies.Clear();
        spawnEvents.Clear();
        nextEventIndex = 0;
        lastSpawnTimeOnLane[0] = -999f;
        lastSpawnTimeOnLane[1] = -999f;
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
}