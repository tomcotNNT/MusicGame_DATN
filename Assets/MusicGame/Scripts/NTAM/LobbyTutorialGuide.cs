using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyTutorialGuide : MonoBehaviour
{
    [Header("Cấu hình Chuyển Scene")]
    [Tooltip("Tên Scene chọn nhân vật")]
    [SerializeField] private string characterSceneName = "Character";

    [Header("UI Hướng Dẫn Tân Thủ")]
    [SerializeField] private GameObject tutorialOverlay; 
    [SerializeField] private RectTransform handPointer;    
    [SerializeField] private Button nextButton;          // Nút PLAY (NEXT)

    [Header("Màn 1 (Fairy Tail - Cần Khóa Với Tân Thủ)")]
    [SerializeField] private GameObject stage1LockIcon;  
    [SerializeField] private Button stage1Button;        

    private bool isFirstTime = true;
    private Vector2 initialHandPos;

    private void Awake()
    {
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }
    }

    private void Start()
    {
        RestoreLobbyAudio();

        // LUÔN BẬT NÚT PLAY ĐỂ NGƯỜI CHƠI CHỌN NHẠC
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (isFirstTime)
        {
            // === TÂN THỦ ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            LockStage1(true);
        }
        else
        {
            // === NGƯỜI CHƠI CŨ (ĐÃ QUA TUTORIAL) ===
            // Chỉ tắt màn mờ và bàn tay chỉ dẫn, GIỮ LẠI NÚT PLAY
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            LockStage1(false);
        }
    }

    private void Update()
    {
        if (isFirstTime && handPointer != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 12f;
            handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
        }
    }

    public void OnNextButtonClicked()
    {
        if (nextButton != null) nextButton.interactable = false;

        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Confirm);
        }

        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (tutorialCompleted == 0)
        {
            PlayerPrefs.SetInt("SelectedStageIndex", 0); // Màn Tokyo Tutorial
        }
        else
        {
            PlayerPrefs.SetInt("SelectedStageIndex", 1); // Màn Fairy Tail
        }
        PlayerPrefs.Save();

        SceneManager.LoadScene(characterSceneName);
    }

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

        if (stage1LockIcon != null) stage1LockIcon.SetActive(isLocked);
    }

    private void RestoreLobbyAudio()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlayLobbyBGM();
        }
    }

    // =========================================================================
    // HÀM ẨN / HIỆN NÚT PLAY KHI MỞ / ĐÓNG CÁC BẢNG POPUP (PANEL)
    // =========================================================================
    public void SetPlayButtonActive(bool isActive)
    {
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(isActive);
        }
    }
}