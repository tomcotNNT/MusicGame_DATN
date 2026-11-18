
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LevelSelectionManager2 : MonoBehaviour
{
    [Serializable]
    public class LevelItem
    {
        [Header("Level Info")]
        public int levelId = 1;
        public string levelName = "Level 1";
        public string sceneName = "Level_1";

        [Header("UI")]
        public RectTransform itemRect;
        public GameObject selectedFrame;
        public GameObject lockedOverlay;
    }

    [Header("Level List")]
    [SerializeField] private List<LevelItem> levels = new List<LevelItem>();

    [Header("Debug")]
    [Tooltip("Bật để mở khóa tất cả các màn (dùng khi test)")]
    [SerializeField] private bool unlockAllLevels = true;

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Play")]
    [SerializeField] private Button playButton;

    [Header("Selected Level UI")]
    [SerializeField] private TMP_Text selectedLevelText;

    [Header("Carousel Settings")]
    [SerializeField] private float spacing = 350f;
    [SerializeField] private float sideScale = 0.75f;
    [SerializeField] private float animationDuration = 0.3f;

    private int selectedIndex = 0;
    private Coroutine animationCoroutine;

    private void Start()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager chưa tồn tại!");
            return;
        }

        if (levels.Count == 0)
        {
            Debug.LogError("Chưa có Level nào trong danh sách!");
            return;
        }

        PlayerDataManager.Instance.UnlockLevel(1);

        SetupLevelItems();

        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousLevel);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextLevel);

        if (playButton != null)
            playButton.onClick.AddListener(PlaySelectedLevel);

        // Tìm Level đầu tiên được mở khóa.
        selectedIndex = 0;

        for (int i = 0; i < levels.Count; i++)
        {
            if (IsUnlocked(levels[i].levelId))
            {
                selectedIndex = i;
                break;
            }
        }

        UpdateCarousel(false);
    }

    private void OnDestroy()
    {
        if (leftButton != null)
            leftButton.onClick.RemoveListener(PreviousLevel);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(NextLevel);

        if (playButton != null)
            playButton.onClick.RemoveListener(PlaySelectedLevel);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }

    private bool IsUnlocked(int levelId)
    {
        if (unlockAllLevels)
            return true;

        return PlayerDataManager.Instance.IsLevelUnlocked(levelId);
    }

    private void SetupLevelItems()
    {
        foreach (LevelItem level in levels)
        {
            if (level.itemRect == null)
                continue;

            if (level.selectedFrame != null)
                level.selectedFrame.SetActive(false);

            if (level.lockedOverlay != null)
            {
                bool unlocked = IsUnlocked(level.levelId);
                level.lockedOverlay.SetActive(!unlocked);
            }
        }
    }

    // Nút L
    private void PreviousLevel()
    {
        selectedIndex--;

        if (selectedIndex < 0)
            selectedIndex = levels.Count - 1;

        UpdateCarousel(true);
    }

    // Nút R
    private void NextLevel()
    {
        selectedIndex++;

        if (selectedIndex >= levels.Count)
            selectedIndex = 0;

        UpdateCarousel(true);
    }

    private void UpdateCarousel(bool animate)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        if (animate)
        {
            animationCoroutine = StartCoroutine(AnimateCarousel());
        }
        else
        {
            ApplyCarouselPositions();
        }

        UpdateSelectedUI();
    }

    private void ApplyCarouselPositions()
    {
        for (int i = 0; i < levels.Count; i++)
        {
            LevelItem item = levels[i];

            if (item.itemRect == null)
                continue;

            int offset = GetCircularOffset(i);

            bool visible = Mathf.Abs(offset) <= 1;
            item.itemRect.gameObject.SetActive(visible);

            if (!visible)
                continue;

            item.itemRect.anchoredPosition =
                new Vector2(offset * spacing, 0);

            float scale = offset == 0 ? 1f : sideScale;

            item.itemRect.localScale = Vector3.one * scale;
        }
    }

    private IEnumerator AnimateCarousel()
    {
        List<Vector2> startPositions = new List<Vector2>();
        List<Vector3> startScales = new List<Vector3>();

        List<Vector2> targetPositions = new List<Vector2>();
        List<Vector3> targetScales = new List<Vector3>();

        // Tính vị trí đích cho từng thẻ.
        for (int i = 0; i < levels.Count; i++)
        {
            LevelItem item = levels[i];

            int offset = GetCircularOffset(i);

            bool visible = Mathf.Abs(offset) <= 1;

            if (visible)
                item.itemRect.gameObject.SetActive(true);

            startPositions.Add(item.itemRect.anchoredPosition);
            startScales.Add(item.itemRect.localScale);

            targetPositions.Add(
                new Vector2(offset * spacing, 0)
            );

            float scale = offset == 0 ? 1f : sideScale;

            targetScales.Add(Vector3.one * scale);
        }

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / animationDuration);
            t = Mathf.SmoothStep(0f, 1f, t);

            for (int i = 0; i < levels.Count; i++)
            {
                RectTransform rect = levels[i].itemRect;

                if (rect == null)
                    continue;

                rect.anchoredPosition = Vector2.Lerp(
                    startPositions[i],
                    targetPositions[i],
                    t
                );

                rect.localScale = Vector3.Lerp(
                    startScales[i],
                    targetScales[i],
                    t
                );
            }

            yield return null;
        }

        ApplyCarouselPositions();

        animationCoroutine = null;
    }

    // Tính vị trí theo vòng tròn.
    private int GetCircularOffset(int index)
    {
        int offset = index - selectedIndex;

        if (offset > levels.Count / 2)
            offset -= levels.Count;

        if (offset < -levels.Count / 2)
            offset += levels.Count;

        return offset;
    }

    private void UpdateSelectedUI()
    {
        LevelItem selected = levels[selectedIndex];

        bool unlocked = IsUnlocked(selected.levelId);

        for (int i = 0; i < levels.Count; i++)
        {
            LevelItem item = levels[i];

            if (item.selectedFrame != null)
                item.selectedFrame.SetActive(i == selectedIndex);

            if (item.lockedOverlay != null)
            {
                bool itemUnlocked = IsUnlocked(item.levelId);

                item.lockedOverlay.SetActive(!itemUnlocked);
            }
        }

        if (selectedLevelText != null)
            selectedLevelText.text = selected.levelName;

        if (playButton != null)
            playButton.interactable = unlocked;

        Debug.Log("Selected Level: " + selected.levelName);
    }

    
    private void PlaySelectedLevel()
    {
        Debug.Log("PLAY BUTTON CLICKED");

        if (PlayerDataManager.Instance == null)
        {
            Debug.LogError("PlayerDataManager chưa tồn tại!");
            return;
        }

        LevelItem selected = levels[selectedIndex];

        Debug.Log("Selected Level ID: " + selected.levelId);
        Debug.Log("Scene Name: " + selected.sceneName);

        if (!IsUnlocked(selected.levelId))
        {
            Debug.LogWarning("Level chưa được mở khóa!");
            return;
        }

        if (string.IsNullOrEmpty(selected.sceneName))
        {
            Debug.LogError("Scene Name đang trống!");
            return;
        }

        SceneManager.LoadScene(selected.sceneName);
    }

}
