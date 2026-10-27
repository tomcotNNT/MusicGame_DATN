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
    [SerializeField] private Button nextButton;              // Kéo NextText / NextBUTTON vào đây
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
        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("[StorySceneController] Chưa cấu hình danh sách Stages trên Inspector!");
            return;
        }

        // 1. LẤY TÊN SCENE MỤC TIÊU ĐÃ ĐƯỢC CHỌN TỪ LOBBY
        string targetSceneFromLobby = PlayerPrefs.GetString("SelectedSceneToPlay", "");
        int indexFromLobby = PlayerPrefs.GetInt("SelectedStageIndex", 0);

        // 2. TỰ ĐỘNG TÌM ĐÚNG STAGE KHỚP THEO TÊN SCENE TRƯỚC (CHỐNG LỆCH INDEX)
        int matchedIndex = -1;
        if (!string.IsNullOrEmpty(targetSceneFromLobby))
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && !string.IsNullOrEmpty(stages[i].targetGameplayScene) &&
                    stages[i].targetGameplayScene.Trim().Equals(targetSceneFromLobby.Trim(), System.StringComparison.OrdinalIgnoreCase))
                {
                    matchedIndex = i;
                    break;
                }
            }
        }

        // Nếu tìm thấy khớp theo tên scene thì lấy, ngược lại mới fallback về index
        if (matchedIndex != -1)
        {
            currentStageIndex = matchedIndex;
        }
        else
        {
            currentStageIndex = Mathf.Clamp(indexFromLobby, 0, stages.Count - 1);
        }

        currentStage = stages[currentStageIndex];
        Debug.Log($"<color=green>[StorySceneController] Đang hiển thị Stage: {currentStage.stageTitle} -> Chuẩn bị vào: {currentStage.targetGameplayScene}</color>");

        // 3. Điền thông tin tiêu đề và cốt truyện
        if (stageTitleText != null) stageTitleText.text = currentStage.stageTitle;
        if (storyContentText != null) storyContentText.text = currentStage.storyContent;

        // 4. Khởi tạo hình nền và chạy vòng lặp đổi ảnh mượt
        InitBackgrounds();
        StartCoroutine(SmoothBackgroundLoopRoutine());

        // 5. Lắng nghe sự kiện bấm nút Next
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
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
        if (isTransitioning) return;
        isTransitioning = true;

        // 1. Xác định Scene đích: Ưu tiên targetGameplayScene của currentStage, dự phòng bằng PlayerPrefs
        string targetScene = "Tutorial";
        if (currentStage != null && !string.IsNullOrEmpty(currentStage.targetGameplayScene))
        {
            targetScene = currentStage.targetGameplayScene;
        }
        else
        {
            targetScene = PlayerPrefs.GetString("SelectedSceneToPlay", "Man1");
        }

        Debug.Log($"<color=yellow>>>> [CLICK OK] Đang chuyển vào Gameplay Scene: {targetScene}</color>");

        // 2. Chuyển scene có hiệu ứng Fade (nếu có FadePanel) hoặc nạp thẳng
        if (fadePanel != null)
        {
            StartCoroutine(LoadGameplaySceneRoutine(targetScene));
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    private IEnumerator LoadGameplaySceneRoutine(string targetScene)
    {
        fadePanel.gameObject.SetActive(true);
        float timer = 0f;
        while (timer < 0.4f)
        {
            timer += Time.deltaTime;
            fadePanel.alpha = Mathf.Clamp01(timer / 0.4f);
            yield return null;
        }

        SceneManager.LoadScene(targetScene);
    }
}