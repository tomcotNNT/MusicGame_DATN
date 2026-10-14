using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Quản lý màn chơi + hiển thị carousel chọn màn:
/// - Danh sách màn (Levels), kéo/snap/scale/sắp xếp card.
/// - Chơi xong màn nào thì tự tích done (PlayerPrefs) -> mở khóa màn sau.
/// - Màn khóa: nút Play giảm alpha và không bấm được.
///
/// Gắn vào đúng object trước đây gắn ListSongControl (object nhận kéo thả).
/// Ở scene gameplay chỉ cần gọi: SelectLevel.CompleteCurrentLevel();
/// </summary>
public class SelectLevel : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Serializable]
    public class LevelEntry
    {
        [Tooltip("ID duy nhất của màn, dùng để lưu tiến trình. Để trống sẽ dùng Scene Name.")]
        public string levelId;

        [Tooltip("Tên scene của màn này (phải có trong Build Settings).")]
        public string sceneName;

        [Tooltip("Card hiển thị của màn này.")]
        public SongItem item;

        [Tooltip("Luôn mở khóa, bất kể màn trước đã xong hay chưa.")]
        public bool unlockedByDefault;

        [Tooltip("Dấu tích hoàn thành (tuỳ chọn).")]
        public GameObject doneMark;
    }

    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Levels (theo thứ tự chơi)")]
    [SerializeField] private List<LevelEntry> levels = new List<LevelEntry>();

    [Header("Layout")]
    [SerializeField] private float spacing = 450f;
    [SerializeField] private float centerScale = 1f;
    [SerializeField] private float sideScale = 0.75f;
    [SerializeField] private float farScale = 0.5f;
    [SerializeField] private int visibleRange = 2;

    [Header("Snap")]
    [SerializeField] private float snapDuration = 0.25f;

    [Header("Drag")]
    [SerializeField] private float dragThreshold = 100f;

    [Header("Play Button & Lock")]
    [SerializeField] private Button playButton;

    [Tooltip("Màn đầu tiên trong list luôn được mở khóa.")]
    [SerializeField] private bool firstLevelAlwaysUnlocked = true;

    [Range(0f, 1f)]
    [SerializeField] private float lockedAlpha = 0.5f;
    [Range(0f, 1f)]
    [SerializeField] private float unlockedAlpha = 1f;

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
            if (levels == null || levels.Count == 0)
                return null;
            return levels[currentIndex].item;
        }
    }

    // =====================================================
    // STATIC API (gọi từ bất kỳ scene nào)
    // =====================================================

    private const string KeyPrefix = "LevelDone_";

    /// <summary>ID của màn đang chơi (được set khi bấm Play).</summary>
    public static string CurrentLevelId { get; private set; }

    /// <summary>Bắn ra mỗi khi tiến trình thay đổi.</summary>
    public static event Action OnProgressChanged;

    public static bool IsDone(string levelId)
    {
        return PlayerPrefs.GetInt(KeyPrefix + levelId, 0) == 1;
    }

    /// <summary>Gọi ở scene gameplay khi người chơi thắng.</summary>
    public static void CompleteCurrentLevel()
    {
        if (string.IsNullOrEmpty(CurrentLevelId))
        {
            Debug.LogWarning("LevelManager: CurrentLevelId rỗng, chưa vào màn từ scene chọn màn?");
            return;
        }

        CompleteLevel(CurrentLevelId);
    }

    public static void CompleteLevel(string levelId)
    {
        if (string.IsNullOrEmpty(levelId) || IsDone(levelId))
            return;

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
        if (levels == null || levels.Count == 0)
            return;

        RefreshLayout();
    }

    private void OnProgressChangedHandler()
    {
        UpdatePlayButtonState();
        UpdateDoneMarks();
    }

    // =====================================================
    // UNLOCK LOGIC
    // =====================================================

    public string GetLevelId(int index)
    {
        if (index < 0 || index >= levels.Count)
            return "Level_" + index;

        LevelEntry entry = levels[index];

        if (!string.IsNullOrEmpty(entry.levelId))
            return entry.levelId;

        if (!string.IsNullOrEmpty(entry.sceneName))
            return entry.sceneName;

        return "Level_" + index;
    }

    public bool IsUnlocked(int index)
    {
        if (index < 0 || index >= levels.Count)
            return false;

        if (levels[index].unlockedByDefault)
            return true;

        if (index == 0)
            return firstLevelAlwaysUnlocked;

        // Mở khóa khi màn ngay trước đã done
        return IsDone(GetLevelId(index - 1));
    }

    // =====================================================
    // DRAG
    // =====================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (levels.Count <= 1)
            return;

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
        if (!isDragging || levels.Count <= 1)
            return;

        float deltaX = eventData.position.x - dragStartPosition.x;
        UpdateDragLayout(deltaX);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging)
            return;

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

    // =====================================================
    // NEXT / PREVIOUS
    // =====================================================

    public void NextLevel()
    {
        if (levels.Count <= 1) return;

        currentIndex++;
        if (currentIndex >= levels.Count) currentIndex = 0;

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
    // LAYOUT
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
        UpdateDoneMarks();
        UpdateSiblingOrder();
    }

    private void SetCardActive(SongItem item, bool active)
    {
        SongCardEffect effect = item.GetComponent<SongCardEffect>();
        if (effect != null)
            effect.SetActiveCard(active);
    }

    // =====================================================
    // PLAY BUTTON (KHÓA / MỞ KHÓA)
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

    private void UpdateDoneMarks()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i].doneMark != null)
                levels[i].doneMark.SetActive(IsDone(GetLevelId(i)));
        }
    }

    public void OnPlayButtonClicked()
    {
        if (levels == null || levels.Count == 0) return;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayClickSound();

        if (!IsUnlocked(currentIndex))
        {
            Debug.Log("Màn chơi này đang bị khóa!");
            return;
        }

        string sceneToLoad = levels[currentIndex].sceneName;

        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogWarning($"SelectLevel: Màn {currentIndex} chưa điền Scene Name!", this);
            return;
        }

        // Ghi nhớ màn đang chơi để scene gameplay biết đánh dấu done màn nào
        CurrentLevelId = GetLevelId(currentIndex);

        SceneLoader.Load(sceneToLoad);         // <-- thay bằng dòng này
    }

    // =====================================================
    // HELPERS
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
        for (int i = 0; i < levels.Count; i++)
            order.Add(i);

        // Xa nhất ở dưới, gần giữa ở trên cùng
        order.Sort((a, b) =>
            Mathf.Abs(GetRelativeIndex(b)).CompareTo(Mathf.Abs(GetRelativeIndex(a))));

        for (int i = 0; i < order.Count; i++)
        {
            SongItem item = levels[order[i]].item;
            if (item != null)
                item.transform.SetSiblingIndex(i);
        }
    }

    // =====================================================
    // DEBUG
    // =====================================================

    [ContextMenu("Reset Progress")]
    public void ResetProgress()
    {
        for (int i = 0; i < levels.Count; i++)
            PlayerPrefs.DeleteKey(KeyPrefix + GetLevelId(i));

        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }

    [ContextMenu("Complete All")]
    private void CompleteAllDebug()
    {
        for (int i = 0; i < levels.Count; i++)
            PlayerPrefs.SetInt(KeyPrefix + GetLevelId(i), 1);

        PlayerPrefs.Save();
        OnProgressChanged?.Invoke();
    }
}