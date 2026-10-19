using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class StorySceneController : MonoBehaviour
{
    [System.Serializable]
    public class StageData
    {
        public string stageTitle = "1. TOKYO";
        [TextArea(2, 4)] public string storyContent = "Nội dung cốt truyện màn chơi...";
        public Sprite background1;
        public Sprite background2;
        public string targetGameplayScene = "Tutorial";
    }

    [Header("UI Tham Chiếu")]
    [SerializeField] private Image backgroundA;              // Kéo Background (1) vào đây
    [SerializeField] private Image backgroundB;              // Kéo Background (2) vào đây
    [SerializeField] private TextMeshProUGUI stageTitleText; // Kéo TitleStoryText vào đây
    [SerializeField] private TextMeshProUGUI storyContentText;// Kéo StoryContextText vào đây
    [SerializeField] private Button nextButton;              // Kéo NextText vào đây
    [SerializeField] private CanvasGroup fadePanel;          // Kéo FadePanel vào đây

    [Header("Cài đặt chuyển đổi 2 Background")]
    [SerializeField] private float displayDuration = 4.0f;   // Thời gian giữ 1 ảnh (giây)
    [SerializeField] private float fadeDuration = 1.5f;      // Thời gian chuyển mờ nhẹ giữa 2 ảnh

    [Header("Dữ liệu 5 Màn Chơi (10 Backgrounds)")]
    [SerializeField] private List<StageData> stages = new List<StageData>();

    private int currentStageIndex = 0;
    private StageData currentStage;
    private bool isTransitioning = false;

    private void Start()
    {
        // 1. Nhận chỉ số màn từ màn chọn bài (0 = Màn 1, 1 = Màn 2, ...)
        currentStageIndex = PlayerPrefs.GetInt("SelectedStageIndex", 0);

        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("Chưa cấu hình danh sách Stages trên Inspector!");
            return;
        }

        currentStageIndex = Mathf.Clamp(currentStageIndex, 0, stages.Count - 1);
        currentStage = stages[currentStageIndex];

        // 2. Điền thông tin tiêu đề và cốt truyện
        if (stageTitleText != null) stageTitleText.text = currentStage.stageTitle;
        if (storyContentText != null) storyContentText.text = currentStage.storyContent;

        // 3. Khởi tạo hình nền và chạy vòng lặp đổi ảnh mượt
        InitBackgrounds();
        StartCoroutine(SmoothBackgroundLoopRoutine());

        // 4. Lắng nghe sự kiện bấm nút Next
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private void InitBackgrounds()
    {
        if (backgroundA != null && currentStage.background1 != null)
        {
            backgroundA.sprite = currentStage.background1;
            backgroundA.color = Color.white;
        }

        if (backgroundB != null && currentStage.background2 != null)
        {
            backgroundB.sprite = currentStage.background2;
            backgroundB.color = new Color(1f, 1f, 1f, 0f); // Ẩn ban đầu để chuyển mờ
        }
    }

    private IEnumerator SmoothBackgroundLoopRoutine()
    {
        while (true)
        {
            // Giữ Background A
            yield return new WaitForSeconds(displayDuration);

            // Hiện mờ dần Background B lên đè lên A
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Clamp01(timer / fadeDuration);
                if (backgroundB != null) backgroundB.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            if (backgroundB != null) backgroundB.color = Color.white;

            // Giữ Background B
            yield return new WaitForSeconds(displayDuration);

            // Làm mờ Background B để lộ lại Background A
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = 1f - Mathf.Clamp01(timer / fadeDuration);
                if (backgroundB != null) backgroundB.color = new Color(1f, 1f, 1f, alpha);
                yield return null;
            }
            if (backgroundB != null) backgroundB.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void OnNextButtonClicked()
    {
        // 1. In log để kiểm tra chuột có chạm được vào nút hay không
        Debug.Log(">>> [CLICK OK] Đã nhận lệnh bấm NEXT, đang chuyển Scene!");

        // 2. Lấy tên scene mục tiêu từ cấu hình stage
        string targetScene = "Tutorial";
        if (stages != null && stages.Count > currentStageIndex && !string.IsNullOrEmpty(stages[currentStageIndex].targetGameplayScene))
        {
            targetScene = stages[currentStageIndex].targetGameplayScene;
        }

        // 3. Chuyển thẳng vào Scene gameplay
        UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
    }
    private IEnumerator LoadGameplaySceneRoutine()
    {
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float timer = 0f;
            while (timer < 0.4f)
            {
                timer += Time.deltaTime;
                fadePanel.alpha = Mathf.Clamp01(timer / 0.4f);
                yield return null;
            }
        }

        string targetScene = string.IsNullOrEmpty(currentStage.targetGameplayScene)
                             ? "Tutorial"
                             : currentStage.targetGameplayScene;

        SceneManager.LoadScene(targetScene);
    }
}