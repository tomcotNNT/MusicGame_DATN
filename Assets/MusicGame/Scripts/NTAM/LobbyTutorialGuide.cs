using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyTutorialGuide : MonoBehaviour
{
    [Header("Cấu hình Chuyển Scene")]
    [Tooltip("Tên Scene chọn nhân vật")]
    [SerializeField] private string characterSceneName = "Character";

    [Header("UI Hướng Dẫn Tân Thủ")]
    [SerializeField] private GameObject tutorialOverlay; // Màn tối phủ mờ sảnh
    [SerializeField] private RectTransform handPointer;    // Bàn tay chỉ dẫn
    [SerializeField] private Button nextButton;          // Nút PLAY/NEXT chính giữa sảnh

    [Header("Màn 1 (Fairy Tail - Cần Khóa Với Tân Thủ)")]
    [SerializeField] private GameObject stage1LockIcon;  // Icon ổ khóa trên Card Màn 1
    [SerializeField] private Button stage1Button;        // Nút chọn Card Màn 1

    private bool isFirstTime = true;
    private Vector2 initialHandPos;

    private void Awake()
    {
        // Kiểm tra trạng thái: 0 = Tân thủ, 1 = Đã xong Tutorial
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }
    }

    private void Start()
    {
        // 1. Tự động mở lại âm thanh toàn cục và đánh thức bài nhạc sảnh (BGM2)
        RestoreLobbyAudio();

        // 2. Thiết lập trạng thái giao diện theo tiến trình
        if (isFirstTime)
        {
            // === TÂN THỦ ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);

            // Khóa chặt Màn 1
            LockStage1(true);

            // Nút Next chỉ dẫn vào chọn nhân vật cho màn Tutorial
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }
        else
        {
            // === NGƯỜI CHƠI CŨ (ĐÃ QUA TUTORIAL) ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);

            // Mở khóa Màn 1
            LockStage1(false);

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }
    }

    private void Update()
    {
        // Hiệu ứng bàn tay nhấp nhô (chỉ chạy khi là tân thủ)
        if (isFirstTime && handPointer != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 12f;
            handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
        }
    }

    /// <summary>
    /// Hàm xử lý khi bấm nút Next/Play tại sảnh
    /// </summary>
    public void OnNextButtonClicked()
    {
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);

        if (tutorialCompleted == 0)
        {
            // Tân thủ: Ép chơi Màn Tokyo Tutorial (Index 0)
            Debug.Log(">>> [LOBBY] Tân thủ: Gán SelectedStageIndex = 0 (Tokyo Tutorial)!");
            PlayerPrefs.SetInt("SelectedStageIndex", 0);
        }
        else
        {
            // Đã xong Tutorial: Gán chơi Màn 1 Fairy Tail (Index 1)
            Debug.Log(">>> [LOBBY] Đã mở khóa: Gán SelectedStageIndex = 1 (Fairy Tail - New York)!");
            PlayerPrefs.SetInt("SelectedStageIndex", 1);
        }

        PlayerPrefs.Save();

        // Chuyển sang Scene chọn nhân vật
        SceneManager.LoadScene(characterSceneName);
    }

    /// <summary>
    /// Đóng/Mở khóa Card Màn 1
    /// </summary>
    private void LockStage1(bool isLocked)
    {
        if (stage1Button != null)
        {
            stage1Button.interactable = !isLocked;

            if (!isLocked)
            {
                stage1Button.onClick.RemoveAllListeners();
                stage1Button.onClick.AddListener(OnNextButtonClicked);
            }
        }

        if (stage1LockIcon != null)
        {
            stage1LockIcon.SetActive(isLocked);
        }
    }

    /// <summary>
    /// Khôi phục âm thanh toàn cục và kích hoạt phát lại BGM từ GameManager
    /// </summary>
    private void RestoreLobbyAudio()
    {
        // 1. Phục hồi thời gian và mở câm âm thanh toàn cục (tránh bị dính từ OptionPanel)
        Time.timeScale = 1f;
        AudioListener.pause = false;

        // 2. Tìm GameManager xuyên Scene từ LoadingSceneManager
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            AudioSource bgmSource = gm.GetComponent<AudioSource>();
            if (bgmSource != null)
            {
                bgmSource.mute = false;
                bgmSource.volume = 1f;

                // Nếu nhạc sảnh đang bị tắt hoặc dừng -> phát lại từ đầu
                if (!bgmSource.isPlaying)
                {
                    bgmSource.Play();
                    Debug.Log(">>> [AUDIO] Đã kích hoạt lại BGM sảnh (BGM2) thành công!");
                }
            }
        }
        else
        {
            Debug.LogWarning(">>> [AUDIO] Không tìm thấy GameManager trong Scene!");
        }
    }

    // =========================================================================
    // CÔNG CỤ TEST NHANH TRONG UNITY EDITOR (Chuột phải vào Script để chọn)
    // =========================================================================
    [ContextMenu("Test: Reset Về Tân Thủ")]
    public void ResetToBeginner()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 0);
        PlayerPrefs.Save();
        Debug.Log(">>> [TEST] Đã Reset về Tân thủ (TutorialCompleted = 0)!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    [ContextMenu("Test: Giả Lập Đã Xong Tutorial")]
    public void SimulateTutorialCompleted()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log(">>> [TEST] Đã mở khóa tự do (TutorialCompleted = 1)!");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}