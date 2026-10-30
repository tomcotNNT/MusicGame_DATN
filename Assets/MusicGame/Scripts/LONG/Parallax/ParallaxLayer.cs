using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Tiles")]
    [SerializeField] private Transform tileA;
    [SerializeField] private Transform tileB;

    [Header("Parallax Settings")]
    [Tooltip("Tốc độ trôi của tầng (Layer càng xa speed càng nhỏ)")]
    [SerializeField] private float speed = 1f;

    private float tileWidth;
    private Camera mainCamera;
    private SpriteRenderer srA;
    private SpriteRenderer srB;

    private void Start()
    {
        mainCamera = Camera.main;

        if (tileA != null)
        {
            srA = tileA.GetComponent<SpriteRenderer>();
            if (srA != null)
            {
                // Lấy kích thước bề rộng thực tế của Sprite theo trục X trong World Space
                tileWidth = srA.bounds.size.x;
            }
        }

        if (tileB != null)
        {
            srB = tileB.GetComponent<SpriteRenderer>();
        }

        // Tự động căn chỉnh Tile B nối tiếp ngay sau Tile A lúc bắt đầu
        if (tileA != null && tileB != null && tileWidth > 0f)
        {
            tileB.position = new Vector3(tileA.position.x + tileWidth, tileB.position.y, tileB.position.z);
        }
    }

    private void Update()
    {
        if (mainCamera == null || tileA == null || tileB == null || tileWidth <= 0f)
            return;

        // 1. DI CHUYỂN NỀN VỀ BÊN TRÁI (Chuẩn theo hướng nhân vật chạy sang phải)
        float movement = speed * Time.deltaTime;
        tileA.position += Vector3.left * movement;
        tileB.position += Vector3.left * movement;

        // 2. TÍNH MÉP TRÁI VIEWPORT CAMERA TRONG WORLD SPACE
        float cameraLeft = mainCamera.transform.position.x - (mainCamera.orthographicSize * mainCamera.aspect);

        // 3. TỰ ĐỘNG BÚNG TILE NỐI TIẾP KHI RA KHỎI MÀN HÌNH
        // Sử dụng bounds.max.x để đảm bảo toàn bộ mép phải đã ra khỏi camera trước khi búng
        float rightEdgeA = (srA != null) ? srA.bounds.max.x : (tileA.position.x + tileWidth);
        if (rightEdgeA <= cameraLeft)
        {
            // Đặt Tile A ra ngay sau mép phải của Tile B
            tileA.position = new Vector3(tileB.position.x + tileWidth, tileA.position.y, tileA.position.z);
        }

        float rightEdgeB = (srB != null) ? srB.bounds.max.x : (tileB.position.x + tileWidth);
        if (rightEdgeB <= cameraLeft)
        {
            // Đặt Tile B ra ngay sau mép phải của Tile A
            tileB.position = new Vector3(tileA.position.x + tileWidth, tileB.position.y, tileB.position.z);
        }
    }
}