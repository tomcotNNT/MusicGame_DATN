using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LetterDebugPanel : MonoBehaviour
{
    [Header("1. Khung & Ô Nhập")]
    [SerializeField] private TMP_InputField inputFieldLetters;

    [Header("2. Các Nút Bấm")]
    [SerializeField] private Button btnAddInputLetters;
    [SerializeField] private Button btnGiveFullCurrent;
    [SerializeField] private Button btnResetCurrent;

    [Header("3. Hiển Thị Kho Chữ")]
    [SerializeField] private TextMeshProUGUI textInventoryStatus;

    [Header("4. Quản Lý Nhân Vật")]
    [SerializeField] private CharacterSystemManager characterManager;

    private void Awake()
    {
        if (characterManager == null)
            characterManager = FindAnyObjectByType<CharacterSystemManager>();
    }

    private void Start()
    {
        if (btnAddInputLetters != null)
            btnAddInputLetters.onClick.AddListener(OnAddInputLettersClicked);

        if (btnGiveFullCurrent != null)
            btnGiveFullCurrent.onClick.AddListener(OnGiveFullForCurrentCharacter);

        if (btnResetCurrent != null)
            btnResetCurrent.onClick.AddListener(OnResetCurrentCharacter);

        UpdateStatusText();

        // Tự ẩn bảng lúc vừa vào game
        gameObject.SetActive(false);
    }

    private void Update()
    {
        // Vẫn hỗ trợ phím M nếu hệ thống phím cũ hoạt động
        if (Input.GetKeyDown(KeyCode.M))
        {
            if (inputFieldLetters != null && inputFieldLetters.isFocused) return;
            ToggleDebugPanel();
        }
    }

    // Hàm gọi trực tiếp từ nút DEBUG trên màn hình
    public void ToggleDebugPanel()
    {
        bool nextState = !gameObject.activeSelf;
        gameObject.SetActive(nextState);

        if (nextState)
        {
            UpdateStatusText();
            if (inputFieldLetters != null)
            {
                inputFieldLetters.text = "";
                inputFieldLetters.ActivateInputField();
            }
        }
    }

    private void OnAddInputLettersClicked()
    {
        if (inputFieldLetters == null || string.IsNullOrEmpty(inputFieldLetters.text)) return;

        string text = inputFieldLetters.text.Trim().ToUpper();
        foreach (char c in text)
        {
            if (char.IsLetter(c))
            {
                LetterInventoryManager.AddLetter(c, 1);
            }
        }

        inputFieldLetters.text = "";
        RefreshSystem();
    }

    private void OnGiveFullForCurrentCharacter()
    {
        if (characterManager == null) return;
        var cur = characterManager.GetCurrentCharacter();
        if (cur == null || string.IsNullOrEmpty(cur.collectWord)) return;

        PlayerPrefs.DeleteKey("Unlocked_" + cur.characterID);

        foreach (char c in cur.collectWord.ToUpper())
        {
            LetterInventoryManager.AddLetter(c, 1);
        }

        RefreshSystem();
    }

    private void OnResetCurrentCharacter()
    {
        if (characterManager == null) return;
        var cur = characterManager.GetCurrentCharacter();
        if (cur == null) return;

        if (cur.characterID != "Ramona")
        {
            PlayerPrefs.DeleteKey("Unlocked_" + cur.characterID);
        }

        if (!string.IsNullOrEmpty(cur.collectWord))
        {
            foreach (char c in cur.collectWord.ToUpper())
            {
                PlayerPrefs.DeleteKey("Letter_Inv_" + c);
            }
        }
        PlayerPrefs.Save();

        RefreshSystem();
    }

    private void RefreshSystem()
    {
        if (characterManager != null)
        {
            characterManager.UpdateUIByCharacter(characterManager.ActiveIndex);
        }
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (textInventoryStatus == null || characterManager == null) return;

        var cur = characterManager.GetCurrentCharacter();
        if (cur == null) return;

        string word = string.IsNullOrEmpty(cur.collectWord) ? "NONE" : cur.collectWord;
        string info = $"<b>Tướng:</b> <color=yellow>{cur.displayName}</color> (Cần: <color=#00FFFF>{word}</color>)\n<b>Kho chữ hiện có:</b> ";

        if (string.IsNullOrEmpty(cur.collectWord))
        {
            textInventoryStatus.text = info + "<color=grey>Không cần chữ</color>";
            return;
        }

        string checkedChars = "";
        foreach (char c in cur.collectWord.ToUpper())
        {
            if (!checkedChars.Contains(c.ToString()))
            {
                int count = LetterInventoryManager.GetLetterCount(c);
                info += $"[{c}: <color={(count > 0 ? "#00FF66" : "#FF4444")}>{count}</color>] ";
                checkedChars += c;
            }
        }

        textInventoryStatus.text = info;
    }
}