using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 1 ô thành tựu trong danh sách. Gắn lên prefab item.
/// - Claimable : glow chạy quanh icon
/// - InProgress: không glow
/// - Claimed   : icon xám + chữ "Claimed"
/// </summary>
public class AchievementItemUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;
    [Header("Phần thưởng (hiển thị trên ô)")]
    [SerializeField] private Image rewardIconImage;     // Icon coin/diamond (có thể bỏ trống)
    [SerializeField] private TMP_Text rewardAmountText; // Số lượng, vd "x100" (có thể bỏ trống)

    [Header("Nhận thưởng")]
    [Tooltip("Nút bấm để nhận thưởng (thường gắn trên icon)")]
    [SerializeField] private Button claimButton;
    [Tooltip("Object chứa chữ Claimed, đặt bên dưới icon")]
    [SerializeField] private GameObject claimedLabel;

    [Header("Glow chạy quanh icon")]
    [Tooltip("Image đặt phía sau icon, lớn hơn icon một chút. Để trống sprite thì tự tạo vòng sáng")]
    [SerializeField] private Image glowImage;
    [Tooltip("Độ/giây. Đổi dấu để đảo chiều chạy")]
    [SerializeField] private float glowSpeed = 180f;

    [Header("Màu icon")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color claimedColor = new Color(0.45f, 0.45f, 0.45f, 1f);

    private AchievementDefinition definition;
    private Action<AchievementDefinition> onClick;
    private AchievementState state = AchievementState.InProgress;

    private static Sprite cachedRingSprite;

    private void Awake()
    {
        if (glowImage != null)
        {
            // Glow không được chặn click vào icon
            glowImage.raycastTarget = false;

            if (glowImage.sprite == null)
                glowImage.sprite = GetRingSprite();
        }

        if (claimButton != null)
            claimButton.onClick.AddListener(HandleClick);
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(HandleClick);
    }

    public void Setup(AchievementDefinition def, Sprite rewardSprite, Action<AchievementDefinition> clickCallback)
    {
        definition = def;
        onClick = clickCallback;

        if (iconImage != null)
            iconImage.sprite = def.icon;

        if (titleText != null)
            titleText.text = def.title;

        if (rewardIconImage != null)
            rewardIconImage.sprite = rewardSprite;

        if (rewardAmountText != null)
            rewardAmountText.text = "x" + def.rewardAmount.ToString("N0");
    }

    public void Refresh(long currentValue, AchievementState newState)
    {
        state = newState;

        bool claimed = state == AchievementState.Claimed;
        bool claimable = state == AchievementState.Claimable;

        if (progressText != null)
        {
            // Hiển thị tối đa bằng mục tiêu (vd 1500/1000 -> 1000/1000)
            long target = definition.targetValue;
            long shown = claimed ? target : Math.Min(currentValue, target);
            progressText.text = shown.ToString("N0") + "/" + target.ToString("N0");
        }

        if (iconImage != null)
            iconImage.color = claimed ? claimedColor : normalColor;

        if (rewardIconImage != null)
            rewardIconImage.color = claimed ? claimedColor : normalColor;

        if (rewardAmountText != null)
            rewardAmountText.color = claimed ? claimedColor : normalColor;

        if (claimedLabel != null)
            claimedLabel.SetActive(claimed);

        if (glowImage != null)
            glowImage.gameObject.SetActive(claimable);
    }

    private void Update()
    {
        if (state == AchievementState.Claimable && glowImage != null)
        {
            glowImage.rectTransform.Rotate(0f, 0f, glowSpeed * Time.unscaledDeltaTime);
        }
    }

    private void HandleClick()
    {
        // Chỉ nhận được khi đủ điều kiện và chưa nhận
        if (state != AchievementState.Claimable)
            return;

        onClick?.Invoke(definition);
    }

    // Tạo sprite vòng tròn có 1 vệt sáng đuôi sao chổi mờ dần, dùng khi chưa có sprite riêng
    private static Sprite GetRingSprite()
    {
        if (cachedRingSprite != null)
            return cachedRingSprite;

        const int size = 128;
        const float thickness = 8f;

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = (size - 1) / 2f;
        float outer = size / 2f - 1f;
        float inner = outer - thickness;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Viền mềm 1px ở mép trong và mép ngoài vòng
                float ring = Mathf.Clamp01(Mathf.Min(dist - inner, outer - dist));

                if (ring <= 0f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                float angle = Mathf.Atan2(dy, dx);                    // -PI..PI
                float t = (angle + Mathf.PI) / (2f * Mathf.PI);       // 0..1
                float brightness = Mathf.Pow(t, 3f);                  // đầu sáng, đuôi mờ

                tex.SetPixel(x, y, new Color(1f, 1f, 1f, ring * brightness));
            }
        }

        tex.Apply();

        cachedRingSprite = Sprite.Create(
            tex,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100f);

        return cachedRingSprite;
    }
}