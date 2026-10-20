
using UnityEngine;

public class AutoParallaxBackground : MonoBehaviour
{
    [Header("Background Sprites")]
    [SerializeField] private Transform backgroundA;
    [SerializeField] private Transform backgroundB;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;

    private float widthA;
    private float widthB;

    private void Start()
    {
        widthA = GetWidth(backgroundA);
        widthB = GetWidth(backgroundB);

        if (widthA <= 0f || widthB <= 0f)
        {
            Debug.LogError("Background thiếu SpriteRenderer hoặc Sprite!");
            enabled = false;
            return;
        }

        // Đặt B sát ngay bên phải A
        backgroundB.position = new Vector3(
            backgroundA.position.x + widthA / 2f + widthB / 2f,
            backgroundA.position.y,
            backgroundA.position.z
        );
    }

    private float GetWidth(Transform bg)
    {
        if (bg == null) return 0f;

        SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();

        if (sr == null || sr.sprite == null) return 0f;

        return sr.bounds.size.x;
    }

    private void Update()
    {
        backgroundA.position += Vector3.left * moveSpeed * Time.deltaTime;
        backgroundB.position += Vector3.left * moveSpeed * Time.deltaTime;

        // A ra khỏi màn hình bên trái: chuyển sang phải B
        if (backgroundA.position.x + widthA / 2f
            <= backgroundB.position.x - widthB / 2f)
        {
            backgroundA.position = new Vector3(
                backgroundB.position.x + widthB / 2f + widthA / 2f,
                backgroundA.position.y,
                backgroundA.position.z
            );
        }

        // B ra khỏi màn hình bên trái: chuyển sang phải A
        if (backgroundB.position.x + widthB / 2f
            <= backgroundA.position.x - widthA / 2f)
        {
            backgroundB.position = new Vector3(
                backgroundA.position.x + widthA / 2f + widthB / 2f,
                backgroundB.position.y,
                backgroundB.position.z
            );
        }
    }
}
