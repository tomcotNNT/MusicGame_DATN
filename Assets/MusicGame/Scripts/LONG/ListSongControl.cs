using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Thêm namespace để dùng Button và Image
using UnityEngine.SceneManagement;
public class ListSongControl : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
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
        if (songItems == null || songItems.Count == 0)
            return;

        RefreshLayout(true);
    }
    [Header("Scene Navigation")]
    [SerializeField] private string selectSongSceneName = "SelectSong1"; // Tên scene sảnh chọn bài

    // Hàm gọi khi bấm nút mũi tên quay lại (Back)
    public void OnBackButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        // Quay trở lại màn hình chọn bài hát
        SceneManager.LoadScene(selectSongSceneName);
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

        // TẠM THỜI: Chỉ mở khóa màn đầu tiên (Index 0 tương ứng với Màn 1), các màn từ Index 1 trở đi bị khóa
        bool isUnlocked = (index == 0);

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
        if (songItems == null || songItems.Count == 0) return;

        // Kiểm tra an toàn: Nếu màn hiện tại chưa mở khóa thì chặn không cho vào game
        if (currentIndex > 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayClickSound();
            }
            Debug.Log("Màn chơi này đang bị khóa!");
            // (Tùy chọn: Bạn có thể bật một bảng thông báo Pop-up "Coming Soon / Locked" ở đây)
            return;
        }

        // Nếu đã mở khóa (Màn 1) thì tiến hành vào game bình thường
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
    }

    // =========================================================
    // HELPER METHODS
    // =========================================================

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