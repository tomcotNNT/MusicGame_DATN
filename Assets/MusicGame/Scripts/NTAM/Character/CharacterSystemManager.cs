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
    public string collectWord = "SCOTT";
}

public class CharacterSystemManager : MonoBehaviour
{
    [Header("1. Danh Sách Cấu Hình Nhân Vật")]
    [SerializeField] private CharacterProfile[] characters;
    [SerializeField] private ListCharSwipeControl swipeControl;

    [Header("2. UI Action Button (Nút Vuông Giữa)")]
    [SerializeField] private Button actionButton;
    [SerializeField] private Image actionButtonBg;
    [SerializeField] private TextMeshProUGUI actionButtonText;
    [SerializeField] private GameObject selectButtonObj;
    [SerializeField] private GameObject playIcon;

    [Header("3. UI Mua Kim Cương (Khung Trái)")]
    [SerializeField] private GameObject diamondBuyPanel;
    [SerializeField] private TextMeshProUGUI priceDiamondText;

    [Header("4. Dãy Chữ Cái Tên Nhân Vật (Khung Phải)")]
    [SerializeField] private CharacterNameDisplay nameDisplay;

    [Header("5. Popup Xác Nhận Tiêu Hao Chữ Cái")]
    [SerializeField] private GameObject unlockConfirmPopup;
    [SerializeField] private TextMeshProUGUI popupMessageText;
    [SerializeField] private Button popupConfirmButton;
    [SerializeField] private Button popupCancelButton;

    [Header("6. Cấu Hình Điều Hướng Scene")]
    [Tooltip("Scene khi bấm nút BACK")]
    [SerializeField] private string backSceneName = "LobbyNT";
    [Tooltip("Scene tiếp theo khi kết thúc Tutorial tân thủ")]
    [SerializeField] private string tutorialNextScene = "Story";
    [Tooltip("Scene màn chơi chính khi bấm nút PLAY")]
    [SerializeField] private string normalNextScene = "Story";

    [Header("7. Tutorial Tân Thủ (Ramona)")]
    [SerializeField] private GameObject tutorialOverlay;
    [SerializeField] private RectTransform handPointer;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject ramonaObject;
    [SerializeField] private GameObject ramonaGlowEffect;

    [Header("8. Bảng Màu Trạng Thái Nút")]
    [SerializeField] private Color playColor = new Color(0.2f, 0.6f, 1f);                 // Xanh biển (PLAY)
    [SerializeField] private Color selectActiveColor = new Color(0f, 0.95f, 0.85f);        // Cyan sáng (Đủ chữ/Đã sở hữu)
    [SerializeField] private Color selectDimmedColor = new Color(0.3f, 0.35f, 0.4f, 0.45f);// Mờ xám (Chưa đủ chữ/Khóa)

    private int activeIndex = 0;
    private bool isTutorial = false;
    private Vector2 initialHandPos;

    private enum ActionState { DimmedSelect, ActiveClaim, EquipSelect, Play }
    private ActionState currentActionState;

    public int ActiveIndex => activeIndex;

    private void Awake()
    {
        // NV1 (Ramona) luôn mở khóa mặc định
        PlayerPrefs.SetInt("Unlocked_Ramona", 1);

        if (handPointer != null) initialHandPos = handPointer.anchoredPosition;

        // Kiểm tra đồng thời cả 2 key tiến trình tân thủ
        int isTutorialDone = PlayerPrefs.GetInt("TutorialCompleted", 0);
        if (isTutorialDone == 0) isTutorialDone = PlayerPrefs.GetInt("TutorialDone", 0);
        
        isTutorial = (isTutorialDone == 0);

        if (isTutorial)
        {
            activeIndex = 0;
            SaveCharacterSelection(0, "Ramona");

            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            if (backButton != null) backButton.interactable = false; // Khóa nút back trong lúc hướng dẫn
            if (swipeControl != null) swipeControl.CanSwipe = false;

            EnableRamonaHighlight();
            SetButtonVisual(ActionState.EquipSelect, "CONFIRM", selectActiveColor, true, false);
            Debug.Log("<color=cyan>[CharacterSystemManager] Kích hoạt chế độ TUTORIAL TÂN THỦ cho Ramona</color>");
        }
        else
        {
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            if (backButton != null) backButton.interactable = true;
            if (swipeControl != null) swipeControl.CanSwipe = true;

            ResetRamonaHighlight();

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
        if (diamondBuyPanel != null)
        {
            Button buyBtn = diamondBuyPanel.GetComponent<Button>();
            if (buyBtn != null)
            {
                buyBtn.onClick.RemoveAllListeners();
                buyBtn.onClick.AddListener(OnDirectBuyWithDiamonds);
            }
        }

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

        if (popupConfirmButton != null)
        {
            popupConfirmButton.onClick.RemoveAllListeners();
            popupConfirmButton.onClick.AddListener(ConfirmUnlockWithLetters);
        }

        if (popupCancelButton != null)
        {
            popupCancelButton.onClick.RemoveAllListeners();
            popupCancelButton.onClick.AddListener(() =>
            {
                if (unlockConfirmPopup != null) unlockConfirmPopup.SetActive(false);
            });
        }

        UpdateUIByCharacter(activeIndex);
    }

    private void Update()
    {
        if (swipeControl != null && swipeControl.CurrentIndex != activeIndex)
        {
            OnCharacterSwiped(swipeControl.CurrentIndex);
        }

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

    public void OnCharacterSwiped(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;
        activeIndex = index;
        UpdateUIByCharacter(activeIndex);
    }

    public void UpdateUIByCharacter(int index)
    {
        if (characters == null || index < 0 || index >= characters.Length) return;
        CharacterProfile cur = characters[index];

        bool isUnlocked = (cur.unlockType == UnlockType.Default) ||
                          PlayerPrefs.GetInt("Unlocked_" + cur.characterID, 0) == 1;
        string equipped = PlayerPrefs.GetString("EquippedCharacter", "Ramona");

        if (nameDisplay != null)
        {
            nameDisplay.DisplayWord(cur.collectWord, isUnlocked);
        }

        if (isTutorial)
        {
            if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
            return;
        }

        if (isUnlocked)
        {
            if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);

            if (cur.characterID == equipped)
            {
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true, true);
            }
            else
            {
                SetButtonVisual(ActionState.EquipSelect, "SELECT", selectActiveColor, true, false);
            }
            return;
        }

        switch (cur.unlockType)
        {
            case UnlockType.LettersOrDiamond:
                if (diamondBuyPanel != null)
                {
                    diamondBuyPanel.SetActive(true);
                    if (priceDiamondText != null) priceDiamondText.text = cur.unlockPriceDiamond.ToString();
                }

                bool hasFullLetters = LetterInventoryManager.HasEnoughLettersForWord(cur.collectWord);

                if (hasFullLetters)
                {
                    SetButtonVisual(ActionState.ActiveClaim, "SELECT", selectActiveColor, true, false);
                }
                else
                {
                    SetButtonVisual(ActionState.DimmedSelect, "SELECT", selectDimmedColor, false, false);
                }
                break;

            case UnlockType.Achievement:
            case UnlockType.GachaWheel:
                if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
                SetButtonVisual(ActionState.DimmedSelect, "LOCKED", selectDimmedColor, false, false);
                break;
        }
    }

