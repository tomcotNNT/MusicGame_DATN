using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ListChar : MonoBehaviour,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler
{
    [Header("Character Items")]
    [SerializeField] private List<SongItem> characterItems = new List<SongItem>();

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

    [Header("Play")]
    [SerializeField] private Button playButton;

    [Tooltip("Scene mặc định nếu chưa chọn scene ở Select Song")]
    [SerializeField] private string defaultSceneName = "GameplayScene";

    private int currentIndex;
    private Vector2 dragStartPosition;
    private bool isDragging;
    private Coroutine snapCoroutine;

    public int CurrentIndex => currentIndex;

    private void Start()
    {
        if (characterItems == null || characterItems.Count == 0)
        {
            Debug.LogWarning("ListCharControl: Chưa gán Character Items.");
            return;
        }

        currentIndex = 0;
        RefreshLayout();

        if (playButton != null)
            playButton.interactable = true;
    }

    // =========================
    // DRAG
    // =========================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (characterItems.Count <= 1)
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
        if (!isDragging || characterItems.Count <= 1)
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
                NextCharacter();
            else
                PreviousCharacter();
        }
        else
        {
            SnapToCurrentCharacter();
        }
    }

    // =========================
    // NEXT / PREVIOUS
    // =========================

    public void NextCharacter()
    {
        if (characterItems.Count <= 1)
            return;

        currentIndex = (currentIndex + 1) % characterItems.Count;
        SnapToCurrentCharacter();
    }

    public void PreviousCharacter()
    {
        if (characterItems.Count <= 1)
            return;

        currentIndex--;

        if (currentIndex < 0)
            currentIndex = characterItems.Count - 1;

        SnapToCurrentCharacter();
    }

    // =========================
    // LAYOUT
    // =========================

    private void UpdateDragLayout(float deltaX)
    {
        int closestIndex = currentIndex;
        float minDistance = float.MaxValue;

        for (int i = 0; i < characterItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float x = relativeIndex * spacing + deltaX;

            if (Mathf.Abs(x) < minDistance)
            {
                minDistance = Mathf.Abs(x);
                closestIndex = i;
            }
        }

        for (int i = 0; i < characterItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);
            float x = relativeIndex * spacing + deltaX;
            float distance = Mathf.Abs(x / spacing);

            characterItems[i].SetPosition(x);
            characterItems[i].SetScale(CalculateScale(distance));
            characterItems[i].SetVisible(true);

            SongCardEffect effect =
                characterItems[i].GetComponent<SongCardEffect>();

            if (effect != null)
                effect.SetActiveCard(i == closestIndex);
        }

        UpdateSiblingOrder();
    }

    private void SnapToCurrentCharacter()
    {
        if (characterItems.Count == 0)
            return;

        if (snapCoroutine != null)
            StopCoroutine(snapCoroutine);

        snapCoroutine = StartCoroutine(SnapAnimation());
    }

    private System.Collections.IEnumerator SnapAnimation()
    {
        List<Vector2> startPositions = new List<Vector2>();
        List<float> startScales = new List<float>();

        foreach (SongItem item in characterItems)
        {
            startPositions.Add(item.GetPosition());
            startScales.Add(item.GetScale());
        }

        float timer = 0f;

        while (timer < snapDuration)
        {
            timer += Time.deltaTime;

            float t = snapDuration > 0f
                ? Mathf.Clamp01(timer / snapDuration)
                : 1f;

            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < characterItems.Count; i++)
            {
                int relativeIndex = GetRelativeIndex(i);
                float targetX = relativeIndex * spacing;
                float targetScale = CalculateScale(Mathf.Abs(relativeIndex));

                characterItems[i].SetPosition(
                    Mathf.Lerp(startPositions[i].x, targetX, t));

                characterItems[i].SetScale(
                    Mathf.Lerp(startScales[i], targetScale, t));

                characterItems[i].SetVisible(
                    Mathf.Abs(relativeIndex) <= visibleRange);

                SongCardEffect effect =
                    characterItems[i].GetComponent<SongCardEffect>();

                if (effect != null)
                    effect.SetActiveCard(relativeIndex == 0);
            }

            UpdateSiblingOrder();
            yield return null;
        }

        RefreshLayout();
        snapCoroutine = null;
    }

    private void RefreshLayout()
    {
        for (int i = 0; i < characterItems.Count; i++)
        {
            int relativeIndex = GetRelativeIndex(i);

            characterItems[i].SetPosition(relativeIndex * spacing);
            characterItems[i].SetScale(
                CalculateScale(Mathf.Abs(relativeIndex)));

            characterItems[i].SetVisible(
                Mathf.Abs(relativeIndex) <= visibleRange);

            SongCardEffect effect =
                characterItems[i].GetComponent<SongCardEffect>();

            if (effect != null)
                effect.SetActiveCard(relativeIndex == 0);
        }

        UpdateSiblingOrder();
    }

    private int GetRelativeIndex(int itemIndex)
    {
        int count = characterItems.Count;

        if (count <= 1)
            return 0;

        int difference = itemIndex - currentIndex;

        if (difference > count / 2)
            difference -= count;

        if (difference < -count / 2)
            difference += count;

        return difference;
    }

    private float CalculateScale(float distance)
    {
        if (distance <= 0.01f)
            return centerScale;

        if (distance <= 1f)
            return sideScale;

        return farScale;
    }

    private void UpdateSiblingOrder()
    {
        if (characterItems.Count == 0)
            return;

        List<SongItem> sorted = new List<SongItem>(characterItems);

        sorted.Sort((a, b) =>
        {
            int indexA = characterItems.IndexOf(a);
            int indexB = characterItems.IndexOf(b);

            return Mathf.Abs(GetRelativeIndex(indexB))
                .CompareTo(Mathf.Abs(GetRelativeIndex(indexA)));
        });

        for (int i = 0; i < sorted.Count; i++)
            sorted[i].transform.SetSiblingIndex(i);
    }

    // =========================
    // PLAY
    // =========================

    public void OnPlayButtonClicked()
    {
        if (characterItems == null || characterItems.Count == 0)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.PlayClickSound();

        // Đọc scene đã chọn từ ListSongControl ở Select Song.
        string selectedScene = PlayerPrefs.GetString(
            "SelectedGameplayScene",
            defaultSceneName
        );

        if (string.IsNullOrWhiteSpace(selectedScene))
        {
            Debug.LogError("ListCharControl: Tên scene gameplay đang trống.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(selectedScene))
        {
            Debug.LogError(
                $"Không tìm thấy scene '{selectedScene}'. " +
                "Kiểm tra tên scene và Build Settings."
            );
            return;
        }

        Debug.Log("Đang vào scene: " + selectedScene);
        SceneManager.LoadScene(selectedScene);
    }
}