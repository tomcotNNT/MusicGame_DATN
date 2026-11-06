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
    [Header("1. Danh Sách Nhân Vật")]
    [SerializeField] private CharacterProfile[] characters;
    [SerializeField] private ListCharSwipeControl swipeControl;

    [Header("2. UI Action Button (Nút Vuông Giữa: PLAY / SELECT / CLAIM)")]
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

    [Header("5. Tutorial Tân Thủ (Ramona)")]
    [SerializeField] private GameObject tutorialOverlay;
    [SerializeField] private RectTransform handPointer;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject ramonaObject;
    [SerializeField] private GameObject ramonaGlowEffect;
    [SerializeField] private string tutorialNextScene = "Story";
    [SerializeField] private string normalNextScene = "LobbyNT";

    [Header("6. Bảng Màu Trạng Thái Nút")]
    [SerializeField] private Color playColor = new Color(0.2f, 0.6f, 1f);     // Xanh biển (PLAY)
    [SerializeField] private Color selectColor = new Color(0f, 0.95f, 0.85f); // Xanh Cyan (SELECT)
    [SerializeField] private Color claimColor = new Color(0f, 1f, 0.5f);      // Xanh lá (CLAIM)
    [SerializeField] private Color lockedColor = new Color(0.3f, 0.3f, 0.35f);// Xám đen (LOCKED)

    private int activeIndex = 0;
    private bool isTutorial = false;
    private Vector2 initialHandPos;

    private enum ActionState { Select, Play, Claim, Locked }
    private ActionState currentActionState;

    private void Awake()
    {
        PlayerPrefs.SetInt("Unlocked_Ramona", 1);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        int isTutorialDone = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isTutorial = (isTutorialDone == 0);

        if (isTutorial)
        {
            activeIndex = 0;
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

        UpdateUIByCharacter(activeIndex);
    }

    private void Update()
    {
        // 1. TỰ ĐỘNG ĐỒNG BỘ: Phát hiện người chơi vừa vuốt sang nhân vật mới
        if (swipeControl != null && swipeControl.CurrentIndex != activeIndex)
        {
            OnCharacterSwiped(swipeControl.CurrentIndex);
        }

        // 2. Xử lý hiệu ứng nhấp nhô khi ở Tutorial
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

        // Đồng bộ dãy chữ cái tên nhân vật
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
                SetButtonVisual(ActionState.Play, "PLAY", playColor, true);
            }
            else
            {
                SetButtonVisual(ActionState.Select, "SELECT", selectColor, false);
            }
            return;
        }

        // Chưa mở khóa: xử lý theo loại mở khóa
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
                    SetButtonVisual(ActionState.Claim, "CLAIM", claimColor, false);
                }
                else
                {
                    SetButtonVisual(ActionState.Locked, "LOCKED", lockedColor, false);
                }
                break;

            case UnlockType.Achievement:
                if (diamondBuyPanel != null) diamondBuyPanel.SetActive(false);
                SetButtonVisual(ActionState.Locked, "QUEST", lockedColor, false);
                break;

            case UnlockType.GachaWheel:
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

        // Cơ chế phòng thủ: Không ẩn nếu playIcon bị kéo nhầm chính actionButton
        if (playIcon != null && (actionButton == null || playIcon != actionButton.gameObject))
        {
            playIcon.SetActive(showPlayIcon);
        }

        if (selectButtonObj != null)
        {
            selectButtonObj.SetActive(state == ActionState.Select);
        }
    }

    public void OnActionButtonClick()
    {
        PlayClickSFX();
        CharacterProfile cur = characters[activeIndex];

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
                LetterInventoryManager.ConsumeLettersForWord(cur.collectWord);
                PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
                SaveCharacterSelection(activeIndex, cur.characterID);
                UpdateUIByCharacter(activeIndex);
                break;
        }
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