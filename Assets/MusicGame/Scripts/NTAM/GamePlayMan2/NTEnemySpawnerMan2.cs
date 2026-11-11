using System.Collections.Generic;
using UnityEngine;

public class NTEnemySpawnerMan2 : MonoBehaviour
{
    [Header("Core Rhythm References (Giữ từ Script Cũ)")]
    [SerializeField] private MidiReader midiReader;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform player;

    [Header("Hit Checkpoints (Căn chuẩn tâm đánh 2 bên)")]
    [Tooltip("Kéo HitPointL của Player vào đây")]
    [SerializeField] private Transform hitPointL;
    [Tooltip("Kéo HitPointR của Player vào đây")]
    [SerializeField] private Transform hitPointR;

    [Header("Danh Sách 4 Loại Enemy (Từ Inspector NT)")]
    [SerializeField] private GameObject batPrefab;       // Quái bay (Lane Cao)
    [SerializeField] private GameObject slimePrefab;     // Quái đất (Lane Thấp)
    [SerializeField] private GameObject ratPrefab;       // Quái đất (Lane Thấp)
    [SerializeField] private GameObject mimicPrefab;     // Quái đặc biệt

    [Header("4 Cổng Spawn Ngoài Màn Hình (Từ Inspector NT)")]
    [SerializeField] private Transform spawnLeftHigh;
    [SerializeField] private Transform spawnLeftLow;
    [SerializeField] private Transform spawnRightHigh;
    [SerializeField] private Transform spawnRightLow;

    [Header("Spawn Settings")]
    [Tooltip("Thời gian quái bay từ cổng tới tâm Checkpoint")]
    [SerializeField] private float spawnAheadTime = 1.5f;
    [SerializeField] private float minInterval = 0.12f;
    [SerializeField] private float speedMultiplier = 1f;

    [Header("Options")]
    [SerializeField] private bool randomLane = false;
    [SerializeField] private bool spawnOnStart = false;

    private List<MidiReader.RhythmNote> notes = new List<MidiReader.RhythmNote>();
    private readonly float[] lastSpawnTime = { -999f, -999f };

    private int nextNoteIndex;
    private double songStartDsp;
    private bool isSpawning;

    public float SongTime => (float)(AudioSettings.dspTime - songStartDsp);

    private void Start()
    {
        if (spawnOnStart)
        {
            StartSpawner(3f);
        }
    }

    public void StartSpawner(float leadIn = 3f)
    {
        if (midiReader == null || audioSource == null || player == null ||
            hitPointL == null || hitPointR == null)
        {
            Debug.LogError("<color=red>[NT Spawner]</color> Thiếu Reference quan trọng (Midi/Audio/Player/HitPoints)!");
            return;
        }

        if (midiReader.Notes == null || midiReader.Notes.Count == 0)
        {
            Debug.LogError("<color=red>[NT Spawner]</color> MIDI chưa có notes!");
            return;
        }

        speedMultiplier = Mathf.Max(0.1f, speedMultiplier);
        notes = midiReader.GetNotesWithSpeed(speedMultiplier);

        nextNoteIndex = 0;
        lastSpawnTime[0] = -999f;
        lastSpawnTime[1] = -999f;

        // Đồng bộ thời gian DSP âm thanh chuẩn xác
        songStartDsp = AudioSettings.dspTime + leadIn;

        audioSource.Stop();
        audioSource.pitch = 1f;
        audioSource.PlayScheduled(songStartDsp);

        isSpawning = true;
        Debug.Log($"<color=green>[NT Spawner Started]</color> Đã nạp {notes.Count} nốt gameplay!");
    }

    private void Update()
    {
        // Giữ nguyên công cụ bấm Space đo Auto-Offset từ script cũ
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (audioSource != null && audioSource.isPlaying && notes != null && notes.Count > 0)
            {
                float autoOffset = audioSource.time - notes[0].time;
                Debug.Log($"<color=yellow>[AUTO-OFFSET]</color> Offset chuẩn xác: <b>{autoOffset:F2}</b>");
            }
        }

        if (!isSpawning) return;

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

            bool isLeft = randomLane ? (Random.value < 0.5f) : (note.lane == 0);
            SpawnEnemyFromNote(isLeft, note.time, now);
        }

        if (nextNoteIndex >= notes.Count)
        {
            isSpawning = false;
        }
    }

    private void SpawnEnemyFromNote(bool isLeft, float hitTime, float now)
    {
        int side = isLeft ? 0 : 1;

        if (hitTime - lastSpawnTime[side] < minInterval)
            return;

        lastSpawnTime[side] = hitTime;

        // Chọn ngẫu nhiên loại quái: 40% Dơi (Bay), 30% Slime (Đất), 30% Chuột (Đất)
        GameObject selectedPrefab;
        bool isFlyingEnemy = false;

        float roll = Random.value;
        if (roll < 0.4f && batPrefab != null)
        {
            selectedPrefab = batPrefab;
            isFlyingEnemy = true;
        }
        else if (roll < 0.7f && slimePrefab != null)
        {
            selectedPrefab = slimePrefab;
        }
        else if (ratPrefab != null)
        {
            selectedPrefab = ratPrefab;
        }
        else
        {
            selectedPrefab = batPrefab; // Fallback
            isFlyingEnemy = true;
        }

        if (selectedPrefab == null) return;

        // Chọn cổng spawn tương ứng theo độ cao của quái
        Transform spawnPoint;
        if (isFlyingEnemy)
        {
            spawnPoint = isLeft ? (spawnLeftHigh ?? spawnLeftLow) : (spawnRightHigh ?? spawnRightLow);
        }
        else
        {
            spawnPoint = isLeft ? (spawnLeftLow ?? spawnLeftHigh) : (spawnRightLow ?? spawnRightHigh);
        }

        Transform targetCheckpoint = isLeft ? hitPointL : hitPointR;

        // Khởi tạo quái tại cổng spawn
        GameObject enemyObj = Instantiate(selectedPrefab, spawnPoint.position, Quaternion.identity);

        // Khởi tạo thông số cho NTEnemyMan2
        NTEnemyMan2 enemy = enemyObj.GetComponent<NTEnemyMan2>();
        if (enemy != null)
        {
            enemy.Init(
                playerTransform: player,
                targetCheckpoint: targetCheckpoint,
                spawnTime: now,
                hitTime: hitTime,
                songTimeProvider: () => SongTime
            );
        }
        else
        {
            Debug.LogError($"<color=red>[Lỗi Prefab]</color> {selectedPrefab.name} chưa gắn script NTEnemyMan2!");
        }
    }

    public void StopSpawner() => isSpawning = false;

    public void ResetSpawner()
    {
        isSpawning = false;
        nextNoteIndex = 0;
        notes.Clear();
        lastSpawnTime[0] = -999f;
        lastSpawnTime[1] = -999f;
    }
}