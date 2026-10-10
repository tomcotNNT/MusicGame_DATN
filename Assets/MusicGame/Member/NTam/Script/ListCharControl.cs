using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ListCharControl : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Character Items")]
    [SerializeField] private List<SongItem> charItems = new List<SongItem>(); // Tận dụng component SongItem cho thẻ nhân vật

    [Header("Layout Settings")]
    [SerializeField] private float spacing = 450f;
    [SerializeField] private float centerScale = 1f;
    [SerializeField] private float sideScale = 0.75f;
    [SerializeField] private float farScale = 0.5f;
    [SerializeField] private int visibleRange = 2;

    [Header("Snap & Drag")]
    [SerializeField] private float snapDuration = 0.25f;
    [SerializeField] private float dragThreshold = 100f;

    [Header("UI Buttons & Scenes")]
    [SerializeField] private Button playButton;                     // Nút Play màu hồng ở giữa
    [SerializeField] private Button returnButton;                   // Nút mũi tên Return (quay lại) ở góc trái
    [SerializeField] private string gameplaySceneName = "GameplayScene"; // Tên scene vào chơi game
    [SerializeField] private string selectSongSceneName = "SelectSong1"; // Tên scene quay lại chọn bài

    private int currentIndex = 0;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private Coroutine snapCoroutine;

    private void Start()
    {
        if (charItems == null || charItems.Count == 0)
            return;

        RefreshLayout(true);
    }

    // =========================================================
    // DRAG HANDLERS
    // =========================================================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (charItems.Count <= 1) return;

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
        if (!isDragging || charItems.Count <= 1) return;

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
            if (deltaX < 0) NextChar();
            else PreviousChar();
        }
        else
        {
            SnapToCurrentChar();
        }
    }

    public void NextChar()
    {
        if (charItems.Count <= 1) return;
        currentIndex++;
        if (currentIndex >= charItems.Count) currentIndex = 0;
        SnapToCurrentChar();
    }

    public void PreviousChar()
    {
        if (charItems.Count <= 1) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = charItems.Count - 1;
        SnapToCurrentChar();
    }

    // =========================================================
    // LAYOUT & SNAP LOGIC
    // =========================================================

    private void UpdateDragLayout(float dragDelta)
    {
        if (charItems.Count == 0) return;

        float offset = dragDelta;
        int closestIndex = 0;
        float minAbsX = float.MaxValue;

        for (int i = 0; i < charItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + offset;

            if (Mathf.Abs(targetX) < minAbsX)
            {
                minAbsX = Mathf.Abs(targetX);
                closestIndex = i;
            }
        }

        for (int i = 0; i < charItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + offset;
            charItems[i].SetPosition(targetX);

            float distance = Mathf.Abs(relativeIndex + offset / spacing);
            float scale = CalculateScale(distance);

            charItems[i].SetScale(scale);
            charItems[i].SetVisible(true);

            SongCardEffect effect = charItems[i].GetComponent<SongCardEffect>();
            if (effect != null)
            {
                effect.SetActiveCard(i == closestIndex);
            }
        }

        UpdatePlayButtonStateForIndex(closestIndex);
        UpdateSiblingOrder();
    }

    private void SnapToCurrentChar()
    {
        if (charItems.Count == 0) return;

        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SnapAnimation());
    }

    private IEnumerator SnapAnimation()
    {
        float duration = snapDuration;
        List<Vector2> startPositions = new List<Vector2>();
        List<float> startScales = new List<float>();

        for (int i = 0; i < charItems.Count; i++)
        {
            startPositions.Add(charItems[i].GetPosition());
            startScales.Add(charItems[i].GetScale());
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < charItems.Count; i++)
            {
                int relativeIndex = GetRelativeIndex(i);
                float targetX = relativeIndex * spacing;
                float distance = Mathf.Abs(relativeIndex);
                float targetScale = CalculateScale(distance);

                float x = Mathf.Lerp(startPositions[i].x, targetX, t);
                float scale = Mathf.Lerp(startScales[i], targetScale, t);

                charItems[i].SetPosition(x);
                charItems[i].SetScale(scale);
                charItems[i].SetVisible(distance <= visibleRange);

                SongCardEffect effect = charItems[i].GetComponent<SongCardEffect>();
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

    private void RefreshLayout(bool instant)
    {
        if (charItems.Count == 0) return;

        for (int i = 0; i < charItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float x = relativeIndex * spacing;
            float scale = CalculateScale(Mathf.Abs(relativeIndex));

            charItems[i].SetPosition(x);
            charItems[i].SetScale(scale);
            charItems[i].SetVisible(true);

            SongCardEffect effect = charItems[i].GetComponent<SongCardEffect>();
            if (effect != null)
            {
                effect.SetActiveCard(relativeIndex == 0);
            }
        }

        UpdatePlayButtonState();
        UpdateSiblingOrder();
    }

    // =========================================================
    // KHÓA / MỞ KHÓA NÚT PLAY THEO NHÂN VẬT
    // =========================================================

    private void UpdatePlayButtonState()
    {
        UpdatePlayButtonStateForIndex(currentIndex);
    }

    private void UpdatePlayButtonStateForIndex(int index)
    {
        if (playButton == null) return;

        // TẠM THỜI: Chỉ mở khóa nhân vật đầu tiên (Index 0), các nhân vật từ Index 1 trở đi bị khóa
        bool isUnlocked = (index == 0);

        playButton.interactable = isUnlocked;

        // Làm mờ nút Play nếu nhân vật bị khóa (Alpha = 0.5)
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
    // SỰ KIỆN NÚT BẤM (BUTTON CLICK)
    // =========================================================

    public void OnPlayButtonClicked()
    {
        if (currentIndex > 0)
        {
            Debug.Log("Nhân vật này đang bị khóa!");
            return; // Chặn không cho vào game nếu chưa mở khóa
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnReturnButtonClicked()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }

        SceneManager.LoadScene(selectSongSceneName); // Quay lại màn hình chọn bài
    }

    // =========================================================
    // HELPER METHODS
    // =========================================================

    private int GetRelativeIndex(int itemIndex)
    {
        int count = charItems.Count;
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
        if (charItems.Count == 0) return;

        List<SongItem> sortedItems = new List<SongItem>(charItems);
        sortedItems.Sort((a, b) =>
        {
            int index_a = charItems.IndexOf(a);
            int index_b = charItems.IndexOf(b);
            float distanceA = Mathf.Abs(GetRelativeIndex(index_a));
            float distanceB = Mathf.Abs(GetRelativeIndex(index_b));
            return distanceB.CompareTo(distanceA);
        });

        for (int i = 0; i < sortedItems.Count; i++)
        {
            sortedItems[i].transform.SetSiblingIndex(i);
        }
    }
}