using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public enum UnlockType
{
    Default,            // NV1: Ramona (Mặc định mở khóa)
    LettersOrDiamond,   // NV2, NV3: Thu thập chữ cái HOẶC Mua Kim Cương (Scott, Stephen)
    Achievement,        // NV4: Thành tựu / Nhiệm vụ (Knives Chau)
    GachaWheel          // NV5: Vòng quay may mắn (Kim Pine / Secret)
}

[System.Serializable]
public class CharacterProfile
{
    public string characterID = "Scott";
    public string displayName = "SCOTT";
    public UnlockType unlockType = UnlockType.LettersOrDiamond;

    [Header("Cấu hình Chữ Cái & Kim Cương (NV2, NV3)")]
    public int unlockPriceDiamond = 999;
    public string collectWord = "SCOTT"; // Từ khóa cần gom đủ để nhận miễn phí
}

public class CharacterSystemManager : MonoBehaviour
{
    [Header("1. Danh Sách Nhân Vật")]
    [SerializeField] private CharacterProfile[] characters;
    [SerializeField] private ListCharSwipeControl swipeControl;

    [Header("2. UI Action Button (Nút Vuông Giữa: PLAY / SELECT / CLAIM)")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionButtonBg;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private GameObject selectButtonObj;       // Ảnh SELECT đè lên (nếu dùng sprite riêng)
    [SerializeField] private GameObject playIcon;              // Icon tam giác / nốt nhạc

    [Header("3. UI Mua Kim Cương (Khung Trái)")]
    [SerializeField] private GameObject diamondBuyPanel;       // Khung Diamond Panel bên trái
    [SerializeField] private TextMeshProUGUI priceDiamondText; // Text số tiền 999

    [Header("4. Dãy Chữ Cái Tên Nhân Vật (Khung Phải)")]
    [SerializeField] private CharacterNameDisplay nameDisplay; // Script quản lý 7 slot chữ cái

    [Header("5. Tutorial Tân Thủ (Ramona)")]
    [SerializeField] private GameObject tutorialOverlay;
    [SerializeField] private RectTransform handPointer;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject ramonaObject;          // Char_01
    [SerializeField] private GameObject ramonaGlowEffect;      // GlowEffect sau lưng Char_01
    [SerializeField] private string tutorialNextScene = "Story";
    [SerializeField] private string normalNextScene = "LobbyNT";

    [Header("6. Bảng Màu Trạng Thái Nút")]
    [SerializeField] private Color playColor = new Color(0.2f, 0.6f, 1f);     // Xanh biển (PLAY)
    [SerializeField] private Color selectColor = new Color(0f, 0.95f, 0.85f); // Xanh Cyan sáng (SELECT)
    [SerializeField] private Color claimColor = new Color(0f, 1f, 0.5f);      // Xanh lá neon (CLAIM)
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.35f);// Xám đen mờ (LOCKED)

    private int activeIndex = 0;
    private bool isTutorial = false;
    private Vector2 initialHandPos;

    private enum ActionState { Select, Play, Claim, Locked }
    private ActionState currentActionState;

