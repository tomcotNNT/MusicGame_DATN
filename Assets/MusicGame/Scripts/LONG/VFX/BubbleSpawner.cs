
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BubbleSpawner : MonoBehaviour
{
    [Header("Bubble Sprites")]
    [SerializeField] private List<Sprite> bubbleSprites = new List<Sprite>();

    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 0.4f;
    [SerializeField] private int maxBubbles = 30;

    [Header("Movement")]
    [SerializeField] private float minSpeed = 40f;
    [SerializeField] private float maxSpeed = 90f;
    [SerializeField] private float horizontalDrift = 15f;

    [Header("Size")]
    [SerializeField] private float minSize = 15f;
    [SerializeField] private float maxSize = 45f;

    [Header("Appearance")]
    [SerializeField] private float minAlpha = 0.35f;
    [SerializeField] private float maxAlpha = 0.8f;

    private RectTransform area;
    private float spawnTimer;

    private class Bubble
    {
        public RectTransform rect;
        public Image image;

        public float speed;
        public float drift;
        public float frequency;
        public float phase;
        public float startX;
    }

    private readonly List<Bubble> activeBubbles = new List<Bubble>();

    private void Awake()
    {
        area = GetComponent<RectTransform>();
    }

    private void Update()
    {
        UpdateBubbles();
        SpawnBubbles();
    }

    private void SpawnBubbles()
    {
        if (bubbleSprites.Count == 0)
            return;

        spawnTimer += Time.deltaTime;

        if (spawnTimer < spawnInterval)
            return;

        spawnTimer = 0f;

        if (activeBubbles.Count >= maxBubbles)
            return;

        CreateBubble();
    }

    private void CreateBubble()
    {
        // Chọn sprite ngẫu nhiên
        Sprite sprite = bubbleSprites[
            Random.Range(0, bubbleSprites.Count)
        ];

        if (sprite == null)
            return;

        // Tạo UI Image
        GameObject obj = new GameObject(
            "Bubble",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        obj.transform.SetParent(transform, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        Image image = obj.GetComponent<Image>();

        // Gán sprite bọt nước
        image.sprite = sprite;

        // Không chặn thao tác chạm hoặc click
        image.raycastTarget = false;

        // Giữ đúng tỷ lệ sprite
        image.preserveAspect = true;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Kích thước ngẫu nhiên
        float size = Random.Range(minSize, maxSize);

        rect.sizeDelta = new Vector2(size, size);

        // Vị trí xuất hiện ở dưới màn hình
        float x = Random.Range(
            -area.rect.width / 2f,
            area.rect.width / 2f
        );

        float y = -area.rect.height / 2f - size;

        rect.anchoredPosition = new Vector2(x, y);

        // Độ trong suốt ngẫu nhiên
        float alpha = Random.Range(minAlpha, maxAlpha);

        image.color = new Color(1f, 1f, 1f, alpha);

        // Thông số chuyển động
        Bubble bubble = new Bubble();

        bubble.rect = rect;
        bubble.image = image;

        bubble.speed = Random.Range(minSpeed, maxSpeed);
        bubble.drift = Random.Range(-horizontalDrift, horizontalDrift);
        bubble.frequency = Random.Range(0.8f, 1.8f);
        bubble.phase = Random.Range(0f, Mathf.PI * 2f);
        bubble.startX = x;

        activeBubbles.Add(bubble);
    }

    private void UpdateBubbles()
    {
        float delta = Time.deltaTime;
        float topLimit = area.rect.height / 2f;

        for (int i = activeBubbles.Count - 1; i >= 0; i--)
        {
            Bubble bubble = activeBubbles[i];

            if (bubble.rect == null)
            {
                activeBubbles.RemoveAt(i);
                continue;
            }

            Vector2 pos = bubble.rect.anchoredPosition;

            // Bay lên trên
            pos.y += bubble.speed * delta;

            // Trôi nhẹ sang trái và phải
            pos.x = bubble.startX +
                Mathf.Sin(Time.time * bubble.frequency + bubble.phase)
                * bubble.drift;

            bubble.rect.anchoredPosition = pos;

            // Xóa khi ra khỏi màn hình
            if (pos.y > topLimit + bubble.rect.rect.height)
            {
                Destroy(bubble.rect.gameObject);
                activeBubbles.RemoveAt(i);
            }
        }
    }

    private void OnDisable()
    {
        foreach (Bubble bubble in activeBubbles)
        {
            if (bubble.rect != null)
                Destroy(bubble.rect.gameObject);
        }

        activeBubbles.Clear();
    }
}
