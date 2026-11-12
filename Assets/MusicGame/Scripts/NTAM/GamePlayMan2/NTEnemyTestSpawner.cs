using System.Collections;
using UnityEngine;

public class NTEnemyTestSpawner : MonoBehaviour
{
    [Header("Audio (Tùy chọn phát bài hát theo beat)")]
    [SerializeField] private AudioSource audioSource;

    [Header("Player & Checkpoints")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform hitPointL;
    [SerializeField] private Transform hitPointR;

    [Header("4 Cổng Spawn Ngoài Màn Hình")]
    [SerializeField] private Transform spawnLeftLow;   // Đất bên trái
    [SerializeField] private Transform spawnRightLow;  // Đất bên phải
    [SerializeField] private Transform spawnLeftHigh;  // Trên không bên trái
    [SerializeField] private Transform spawnRightHigh; // Trên không bên phải

    [Header("Prefabs Quái")]
    [SerializeField] private GameObject batPrefab;     // Quái bay
    [SerializeField] private GameObject slimePrefab;   // Quái đất
    [SerializeField] private GameObject ratPrefab;     // Quái đất

    [Header("Tốc Độ Di Chuyển Của Quái")]
    [Tooltip("Thời gian quái bay từ cổng tới Checkpoint (giây). Càng lớn quái chạy càng chậm, 2.0s - 2.5s là vừa mắt")]
    [SerializeField] private float travelDuration = 2.2f;

    [Header("Cài Đặt Nhịp (BPM)")]
    [SerializeField] private float bpm = 135f;

    private float timer = 0f;
    private bool isRunning = false;
    private Coroutine spawnCoroutine;

    // Bộ đếm thời gian giả lập SongTime cho NTEnemyMan2
    public float CurrentTestTime => timer;

    private void Start()
    {
        StartTest();
    }

    public void StartTest()
    {
        timer = 0f;
        isRunning = true;

        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.time = 0f;
            audioSource.Play();
        }

        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(TestSpawnRoutine());
    }

    private void Update()
    {
        if (isRunning)
        {
            timer += Time.deltaTime;
        }
    }

    private IEnumerator TestSpawnRoutine()
    {
        // 1 phách (beat) ở 135 BPM tương đương ~ 0.44s
        float beatInterval = 60f / bpm; 

        while (timer < 60f)
        {
            float waitTime = beatInterval * 3f; // Mặc định giãn cách

            // ==========================================
            // GIAI ĐOẠN 1: 0s - 20s (Chỉ 2 Lane Dưới - Quái Đất)
            // ==========================================
            if (timer < 20f)
            {
                bool isLeft = Random.value < 0.5f;
                SpawnGroundEnemy(isLeft);
                waitTime = beatInterval * 3.5f; // Khoảng 1.5s sinh 1 con
            }
            // ==========================================
            // GIAI ĐOẠN 2: 20s - 40s (Kích Hoạt Thêm 2 Lane Trên)
            // ==========================================
            else if (timer >= 20f && timer < 40f)
            {
                bool isLeft = Random.value < 0.5f;
                bool isHighLane = Random.value < 0.45f; // 45% xuất hiện quái bay

                if (isHighLane)
                    SpawnAirEnemy(isLeft);
                else
                    SpawnGroundEnemy(isLeft);

                waitTime = beatInterval * 2f; // Khoảng 0.88s sinh 1 con
            }
            // ==========================================
            // GIAI ĐOẠN 3: 40s - 60s (Dồn Dập Cả 4 Lane)
            // ==========================================
            else if (timer >= 40f && timer < 60f)
            {
                bool isLeft = Random.value < 0.5f;
                bool isHighLane = Random.value < 0.5f;

                if (isHighLane)
                    SpawnAirEnemy(isLeft);
                else
                    SpawnGroundEnemy(isLeft);

                waitTime = beatInterval; // Cực nhanh: 0.44s sinh 1 con (đúng từng phách nhạc)
            }

            yield return new WaitForSeconds(waitTime);
        }

        Debug.Log("<color=cyan>[Test Spawner]</color> Hoàn tất chu kỳ test 60 giây!");
        isRunning = false;
    }

    // Sinh quái đất (Khóa cao độ sát sàn)
    private void SpawnGroundEnemy(bool isLeft)
    {
        Transform spawnPoint = isLeft ? spawnLeftLow : spawnRightLow;
        Transform checkpoint = isLeft ? hitPointL : hitPointR;
        GameObject prefab = (Random.value < 0.5f && ratPrefab != null) ? ratPrefab : slimePrefab;

        if (prefab == null || spawnPoint == null) return;

        GameObject enemyObj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        NTEnemyMan2 enemy = enemyObj.GetComponent<NTEnemyMan2>();

        if (enemy != null)
        {
            enemy.isHighLane = false; // Quái đất
            enemy.Init(
                playerTransform: player,
                targetCheckpoint: checkpoint,
                spawnTime: timer,
                hitTime: timer + travelDuration,
                songTimeProvider: () => CurrentTestTime
            );
        }
    }

    // Sinh quái bay (Giữ cao độ trên không)
    private void SpawnAirEnemy(bool isLeft)
    {
        Transform spawnPoint = isLeft ? spawnLeftHigh : spawnRightHigh;
        Transform checkpoint = isLeft ? hitPointL : hitPointR;

        if (batPrefab == null || spawnPoint == null) return;

        GameObject enemyObj = Instantiate(batPrefab, spawnPoint.position, Quaternion.identity);
        NTEnemyMan2 enemy = enemyObj.GetComponent<NTEnemyMan2>();

        if (enemy != null)
        {
            enemy.isHighLane = true; // Quái bay
            enemy.Init(
                playerTransform: player,
                targetCheckpoint: checkpoint,
                spawnTime: timer,
                hitTime: timer + travelDuration,
                songTimeProvider: () => CurrentTestTime
            );
        }
    }
}