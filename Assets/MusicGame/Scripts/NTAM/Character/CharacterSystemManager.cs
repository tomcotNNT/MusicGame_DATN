using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class CharacterProfile
{
    public string characterID = "Scott";
    public string displayName = "SCOTT";
    public bool isDefaultUnlocked = false; 
    public int unlockPriceDiamond = 999;
    public string collectWord = "SCOTT";   
}

public class CharacterSystemManager : MonoBehaviour
{
    [Header("1. Danh Sách Nhân Vật")]
    [SerializeField] private CharacterProfile[] characters;
    [SerializeField] private ListCharSwipeControl swipeControl;

    [Header("2. UI Action Button (Play - Select)")]
    [SerializeField] private Button actionButton;           
    [SerializeField] private GameObject selectButtonObj;    
    [SerializeField] private GameObject playIcon;           

    [Header("3. UI Info Panel (Bảng Bên Trái)")]
    [SerializeField] private GameObject infoPanel;
    [SerializeField] private GameObject priceDiamondGroup;
    [SerializeField] private TextMeshProUGUI priceDiamondText;
    [SerializeField] private TextMeshProUGUI letterProgressText;

    [Header("4. Tên Lớn Góc Phải")]
    [SerializeField] private TextMeshProUGUI bigNameText;

    [Header("5. Tutorial Tân Thủ (Highlight & Bàn Tay)")]
    [SerializeField] private GameObject tutorialOverlay;
    [SerializeField] private RectTransform handPointer;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject ramonaObject;        
    [SerializeField] private GameObject ramonaGlowEffect;    
    [SerializeField] private string tutorialNextScene = "Story";
    [SerializeField] private string normalNextScene = "LobbyNT";

    private int activeIndex = 0;
    private bool isTutorial = false;
    private Vector2 initialHandPos;
    private enum ActionState { Select, Play, Unlock, Claim }
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
            SetButtonVisual(ActionState.Select, false);
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
        UpdateUIByCharacter(activeIndex);

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

    public void OnCharacterSwiped(int index)
    {
        if (index < 0 || index >= characters.Length) return;
        activeIndex = index;
        UpdateUIByCharacter(activeIndex);
    }

    public void UpdateUIByCharacter(int index)
    {
        CharacterProfile cur = characters[index];

        if (bigNameText != null) bigNameText.text = cur.displayName;

        if (isTutorial)
        {
            if (infoPanel != null) infoPanel.SetActive(false);
            return;
        }

        bool isUnlocked = cur.isDefaultUnlocked || PlayerPrefs.GetInt("Unlocked_" + cur.characterID, 0) == 1;
        string equipped = PlayerPrefs.GetString("EquippedCharacter", "Ramona");

        if (isUnlocked)
        {
            if (infoPanel != null) infoPanel.SetActive(false);

            if (cur.characterID == equipped)
                SetButtonVisual(ActionState.Play, true);
            else
                SetButtonVisual(ActionState.Select, false);
        }
        else
        {
            if (infoPanel != null) infoPanel.SetActive(true);

            int collected = PlayerPrefs.GetInt("LettersCount_" + cur.characterID, 0);
            int target = string.IsNullOrEmpty(cur.collectWord) ? 0 : cur.collectWord.Length;

            if (target > 0 && collected >= target)
            {
                if (priceDiamondGroup != null) priceDiamondGroup.SetActive(false);
                if (letterProgressText != null)
                {
                    letterProgressText.gameObject.SetActive(true);
                    letterProgressText.text = "COMPLETE! (" + cur.collectWord + ")";
                }
                SetButtonVisual(ActionState.Claim, false);
            }
            else
            {
                if (priceDiamondGroup != null)
                {
                    priceDiamondGroup.SetActive(true);
                    if (priceDiamondText != null) priceDiamondText.text = cur.unlockPriceDiamond.ToString();
                }

                if (letterProgressText != null)
                {
                    letterProgressText.gameObject.SetActive(true);
                    letterProgressText.text = FormatLetterProgress(cur.collectWord, collected);
                }

                SetButtonVisual(ActionState.Unlock, false);
            }
        }
    }

    private void SetButtonVisual(ActionState state, bool showPlayIcon)
    {
        currentActionState = state;
        if (actionButton != null) actionButton.interactable = true;

        switch (state)
        {
            case ActionState.Select:
                if (selectButtonObj != null) selectButtonObj.SetActive(true);
                if (playIcon != null) playIcon.SetActive(false);
                break;

            case ActionState.Play:
                if (selectButtonObj != null) selectButtonObj.SetActive(false);
                if (playIcon != null) playIcon.SetActive(showPlayIcon);
                break;

            case ActionState.Unlock:
            case ActionState.Claim:
                if (selectButtonObj != null) selectButtonObj.SetActive(false);
                if (playIcon != null) playIcon.SetActive(false);
                break;
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
                SetButtonVisual(ActionState.Play, true);
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
                SetButtonVisual(ActionState.Play, true);
                break;

            case ActionState.Play:
                SceneManager.LoadScene(normalNextScene);
                break;

            case ActionState.Unlock:
                int currentDiamond = PlayerPrefs.GetInt("UserDiamonds", 9999);
                if (currentDiamond >= cur.unlockPriceDiamond)
                {
                    currentDiamond -= cur.unlockPriceDiamond;
                    PlayerPrefs.SetInt("UserDiamonds", currentDiamond);
                    PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
                    SaveCharacterSelection(activeIndex, cur.characterID);

                    if (infoPanel != null) infoPanel.SetActive(false);
                    SetButtonVisual(ActionState.Play, true);
                }
                break;

            case ActionState.Claim:
                PlayerPrefs.SetInt("Unlocked_" + cur.characterID, 1);
                SaveCharacterSelection(activeIndex, cur.characterID);

                if (infoPanel != null) infoPanel.SetActive(false);
                SetButtonVisual(ActionState.Play, true);
                break;
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

    private string FormatLetterProgress(string word, int count)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string display = "";
        for (int i = 0; i < word.Length; i++)
        {
            if (i < count) display += word[i] + " ";
            else display += "_ ";
        }
        return $"{display.TrimEnd()} ({count}/{word.Length})";
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