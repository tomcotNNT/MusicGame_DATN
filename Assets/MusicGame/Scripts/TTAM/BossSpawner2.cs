
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossSpawner2 : MonoBehaviour, IRhythmSongClock, ISongPausable
{
    public event Action SongEnded;

    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private BossController2 boss;
    [SerializeField] private Transform player;
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private bool avoidRepeat = true;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointLeft;
    [SerializeField] private Transform hitPointRight;

    [Header("Hit Point Animations")]
    [SerializeField] private Animator hitPointLeftAnimator;
    [SerializeField] private Animator hitPointRightAnimator;

    [Header("Celtic Animations")]
    [SerializeField] private List<Animator> celticAnimators = new List<Animator>();
    [SerializeField] private string celticHitTrigger = "Hit";

    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string missTrigger = "Miss";

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float leadIn = 3f;

    [Header("MIDI Settings")]
    [Tooltip("Enemy spawn trước thời điểm nốt bao nhiêu giây")]
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

    private int lastPrefabIndex = -1;
    private int nextEventIndex;

    private readonly SongClock clock = new SongClock();

    private bool isPlaying;
    private bool songEnded;

    private Coroutine spawnEventCoroutine;

    // Đứng yên khi pause (xem SongClock)
    public float SongTime => isPlaying ? clock.SongTime : 0f;

    public bool IsPlaying => isPlaying;

    public void PauseSong()
    {
        if (isPlaying) clock.Pause(musicSource);
    }

    public void ResumeSong()
    {
        if (isPlaying) clock.Resume(musicSource);
    }

    private void Start()
    {
        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        if (spawnOnStart)
            StartSpawner(leadIn);
    }

    // =========================================
    // START
    // =========================================

    public void StartSpawner(float countdown = 3f)
    {
        if (isPlaying)
            return;

        if (midiReader == null ||
            musicSource == null ||
            musicSource.clip == null ||
            boss == null ||
            player == null ||
            GetRandomPrefab() == null ||
            hitPointLeft == null ||
            hitPointRight == null)
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

        if (spawnEventCoroutine != null)
        {
            StopCoroutine(spawnEventCoroutine);
            spawnEventCoroutine = null;
        }

        clock.Start(musicSource, countdown);

        isPlaying = true;

        Debug.Log($"[Man2] Bắt đầu bài: {spawnEvents.Count} sự kiện, nốt đầu tại {spawnEvents[0].time:F2}s, đếm ngược {countdown:F2}s");
    }

    // =========================================
    // BUILD MIDI EVENTS
    // =========================================

    private void BuildMidiEvents()
    {
        spawnEvents.Clear();

        var notes = new List<MidiReader.RhythmNote>(
            midiReader.GetNotesWithSpeed(1)
        );

        notes.Sort((a, b) => a.time.CompareTo(b.time));

        foreach (var note in notes)
        {
            if (note.lane != 0 && note.lane != 1)
                continue;

            MidiSpawnEvent midiEvent = null;

            if (spawnEvents.Count > 0)
            {
                MidiSpawnEvent previous =
                    spawnEvents[spawnEvents.Count - 1];

                if (Mathf.Abs(previous.time - note.time) <= simultaneousThreshold)
                    midiEvent = previous;
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

        // Nếu có hai lane cùng lúc, ưu tiên lane trái để Boss di chuyển trước
        foreach (MidiSpawnEvent ev in spawnEvents)
            ev.lanes.Sort();
    }

    // =========================================
    // UPDATE
    // =========================================

    private void Update()
    {
        if (!isPlaying)
            return;

        SpawnUpcomingEnemies();

        CheckMissedEnemies();
        CheckSongEnd();
    }

    // =========================================
    // SPAWN EVENT
    // =========================================

    private void SpawnUpcomingEnemies()
    {
        // Đang xử lý một sự kiện thì không tạo coroutine khác
        if (spawnEventCoroutine != null)
            return;

        if (nextEventIndex >= spawnEvents.Count)
            return;

        // Chưa hết đếm ngược (SongTime < 0) thì chưa cho Boss dịch chuyển / thả quái
        if (SongTime < 0f)
            return;

        MidiSpawnEvent ev = spawnEvents[nextEventIndex];

        // Nốt quá sát đầu bài: spawn ngay khi hết đếm ngược, quái bay nhanh hơn để vẫn kịp nhịp
        float spawnTime = Mathf.Max(0f, ev.time - travelDuration);

        // Bắt đầu chuẩn bị trước khi cần spawn
        float prepareTime = spawnTime - boss.TeleportTotalDuration;

        if (SongTime < prepareTime)
            return;

        nextEventIndex++;

        spawnEventCoroutine = StartCoroutine(
            PrepareBossAndSpawn(ev, spawnTime)
        );
    }

    private IEnumerator PrepareBossAndSpawn(
        MidiSpawnEvent ev,
        float spawnTime)
    {
        if (ev.lanes.Count == 0)
        {
            spawnEventCoroutine = null;
            yield break;
        }

        // Chọn lane đầu tiên để Boss di chuyển đến
        int targetLane = ev.lanes[0];

        // Đợi nếu Boss đang thực hiện một lần dịch chuyển khác
        while (boss.IsTeleporting)
        {
            if (boss.IsDefeated)
            {
                spawnEventCoroutine = null;
                yield break;
            }

            yield return null;
        }

        if (boss.IsDefeated)
        {
            spawnEventCoroutine = null;
            yield break;
        }

        // Lane 0 = Point A (trái)
        // Lane 1 = Point B (phải)
        bool targetPointA = targetLane == 0;

        // Nếu Boss chưa ở đúng vị trí thì dịch chuyển
        if (boss.IsAtPointA != targetPointA)
        {
            boss.MoveToLane(targetLane);

            // Chờ Vanish -> di chuyển -> Appear hoàn thành
            while (boss.IsTeleporting)
            {
                if (boss.IsDefeated)
                {
                    spawnEventCoroutine = null;
                    yield break;
                }

                yield return null;
            }
        }

        if (boss.IsDefeated)
        {
            spawnEventCoroutine = null;
            yield break;
        }

        // Chờ đến đúng thời điểm spawn đã tính từ MIDI
        while (SongTime < spawnTime)
        {
            if (boss.IsDefeated)
            {
                spawnEventCoroutine = null;
                yield break;
            }

            yield return null;
        }

        // Boss chạy Attack1 trước khi thả dơi
        boss.PlayAttack();

        // Nếu có hợp âm, spawn các lane trong cùng sự kiện
        foreach (int lane in ev.lanes)
        {
            SpawnEnemy(lane, ev.time, spawnTime);
        }

        spawnEventCoroutine = null;
    }

    // =========================================
    // SPAWN ENEMY
    // =========================================

    private bool SpawnEnemy(int lane, float hitTime, float spawnTime)
    {
        bool isLeft = lane == 0;

        if (hitTime - lastSpawnTimeOnLane[lane] < minSpawnInterval)
            return false;

        // Bỏ nốt nếu thời điểm đánh đã qua
        if (hitTime - SongTime <= 0f)
        {
            if (showDebug)
                Debug.LogWarning($"Skip late enemy: Lane {lane}");

            return false;
        }

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
            return false;

        Transform hitPoint = isLeft
            ? hitPointLeft
            : hitPointRight;

        Vector3 start = boss.GetDropPosition();

        GameObject obj = Instantiate(
            prefab,
            start,
            Quaternion.identity
        );

        EnemyMan2 movement = obj.GetComponent<EnemyMan2>();
        EnemyRhythmController rhythm =
            obj.GetComponent<EnemyRhythmController>();

        if (movement == null || rhythm == null)
        {
            Debug.LogError(
                "Enemy prefab requires EnemyMan2 and EnemyRhythmController!"
            );

            Destroy(obj);
            return false;
        }

        lastSpawnTimeOnLane[lane] = hitTime;

        movement.Init(
            player,
            hitPoint,
            spawnTime,
            hitTime,
            () => SongTime
        );

        rhythm.Initialize(
            new float[] { hitTime },
            isLeft,
            () => SongTime
        );

        // Trúng / trượt do spawner báo cho ComboSystem -> enemy không báo nữa (tránh cộng 2 lần)
        rhythm.ReportToCombo = false;

        activeEnemies.Add(new ActiveEnemy
        {
            rhythm = rhythm,
            movement = movement,
            hitTime = hitTime
        });

        if (showDebug)
        {
            Debug.Log(
                $"Spawn {(isLeft ? "LEFT" : "RIGHT")} | " +
                $"HitTime: {hitTime:F3} | SongTime: {SongTime:F3}"
            );
        }

        return true;
    }

    // =========================================
    // MISS
    // =========================================

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
                PlayHitPointAnimation(e.rhythm.IsLeft, false);

                if (comboSystem != null)
                    comboSystem.MissNote();

                // Cho cơ chế màn (ví dụ quái phát nổ ở Màn 4) biết vị trí bị bỏ lỡ
                RhythmEvents.RaiseNoteMissed(e.rhythm.transform.position, e.rhythm.IsLeft);

                activeEnemies.RemoveAt(i);

                Destroy(e.rhythm.gameObject);
            }
        }
    }

    // =========================================
    // SONG END
    // =========================================

    private void CheckSongEnd()
    {
        if (songEnded ||
            musicSource == null ||
            musicSource.clip == null)
            return;

        bool finished =
            nextEventIndex >= spawnEvents.Count &&
            spawnEventCoroutine == null &&
            SongTime >= musicSource.clip.length &&
            activeEnemies.Count == 0;

        if (!finished)
            return;

        songEnded = true;
        isPlaying = false;

        SongEnded?.Invoke();
    }

    // =========================================
    // RANDOM ENEMY
    // =========================================

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

        int pick = valid[
            UnityEngine.Random.Range(0, valid.Count)
        ];

        if (avoidRepeat && valid.Count > 1)
        {
            int guard = 0;

            while (pick == lastPrefabIndex && guard++ < 10)
            {
                pick = valid[
                    UnityEngine.Random.Range(0, valid.Count)
                ];
            }
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

        // Đang pause / Game Over / chưa chạy bài: không cho đánh (nốt đứng yên ngay vạch)
        if (!isPlaying || clock.IsPaused || Time.timeScale == 0f)
            return false;

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

        PlayHitPointAnimation(target.rhythm.IsLeft, true);
        PlayCelticHit();

        if (target.movement != null)
            target.movement.MarkHit();

        activeEnemies.Remove(target);

        boss.TakeHit();

        // Spawner là nơi DUY NHẤT báo trúng cho ComboSystem (EnemyRhythm không báo nữa)
        if (comboSystem != null)
            comboSystem.HitNoteTimed(bestError);

        return true;
    }

    // =========================================
    // STOP / RESET
    // =========================================

    public void StopSpawner()
    {
        isPlaying = false;

        if (spawnEventCoroutine != null)
        {
            StopCoroutine(spawnEventCoroutine);
            spawnEventCoroutine = null;
        }

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

    // =========================================
    // COUNT HP
    // =========================================

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

    // =========================================
    // HIT POINT ANIMATION
    // =========================================

    private void PlayHitPointAnimation(bool isLeft, bool isHit)
    {
        Animator animator = isLeft
            ? hitPointLeftAnimator
            : hitPointRightAnimator;

        if (animator == null)
            return;

        animator.ResetTrigger(hitTrigger);
        animator.ResetTrigger(missTrigger);

        animator.SetTrigger(isHit ? hitTrigger : missTrigger);
    }

    // =========================================
    // CELTIC ANIMATION
    // =========================================

    private void PlayCelticHit()
    {
        if (celticAnimators == null || celticAnimators.Count == 0)
            return;

        foreach (Animator animator in celticAnimators)
        {
            if (animator == null)
                continue;

            animator.ResetTrigger(celticHitTrigger);
            animator.SetTrigger(celticHitTrigger);
        }
    }
}
