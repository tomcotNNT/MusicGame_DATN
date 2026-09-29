using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
            if (deltaX < 0)
            {
                NextSong();
            }
            else
            {
                PreviousSong();
            }
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
        if (songItems.Count <= 1)
            return;

        currentIndex++;

        // Vòng lại bài đầu
        if (currentIndex >= songItems.Count)
        {
            currentIndex = 0;
        }

        SnapToCurrentSong();
    }

    public void PreviousSong()
    {
        if (songItems.Count <= 1)
            return;

        currentIndex--;

        // Vòng lại bài cuối
        if (currentIndex < 0)
        {
            currentIndex = songItems.Count - 1;
        }

        SnapToCurrentSong();
    }

    // =========================================================
    // DRAG LAYOUT
    // =========================================================

    private void UpdateDragLayout(float dragDelta)
    {
        if (songItems.Count == 0)
            return;

        float offset = dragDelta;

        for (int i = 0; i < songItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);

            // Mỗi item cách nhau đúng spacing
            float targetX = relativeIndex * spacing + offset;

            songItems[i].SetPosition(targetX);

            float distance = Mathf.Abs(
                relativeIndex + offset / spacing
            );

            float scale = CalculateScale(distance);

            songItems[i].SetScale(scale);

            songItems[i].SetVisible(true);
        }

        UpdateSiblingOrder();
    }

    // =========================================================
    // SNAP
    // =========================================================

    private void SnapToCurrentSong()
    {
        if (songItems.Count == 0)
            return;

        if (snapCoroutine != null)
        {
            StopCoroutine(snapCoroutine);
        }

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

            // Smooth easing
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < songItems.Count; i++)
            {
                int relativeIndex = GetRelativeIndex(i);

                float targetX = relativeIndex * spacing;

                float distance = Mathf.Abs(relativeIndex);

                float targetScale = CalculateScale(distance);

                float x = Mathf.Lerp(
                    startPositions[i].x,
                    targetX,
                    t
                );

                float scale = Mathf.Lerp(
                    startScales[i],
                    targetScale,
                    t
                );

                songItems[i].SetPosition(x);
                songItems[i].SetScale(scale);

                songItems[i].SetVisible(
                    distance <= visibleRange
                );
            }

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
        if (songItems.Count == 0)
            return;

        for (int i = 0; i < songItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);

            float x = relativeIndex * spacing;

            float scale = CalculateScale(
                Mathf.Abs(relativeIndex)
            );

            songItems[i].SetPosition(x);
            songItems[i].SetScale(scale);

            songItems[i].SetVisible(true);
        }

        UpdateSiblingOrder();
    }

    // =========================================================
    // RELATIVE INDEX
    // =========================================================

    private int GetRelativeIndex(int itemIndex)
    {
        int count = songItems.Count;

        if (count <= 1)
            return 0;

        int difference = itemIndex - currentIndex;

        // Vòng tròn
        if (difference > count / 2)
            difference -= count;

        if (difference < -count / 2)
            difference += count;

        return difference;
    }

    // =========================================================
    // SCALE
    // =========================================================

    private float CalculateScale(float distance)
    {
        if (distance <= 0.01f)
        {
            return centerScale;
        }

        if (distance <= 1f)
        {
            return sideScale;
        }

        return farScale;
    }

    // =========================================================
    // VISIBILITY
    // =========================================================

    private bool IsVisible(int relativeIndex)
    {
        return Mathf.Abs(relativeIndex) <= visibleRange;
    }

    // =========================================================
    // SIBLING ORDER
    // =========================================================

    private void UpdateSiblingOrder()
    {
        if (songItems.Count == 0)
            return;

        // Đưa các item xa ra phía sau trước
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