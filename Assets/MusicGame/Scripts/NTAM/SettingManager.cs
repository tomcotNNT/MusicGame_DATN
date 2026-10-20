using UnityEngine;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    [Header("=== GIAO DIỆN NÚT (Kéo Image vào đây) ===")]
    public Image musicButtonImage;
    public Image soundButtonImage;
    public Image vibrateButtonImage;

    [Header("=== BẢNG CON ===")]
    public GameObject aboutPanel; // Kéo About_Panel vào đây

    // Biến lưu trữ trạng thái
    private bool isMusicOn;
    private bool isSoundOn;
    private bool isVibrateOn;

    // Hàm này chạy MỖI KHI bảng Setting được bật lên
    private void OnEnable()
    {
        // Tải dữ liệu đã lưu (nếu chưa có thì mặc định là 1 - Bật)
        isMusicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        isSoundOn = PlayerPrefs.GetInt("Sound", 1) == 1;
        isVibrateOn = PlayerPrefs.GetInt("Vibrate", 1) == 1;

        // Cập nhật độ mờ/rõ của nút ngay lập tức
        UpdateAllButtonsUI();
    }

    // ================== CÁC HÀM XỬ LÝ NÚT BẤM ==================

    public void ToggleMusic()
    {
        isMusicOn = !isMusicOn; // Đảo trạng thái
        PlayerPrefs.SetInt("Music", isMusicOn ? 1 : 0); // Lưu vào máy
        UpdateButtonAlpha(musicButtonImage, isMusicOn); // Đổi UI
        
        // TODO: Viết code tắt/mở AudioSource Nhạc nền tại đây
    }

    public void ToggleSound()
    {
        isSoundOn = !isSoundOn;
        PlayerPrefs.SetInt("Sound", isSoundOn ? 1 : 0);
        UpdateButtonAlpha(soundButtonImage, isSoundOn);
        
        // TODO: Viết code tắt/mở AudioSource Âm thanh UI/Hiệu ứng tại đây
    }

    public void ToggleVibrate()
    {
        isVibrateOn = !isVibrateOn;
        PlayerPrefs.SetInt("Vibrate", isVibrateOn ? 1 : 0);
        UpdateButtonAlpha(vibrateButtonImage, isVibrateOn);
        
        if (isVibrateOn) Handheld.Vibrate(); // Test rung nhẹ trên điện thoại
    }

    // ================== QUẢN LÝ BẢNG ABOUT ==================

    public void OpenAboutPanel()
    {
        if (aboutPanel != null) aboutPanel.SetActive(true);
    }

    public void CloseAboutPanel()
    {
        if (aboutPanel != null) aboutPanel.SetActive(false);
    }

    // ================== ĐÓNG BẢNG SETTING ==================

    public void CloseSettingPanel()
    {
        gameObject.SetActive(false);
    }

    // ================== HÀM HỖ TRỢ (UI) ==================

    private void UpdateAllButtonsUI()
    {
        UpdateButtonAlpha(musicButtonImage, isMusicOn);
        UpdateButtonAlpha(soundButtonImage, isSoundOn);
        UpdateButtonAlpha(vibrateButtonImage, isVibrateOn);
    }

    // Hàm chỉnh độ mờ: Bật = Alpha 1f (Rõ), Tắt = Alpha 0.4f (Mờ)
    private void UpdateButtonAlpha(Image img, bool isOn)
    {
        if (img != null)
        {
            Color c = img.color;
            c.a = isOn ? 1f : 0.4f; 
            img.color = c;
        }
    }
}