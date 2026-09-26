using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Hiệu ứng UI")]
    public TextMeshProUGUI tapToStartText; // Kéo object Text vào đây
    public Color clickedColor = Color.red; // Màu đỏ báo hiệu đã bấm thành công

    [Header("Hệ thống Âm thanh")]
    public AudioSource bgmSource;
    public AudioSource sfxClickSource;
    public AudioSource sfxStartSource;

    [Header("Cài đặt Chuyển cảnh")]
    public float transitionDelay = 1.5f; // Đợi âm thanh Start kêu xong rồi mới chuyển Scene
    private bool isStarting = false;

    // Hàm gắn vào các nút bấm thông thường (Settings, Shop...)
    public void PlayClickSound()
    {
        if (sfxClickSource != null)
        {
            sfxClickSource.Play();
        }
    }

    // Hàm riêng gắn vào nút Tap To Start khổng lồ
    public void OnTapToStartClicked()
    {
        if (isStarting) return;
        isStarting = true;

        // Bật hiệu ứng phản hồi thị giác
        if (tapToStartText != null)
        {
            // Đổi ngay lập tức sang màu đỏ
            tapToStartText.color = clickedColor;

            // Tùy chọn: Làm hiệu ứng chớp tắt liên tục (Flicker)
            StartCoroutine(FlickerTextEffect());
        }

        if (sfxStartSource != null) sfxStartSource.Play();
        if (bgmSource != null) StartCoroutine(FadeOutBGM());

        StartCoroutine(LoadLobbyScene());
    }

    private IEnumerator FlickerTextEffect()
    {
        while (true) // Chớp liên tục cho đến khi chuyển Scene
        {
            tapToStartText.enabled = !tapToStartText.enabled; // Bật/Tắt hiển thị
            yield return new WaitForSeconds(0.1f); // Tốc độ chớp tắt (0.1 giây)
        }
    }
    public void OnSettingsClicked()
    {
        PlayClickSound(); // Phát tiếng tick
        Debug.Log("Mở bảng Cài đặt!");
        // Sau này bạn sẽ code lệnh mở UI Settings (Popup) tại đây
    }

    private IEnumerator FadeOutBGM()
    {
        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * (Time.deltaTime / transitionDelay);
            yield return null;
        }
    }

    private IEnumerator LoadLobbyScene()
    {
        // Chờ bằng đúng thời gian transitionDelay
        yield return new WaitForSeconds(transitionDelay);

        // Load Scene Sảnh chính (Đảm bảo đã tạo Lobby_Scene và gán số 2 trong Build Settings)
        SceneManager.LoadScene(2);
    }
}