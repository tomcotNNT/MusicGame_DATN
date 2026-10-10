using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Sử dụng TextMeshPro cho điểm số

public class WinPanelManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject winPanelRoot;       // Kéo chính đối tượng WinPanel vào đây
    [SerializeField] private TextMeshProUGUI scoreText;     // Text hiển thị điểm số
    [SerializeField] private GameObject[] starIcons;        // Mảng chứa 3 icon ngôi sao (Star)

    [Header("Scene Navigation")]
    [SerializeField] private string selectSongSceneName = "SelectSong1"; // Tên scene sảnh chính / chọn bài
    [SerializeField] private string nextLevelSceneName = "GameplayScene";  // Tên scene màn tiếp theo

    private void Start()
    {
        // Khi bắt đầu màn chơi, ẩn bảng Win đi
        if (winPanelRoot != null)
        {
            winPanelRoot.SetActive(false);
        }
    }

    /// <summary>
    /// Hàm gọi để hiển thị WinPanel khi người chơi chiến thắng màn chơi
    /// </summary>
    /// <param name="finalScore">Điểm số tổng kết</param>
    /// <param name="earnedStars">Số sao đạt được (từ 0 đến 3)</param>
    public void ShowWinPanel(int finalScore, int earnedStars)
    {
        if (winPanelRoot != null)
        {
            winPanelRoot.SetActive(true);
        }

        // Hiển thị điểm số theo định dạng 7 chữ số (giống concept: 0000000)
        if (scoreText != null)
        {
            scoreText.text = finalScore.ToString("D7");
        }

        // Kích hoạt các ngôi sao dựa trên số sao đạt được
        for (int i = 0; i < starIcons.Length; i++)
        {
            if (starIcons[i] != null)
            {
                // Nếu vị trí sao nhỏ hơn số sao đạt được thì bật sáng, ngược lại tắt
                starIcons[i].SetActive(i < earnedStars);
            }
        }
    }

    // =========================================================
    // BUTTON EVENT HANDLERS
    // =========================================================

    // 1. Nút Chơi Lại (Replay)
    public void OnReplayButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Tải lại Scene hiện tại
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 2. Nút Về Trang Chủ / Chọn Bài (Home)
    public void OnHomeButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Chuyển về Scene chọn bài hát
        SceneManager.LoadScene(selectSongSceneName);
    }

    // 3. Nút Chơi Màn Tiếp Theo (Next)
    public void OnNextButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Chuyển sang màn chơi tiếp theo
        SceneManager.LoadScene(nextLevelSceneName);
    }
}