using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LobbyTutorialGuide : MonoBehaviour
{
    [Header("UI Tham chiếu")]
    [SerializeField] private GameObject tutorialOverlay; // Kéo TutorialOverlay vào đây
    [SerializeField] private RectTransform handPointer;    // Kéo HandPointer vào đây
    [SerializeField] private Button nextButton;          // Kéo Button NEXT vào đây

    [Header("Card / Màn chơi cần khóa")]
    [SerializeField] private GameObject stage1LockIcon;  // Icon ổ khóa của Màn 1 (nếu có)
    [SerializeField] private Button stage1Button;        // Nút chọn Màn 1

    private bool isFirstTime = true;
    private Vector2 initialHandPos;

    private void Start()
    {
        // 0: Chưa chơi Tutorial, 1: Đã hoàn thành Tutorial
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        if (isFirstTime)
        {
            // Bật màn tối và bàn tay chỉ dẫn
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);

            // Khóa màn 1
            LockStage1(true);

            // Gán hành động khi ấn NEXT ở lần đầu
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnFirstTimeNextClicked);
        }
        else
        {
            // Người chơi cũ: Tắt toàn bộ lớp hướng dẫn, mở khóa màn 1
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);

            LockStage1(false);
        }
    }

    private void Update()
    {
        // Hiệu ứng bàn tay nhấp nhô nhẹ nhàng thu hút sự chú ý
        if (isFirstTime && handPointer != null)
        {
            float bounce = Mathf.Sin(Time.time * 6f) * 12f;
            handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
        }
    }

    private void OnFirstTimeNextClicked()
    {
        // Gán chỉ số màn là 0 (hoặc index của Tutorial) để Scene Story load đúng truyện
        PlayerPrefs.SetInt("SelectedStageIndex", 0);
        PlayerPrefs.Save();

        // Chuyển thẳng sang Scene Story của Tutorial
        SceneManager.LoadScene("Story");
    }

    private void LockStage1(bool isLocked)
    {
        if (stage1Button != null)
        {
            stage1Button.interactable = !isLocked; // Không cho click chọn bài khác
        }
        if (stage1LockIcon != null)
        {
            stage1LockIcon.SetActive(isLocked); // Bật icon ổ khóa trên Màn 1
        }
    }
}