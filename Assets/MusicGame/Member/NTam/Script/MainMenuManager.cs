using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Hiệu ứng UI")]
    public TextMeshProUGUI tapToStartText; // Kéo object Text (chữ Tap To Start) vào đây
    public Color clickedColor = Color.red; // Màu báo hiệu đã bấm thành công

    [Header("UI Cài Đặt")]
    public GameObject settingsPopup; // BỔ SUNG: Kéo object Setting_Panel vào đây

    [Header("Hệ thống Âm thanh")]
    public AudioSource bgmSource;
    public AudioSource sfxClickSource;
    public AudioSource sfxStartSource;

    [Header("Cài đặt Chuyển cảnh")]
    public float transitionDelay = 1.5f; // Đợi âm thanh Start kêu xong rồi mới chuyển Scene
    private bool isStarting = false;

    // ==========================================
    // CÁC HÀM XỬ LÝ ÂM THANH & TƯƠNG TÁC CHUNG
    // ==========================================

    // Hàm phát tiếng Click, gắn vào các nút bấm thông thường (Settings, Shop...)
    public void PlayClickSound()
    {
        if (sfxClickSource != null)
        {
            sfxClickSource.Play();
        }
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ BẢNG CÀI ĐẶT (SETTINGS)
    // ==========================================

    // Hàm gắn vào Icon Bánh răng ngoài màn hình
    public void OnSettingsOpenClicked()
    {
        if (isStarting) return; // Nếu đang load vào game thì chặn không cho mở Setting nữa
        
        PlayClickSound(); // Phát tiếng tick
        
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(true); // Hiển thị bảng Popup
        }
    }

    // BỔ SUNG: Hàm gắn vào Nút [X] màu đỏ trong bảng Cài đặt
    public void OnSettingsCloseClicked()
    {
        PlayClickSound(); // Phát tiếng tick
        
        if (settingsPopup != null)
        {
            settingsPopup.SetActive(false); // Ẩn bảng Popup đi
        }
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ VÀO GAME (TAP TO START)
    // ==========================================

    // Hàm riêng gắn vào nút Tap To Start khổng lồ
    public void OnTapToStartClicked()
    {
        if (isStarting) return; // Chặn spam click

        // CHỐT AN TOÀN: Nếu bảng Setting đang bật, bấm ra ngoài sẽ không bị lọt vào game
        if (settingsPopup != null && settingsPopup.activeInHierarchy) return;

        isStarting = true;

        // 1. Bật hiệu ứng phản hồi thị giác
        if (tapToStartText != null)
        {
            // Đổi ngay lập tức sang màu đỏ (hoặc màu bạn đã chọn)
            tapToStartText.color = clickedColor;

            // Làm hiệu ứng chớp tắt liên tục (Flicker)
            StartCoroutine(FlickerTextEffect());
        }

        // 2. Xử lý Âm thanh (Phát tiếng Start, nhỏ dần BGM)
        if (sfxStartSource != null) sfxStartSource.Play();
        if (bgmSource != null) StartCoroutine(FadeOutBGM());

        // 3. Tiến hành load Scene
        StartCoroutine(LoadLobbyScene());
    }

    private IEnumerator FlickerTextEffect()
    {
        while (true) // Chớp liên tục cho đến khi chuyển Scene hoàn tất
        {
            tapToStartText.enabled = !tapToStartText.enabled; // Bật/Tắt hiển thị
            yield return new WaitForSeconds(0.1f); // Tốc độ chớp tắt (0.1 giây)
        }
    }

    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            // Ép âm lượng về 0 từ từ dựa theo transitionDelay
            bgmSource.volume -= startVolume * (Time.deltaTime / transitionDelay);
            yield return null;
        }
    }

    private IEnumerator LoadLobbyScene()
    {
        // Chờ bằng đúng thời gian transitionDelay để nghe hết tiếng Effect Start
        yield return new WaitForSeconds(transitionDelay);

        // Load Scene Sảnh chính (Đảm bảo đã tạo Lobby_Scene và gán số 2 trong Build Settings)
        SceneManager.LoadScene(2);
    }
}