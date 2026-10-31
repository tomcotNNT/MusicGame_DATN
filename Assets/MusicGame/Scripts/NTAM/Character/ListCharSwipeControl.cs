using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ListCharSwipeControl : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Danh Sách Thẻ Nhân Vật (Char_01, Char_02...)")]
    [SerializeField] private List<RectTransform> charItems = new List<RectTransform>();

    [Header("Khoảng Cách & Tỉ Lệ Thu Phóng")]
    [SerializeField] private float spacing = 320f;
    [SerializeField] private float centerScale = 1.0f;
    [SerializeField] private float sideScale = 0.8f;
    [SerializeField] private float farScale = 0.6f;
    [SerializeField] private int visibleRange = 2;

    [Header("Hiệu Ứng Hít (Snap)")]
    [SerializeField] private float snapDuration = 0.2f;
    [SerializeField] private float dragThreshold = 80f;

    [Header("Kết Nối Logic Quản Lý")]
    [SerializeField] private CharacterSystemManager characterManager;

    private int currentIndex = 0;
    private Vector2 dragStartPosition;
    private bool isDragging = false;
    private Coroutine snapCoroutine;

    public int CurrentIndex => currentIndex;
    public bool CanSwipe { get; set; } = true;

    private void Start()
    {
        if (charItems == null || charItems.Count == 0) return;
        RefreshLayout();
        NotifyManager(currentIndex);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanSwipe || charItems.Count <= 1) return;

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
        if (!isDragging || !CanSwipe || charItems.Count <= 1) return;

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
        if (!CanSwipe || charItems.Count <= 1) return;
        currentIndex++;
        if (currentIndex >= charItems.Count) currentIndex = 0;
        SnapToCurrentChar();
    }

    public void PreviousChar()
    {
        if (!CanSwipe || charItems.Count <= 1) return;
        currentIndex--;
        if (currentIndex < 0) currentIndex = charItems.Count - 1;
        SnapToCurrentChar();
    }

    private void UpdateDragLayout(float dragDelta)
    {
        int closestIndex = 0;
        float minAbsX = float.MaxValue;

        for (int i = 0; i < charItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float targetX = relativeIndex * spacing + dragDelta;

            if (Mathf.Abs(targetX) < minAbsX)
            {
                minAbsX = Mathf.Abs(targetX);
                closestIndex = i;
            }

            charItems[i].anchoredPosition = new Vector2(targetX, charItems[i].anchoredPosition.y);
            float distance = Mathf.Abs(relativeIndex + dragDelta / spacing);
            charItems[i].localScale = Vector3.one * CalculateScale(distance);
        }

        NotifyManager(closestIndex);
        UpdateSiblingOrder();
    }

    private void SnapToCurrentChar()
    {
        if (snapCoroutine != null) StopCoroutine(snapCoroutine);
        snapCoroutine = StartCoroutine(SnapAnimation());
    }

    private IEnumerator SnapAnimation()
    {
        float timer = 0f;
        List<Vector2> startPos = new List<Vector2>();
        List<float> startScales = new List<float>();

        for (int i = 0; i < charItems.Count; i++)
        {
            startPos.Add(charItems[i].anchoredPosition);
            startScales.Add(charItems[i].localScale.x);
        }

        while (timer < snapDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / snapDuration);

            for (int i = 0; i < charItems.Count; i++)
            {
                int relIndex = GetRelativeIndex(i);
                float targetX = relIndex * spacing;
                float targetScale = CalculateScale(Mathf.Abs(relIndex));

                charItems[i].anchoredPosition = new Vector2(Mathf.Lerp(startPos[i].x, targetX, t), charItems[i].anchoredPosition.y);
                charItems[i].localScale = Vector3.one * Mathf.Lerp(startScales[i], targetScale, t);
            }

            UpdateSiblingOrder();
            yield return null;
        }

        RefreshLayout();
        NotifyManager(currentIndex);
        snapCoroutine = null;
    }

    private void RefreshLayout()
    {
        for (int i = 0; i < charItems.Count; i++)
        {
            int relIndex = GetRelativeIndex(i);
            charItems[i].anchoredPosition = new Vector2(relIndex * spacing, charItems[i].anchoredPosition.y);
            charItems[i].localScale = Vector3.one * CalculateScale(Mathf.Abs(relIndex));
            charItems[i].gameObject.SetActive(Mathf.Abs(relIndex) <= visibleRange);
        }
        UpdateSiblingOrder();
    }

    private void NotifyManager(int index)
    {
        if (characterManager != null)
        {
            characterManager.OnCharacterSwiped(index);
        }
    }

    private int GetRelativeIndex(int itemIndex)
    {
        int count = charItems.Count;
        if (count <= 1) return 0;
        int diff = itemIndex - currentIndex;
        if (diff > count / 2) diff -= count;
        if (diff < -count / 2) diff += count;
        return diff;
    }

    private float CalculateScale(float distance)
    {
        if (distance <= 0.01f) return centerScale;
        if (distance <= 1f) return sideScale;
        return farScale;
    }

    private void UpdateSiblingOrder()
    {
        List<RectTransform> sorted = new List<RectTransform>(charItems);
        sorted.Sort((a, b) =>
        {
            float distA = Mathf.Abs(GetRelativeIndex(charItems.IndexOf(a)));
            float distB = Mathf.Abs(GetRelativeIndex(charItems.IndexOf(b)));
            return distB.CompareTo(distA);
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].SetSiblingIndex(i);
        }
    }
}