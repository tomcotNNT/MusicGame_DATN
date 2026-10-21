using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyTutorialGuide : MonoBehaviour
{
    [Header("Cấu hình Chuyển Scene")]
    [SerializeField] private string characterSceneName = "Character"; // Scene chọn nhân vật

    [Header("UI Hướng Dẫn")]
    [SerializeField] private GameObject tutorialOverlay; 
    [SerializeField] private RectTransform handPointer;    
    [SerializeField] private Button nextButton;          

    [Header("Màn 1 Cần Khóa")]
    [SerializeField] private GameObject stage1LockIcon;  
    [SerializeField] private Button stage1Button;        

    private bool isFirstTime = true;
    private Vector2 initialHandPos;

    private void Awake()
    {
        // 0: Chưa chơi Tutorial (Tân thủ), 1: Đã hoàn thành Tutorial
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        if (isFirstTime)
        {
            // Tân thủ: Bật màn tối, hiện bàn tay, khóa Màn 1
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            LockStage1(true);
        }
        else
        {
            // Người chơi cũ: Tắt màn tối, tắt bàn tay, mở khóa Màn 1
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            LockStage1(false);
        }
    }

    private void Update()
    {
        // Bàn tay nhấp nhô chỉ chạy khi là tân thủ
        if (isFirstTime && handPointer != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 12f;
            handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
        }
    }

    // === HÀM PUBLIC DUY NHẤT ĐỂ GÁN VÀO ON CLICK CỦA NÚT PLAY ===
    public void OnNextButtonClicked()
    {
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);

        if (tutorialCompleted == 0)
        {
            Debug.Log(">>> [LOBBY] Tân thủ: Gán SelectedStageIndex = 0 -> Nạp Character!");
            PlayerPrefs.SetInt("SelectedStageIndex", 0); // Màn Tokyo Tutorial
        }
        else
        {
            Debug.Log(">>> [LOBBY] Đã mở khóa: Gán SelectedStageIndex = 1 -> Nạp Character!");
            PlayerPrefs.SetInt("SelectedStageIndex", 1); // Màn 1 Fairy Tail (New York)
        }

        PlayerPrefs.Save();

        // Luôn luôn chuyển sang Scene Character
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

        if (stage1LockIcon != null)
        {
            stage1LockIcon.SetActive(isLocked);
        }
    }

    // --- CÔNG CỤ TEST NHANH TRÊN UNITY ---
    [ContextMenu("Test: Reset Về Tân Thủ")]
    public void ResetToBeginner()
    {
        PlayerPrefs.DeleteKey("TutorialCompleted");
        PlayerPrefs.Save();
        Debug.Log(">>> [TEST] Đã Reset về Tân thủ (TutorialCompleted = 0)!");
    }
}