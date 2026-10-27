using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý màn chơi + carousel chọn màn:
/// - Mở khóa dựa trên tiến trình Tutorial và màn trước.
/// - Đồng bộ nút Play (khóa/mở) và quản lý tải Scene gameplay.
/// </summary>
public class SelectLevel : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("ID duy nhất của màn, dùng để lưu tiến trình. Để trống sẽ tự lấy sceneName.")]
        public string levelId;

        [Tooltip("Tên chính xác của file Scene trong Build Settings (VD: Man1, Man2, Man3).")]
        public string sceneName;

        [Tooltip("Card hiển thị của màn này trên Carousel.")]
        public SongItem item;

        [Tooltip("Luôn mở khóa màn này, bỏ qua mọi điều kiện.")]
        public bool unlockedByDefault = false;

        [Tooltip("Yêu cầu bắt buộc phải hoàn thành Tutorial trước khi mở màn này.")]
        public bool requiresTutorial = true;

        [Tooltip("Icon ổ khóa hiển thị trên card khi màn bị khóa (tùy chọn).")]
        public GameObject lockIcon;

        [Tooltip("Dấu tích hoàn thành khi đã vượt qua màn (tùy chọn).")]
        public GameObject doneMark;
    }

    // =====================================================
    // INSPECTOR SETTINGS
    // =====================================================

    [Header("Progression Rules (Quy tắc mở khóa)")]
    [Tooltip("Tích chọn: Hoàn thành Tutorial sẽ mở khóa ngay toàn bộ cả 3 màn chơi.\nBỏ tích: Mở khóa tuần tự từng màn một.")]
    [SerializeField] private bool unlockAllAfterTutorial = true;

    [Tooltip("Màn đầu tiên trong danh sách (Màn 1) luôn mở khóa nếu bỏ qua Tutorial.")]
    [SerializeField] private bool firstLevelAlwaysUnlocked = false;

    [Header("Character Select & Scene Routing")]
    [Tooltip("Tên scene chọn nhân vật. Để trống = vào thẳng màn chơi gameplay.")]
    [SerializeField] private string characterSelectScene = "SelectChar";

    [Tooltip("Bật nếu muốn bỏ qua bước chọn nhân vật để vào thẳng màn chơi khi bấm Play.")]
    [SerializeField] private bool skipCharacterSelect = false;

    [Header("Levels List (Theo thứ tự chơi)")]
    [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();

    [Header("Layout Settings")]
    [SerializeField] private float spacing = 450f;
    [SerializeField] private float centerScale = 1f;
    [SerializeField] private float sideScale = 0.75f;
    [SerializeField] private float farScale = 0.5f;
    [SerializeField] private int visibleRange = 2;

    [Header("Snap & Drag")]
    [SerializeField] private float snapDuration = 0.25f;
    [SerializeField] private float dragThreshold = 100f;

    [Header("Play Button & Visual Feedback")]
    [SerializeField] private Button playButton;
    [Range(0f, 1f)] [SerializeField] private float lockedAlpha = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float unlockedAlpha = 1f;

    // =====================================================
    // STATE
    // =====================================================

    private int currentIndex = 0;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private Coroutine snapCoroutine;

    public int CurrentIndex => currentIndex;

    public SongItem CurrentItem
    {
        get
        {
            if (levels == null || levels.Count == 0) return null;
            return levels[currentIndex].item;
        }
    }

    // =====================================================
    // STATIC PROGRESSION API
    // =====================================================

    public const string TutorialKey = "TutorialDone";
    private const string KeyPrefix = "LevelDone_";

    public static string CurrentLevelId { get; private set; }
    public static event Action OnProgressChanged;

    /// <summary>Kiểm tra trạng thái hoàn thành Tutorial.</summary>
    public static bool IsTutorialCompleted()
    {
        return PlayerPrefs.GetInt(TutorialKey, 0) == 1;
    }

    /// <summary>Gọi hàm này ở Scene Tutorial khi hoàn thành hướng dẫn.</summary>
    public static void SetTutorialDone(bool isDone = true)
    {
        PlayerPrefs.SetInt(TutorialKey, isDone ? 1 : 0);
        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    /// <summary>Kiểm tra màn chơi đã vượt qua chưa.</summary>
    public static bool IsDone(string levelId)
    {
        return PlayerPrefs.GetInt(KeyPrefix + levelId, 0) == 1;
    }

    /// <summary>Gọi tại scene gameplay khi người chơi chiến thắng.</summary>
    public static void CompleteCurrentLevel()
    {
        if (string.IsNullOrEmpty(CurrentLevelId))
        {
            Debug.LogWarning("SelectLevel: CurrentLevelId đang rỗng, không thể đánh dấu hoàn thành!");
            return;
        }

        CompleteLevel(CurrentLevelId);
    }

    public static void CompleteLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId) || IsDone(levelId)) return;

        PlayerPrefs.SetInt(KeyPrefix + levelId, 1);
        PlayerPrefs.Save();

        OnProgressChanged?.Invoke();
    }

    // =====================================================
    // LIFECYCLE
    // =====================================================

    private void OnEnable()
    {
        OnProgressChanged += OnProgressChangedHandler;
    }

    private void OnDisable()
    {
        OnProgressChanged -= OnProgressChangedHandler;
    }

    private void Start()
    {
        if (levels == null || levels.Count == 0) return;
        RefreshLayout();
    }

    private void Update()
    {
        // Phím nóng Debug hỗ trợ test nghiệm thu đồ án
        if (Input.GetKeyDown(KeyCode.G))
        {
            UnlockAllCheat();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetProgress();
        }
    }

    private void OnProgressChangedHandler()
    {
        UpdatePlayButtonState();
        UpdateCardLockAndDoneMarks();
    }

    // =====================================================
    // UNLOCK LOGIC
    // =====================================================

    public string GetLevelId(int index)
    {
        if (index < 0 || index >= levels.Count) return "Level_" + index;

        LevelEntry entry = levels[index];
        if (!string.IsNullOrEmpty(entry.levelId)) return entry.levelId;
        if (!string.IsNullOrEmpty(entry.sceneName)) return entry.sceneName;

        return "Level_" + index;
    }

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= levels.Count) return false;

        LevelEntry entry = levels[index];

        // 1. Luôn mở khóa nếu được đánh dấu ưu tiên
        if (entry.unlockedByDefault) return true;

        // 2. Chặn nếu màn yêu cầu Tutorial nhưng chưa hoàn thành
        if (entry.requiresTutorial && !IsTutorialCompleted()) return false;

        // 3. Nếu bật mở khóa tất cả các màn sau Tutorial
        if (unlockAllAfterTutorial && IsTutorialCompleted()) return true;

        // 4. Màn đầu tiên trong danh sách (nếu không bắt buộc Tutorial)
        if (index == 0) return firstLevelAlwaysUnlocked;

        // 5. Mở khóa tuần tự theo tiến độ màn trước đó
        return IsDone(GetLevelId(index - 1));
    }

    // =====================================================
    // DRAG & CAROUSEL INTERACTIONS
    // =====================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (levels.Count <= 1) return;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
            snapCoroutine = null;
        }

        isDragging = true;
        dragStartPosition = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || levels.Count <= 1) return;

        float deltaX = eventData.position.x - dragStartPosition.x;
        UpdateDragLayout(deltaX);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        float deltaX = eventData.position.x - dragStartPosition.x;

        if (Mathf.Abs(deltaX) >= dragThreshold)
        {
            if (deltaX < 0) NextLevel();
            else PreviousLevel();
        }
        else
        {
            SnapToCurrent();
        }
    }

    public void NextLevel()
    {
        if (levels.Count <= 1) return;
        currentIndex = (currentIndex + 1) % levels.Count;
        SnapToCurrent();
    }

    public void PreviousLevel()
    {
        if (levels.Count <= 1) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = levels.Count - 1;
        SnapToCurrent();
    }

    // =====================================================
    // LAYOUT CALCULATIONS
    // =====================================================

    private void UpdateDragLayout(float dragDelta)
    {
        if (levels.Count == 0) return;

        int closestIndex = 0;
        float minAbsX = float.MaxValue;

        for (int i = 0; i < levels.Count; i++)
        {
            float targetX = GetRelativeIndex(i) * spacing + dragDelta;
            if (Mathf.Abs(targetX) < minAbsX)
            {
                minAbsX = Mathf.Abs(targetX);
                closestIndex = i;
            }
        }

        for (int i = 0; i < levels.Count; i++)
        {
            SongItem item = levels[i].item;
            if (item == null) continue;

            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + dragDelta;

            item.SetPosition(targetX);
            float distance = Mathf.Abs(relativeIndex + dragDelta / spacing);
            item.SetScale(CalculateScale(distance));
            item.SetVisible(true);

            SetCardActive(item, i == closestIndex);
        }

        UpdatePlayButtonStateForIndex(closestIndex);
        UpdateSiblingOrder();
    }

    private void SnapToCurrent()
    {
        if (levels.Count == 0) return;
        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SnapAnimation());
    }

    private IEnumerator SnapAnimation()
    {
        List<Vector2> startPositions = new List<Vector2>();
        List<float> startScales = new List<float>();

        for (int i = 0; i < levels.Count; i++)
        {
            SongItem item = levels[i].item;
            startPositions.Add(item != null ? item.GetPosition() : Vector2.zero);
            startScales.Add(item != null ? item.GetScale() : 1f);
        }

        float timer = 0f;
        while (timer < snapDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(timer / snapDuration));

            for (int i = 0; i < levels.Count; i++)
            {
                SongItem item = levels[i].item;
                if (item == null) continue;

                int relativeIndex = GetRelativeIndex(i);
                float distance = Mathf.Abs(relativeIndex);

                float x = Mathf.Lerp(startPositions[i].x, relativeIndex * spacing, t);
                float scale = Mathf.Lerp(startScales[i], CalculateScale(distance), t);

                item.SetPosition(x);
                item.SetScale(scale);
                item.SetVisible(distance <= visibleRange);

                SetCardActive(item, relativeIndex == 0);
            }

            UpdatePlayButtonState();
            UpdateSiblingOrder();
            yield return null;
        }

        RefreshLayout();
        snapCoroutine = null;
    }

    private void RefreshLayout()
    {
        if (levels.Count == 0) return;

        for (int i = 0; i < levels.Count; i++)
        {
            SongItem item = levels[i].item;
            if (item == null) continue;

            int relativeIndex = GetRelativeIndex(i);

            item.SetPosition(relativeIndex * spacing);
            item.SetScale(CalculateScale(Mathf.Abs(relativeIndex)));
            item.SetVisible(true);

            SetCardActive(item, relativeIndex == 0);
        }

        UpdatePlayButtonState();
        UpdateCardLockAndDoneMarks();
        UpdateSiblingOrder();
    }

    private void SetCardActive(SongItem item, bool active)
    {
        SongCardEffect effect = item.GetComponent<SongCardEffect>();
        if (effect != null) effect.SetActiveCard(active);
    }

    // =====================================================
    // PLAY BUTTON & LOCK STATUS
    // =====================================================

    private void UpdatePlayButtonState()
    {
        UpdatePlayButtonStateForIndex(currentIndex);
    }

    private void UpdatePlayButtonStateForIndex(int index)
    {
        if (playButton == null) return;

        bool unlocked = IsUnlocked(index);
        float alpha = unlocked ? unlockedAlpha : lockedAlpha;

        playButton.interactable = unlocked;

        CanvasGroup group = playButton.GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = alpha;
        }
        else
        {
            Image img = playButton.GetComponent<Image>();
            if (img != null)
            {
                Color c = img.color;
                c.a = alpha;
                img.color = c;
            }
        }
    }

    private void UpdateCardLockAndDoneMarks()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            bool unlocked = IsUnlocked(i);
            bool done = IsDone(GetLevelId(i));

            if (levels[i].doneMark != null)
                levels[i].doneMark.SetActive(done);

            if (levels[i].lockIcon != null)
                levels[i].lockIcon.SetActive(!unlocked);
        }
    }

    public void OnPlayButtonClicked()
    {
        if (levels == null || levels.Count == 0) return;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayClickSound();

        if (!IsUnlocked(currentIndex))
        {
            Debug.Log("<color=yellow>[SelectLevel] Màn chơi này đang bị khóa!</color>");
            return;
        }

        string sceneToLoad = levels[currentIndex].sceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"[SelectLevel] Màn {currentIndex} chưa điền Scene Name!", this);
            return;
        }

        CurrentLevelId = GetLevelId(currentIndex);

        // Chuyển hướng sang màn chọn nhân vật hoặc vào thẳng màn chơi
        if (!skipCharacterSelect && !string.IsNullOrEmpty(characterSelectScene))
        {
            CharacterSelection.PendingScene = sceneToLoad;
            SceneLoader.Load(characterSelectScene);
        }
        else
        {
            SceneLoader.Load(sceneToLoad);
        }
    }

    // =====================================================
    // HELPERS & SIBLING ORDER
    // =====================================================

    private int GetRelativeIndex(int itemIndex)
    {
        int count = levels.Count;
        if (count <= 1) return 0;

        int difference = itemIndex - currentIndex;
        if (difference > count / 2) difference -= count;
        if (difference < -count / 2) difference += count;

        return difference;
    }

    private float CalculateScale(float distance)
    {
        if (distance <= 0.01f) return centerScale;
        if (distance <= 1f) return sideScale;
        return farScale;
    }

    private void UpdateSiblingOrder()
    {
        if (levels.Count == 0) return;

        List<int> order = new List<int>();
        for (int i = 0; i < levels.Count; i++) order.Add(i);

        order.Sort((a, b) => Mathf.Abs(GetRelativeIndex(b)).CompareTo(Mathf.Abs(GetRelativeIndex(a))));

        for (int i = 0; i < order.Count; i++)
        {
            SongItem item = levels[order[i]].item;
            if (item != null) item.transform.SetSiblingIndex(i);
        }
    }

    // =====================================================
    // DEBUG & CHEAT COMMANDS
    // =====================================================

    [ContextMenu("Unlock All (Cheat Key G)")]
    public void UnlockAllCheat()
    {
        PlayerPrefs.SetInt(TutorialKey, 1);
        for (int i = 0; i < levels.Count; i++)
        {
            PlayerPrefs.SetInt(KeyPrefix + GetLevelId(i), 1);
        }
        PlayerPrefs.Save();

        Debug.Log("<color=green>[CHEAT ACTIVE] Đã mở khóa Tutorial và toàn bộ các màn chơi!</color>");
        OnProgressChanged?.Invoke();
    }

    [ContextMenu("Reset Progress (Cheat Key R)")]
    public void ResetProgress()
    {
        PlayerPrefs.DeleteKey(TutorialKey);
        for (int i = 0; i < levels.Count; i++)
        {
            PlayerPrefs.DeleteKey(KeyPrefix + GetLevelId(i));
        }
        PlayerPrefs.Save();

        Debug.Log("<color=yellow>[RESET] Đã xóa toàn bộ tiến trình Tutorial và màn chơi!</color>");
        OnProgressChanged?.Invoke();
    }
}