    private void Awake()
    {
        // NV1 (Ramona) luôn luôn mở khóa
        PlayerPrefs.SetInt("Unlocked_Ramona", 1);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        int isTutorialDone = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isTutorial = (isTutorialDone == 0);

        if (isTutorial)
        {
            activeIndex = 0; // Cố định Ramona cho tân thủ
            SaveCharacterSelection(0, "Ramona");

            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            if (backButton != null) backButton.interactable = false;
            if (swipeControl != null) swipeControl.CanSwipe = false;

            EnableRamonaHighlight();
            SetButtonVisual(ActionState.Select, "CONFIRM", selectColor, false);
        }
        else
        {
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            if (backButton != null) backButton.interactable = true;
            if (swipeControl != null) swipeControl.CanSwipe = true;

            ResetRamonaHighlight();

            // Khôi phục nhân vật đang trang bị từ trước
            string equipped = PlayerPrefs.GetString("EquippedCharacter", "Ramona");
            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i].characterID == equipped)
                {
                    activeIndex = i;
                    break;
                }
            }
        }
    }

    private void Start()
    {
        // 1. Tự động gắn sự kiện click mua kim cương vào chính Diamond Panel
        if (diamondBuyPanel != null)
        {
            Button buyBtn = diamondBuyPanel.GetComponent<Button>();
            if (buyBtn != null)
            {
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(OnDirectBuyWithDiamonds);
            }
        }

        // 2. Gắn sự kiện cho nút Action và Back
        if (actionButton != null)
        {
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(OnActionButtonClick);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        UpdateUIByCharacter(activeIndex);
    }

    private void Update()
    {
        if (isTutorial)
        {
            if (handPointer != null && handPointer.gameObject.activeInHierarchy)
            {
                float bounce = Mathf.Sin(Time.time * 6f) * 12f;
                handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
            }

            if (ramonaGlowEffect != null && ramonaGlowEffect.activeInHierarchy)
            {
                float pulse = 0.8f + Mathf.PingPong(Time.time * 2f, 0.4f);
                ramonaGlowEffect.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    // Được gọi tự động từ ListCharSwipeControl khi người chơi vuốt xong
    public void OnCharacterSwiped(int index)
    {
        if (index < 0 || index >= characters.Length) return;
        activeIndex = index;
        UpdateUIByCharacter(activeIndex);
    }

    public void UpdateUIByCharacter(int index)
    {
        CharacterProfile cur = characters[index];

        bool isUnlocked = (cur.unlockType == UnlockType.Default) ||
                          PlayerPrefs.GetInt("Unlocked_" + cur.characterID, 0) == 1;
        string equipped = PlayerPrefs.GetString("EquippedCharacter", "Ramona");

        // 1. Cập nhật Dãy Chữ Cái (sáng Neon hoặc tối Xám dựa vào kho đồ)
        if (nameDisplay != null)
        {
            nameDisplay.DisplayWord(cur.collectWord, isUnlocked);
        }

        // Nếu đang trong Tutorial tân thủ: ẩn toàn bộ khung mua bán
        if (isTutorial)
        {
            if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
            return;
        }

        // 2. Trạng thái ĐÃ SỞ HỮU (Áp dụng cho mọi nhân vật đã mở khóa)
        if (isUnlocked)
        {
            if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);

            if (cur.characterID == equipped)
            {
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true);
            }
            else
            {
                SetButtonVisual(ActionState.Select, "SELECT", selectColor, false);
            }
            return;
        }

        // 3. Trạng thái CHƯA SỞ HỮU: Phân nhánh theo cơ chế mở khóa
        switch (cur.unlockType)
        {
            case UnlockType.LettersOrDiamond: // NV2 (Scott) & NV3 (Stephen)
                if (diamondBuyPanel != null)
                {
                    diamondBuyPanel.SetActive(true);
                    if (priceDiamondText != null) priceDiamondText.text = cur.unlockPriceDiamond.ToString();
                }

                // Kiểm tra xem túi đồ chữ cái đã đủ ghép thành từ chưa
                bool hasFullLetters = LetterInventoryManager.HasEnoughLettersForWord(cur.collectWord);

                if (hasFullLetters)
                {
                    // Đã đủ chữ -> Nút giữa biến thành CLAIM màu xanh sáng để nhận miễn phí
                    SetButtonVisual(ActionState.Claim, "CLAIM", claimColor, false);
                }
                else
                {
                    // Chưa đủ chữ -> Nút giữa tạm khóa (người chơi có thể bấm khung Diamond Panel bên trái để mua thẳng)
                    SetButtonVisual(ActionState.Locked, "LOCKED", lockedColor, false);
                }
                break;

            case UnlockType.Achievement: // NV4: Knives Chau
                if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
                SetButtonVisual(ActionState.Locked, "QUEST", lockedColor, false);
                break;

            case UnlockType.GachaWheel:  // NV5: Kim Pine / Mystery
                if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
                SetButtonVisual(ActionState.Locked, "LUCKY", lockedColor, false);
                break;
        }
    }

    private void SetButtonVisual(ActionState state, string label, Color color, bool showPlayIcon)
    {
        currentActionState = state;

        if (actionButton != null)
        {
            actionButton.interactable = (state != ActionState.Locked);
        }
        if (actionButtonBg != null) actionButtonBg.color = color;
        if (actionButtonText != null) actionButtonText.text = label;
        if (playIcon != null) playIcon.SetActive(showPlayIcon);

        if (selectButtonObj != null)
        {
            selectButtonObj.SetActive(state == ActionState.Select);
        }
    }

    // Xử lý khi bấm nút Action Button ở giữa màn hình
    public void OnActionButtonClick()
    {
        PlayClickSFX();
        CharacterProfile cur = characters[activeIndex];

        // --- Xử lý Tutorial tân thủ ---
        if (isTutorial)
        {
            if (currentActionState == ActionState.Select)
            {
                SaveCharacterSelection(0, "Ramona");
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true);
            }
            else if (currentActionState == ActionState.Play)
            {
                PlayerPrefs.SetInt("TutorialCompleted", 1);
                PlayerPrefs.SetInt("Tutorial_SelectCharacter_Done", 1);
                PlayerPrefs.Save();

                ResetRamonaHighlight();
                SceneManager.LoadScene(tutorialNextScene);
            }
            return;
        }

        // --- Xử lý chế độ thường ---
        switch (currentActionState)
        {
            case ActionState.Select:
                SaveCharacterSelection(activeIndex, cur.characterID);
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true);
                break;

            case ActionState.Play:
                SceneManager.LoadScene(normalNextScene);
                break;

            case ActionState.Claim:
                // 1. Trừ chính xác số lượng chữ cái tương ứng trong túi đồ
                LetterInventoryManager.ConsumeLettersForWord(cur.collectWord);

                // 2. Lưu trạng thái mở khóa và trang bị nhân vật
                PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
                SaveCharacterSelection(activeIndex, cur.characterID);

                // 3. Vẽ lại giao diện (Nút chuyển ngay sang PLAY)
                UpdateUIByCharacter(activeIndex);
                break;
        }
    }

    // Xử lý khi bấm trực tiếp vào khung Diamond Panel bên trái
    public void OnDirectBuyWithDiamonds()
    {
        PlayClickSFX();
        CharacterProfile cur = characters[activeIndex];

        int currentDiamond = PlayerPrefs.GetInt("UserDiamonds", 9999);
        if (currentDiamond >= cur.unlockPriceDiamond)
        {
            currentDiamond -= cur.unlockPriceDiamond;
            PlayerPrefs.SetInt("UserDiamonds", currentDiamond);
            PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
            SaveCharacterSelection(activeIndex, cur.characterID);

            UpdateUIByCharacter(activeIndex);
        }
        else
        {
            Debug.LogWarning("Không đủ Kim Cương để mua trực tiếp " + cur.displayName);
        }
    }

    private void SaveCharacterSelection(int index, string id)
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", index);
        PlayerPrefs.SetString("EquippedCharacter", id);
        PlayerPrefs.Save();
    }

    private void EnableRamonaHighlight()
    {
        if (ramonaObject != null)
        {
            Canvas c = ramonaObject.GetComponent<Canvas>();
            if (c != null)
            {
                c.enabled = true;
                c.overrideSorting = true;
                c.sortingOrder = 5;
            }
        }
        if (ramonaGlowEffect != null) ramonaGlowEffect.SetActive(true);
    }

    private void ResetRamonaHighlight()
    {
        if (ramonaObject != null)
        {
            Canvas c = ramonaObject.GetComponent<Canvas>();
            if (c != null)
            {
                c.overrideSorting = false;
                c.enabled = true;
            }
        }
        if (ramonaGlowEffect != null) ramonaGlowEffect.SetActive(false);
    }

    public void OnBackButtonClicked()
    {
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Back);
        }
        SceneManager.LoadScene(normalNextScene);
    }

    private void PlayClickSFX()
    {
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Click);
        }
    }
}