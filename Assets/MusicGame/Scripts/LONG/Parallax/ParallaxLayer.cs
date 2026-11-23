
using System.Collections.Generic;
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Tile Templates")]
    [SerializeField] private Transform tileA;
    [SerializeField] private Transform tileB;

    [Header("Parallax Settings")]
    [SerializeField] private float speed = 1.5f;

    private Camera mainCamera;

    private class TileData
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public bool isA;
        public bool hasSpawned;
    }

    private List<TileData> allTiles = new List<TileData>();

    private Transform latestA;
    private Transform latestB;

    private void Start()
    {
        mainCamera = Camera.main;

        if (tileA == null || tileB == null)
            return;

        // Đăng ký hai Tile ban đầu
        AddTile(tileA, true);
        AddTile(tileB, false);

        latestA = tileA;
        latestB = tileB;

        // Đặt B ngay bên phải A
        SpriteRenderer srA = tileA.GetComponent<SpriteRenderer>();
        SpriteRenderer srB = tileB.GetComponent<SpriteRenderer>();

        if (srA != null && srB != null)
        {
            float offset = srA.bounds.max.x - srB.bounds.min.x;

            tileB.position += Vector3.right * offset;
        }
    }

    private void Update()
    {
        if (mainCamera == null || allTiles.Count == 0)
            return;

        // 1. Di chuyển toàn bộ Tile sang trái
        float movement = speed * Time.deltaTime;

        foreach (TileData tile in allTiles)
        {
            tile.transform.position += Vector3.left * movement;
        }

        // 2. Tính mép trái Camera
        float cameraLeft = mainCamera.transform.position.x
            - mainCamera.orthographicSize * mainCamera.aspect;

        // 3. Kiểm tra từng Tile để tạo bản sao
        int currentCount = allTiles.Count;

        for (int i = 0; i < currentCount; i++)
        {
            TileData tile = allTiles[i];

            if (tile.hasSpawned)
                continue;

            if (tile.renderer.bounds.center.x <= cameraLeft)
            {
                tile.hasSpawned = true;

                if (tile.isA)
                {
                    SpawnClone(true, latestB);
                }
                else
                {
                    SpawnClone(false, latestA);
                }
            }
        }
        // 4. Dọn các Clone đã ra khỏi Camera
        CleanupTiles(cameraLeft);
    }

    // Đăng ký Tile vào danh sách quản lý
    private void AddTile(Transform tile, bool isA)
    {
        SpriteRenderer sr = tile.GetComponent<SpriteRenderer>();

        if (sr == null)
            return;

        allTiles.Add(new TileData
        {
            transform = tile,
            renderer = sr,
            isA = isA,
            hasSpawned = false
        });
    }

    // Tạo bản sao mới và đặt bên phải Tile đối diện
    private void SpawnClone(bool isA, Transform targetTile)
    {
        if (targetTile == null)
            return;

        Transform template = isA ? tileA : tileB;

        // Instantiate bản sao mới
        GameObject clone = Instantiate(
            template.gameObject,
            template.parent
        );

        Transform cloneTransform = clone.transform;

        SpriteRenderer cloneRenderer =
            clone.GetComponent<SpriteRenderer>();

        SpriteRenderer targetRenderer =
            targetTile.GetComponent<SpriteRenderer>();

        if (cloneRenderer == null || targetRenderer == null)
        {
            Destroy(clone);
            return;
        }

        // Đặt bản sao ngay bên phải Tile đối diện
        float offset = targetRenderer.bounds.max.x
            - cloneRenderer.bounds.min.x;

        cloneTransform.position += Vector3.right * offset;

        // Đăng ký bản sao để tiếp tục di chuyển và spawn
        AddTile(cloneTransform, isA);

        // Cập nhật Tile mới nhất của từng loại
        if (isA)
            latestA = cloneTransform;
        else
            latestB = cloneTransform;
    }

    private void CleanupTiles(float cameraLeft)
    {
        for (int i = allTiles.Count - 1; i >= 0; i--)
        {
            TileData tile = allTiles[i];

            // Giữ lại hai Tile ban đầu làm mẫu
            if (tile.transform == tileA || tile.transform == tileB)
                continue;

            // Chỉ xóa khi toàn bộ Tile đã ra khỏi Camera
            if (tile.renderer.bounds.max.x < cameraLeft)
            {
                allTiles.RemoveAt(i);
                Destroy(tile.transform.gameObject);
            }
        }
    }

}
