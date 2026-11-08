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
        public string stageID = "Man1";                     // Định danh: Tutorial, Man1, Man2, Man3...
        public string stageTitle = "1. NEW YORK CITY";
        [TextArea(2, 4)] public string storyContent = "Nội dung cốt truyện màn chơi...";
        public Sprite background1;
        public Sprite background2;
        public string targetGameplayScene = "Man1";         // Tên file Scene gameplay thực tế
    }

    [Header("1. UI Tham Chiếu")]
    [SerializeField] private Image backgroundA;              // Background (1)
    [SerializeField] private Image backgroundB;              // Background (2)
    [SerializeField] private TextMeshProUGUI stageTitleText; // TitleStoryText
    [SerializeField] private TextMeshProUGUI storyContentText;// StoryContextText
    [SerializeField] private Button nextButton;              // NextBUTTON
    [SerializeField] private CanvasGroup fadePanel;          // FadePanel

    [Header("2. Cài Đặt Chuyển Đổi Background")]
    [SerializeField] private float displayDuration = 4.0f;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("3. Dữ Liệu Các Màn Chơi")]
    [SerializeField] private List<StageData> stages = new List<StageData>();

    private int currentStageIndex = 0;
    private StageData currentStage;
    private bool isTransitioning = false;
    private Coroutine bgLoopCoroutine;

    private void Start()
    {
        if (stages == null || stages.Count == 0)
        {
            Debug.LogError("[StorySceneController] Danh sách Stages trên Inspector đang trống!");
            return;
        }

        // 1. ĐỌC DỮ LIỆU ĐƯỢC CHỌN TỪ LOBBY
        string targetSceneFromLobby = PlayerPrefs.GetString("SelectedSceneToPlay", "").Trim();
        int indexFromLobby = PlayerPrefs.GetInt("SelectedStageIndex", -1);

        Debug.Log($"[StorySceneController] Nhận từ Lobby -> Scene: '{targetSceneFromLobby}' | Index: {indexFromLobby}");

        // 2. TÌM KIẾM STAGE PHÙ HỢP
        currentStageIndex = ResolveStageIndex(targetSceneFromLobby, indexFromLobby);
        currentStage = stages[currentStageIndex];

        Debug.Log($"<color=cyan>[StorySceneController] Kích hoạt hiển thị: {currentStage.stageTitle} (Index: {currentStageIndex})</color>");

        // 3. HIỂN THỊ NỘI DUNG LÊN UI
        if (stageTitleText != null) stageTitleText.text = currentStage.stageTitle;
        if (storyContentText != null) storyContentText.text = currentStage.storyContent;

        // 4. KHỞI TẠO VÀ CHẠY CHUYỂN ẢNH NỀN
        InitBackgrounds();
        if (bgLoopCoroutine != null) StopCoroutine(bgLoopCoroutine);
        bgLoopCoroutine = StartCoroutine(SmoothBackgroundLoopRoutine());

        // 5. GẮN SỰ KIỆN NÚT NEXT
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    private int ResolveStageIndex(string targetScene, int lobbyIndex)
    {
        // Cách 1: So khớp trực tiếp theo tên file Scene gameplay
        if (!string.IsNullOrEmpty(targetScene))
        {
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && !string.IsNullOrEmpty(stages[i].targetGameplayScene))
                {
                    if (stages[i].targetGameplayScene.Trim().Equals(targetScene, System.StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }

            // Cách 1b: So khớp theo stageID hoặc stageTitle (phòng hờ targetGameplayScene bị điền nhầm)
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null)
                {
                    if (stages[i].stageID.Trim().Equals(targetScene, System.StringComparison.OrdinalIgnoreCase) ||
                        stages[i].stageTitle.ToLower().Contains(targetScene.ToLower()))
                    {
                        return i;
                    }
                }
            }
        }

        // Cách 2: Phân giải theo Index từ Lobby (nếu Index 0 ở Lobby là Màn 1 còn Element 0 của Story là Tutorial)
        if (lobbyIndex >= 0)
        {
            // Nếu Element 0 là Tutorial và lobby chọn Index 0 ứng với Màn 1 -> lấy phần tử số 1
            if (stages.Count > 1 && stages[0].targetGameplayScene.ToLower().Contains("tutorial") && lobbyIndex == 0 && targetScene.ToLower().Contains("man1"))
            {
                return 1;
            }

            if (lobbyIndex < stages.Count)
            {
                return lobbyIndex;
            }
        }

        // Fallback an toàn: nếu có Màn 1 (Element 1) thì trả về 1, ngược lại về 0
        return (stages.Count > 1) ? 1 : 0;
    }

    private void InitBackgrounds()
    {
        if (backgroundA != null)
        {
            backgroundA.sprite = currentStage.background1;
            backgroundA.color = Color.white;
        }

        if (backgroundB != null)
        {
            backgroundB.sprite = currentStage.background2;
            backgroundB.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    private IEnumerator SmoothBackgroundLoopRoutine()
    {
        // Nếu không có đủ 2 ảnh nền thì không cần chạy vòng lặp fade
        if (currentStage.background1 == null || currentStage.background2 == null) yield break;

        while (true)
        {
            yield return new WaitForSeconds(displayDuration);

            // Hiện dần B đè lên A
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

            // Làm mờ B để hiển thị lại A
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

        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Click);
        }

        // ƯU TIÊN TUYỆT ĐỐI: Lấy tên Scene đã lưu từ Lobby, nếu trống mới lấy từ data của Stage
        string targetScene = PlayerPrefs.GetString("SelectedSceneToPlay", "");
        if (string.IsNullOrEmpty(targetScene) || targetScene.ToLower().Contains("tutorial"))
        {
            targetScene = (currentStage != null && !string.IsNullOrEmpty(currentStage.targetGameplayScene))
                          ? currentStage.targetGameplayScene
                          : "Man1";
        }

        Debug.Log($"<color=yellow>[StorySceneController] Chuyển tiếp vào Gameplay: {targetScene}</color>");

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

    private void OnDestroy()
    {
        if (bgLoopCoroutine != null) StopCoroutine(bgLoopCoroutine);
    }
}