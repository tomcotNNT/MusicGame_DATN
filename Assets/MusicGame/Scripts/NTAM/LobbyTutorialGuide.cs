using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyTutorialGuide : MonoBehaviour
{
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
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        if (isFirstTime)
        {
            // === NGƯỜI CHƠI MỚI: BẮT BUỘC CHƠI TUTORIAL (INDEX 0) ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);

            LockStage1(true);

            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnFirstTimeNextClicked);
            }
        }
        else
        {
            // === ĐÃ XONG TUTORIAL: CHUYỂN SANG MÀN 1 NEW YORK (INDEX 1) ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);

            LockStage1(false);

            // Gán nút NEXT ở giữa sảnh dẫn vào Màn 1 Fairy Tail (New York City)
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnPlayStage1Clicked);
            }
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

    // Dành cho tân thủ: nạp Tokyo Tutorial
    private void OnFirstTimeNextClicked()
    {
        PlayerPrefs.SetInt("SelectedStageIndex", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Story");
    }

    // Dành cho người đã mở khóa Màn 1: nạp 1.New York City
    public void OnPlayStage1Clicked()
    {
        Debug.Log(">>> [LOBBY] Bấm NEXT Màn 1: Gán SelectedStageIndex = 1 và mở Story New York!");
        PlayerPrefs.SetInt("SelectedStageIndex", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene("Story");
    }

    private void LockStage1(bool isLocked)
    {
        if (stage1Button != null)
        {
            stage1Button.interactable = !isLocked;
            
            // Nếu người chơi bấm trực tiếp vào Card bài hát Fairy Tail cũng sẽ nạp Màn 1
            if (!isLocked)
            {
                stage1Button.onClick.RemoveAllListeners();
                stage1Button.onClick.AddListener(OnPlayStage1Clicked);
            }
        }

        if (stage1LockIcon != null)
        {
            stage1LockIcon.SetActive(isLocked);
        }
    }
}