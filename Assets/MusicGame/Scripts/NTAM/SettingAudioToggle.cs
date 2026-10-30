using UnityEngine;
using UnityEngine.UI;

public enum AudioToggleType
{
    Music, // BGM
    Sound  // SFX
}

[RequireComponent(typeof(Button))]
public class SettingAudioToggle : MonoBehaviour
{
    [Header("Loại Kênh Điều Khiển")]
    [SerializeField] private AudioToggleType toggleType;

    [Header("Hiệu Ứng Trực Quan Khi Bật / Tắt")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Chế Độ Làm Tối Màu Khi Tắt (Dùng nếu không có Sprite gạch chéo)")]
    [SerializeField] private bool useColorDim = true;
    [SerializeField] private Color onColor = Color.white;
    [SerializeField] private Color offColor = new Color(0.45f, 0.45f, 0.45f, 0.7f); // Xám tối

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        btn.onClick.AddListener(OnToggleClicked);
    }

    private void OnEnable()
    {
        // Cập nhật đúng trạng thái hiển thị mỗi khi bảng SettingPanel được mở lên
        UpdateButtonVisual();
    }

    private void OnToggleClicked()
    {
        if (GlobalAudioManager.Instance == null) return;

        if (toggleType == AudioToggleType.Music)
        {
            GlobalAudioManager.Instance.ToggleBGM();
        }
        else
        {
            GlobalAudioManager.Instance.ToggleSFX();
        }

        // Phát tiếng click phản hồi nếu SFX chưa bị tắt
        GlobalAudioManager.Instance.PlaySFX(UISfxType.Click);

        UpdateButtonVisual();
    }

    public void UpdateButtonVisual()
    {
        if (GlobalAudioManager.Instance == null || targetImage == null) return;

        bool isActive = (toggleType == AudioToggleType.Music)
            ? GlobalAudioManager.Instance.IsBGMActive
            : GlobalAudioManager.Instance.IsSFXActive;

        // 1. Đổi sprite nếu có gán sprite On / Off
        if (onSprite != null && offSprite != null)
        {
            targetImage.sprite = isActive ? onSprite : offSprite;
        }

        // 2. Đổi màu sáng / mờ tối để người chơi nhận diện
        if (useColorDim)
        {
            targetImage.color = isActive ? onColor : offColor;
        }
    }
}