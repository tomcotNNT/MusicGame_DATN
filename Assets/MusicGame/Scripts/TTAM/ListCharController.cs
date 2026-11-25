using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Thêm namespace để dùng Button và Image
using UnityEngine.SceneManagement;
public class ListCharController : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Icon khóa (thứ tự giống Song Items)")]
    [Tooltip("Kéo icon ổ khóa có sẵn của từng nhân vật vào, đúng thứ tự")]
    [SerializeField] private List<GameObject> lockIcons = new List<GameObject>();
    [SerializeField] private Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    // Ảnh của từng card (tự lấy) + màu gốc để trả lại khi mở khóa
    private readonly List<List<Image>> cardImages = new List<List<Image>>();
    private readonly List<List<Color>> cardColors = new List<List<Color>>();

    [Header("Song Items")]
    [SerializeField] private List<SongItem> songItems = new List<SongItem>();

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

    [Header("UI Play Button & Lock Settings")]
    [SerializeField] private Button playButton; // Kéo nút Play vào đây trên Inspector

    [Tooltip("Chỉ dùng khi không có dữ liệu màn chơi từ scene chọn màn")]
    [SerializeField] private string gameplaySceneName = "GameplayScene";

    private int currentIndex = 0;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private Coroutine snapCoroutine;

    public int CurrentIndex => currentIndex;

    public SongItem CurrentSong
    {
        get
        {
            if (songItems == null || songItems.Count == 0)
                return null;
            return songItems[currentIndex];
        }
    }

    private void Start()
    {
        // Màn chơi đã chọn từ scene Level Select
        Debug.Log("Select Char nhận Level ID: " + SelectedLevelData.LevelId +
                  " | Scene: " + SelectedLevelData.SceneName);

        // Tự gắn sự kiện cho nút Play (không cần kéo OnClick trong Inspector)
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
            playButton.onClick.AddListener(OnPlayButtonClicked);
        }
        else
        {
            Debug.LogError("[ListChar] Chưa kéo Play Button vào Inspector!");
        }

        if (songItems == null || songItems.Count == 0)
        {
            Debug.LogError("[ListChar] Danh sách nhân vật (songItems) đang trống!");
            return;
        }

        // Trạng thái khóa/mở chỉ đọc từ PlayerData.json (mở/khóa bằng Admin hoặc khi mua)
        SetupLockVisuals();

        // Tự cập nhật ổ khóa khi nhân vật được mở/khóa
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnProgressionChanged += RefreshLockVisuals;

        // Mở lại nhân vật đã chọn lần trước (nếu còn hợp lệ)
        int savedChar = SelectedLevelData.CharacterIndex;
        if (savedChar >= 0 && savedChar < songItems.Count && IsCharacterUnlocked(savedChar))
            currentIndex = savedChar;

        RefreshLayout(true);
        RefreshLockVisuals();
    }
    [Header("Scene Navigation")]
    [SerializeField] private string selectSongSceneName = "SelectSong"; // Tên scene sảnh chọn bài

    // Hàm gọi khi bấm nút mũi tên quay lại (Back)
    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnProgressionChanged -= RefreshLockVisuals;

        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayButtonClicked);
    }

    public void OnBackButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Quay trở lại màn hình chọn bài hát
        SceneFader.Load(selectSongSceneName);   // tối dần rồi chuyển scene
    }

    // =========================================================
    // DRAG
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (songItems.Count <= 1)
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
        if (!isDragging || songItems.Count <= 1)
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
            if (deltaX < 0) NextSong();
            else PreviousSong();
        }
        else
        {
            SnapToCurrentSong();
        }
    }

    // =========================================================
    // NEXT / PREVIOUS
    // =========================================================

    public void NextSong()
    {
        if (songItems.Count <= 1) return;
        currentIndex++;
        if (currentIndex >= songItems.Count) currentIndex = 0;
        SnapToCurrentSong();
    }

    public void PreviousSong()
    {
        if (songItems.Count <= 1) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = songItems.Count - 1;
        SnapToCurrentSong();
    }

    // =========================================================
    // DRAG LAYOUT
    // =========================================================

    private void UpdateDragLayout(float dragDelta)
    {
        if (songItems.Count == 0) return;

        float offset = dragDelta;
        int closestIndex = 0;
        float minAbsX = float.MaxValue;

        for (int i = 0; i < songItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + offset;

            if (Mathf.Abs(targetX) < minAbsX)
            {
                minAbsX = Mathf.Abs(targetX);
                closestIndex = i;
            }
        }

        for (int i = 0; i < songItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + offset;
            songItems[i].SetPosition(targetX);

            float distance = Mathf.Abs(relativeIndex + offset / spacing);
            float scale = CalculateScale(distance);

            songItems[i].SetScale(scale);
            songItems[i].SetVisible(true);

            SongCardEffect effect = songItems[i].GetComponent<SongCardEffect>();
            if (effect != null)
            {
                effect.SetActiveCard(i == closestIndex);
            }
        }

        UpdatePlayButtonStateForIndex(closestIndex);
        UpdateSiblingOrder();
    }

    // =========================================================
    // SNAP
    // =========================================================

    private void SnapToCurrentSong()
    {
        if (songItems.Count == 0) return;

        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SnapAnimation());
    }

    private IEnumerator SnapAnimation()
    {
        float duration = snapDuration;
        List<Vector2> startPositions = new List<Vector2>();
        List<float> startScales = new List<float>();

        for (int i = 0; i < songItems.Count; i++)
        {
            startPositions.Add(songItems[i].GetPosition());
            startScales.Add(songItems[i].GetScale());
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < songItems.Count; i++)
            {
                int relativeIndex = GetRelativeIndex(i);
                float targetX = relativeIndex * spacing;
                float distance = Mathf.Abs(relativeIndex);
                float targetScale = CalculateScale(distance);

                float x = Mathf.Lerp(startPositions[i].x, targetX, t);
                float scale = Mathf.Lerp(startScales[i], targetScale, t);

                songItems[i].SetPosition(x);
                songItems[i].SetScale(scale);
                songItems[i].SetVisible(distance <= visibleRange);

                SongCardEffect effect = songItems[i].GetComponent<SongCardEffect>();
                if (effect != null)
                {
                    effect.SetActiveCard(relativeIndex == 0);
                }
            }

            UpdatePlayButtonState();
            UpdateSiblingOrder();
            yield return null;
        }

        RefreshLayout(false);
        snapCoroutine = null;
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshLayout(bool instant)
    {
        if (songItems.Count == 0) return;

        for (int i = 0; i < songItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float x = relativeIndex * spacing;
            float scale = CalculateScale(Mathf.Abs(relativeIndex));

            songItems[i].SetPosition(x);
            songItems[i].SetScale(scale);
            songItems[i].SetVisible(true);

            SongCardEffect effect = songItems[i].GetComponent<SongCardEffect>();
            if (effect != null)
            {
                effect.SetActiveCard(relativeIndex == 0);
            }
        }

        UpdatePlayButtonState();
        UpdateSiblingOrder();
    }

    // =========================================================
    // UPDATE PLAY BUTTON STATE (KHÓA / MỞ KHÓA MÀN CHƠI)
    // =========================================================

    private void UpdatePlayButtonState()
    {
        UpdatePlayButtonStateForIndex(currentIndex);
    }

    private void UpdatePlayButtonStateForIndex(int index)
    {
        if (playButton == null) return;

        // Đọc trạng thái mở khóa nhân vật từ file JSON
        bool isUnlocked = IsCharacterUnlocked(index);

        playButton.interactable = isUnlocked; // Cho phép hoặc chặn bấm nút

        // Làm mờ nhẹ nút Play nếu màn chơi bị khóa (Alpha = 0.5)
        CanvasGroup canvasGroup = playButton.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isUnlocked ? 1f : 0.5f;
        }
        else
        {
            Image btnImg = playButton.GetComponent<Image>();
            if (btnImg != null)
            {
                Color c = btnImg.color;
                c.a = isUnlocked ? 1f : 0.5f;
                btnImg.color = c;
            }
        }
    }

    // =========================================================
    // PLAY BUTTON CLICKED
    // =========================================================

    public void OnPlayButtonClicked()
    {
        Debug.Log("[ListChar] PLAY CLICKED - nhân vật index: " + currentIndex);

        if (songItems == null || songItems.Count == 0)
        {
            Debug.LogError("[ListChar] Danh sách nhân vật trống!");
            return;
        }

        // Kiểm tra an toàn: Nếu màn hiện tại chưa mở khóa thì chặn không cho vào game
        if (!IsCharacterUnlocked(currentIndex))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayClickSound();
            }
            Debug.LogWarning("[ListChar] Nhân vật index " + currentIndex + " đang bị khóa!");
            // (Tùy chọn: Bạn có thể bật một bảng thông báo Pop-up "Coming Soon / Locked" ở đây)
            return;
        }

        // Nếu đã mở khóa (Màn 1) thì tiến hành vào game bình thường
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Lưu nhân vật đã chọn
        string charName = songItems[currentIndex] != null ? songItems[currentIndex].gameObject.name : "";
        SelectedLevelData.SetCharacter(currentIndex, charName);
        Debug.Log("[ListChar] Đã lưu nhân vật: index " + currentIndex + " - tên card: '" + charName + "'");

        // Vào đúng màn đã chọn ở scene Level Select
        string sceneToLoad = SelectedLevelData.SceneName;
        if (string.IsNullOrEmpty(sceneToLoad))
            sceneToLoad = gameplaySceneName;

        Debug.Log("[ListChar] Vào Level " + SelectedLevelData.LevelId + " (" + sceneToLoad +
                  ") với nhân vật index " + currentIndex);

        if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
        {
            Debug.LogError("[ListChar] Không load được scene '" + sceneToLoad +
                           "'. Kiểm tra tên scene (sceneName trong Level Select) và đã thêm vào File > Build Settings chưa.");
            return;
        }

        // Đang chuyển scene (bấm đúp) -> không trừ thêm lượt
        if (SceneFader.IsLoading)
            return;

        // Trừ 1 lượt chơi (lưu vào PlayerData.json). Hết lượt thì không vào màn
        if (PlayerDataManager.Instance != null && !PlayerDataManager.Instance.UseTurn())
        {
            Debug.LogWarning("[ListChar] Hết lượt chơi, không thể vào màn!");
            return;
        }

        SceneFader.Load(sceneToLoad);   // tối dần rồi chuyển scene
    }

    // =========================================================
    // HELPER METHODS
    // =========================================================

    // =========================================================
    // Ổ KHÓA + ẢNH XÁM
    // =========================================================

    private void SetupLockVisuals()
    {
        cardImages.Clear();
        cardColors.Clear();

        for (int i = 0; i < songItems.Count; i++)
        {
            GameObject lockIcon = i < lockIcons.Count ? lockIcons[i] : null;
            List<Image> images = new List<Image>();
            List<Color> colors = new List<Color>();

            if (songItems[i] != null)
            {
                // Lấy mọi ảnh trong card, bỏ qua ảnh của icon khóa
                foreach (Image img in songItems[i].GetComponentsInChildren<Image>(true))
                {
                    if (lockIcon != null && img.transform.IsChildOf(lockIcon.transform))
                        continue;

                    images.Add(img);
                    colors.Add(img.color);
                }
            }

            cardImages.Add(images);
            cardColors.Add(colors);
        }
    }

    private void RefreshLockVisuals()
    {
        for (int i = 0; i < songItems.Count; i++)
        {
            bool locked = !IsCharacterUnlocked(i);

            // Bật icon khóa khi bị khóa, tắt khi đã mở
            if (i < lockIcons.Count && lockIcons[i] != null)
                lockIcons[i].SetActive(locked);

            // Làm xám ảnh khi bị khóa
            if (i >= cardImages.Count) continue;

            for (int j = 0; j < cardImages[i].Count; j++)
            {
                Image img = cardImages[i][j];
                if (img == null) continue;

                Color original = cardColors[i][j];
                img.color = locked
                    ? new Color(original.r * lockedColor.r, original.g * lockedColor.g,
                                original.b * lockedColor.b, original.a)
                    : original;
            }
        }

        UpdatePlayButtonState();
    }

    private bool IsCharacterUnlocked(int index)
    {
        // Không có PlayerDataManager (test thẳng scene): chỉ nhân vật 0 được mở
        if (PlayerDataManager.Instance == null)
            return index == 0;

        return PlayerDataManager.Instance.IsCharacterUnlocked(index);
    }

    private int GetRelativeIndex(int itemIndex)
    {
        int count = songItems.Count;
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

    private bool IsVisible(int relativeIndex)
    {
        return Mathf.Abs(relativeIndex) <= visibleRange;
    }

    private void UpdateSiblingOrder()
    {
        if (songItems.Count == 0) return;

        List<SongItem> sortedItems = new List<SongItem>(songItems);
        sortedItems.Sort((a, b) =>
        {
            int indexA = songItems.IndexOf(a);
            int indexB = songItems.IndexOf(b);
            float distanceA = Mathf.Abs(GetRelativeIndex(indexA));
            float distanceB = Mathf.Abs(GetRelativeIndex(indexB));
            return distanceB.CompareTo(distanceA);
        });

        for (int i = 0; i < sortedItems.Count; i++)
        {
            sortedItems[i].transform.SetSiblingIndex(i);
        }
    }
}