
using UnityEngine;

public class ParallaxLayer : MonoBehaviour
{
    [Header("Tiles")]
    [SerializeField] private Transform tileA;
    [SerializeField] private Transform tileB;

    [Header("Parallax Settings")]
    [SerializeField] private float speed = 1f;

    private Transform player;
    private float tileWidth;
    private Camera mainCamera;

    private void Start()
    {
        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        mainCamera = Camera.main;

        if (tileA != null)
        {
            SpriteRenderer sr =
                tileA.GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                tileWidth = sr.bounds.size.x;
            }
        }
    }

    private void Update()
    {
        if (player == null || mainCamera == null)
            return;

        if (tileA == null || tileB == null || tileWidth <= 0f)
            return;

        // Di chuyển nền từ phải sang trái
        float movement = speed * Time.deltaTime;

        tileA.position += Vector3.left * movement;
        tileB.position += Vector3.left * movement;

        // Mép trái của camera trong world space
        float cameraLeft =
            mainCamera.transform.position.x
            - mainCamera.orthographicSize
            * mainCamera.aspect;

        // Đưa tile đã ra khỏi màn hình về phía trước
        if (tileA.position.x + tileWidth / 2f
            <= cameraLeft)
        {
            tileA.position = new Vector3(
                tileB.position.x + tileWidth,
                tileA.position.y,
                tileA.position.z
            );
        }

        if (tileB.position.x + tileWidth / 2f
            <= cameraLeft)
        {
            tileB.position = new Vector3(
                tileA.position.x + tileWidth,
                tileB.position.y,
                tileB.position.z
            );
        }
    }
}
