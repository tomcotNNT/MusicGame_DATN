using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 2 nút bật/tắt Nhạc nền và Âm thanh trong menu.
/// Lưu vào AudioPrefs (SETTING_BGM / SETTING_SFX) nên giữ nguyên khi tắt game,
/// và áp dụng ngay cho BackgroundMusic, tiếng nút, tiếng đánh nốt.
/// </summary>
public class AudioToggleControl : MonoBehaviour
{
    [Header("Music")]
    public Button musicButton;
    public Image musicImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;

    [Header("Sound")]
    public Button soundButton;
    public Image soundImage;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    private void Start()
    {
        if (musicButton != null) musicButton.onClick.AddListener(ToggleMusic);
        if (soundButton != null) soundButton.onClick.AddListener(ToggleSound);

        RefreshSprites();
    }

    private void OnEnable()
    {
        AudioPrefs.Changed += RefreshSprites;
        RefreshSprites();
    }

    private void OnDisable()
    {
        AudioPrefs.Changed -= RefreshSprites;
    }

    private void ToggleMusic()
    {
        AudioPrefs.MusicOn = !AudioPrefs.MusicOn;
    }

    private void ToggleSound()
    {
        AudioPrefs.SfxOn = !AudioPrefs.SfxOn;
    }

    private void RefreshSprites()
    {
        if (musicImage != null)
            musicImage.sprite = AudioPrefs.MusicOn ? musicOnSprite : musicOffSprite;

        if (soundImage != null)
            soundImage.sprite = AudioPrefs.SfxOn ? soundOnSprite : soundOffSprite;
    }
}
