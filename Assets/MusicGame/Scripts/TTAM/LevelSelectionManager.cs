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

        [Header("Unlock")]
        [Tooltip("Giá để mở khóa màn này. Màn 1 để 0")]
        public int unlockCost = 0;

        [Header("UI")]
        public RectTransform itemRect;
        public GameObject selectedFrame;
        public GameObject lockedOverlay;

        // Lưu lại Y gốc đã canh trong Editor để không bị dịch lên
        [HideInInspector] public float baseY;
    }

    [Header("Level List")]
    [SerializeField] private List<LevelItem> levels = new List<LevelItem>();

    [Header("Điều kiện mở khóa")]
    [Tooltip("Bật = phải qua màn đứng trước trong danh sách (đủ số sao bên dưới) mới được trả coin mở màn sau")]
    [SerializeField] private bool requirePreviousClear = true;

    [Tooltip("Số sao tối thiểu ở màn trước")]
    [SerializeField, Range(1, 3)] private int requiredStarsOnPrevious = 1;

    [Header("Debug")]
    [Tooltip("Bật để mở khóa tất cả các màn (dùng khi test)")]
    [SerializeField] private bool unlockAllLevels = false;

    [Header("Navigation")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;

    [Header("Play / Unlock (2 nút đặt chồng cùng vị trí)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button unlockButton;
    [SerializeField] private TMP_Text unlockCostText; // Chữ giá trên nút Unlock (có thể bỏ trống)

    [Header("Unlock UI")]
    [SerializeField] private TMP_Text coinText;     // Tiền hiện có (có thể bỏ trống)
    [SerializeField] private TMP_Text messageText;  // Thông báo (có thể bỏ trống)

    [Header("Purchase Panel (hiện khi không đủ tiền)")]
    [Tooltip("Script PurchasePanelControl, đã có sẵn animation hạ xuống")]
    [SerializeField] private PurchasePanelControl purchasePanelControl;

    [Header("Scene chọn nhân vật")]
    [Tooltip("Tên scene Select Char (phải có trong Build Settings)")]
    [SerializeField] private string characterSelectSceneName = "SelectChar";

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

        // Tự cập nhật số coin khi tiền thay đổi (mua coin, trừ tiền...)
        PlayerDataManager.Instance.OnCurrencyChanged += UpdateCoinUI;

        // Tự cập nhật lock/unlock khi level được mở, khóa hoặc reset
        PlayerDataManager.Instance.OnProgressionChanged += UpdateSelectedUI;

        // Màn 1 luôn miễn phí
        PlayerDataManager.Instance.UnlockLevel(1);

        SetupLevelItems();

        if (leftButton != null)
            leftButton.onClick.AddListener(PreviousLevel);

        if (rightButton != null)
            rightButton.onClick.AddListener(NextLevel);

        if (playButton != null)
            playButton.onClick.AddListener(PlaySelectedLevel);

        if (unlockButton != null)
            unlockButton.onClick.AddListener(UnlockSelectedLevel);

        // Chọn màn đầu tiên
        selectedIndex = 0;

        UpdateCarousel(false);
    }

    private void OnDestroy()
    {
        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnCurrencyChanged -= UpdateCoinUI;
            PlayerDataManager.Instance.OnProgressionChanged -= UpdateSelectedUI;
        }

        if (leftButton != null)
            leftButton.onClick.RemoveListener(PreviousLevel);

        if (rightButton != null)
            rightButton.onClick.RemoveListener(NextLevel);

        if (playButton != null)
            playButton.onClick.RemoveListener(PlaySelectedLevel);

        if (unlockButton != null)
            unlockButton.onClick.RemoveListener(UnlockSelectedLevel);

        if (animationCoroutine != null)
            StopCoroutine(animationCoroutine);
    }

    private bool IsUnlocked(int levelId)
    {
        if (unlockAllLevels)
            return true;

        return PlayerDataManager.Instance.IsLevelUnlocked(levelId);
    }

    // Đã qua màn đứng trước (đủ sao) chưa -> mới được trả coin mở màn này.
    // Màn đầu danh sách, hoặc tắt requirePreviousClear, thì luôn đạt.
    private bool IsRequirementMet(int index, out LevelItem previous)
    {
        previous = index > 0 && index < levels.Count ? levels[index - 1] : null;

        if (unlockAllLevels || !requirePreviousClear || previous == null)
            return true;

        return PlayerDataManager.Instance.GetBestStars(previous.levelId) >= requiredStarsOnPrevious;
    }

    private string RequirementMessage(LevelItem previous)
    {
        // Không dùng ký tự ★ / ≥: font TMP mặc định có thể không có -> hiện ô vuông
        return "Cần qua " + previous.levelName + " (ít nhất " + requiredStarsOnPrevious + " sao) trước!";
    }

    private void SetupLevelItems()
    {
        foreach (LevelItem level in levels)
        {
            if (level.itemRect == null)
                continue;

            // Lưu Y gốc đã canh trong Editor
            level.baseY = level.itemRect.anchoredPosition.y;

            if (level.selectedFrame != null)
                level.selectedFrame.SetActive(false);

            if (level.lockedOverlay != null)
                level.lockedOverlay.SetActive(!IsUnlocked(level.levelId));
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
            animationCoroutine = StartCoroutine(AnimateCarousel());
        else
            ApplyCarouselPositions();

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
                new Vector2(offset * spacing, item.baseY);

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

        for (int i = 0; i < levels.Count; i++)
        {
            LevelItem item = levels[i];

            if (item.itemRect == null)
            {
                startPositions.Add(Vector2.zero);
                startScales.Add(Vector3.one);
                targetPositions.Add(Vector2.zero);
                targetScales.Add(Vector3.one);
                continue;
            }

            int offset = GetCircularOffset(i);
            bool visible = Mathf.Abs(offset) <= 1;

            if (visible)
                item.itemRect.gameObject.SetActive(true);

            startPositions.Add(item.itemRect.anchoredPosition);
            startScales.Add(item.itemRect.localScale);

            targetPositions.Add(new Vector2(offset * spacing, item.baseY));

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

                rect.anchoredPosition = Vector2.Lerp(startPositions[i], targetPositions[i], t);
                rect.localScale = Vector3.Lerp(startScales[i], targetScales[i], t);
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
                item.lockedOverlay.SetActive(!IsUnlocked(item.levelId));
        }

        if (selectedLevelText != null)
            selectedLevelText.text = selected.levelName;

        // 2 nút cùng vị trí: màn đã mở -> hiện Play, màn khóa -> hiện Unlock
        if (playButton != null)
            playButton.gameObject.SetActive(unlocked);

        if (unlockButton != null)
            unlockButton.gameObject.SetActive(!unlocked);

        if (unlockCostText != null)
            unlockCostText.text = selected.unlockCost.ToString();

        if (messageText != null)
        {
            // Màn khóa mà chưa qua màn trước -> nhắc điều kiện ngay khi chọn
            if (!unlocked && !IsRequirementMet(selectedIndex, out LevelItem previous))
                messageText.text = RequirementMessage(previous);
            else
                messageText.text = "";
        }

        UpdateCoinUI();

        Debug.Log("Selected Level: " + selected.levelName);
    }

    private void UpdateCoinUI()
    {
        if (coinText != null)
            coinText.text = PlayerDataManager.Instance.Data.currency.coin.ToString();
    }

    private void ShowPurchasePanel()
    {
        if (purchasePanelControl == null)
        {
            Debug.LogWarning("Chưa gán Purchase Panel Control trong Inspector!");
            return;
        }

        // Dùng animation hạ xuống có sẵn trong PurchasePanelControl
        purchasePanelControl.OpenPurchase();
    }

    private void UnlockSelectedLevel()
    {
        LevelItem selected = levels[selectedIndex];

        if (IsUnlocked(selected.levelId))
            return;

        // Chưa qua màn trước -> không cho trả tiền (giữ đúng thứ tự cốt truyện), không mở bảng nạp
        if (!IsRequirementMet(selectedIndex, out LevelItem previous))
        {
            Debug.Log("Chưa đủ điều kiện mở " + selected.levelName + ": " + RequirementMessage(previous));

            if (messageText != null)
                messageText.text = RequirementMessage(previous);

            return;
        }

        // SpendCoin trả về false khi amount <= 0, nên màn giá 0 thì bỏ qua bước trừ tiền
        bool paid = selected.unlockCost <= 0 ||
                    PlayerDataManager.Instance.SpendCoin(selected.unlockCost);

        if (!paid)
        {
            Debug.Log("Không đủ tiền để mở " + selected.levelName);

            if (messageText != null)
                messageText.text = "Không đủ tiền!";

            ShowPurchasePanel();

            return;
        }

        PlayerDataManager.Instance.UnlockLevel(selected.levelId);

        Debug.Log("Đã mở khóa " + selected.levelName);

        UpdateSelectedUI();
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

        if (string.IsNullOrEmpty(characterSelectSceneName))
        {
            Debug.LogError("Chưa đặt tên scene Select Char!");
            return;
        }

        // Hết lượt thì không cho sang Select Char (lượt chỉ bị trừ khi thật sự vào màn)
        if (!PlayerDataManager.Instance.CheckCanPlay())
        {
            if (messageText != null)
                messageText.text = "Hết lượt chơi!";
            return;
        }

        // Lưu màn đã chọn để scene Select Char dùng
        SelectedLevelData.Set(selected.levelId, selected.sceneName);

        SceneFader.Load(characterSelectSceneName);   // tối dần rồi chuyển scene
    }
}