using UnityEngine;

// Gắn vào prefab VFX dạng ảnh (có SpriteRenderer). Tự phóng to, mờ dần rồi tự hủy.
// Có thể dùng 1 sprite tĩnh hoặc một dãy sprite (frame animation).
[RequireComponent(typeof(SpriteRenderer))]
public class HitImageVfx : MonoBehaviour
{
    [Header("Thời gian")]
    [SerializeField] private float lifetime = 0.35f;

    [Header("Scale")]
    [SerializeField] private float startScale = 0.6f;
    [SerializeField] private float endScale = 1.3f;

    [Header("Mờ dần")]
    [Tooltip("Bắt đầu mờ khi đã chạy được bao nhiêu phần (0.5 = nửa đời)")]
    [SerializeField, Range(0f, 1f)] private float fadeStartAt = 0.4f;

    [Header("Bay lên (tuỳ chọn)")]
    [SerializeField] private float riseDistance = 0f;

    [Header("Frame animation (tuỳ chọn)")]
    [Tooltip("Để trống = dùng sprite đang gán trong SpriteRenderer")]
    [SerializeField] private Sprite[] frames;

    [Header("Xoay ngẫu nhiên nhẹ (0 = tắt)")]
    [SerializeField] private float randomRotation = 0f;

    private SpriteRenderer sr;
    private Vector3 baseScale;
    private Vector3 startPos;
    private Color baseColor;
    private float elapsed;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = sr.color;
    }

    private void OnEnable()
    {
        elapsed = 0f;
        startPos = transform.position;
        transform.localScale = baseScale * startScale;

        if (randomRotation > 0f)
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-randomRotation, randomRotation));

        if (frames != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));

        // Scale: ease-out cho cảm giác "bung" nhanh rồi chậm lại
        float eased = 1f - (1f - t) * (1f - t);
        transform.localScale = baseScale * Mathf.Lerp(startScale, endScale, eased);

        if (riseDistance != 0f)
            transform.position = startPos + Vector3.up * (riseDistance * eased);

        // Frame animation: chia đều các frame theo đời VFX
        if (frames != null && frames.Length > 1)
        {
            int index = Mathf.Min(frames.Length - 1, (int)(t * frames.Length));
            sr.sprite = frames[index];
        }

        // Mờ dần
        float fadeT = Mathf.InverseLerp(fadeStartAt, 1f, t);
        Color c = baseColor;
        c.a = baseColor.a * (1f - fadeT);
        sr.color = c;

        if (t >= 1f)
            Destroy(gameObject);
    }
}