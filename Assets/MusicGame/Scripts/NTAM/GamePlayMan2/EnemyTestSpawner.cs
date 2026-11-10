using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTestSpawner : MonoBehaviour
{
    [Header("Danh sách 4 loại Enemy")]
    public GameObject batPrefab;    // Quái bay (Làn trên)
    public GameObject slimePrefab;  // Quái bộ 1 (Làn dưới)
    public GameObject ratPrefab;    // Quái bộ 2 (Làn dưới)
    public GameObject mimicPrefab;  // Quái bộ 3 (Làn dưới)

    [Header("4 Cổng Spawn (Ngoài màn hình)")]
    public Transform spawnLeftHigh;
    public Transform spawnLeftLow;
    public Transform spawnRightHigh;
    public Transform spawnRightLow;

    [Header("Cài đặt Nhịp & Tiến trình")]
    public float totalDuration = 30f;       // Tổng thời gian test: 30 giây
    public float climaxStartTime = 15f;     // Mốc 15s bắt đầu xuất hiện hàng trên
    public float spawnInterval = 1.0f;      // Mỗi 1.0 giây xuất hiện 1 con (khớp nhịp)

    [Header("Trạng thái hiển thị (Runtime)")]
    public float currentTimer = 0f;
    public bool isClimaxPhase = false;
    public bool isTesting = false;

    private Coroutine gameRoutine;

    void Start()
    {
        Start30sTest();
    }

    void Update()
    {
        // Phím R: Reset chạy lại bài test 30s bất cứ lúc nào
        if (Input.GetKeyDown(KeyCode.R))
        {
            Start30sTest();
        }
    }

    public void Start30sTest()
    {
        if (gameRoutine != null) StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(TimelineRoutine());
    }

    IEnumerator TimelineRoutine()
    {
        isTesting = true;
        currentTimer = 0f;
        Debug.Log("<color=cyan>=== BẮT ĐẦU TEST TIẾN TRÌNH 30S (15S ĐẦU: CHỈ 1 HÀNG DƯỚI) ===</color>");

        while (currentTimer < totalDuration)
        {
            yield return new WaitForSeconds(spawnInterval);
            currentTimer += spawnInterval;

            // Kiểm tra mốc 15 giây
            isClimaxPhase = (currentTimer >= climaxStartTime);

            if (!isClimaxPhase)
            {
                // [0s - 15s]: Chỉ xuất hiện hàng dưới, ngẫu nhiên Slime / Rat / Mimic
                SpawnGroundRandom();
            }
            else
            {
                // [15s - 30s]: Xuất hiện cả hàng trên (Bat) lẫn hàng dưới
                // Tỉ lệ 50% ra hàng trên, 50% ra hàng dưới
                bool spawnHigh = Random.value > 0.5f;

                if (spawnHigh)
                {
                    SpawnAirEnemy();
                }
                else
                {
                    SpawnGroundRandom();
                }
            }
        }

        isTesting = false;
        Debug.Log("<color=green>=== HOÀN TẤT BÀI TEST 30 GIÂY THÀNH CÔNG! ===</color>");
    }

    // Sinh quái bộ làn dưới (Trái hoặc Phải)
    void SpawnGroundRandom()
    {
        bool isLeft = Random.value > 0.5f;
        Transform spawnPt = isLeft ? spawnLeftLow : spawnRightLow;

        // Chọn ngẫu nhiên giữa Slime, Rat và Mimic
        GameObject selectedPrefab = slimePrefab;
        int rand = Random.Range(0, 3);
        if (rand == 1 && ratPrefab != null) selectedPrefab = ratPrefab;
        else if (rand == 2 && mimicPrefab != null) selectedPrefab = mimicPrefab;

        Spawn(spawnPt, selectedPrefab, isLeft, isHigh: false);
    }

    // Sinh quái bay làn trên (Trái hoặc Phải)
    void SpawnAirEnemy()
    {
        bool isLeft = Random.value > 0.5f;
        Transform spawnPt = isLeft ? spawnLeftHigh : spawnRightHigh;
        Spawn(spawnPt, batPrefab, isLeft, isHigh: true);
    }

    void Spawn(Transform spawnPoint, GameObject prefab, bool isLeft, bool isHigh)
    {
        if (prefab == null || spawnPoint == null) return;

        GameObject obj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);
        OFDP_Enemy enemy = obj.GetComponent<OFDP_Enemy>();
        if (enemy != null)
        {
            enemy.Init(isLeft, isHigh, 6f);
        }
    }
}