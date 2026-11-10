
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawnerMan2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MidiReader midiReader;
[Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefabs = new List<GameObject>();
    [SerializeField] private Transform player;

    [Header("Spawn Points")]
    [SerializeField] private Transform spawnLeft;
    [SerializeField] private Transform spawnRight;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnAheadTime = 1.5f;
    [SerializeField] private float minInterval = 0.15f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Options")]
    [SerializeField] private bool randomLane = false;
    [SerializeField] private bool spawnOnStart = false;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private List<MidiReader.RhythmNote> notes =
        new List<MidiReader.RhythmNote>();

    private readonly float[] lastSpawnTime = { -999f, -999f };

    private int nextNoteIndex;
    private double songStartDsp;
    private bool isSpawning;

    private float SongTime =>
        (float)(AudioSettings.dspTime - songStartDsp);

    private void Start()
    {
        if (spawnOnStart)
            StartSpawner();
    }

    private GameObject GetRandomEnemy()
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0)
        {
            Debug.LogError("EnemySpawnerMan2: Enemy Prefabs List đang trống!");
            return null;
        }

        // Lọc những prefab hợp lệ
        List<GameObject> validEnemies = enemyPrefabs.FindAll(
            enemy => enemy != null
        );

        if (validEnemies.Count == 0)
            return null;

        int randomIndex = Random.Range(0, validEnemies.Count);

        return validEnemies[randomIndex];
    }

    public void StartSpawner(float leadIn = 3f)
    {
        if (midiReader == null || enemyPrefabs == null ||
            enemyPrefabs.Count == 0 ||
            player == null || spawnLeft == null || spawnRight == null)
        {
            Debug.LogError("EnemySpawnerMan2: Thiếu reference!");
            return;
        }

        if (midiReader.Notes == null || midiReader.Notes.Count == 0)
        {
            Debug.LogError("EnemySpawnerMan2: MIDI chưa có notes!");
            return;
        }

        if (audioSource == null || audioSource.clip == null)
        {
            Debug.LogError("EnemySpawnerMan2: Chưa gán AudioSource hoặc AudioClip!");
            return;
        }

        speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        notes = midiReader.GetNotesWithSpeed(speedMultiplier);

        nextNoteIndex = 0;
        lastSpawnTime[0] = -999f;
        lastSpawnTime[1] = -999f;

        songStartDsp = AudioSettings.dspTime + leadIn;

        audioSource.Stop();
        audioSource.pitch = 1f;
        audioSource.PlayScheduled(songStartDsp);

        isSpawning = true;

        Debug.Log($"EnemySpawnerMan2: Started - {notes.Count} notes");
    }

    private void Update()
    {
        if (!isSpawning)
            return;

        float now = SongTime;

        while (nextNoteIndex < notes.Count)
        {
            MidiReader.RhythmNote note = notes[nextNoteIndex];

            float travelTime = note.time - now;

            if (travelTime > spawnAheadTime)
                break;

            nextNoteIndex++;

            if (travelTime <= 0f)
                continue;

            bool left = randomLane
                ? Random.value < 0.5f
                : note.lane == 0;

            SpawnEnemy(left, note.time, now);
        }

        if (nextNoteIndex >= notes.Count)
            isSpawning = false;
    }

    private void SpawnEnemy(bool left, float hitTime, float now)
{
    int side = left ? 0 : 1;

    if (hitTime - lastSpawnTime[side] < minInterval)
        return;

    lastSpawnTime[side] = hitTime;

    Transform spawnPoint = left ? spawnLeft : spawnRight;

    // Random enemy
    GameObject selectedEnemy = GetRandomEnemy();

    if (selectedEnemy == null)
        return;

    GameObject enemy = Instantiate(
        selectedEnemy,
        spawnPoint.position,
        Quaternion.identity
    );

    EnemyMan2 movement = enemy.GetComponent<EnemyMan2>();

    if (movement != null)
    {
        movement.Init(
            player,
            now,
            hitTime,
            () => SongTime
        );
    }
    else
    {
        Debug.LogWarning(
            $"{selectedEnemy.name} chưa có EnemyMan2 component!"
        );
    }

    Debug.Log(
        $"Spawn {selectedEnemy.name} | " +
        $"{(left ? "LEFT" : "RIGHT")} | Hit: {hitTime:F2}"
    );
}

    public void StopSpawner()
    {
        isSpawning = false;
    }

    public void ResetSpawner()
    {
        isSpawning = false;
        nextNoteIndex = 0;
        notes.Clear();

        lastSpawnTime[0] = -999f;
        lastSpawnTime[1] = -999f;
    }
}
