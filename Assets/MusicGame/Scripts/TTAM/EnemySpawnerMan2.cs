
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerMan2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private Transform player;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnLeft;
    [SerializeField] private Transform spawnRight;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointLeft;
    [SerializeField] private Transform hitPointRight;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float leadIn = 3f;

    [Header("MIDI Spawn Settings")]
    [Tooltip("Enemy xuất hiện trước thời điểm MIDI bao nhiêu giây")]
    [SerializeField] private float spawnAheadTime = 1.5f;

    [Tooltip("Khoảng cách thời gian để ghép hai nốt L/R thành hợp âm")]
    [SerializeField] private float simultaneousThreshold = 0.03f;

    [Tooltip("Bật để tự động spawn khi vào scene")]
    [SerializeField] private bool spawnOnStart = false;

    [Header("Lane Settings")]
    [Tooltip("Bật để random lane thay vì theo MIDI")]
    [SerializeField] private bool randomLane = false;

    [Header("Spawn Limit")]
    [Tooltip("Khoảng cách tối thiểu giữa hai enemy cùng lane")]
    [SerializeField] private float minSpawnInterval = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private class MidiSpawnEvent
    {
        public float time;
        public List<int> lanes = new List<int>();
    }

    private readonly List<MidiSpawnEvent> spawnEvents =
        new List<MidiSpawnEvent>();

    private readonly List<EnemyRhythmController> activeEnemies =
        new List<EnemyRhythmController>();

    private readonly float[] lastSpawnTimeOnLane =
        { -999f, -999f };

    private int nextEventIndex;

    private double songStartDsp;

    private bool isPlaying;

    public float SongTime
    {
        get
        {
            if (!isPlaying)
                return 0f;

            return (float)(
                AudioSettings.dspTime - songStartDsp
            );
        }
    }

    public bool IsPlaying => isPlaying;

    // =========================================
    // START
    // =========================================

    private void Start()
    {
        if (spawnOnStart)
            StartSpawner(leadIn);
    }

    public void StartSpawner(float countdown = 3f)
    {
        if (isPlaying)
            return;

        if (midiReader == null ||
            musicSource == null ||
            musicSource.clip == null ||
            player == null ||
            spawnLeft == null ||
            spawnRight == null ||
            hitPointLeft == null ||
            hitPointRight == null)
        {
            Debug.LogError(
                "EnemySpawnerMan2: Missing references!"
            );
            return;
        }

        BuildMidiEvents();

        if (spawnEvents.Count == 0)
        {
            Debug.LogError(
                "EnemySpawnerMan2: No MIDI events found!"
            );
            return;
        }

        nextEventIndex = 0;

        lastSpawnTimeOnLane[0] = -999f;
        lastSpawnTimeOnLane[1] = -999f;

        activeEnemies.Clear();

        songStartDsp = AudioSettings.dspTime + countdown;

        musicSource.Stop();
        musicSource.PlayScheduled(songStartDsp);

        isPlaying = true;

        if (showDebug)
        {
            Debug.Log(
                $"Rhythm started: {spawnEvents.Count} MIDI events"
            );
        }
    }

    // =========================================
    // BUILD MIDI EVENTS
    // =========================================

    private void BuildMidiEvents()
    {
        spawnEvents.Clear();

        var midiNotes = midiReader.GetNotesWithSpeed(1);

        List<MidiReader.RhythmNote> notes =
            new List<MidiReader.RhythmNote>(midiNotes);

        notes.Sort((a, b) => a.time.CompareTo(b.time));

        foreach (var note in notes)
        {
            if (note.lane != 0 && note.lane != 1)
                continue;

            MidiSpawnEvent midiEvent = null;

            // Ghép các note gần nhau thành một event
            if (spawnEvents.Count > 0)
            {
                MidiSpawnEvent previous =
                    spawnEvents[spawnEvents.Count - 1];

                if (Mathf.Abs(previous.time - note.time)
                    <= simultaneousThreshold)
                {
                    midiEvent = previous;
                }
            }

            if (midiEvent == null)
            {
                midiEvent = new MidiSpawnEvent
                {
                    time = note.time
                };

                spawnEvents.Add(midiEvent);
            }

            if (!midiEvent.lanes.Contains(note.lane))
                midiEvent.lanes.Add(note.lane);
        }

        if (showDebug)
        {
            Debug.Log(
                $"Built {spawnEvents.Count} MIDI spawn events"
            );
        }
    }

    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        if (!isPlaying)
            return;

        SpawnUpcomingEnemies();

        activeEnemies.RemoveAll(
            enemy => enemy == null
        );
    }

    // =========================================
    // SPAWN MIDI ENEMIES
    // =========================================

    private void SpawnUpcomingEnemies()
    {
        float now = SongTime;

        while (nextEventIndex < spawnEvents.Count)
        {
            MidiSpawnEvent midiEvent =
                spawnEvents[nextEventIndex];

            // Chưa tới thời điểm spawn
            float spawnTime =
                midiEvent.time - spawnAheadTime;

            if (now < spawnTime)
                break;

            nextEventIndex++;

            // Spawn từng lane trong event.
            // Nếu là hợp âm L/R thì spawn cả hai enemy.
            foreach (int originalLane in midiEvent.lanes)
            {
                int lane = randomLane
                    ? UnityEngine.Random.Range(0, 2)
                    : originalLane;

                SpawnEnemy(
                    lane,
                    midiEvent.time,
                    spawnTime,
                    now
                );
            }
        }
    }

    // =========================================
    // RANDOM PREFAB
    // =========================================

    private GameObject GetRandomPrefab()
    {
        if (enemyPrefabs == null ||
            enemyPrefabs.Count == 0)
            return null;

        List<GameObject> validPrefabs =
            enemyPrefabs.FindAll(p => p != null);

        if (validPrefabs.Count == 0)
            return null;

        int index = UnityEngine.Random.Range(
            0,
            validPrefabs.Count
        );

        return validPrefabs[index];
    }

    // =========================================
    // SPAWN ENEMY
    // =========================================

    private void SpawnEnemy(
        int lane,
        float hitTime,
        float spawnTime,
        float now)
    {
        bool isLeft = lane == 0;

        Transform spawnPoint = isLeft
            ? spawnLeft
            : spawnRight;

        Transform hitPoint = isLeft
            ? hitPointLeft
            : hitPointRight;

        // Chặn hai enemy cùng lane spawn quá sát nhau
        if (hitTime - lastSpawnTimeOnLane[lane]
            < minSpawnInterval)
        {
            if (showDebug)
                Debug.Log("Skipped enemy: MIDI notes too close");

            return;
        }

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
        {
            Debug.LogError(
                "EnemySpawnerMan2: No valid enemy prefab!"
            );
            return;
        }

        float travelTime = hitTime - now;

        if (travelTime <= 0f)
            return;

        GameObject obj = Instantiate(
            prefab,
            spawnPoint.position,
            Quaternion.identity
        );

        EnemyMan2 movement =
            obj.GetComponent<EnemyMan2>();

        EnemyRhythmController rhythm =
            obj.GetComponent<EnemyRhythmController>();

        if (movement == null || rhythm == null)
        {
            Debug.LogError(
                "Enemy prefab requires EnemyMan2 and EnemyRhythmController!"
            );

            Destroy(obj);
            return;
        }

        // Cập nhật thời điểm spawn lane
        lastSpawnTimeOnLane[lane] = hitTime;

        // Enemy di chuyển tới hit point theo MIDI
        movement.Init(
            player,
            hitPoint,
            spawnTime,
            hitTime,
            () => SongTime
        );

        // Mỗi enemy xử lý một nốt MIDI
        rhythm.Initialize(
            new float[] { hitTime },
            isLeft,
            () => SongTime
        );

        activeEnemies.Add(rhythm);

        if (showDebug)
        {
            Debug.Log(
                $"Spawn {(isLeft ? "LEFT" : "RIGHT")} " +
                $"| SpawnTime: {spawnTime:F3}" +
                $" | HitTime: {hitTime:F3}" +
                $" | Travel: {travelTime:F3}"
            );
        }
    }

    // =========================================
    // PLAYER INPUT
    // =========================================

    public bool TryHit(bool isLeft)
    {
        EnemyRhythmController target = null;

        float bestError = float.MaxValue;

        foreach (EnemyRhythmController enemy in activeEnemies)
        {
            if (enemy == null || enemy.IsCompleted)
                continue;

            if (enemy.IsLeft != isLeft)
                continue;

            if (!enemy.CanHitNow())
                continue;

            if (enemy.TimingError < bestError)
            {
                bestError = enemy.TimingError;
                target = enemy;
            }
        }

        if (target == null)
            return false;

        EnemyRhythmController.HitGrade grade;

        bool success = target.TryHit(out grade);

        if (success)
        {
            EnemyMan2 movement =
                target.GetComponent<EnemyMan2>();

            if (movement != null)
                movement.MarkHit();
        }

        return success;
    }

    // =========================================
    // STOP / RESET
    // =========================================

    public void StopSpawner()
    {
        isPlaying = false;

        if (musicSource != null)
            musicSource.Stop();

        foreach (EnemyRhythmController enemy in activeEnemies)
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }

        activeEnemies.Clear();
        spawnEvents.Clear();

        nextEventIndex = 0;

        lastSpawnTimeOnLane[0] = -999f;
        lastSpawnTimeOnLane[1] = -999f;
    }
}