    private void SetButtonVisual(ActionState state, string label, Color color, bool isInteractable, bool showPlayIcon)
    {
        currentActionState = state;

        if (actionButton != null) actionButton.interactable = isInteractable;
        if (actionButtonBg != null) actionButtonBg.color = color;

        if (actionButtonText != null)
        {
            actionButtonText.text = label;
            actionButtonText.color = isInteractable ? Color.white : new Color(0.75f, 0.75f, 0.75f, 0.45f);
        }

        if (playIcon != null && (actionButton == null || playIcon != actionButton.gameObject))
        {
            playIcon.SetActive(showPlayIcon);
        }

        if (selectButtonObj != null)
        {
            selectButtonObj.SetActive(!showPlayIcon);
            Image selImg = selectButtonObj.GetComponent<Image>();
            if (selImg != null)
            {
                selImg.color = isInteractable ? Color.white : new Color(1f, 1f, 1f, 0.4f);
            }
        }
    }

    public void OnActionButtonClick()
    {
        PlayClickSFX();
        CharacterProfile cur = characters[activeIndex];

        // =========================================================================
        // XỬ LÝ TIẾN TRÌNH TUTORIAL
        // =========================================================================
        if (isTutorial)
        {
            if (currentActionState == ActionState.EquipSelect)
            {
                SaveCharacterSelection(0, "Ramona");
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true, true);
            }
            else if (currentActionState == ActionState.Play)
            {
                // BẮT BUỘC: Đặt rõ ràng màn sắp tới là Tutorial (Element 0 trên Story)
                PlayerPrefs.SetString("SelectedSceneToPlay", "Tutorial");
                PlayerPrefs.SetInt("SelectedStageIndex", 0);
                PlayerPrefs.Save();

                ResetRamonaHighlight();

                Debug.Log($"<color=magenta>[CHECK POINT] Đang chuyển từ Character -> {tutorialNextScene} với dữ liệu TUTORIAL (Index 0)</color>");
                
                // Tải Scene Story
                SceneManager.LoadScene(tutorialNextScene);
            }
            return;
        }

        // =========================================================================
        // XỬ LÝ CHỌN TƯỚNG BÌNH THƯỜNG (SAU KHI XONG TUTORIAL)
        // =========================================================================
        switch (currentActionState)
        {
            case ActionState.EquipSelect:
                SaveCharacterSelection(activeIndex, cur.characterID);
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true, true);
                break;

            case ActionState.ActiveClaim:
                if (unlockConfirmPopup != null)
                {
                    if (popupMessageText != null)
                    {
                        popupMessageText.text = $"Bạn có chắc muốn dùng các chữ cái <color=#00FFFF>[{cur.collectWord}]</color> để mở khóa <color=yellow>{cur.displayName}</color> không?";
                    }
                    unlockConfirmPopup.SetActive(true);
                }
                break;

            case ActionState.Play:
                SceneManager.LoadScene(normalNextScene);
                break;
        }
    }

    public void OnBackButtonClicked()
    {
        PlayClickSFX();
        string target = string.IsNullOrEmpty(backSceneName) ? "LobbyNT" : backSceneName;
        SceneManager.LoadScene(target);
    }

    private void ConfirmUnlockWithLetters()
    {
        PlayClickSFX();
        CharacterProfile cur = characters[activeIndex];

        LetterInventoryManager.ConsumeLettersForWord(cur.collectWord);
        PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
        SaveCharacterSelection(activeIndex, cur.characterID);

        if (unlockConfirmPopup != null) unlockConfirmPopup.SetActive(false);
        UpdateUIByCharacter(activeIndex);
    }

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

    private void PlayClickSFX()
    {
        if (GlobalAudioManager.Instance != null)
        {
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Click);
        }
    }

    public CharacterProfile GetCurrentCharacter() => characters[activeIndex];
}