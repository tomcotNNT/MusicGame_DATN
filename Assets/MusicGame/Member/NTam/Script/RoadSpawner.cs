using UnityEngine;

public class RoadSpawner : MonoBehaviour
{
    // Cấu trúc chứa thông tin của từng loại đất
    [System.Serializable]
    public class GroundType
    {
        public string name;          // Đặt tên gợi nhớ (vd: Ngắn 10, Trung bình 20, Dài 30)
        public GameObject prefab;    // Prefab tương ứng
        public float width;          // 10, 20 hoặc 30
    }

    [Header("Ground Types Configuration")]
    [SerializeField] private GroundType[] groundTypes; // Mảng 3 loại đất

    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnAheadDistance = 30f; // Tầm nhìn trước để sinh đất
    [SerializeField] private float groundYPosition = -2f;     // Độ cao mặt đất (Y)

    [Header("Gap Settings")]
    [SerializeField] private float minGap = 1.8f;              // Khoảng hở nhỏ nhất
    [SerializeField] private float maxGap = 3.5f;              // Khoảng hở lớn nhất (tùy theo lực nhảy)

    private float nextSpawnX;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        nextSpawnX = player.position.x - 3f;

        // Sinh trước 3 đoạn đất ban đầu không có rãnh để Player chạy xuất phát
        for (int i = 0; i < 3; i++)
        {
            SpawnRandomGround(false);
        }
    }

    void Update()
    {
        if (player == null) return;

        // Cứ khi nào mép đất tiếp theo nằm trong tầm nhìn trước của Player thì sinh tiếp
        while (nextSpawnX < player.position.x + spawnAheadDistance)
        {
            SpawnRandomGround(true);
        }
    }

    private void SpawnRandomGround(bool createGap)
    {
        if (groundTypes == null || groundTypes.Length == 0) return;

        // 1. Chọn ngẫu nhiên 1 trong các loại đất trong mảng
        int randomIndex = Random.Range(0, groundTypes.Length);
        GroundType chosenGround = groundTypes[randomIndex];

        // 2. Nếu cho phép rãnh, cộng thêm khoảng cách ngẫu nhiên trước khi đặt đất
        if (createGap)
        {
            float randomGap = Random.Range(minGap, maxGap);
            nextSpawnX += randomGap;
        }

        // 3. Tính tâm toạ độ X = Điểm bắt đầu + (Độ dài đất / 2)
        float spawnCenterX = nextSpawnX + (chosenGround.width / 2f);
        Vector2 spawnPosition = new Vector2(spawnCenterX, groundYPosition);

        // 4. Sinh Prefab
        Instantiate(chosenGround.prefab, spawnPosition, Quaternion.identity);

        // 5. Đẩy điểm nối tiếp theo tới mép cuối của thanh đất vừa sinh
        nextSpawnX += chosenGround.width;
    }
}