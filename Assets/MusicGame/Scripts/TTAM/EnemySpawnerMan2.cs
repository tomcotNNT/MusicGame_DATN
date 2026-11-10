
using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerMan2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private List<GameObject> enemyPrefabs;
    [SerializeField] private Transform player;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnLeft;
    [SerializeField] private Transform spawnRight;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float leadIn = 3f;

    [Header("Sequence Grouping")]
    [Tooltip("Khoảng cách tối đa giữa hai nốt trong cùng một Enemy")]
    [SerializeField] private float phraseGapThreshold = 0.8f;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnAheadTime = 1.5f;
    [SerializeField] private bool randomLane = false;
    [SerializeField] private bool spawnOnStart = false;

    private class MidiEventData
    {
        public float time;
        public int lane;
    }

    private class EnemyGroup
    {
        public List<float> noteTimes = new List<float>();
        public int lane;

        public float StartTime => noteTimes[0];
    }

    private readonly List<EnemyGroup> groups = new();
    private readonly List<EnemyRhythmController> activeEnemies = new();

    private int nextGroupIndex;
    private double songStartDsp;
    private bool isPlaying;

    public float SongTime
    {
        get
        {
            if (!isPlaying)
                return 0f;

            return (float)(AudioSettings.dspTime - songStartDsp);
        }
    }

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
            spawnRight == null)
        {
            Debug.LogError("EnemySpawnerMan2: Missing references!");
            return;
        }

        BuildEnemyGroups();

        if (groups.Count == 0)
        {
            Debug.LogError("No MIDI enemy groups found!");
            return;
        }

        nextGroupIndex = 0;

        songStartDsp = AudioSettings.dspTime + countdown;

        musicSource.PlayScheduled(songStartDsp);

        isPlaying = true;
    }

    private void BuildEnemyGroups()
    {
        groups.Clear();

        // Tương thích API MidiReader hiện tại.
        var midiNotes = midiReader.GetNotesWithSpeed(1);

        List<MidiEventData> notes = new List<MidiEventData>();

        foreach (var note in midiNotes)
        {
            notes.Add(new MidiEventData
            {
                time = (float)note.time,
                lane = note.lane
            });
        }

        notes.Sort((a, b) => a.time.CompareTo(b.time));

        if (notes.Count == 0)
            return;

        EnemyGroup currentGroup = new EnemyGroup();

        currentGroup.lane = notes[0].lane;
        currentGroup.noteTimes.Add(notes[0].time);

        for (int i = 1; i < notes.Count; i++)
        {
            MidiEventData previous = notes[i - 1];
            MidiEventData current = notes[i];

            float gap = current.time - previous.time;

            // Tách nhóm nếu khoảng cách quá lớn
            // hoặc lane thay đổi.
            bool newGroup =
                gap > phraseGapThreshold ||
                current.lane != currentGroup.lane;

            if (newGroup)
            {
                groups.Add(currentGroup);

                currentGroup = new EnemyGroup();
                currentGroup.lane = current.lane;
            }

            currentGroup.noteTimes.Add(current.time);
        }

        if (currentGroup.noteTimes.Count > 0)
            groups.Add(currentGroup);

        Debug.Log("MIDI Notes: " + notes.Count);
        Debug.Log("Enemy Groups: " + groups.Count);

        foreach (EnemyGroup group in groups)
        {
            Debug.Log(
                "Enemy Group | Lane: " + group.lane +
                " | Notes: " + group.noteTimes.Count +
                " | Start: " + group.StartTime
            );
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        SpawnUpcomingEnemies();

        activeEnemies.RemoveAll(
            enemy => enemy == null
        );
    }

    private void SpawnUpcomingEnemies()
    {
        float now = SongTime;

        while (nextGroupIndex < groups.Count)
        {
            EnemyGroup group = groups[nextGroupIndex];

            float timeUntilHit = group.StartTime - now;

            if (timeUntilHit > spawnAheadTime)
                break;

            SpawnEnemy(group, now);

            nextGroupIndex++;
        }
    }

    private GameObject GetRandomPrefab()
    {
        if (enemyPrefabs != null && enemyPrefabs.Count > 0)
        {
            List<GameObject> validPrefabs =
                enemyPrefabs.FindAll(p => p != null);

            if (validPrefabs.Count > 0)
            {
                return validPrefabs[
                    UnityEngine.Random.Range(0, validPrefabs.Count)
                ];
            }
        }

        return enemyPrefab;
    }

    private void SpawnEnemy(EnemyGroup group, float now)
    {
        bool isLeft = randomLane
            ? UnityEngine.Random.value < 0.5f
            : group.lane == 0;

        Transform spawnPoint = isLeft
            ? spawnLeft
            : spawnRight;

        GameObject prefab = GetRandomPrefab();

        if (prefab == null)
        {
            Debug.LogError("Enemy prefab is missing!");
            return;
        }

        GameObject obj = Instantiate(
            prefab,
            spawnPoint.position,
            Quaternion.identity
        );

        // Enemy movement hiện tại của project.
        EnemyMan2 movement = obj.GetComponent<EnemyMan2>();

        if (movement != null)
        {
            movement.Init(
                player,
                now,
                group.StartTime,
                () => SongTime
            );
        }
        else
        {
            Debug.LogError(
                "Enemy prefab needs EnemyMan2 component!"
            );
        }

        // Khởi tạo rhythm sequence.
        EnemyRhythmController rhythm =
            obj.GetComponent<EnemyRhythmController>();

        if (rhythm == null)
        {
            Debug.LogError(
                "Enemy prefab needs EnemyRhythmController!"
            );

            return;
        }

        rhythm.Initialize(
            group.noteTimes.ToArray(),
            isLeft,
            () => SongTime
        );

        activeEnemies.Add(rhythm);
    }

    public bool TryHit(bool isLeft)
    {
        EnemyRhythmController target = null;
        float bestError = float.MaxValue;

        // Chọn Enemy cùng hướng có timing gần nhất.
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

        return target.TryHit(out grade);
    }
}
