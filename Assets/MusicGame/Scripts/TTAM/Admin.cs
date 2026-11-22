using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bộ nút admin để test: +/- coin, diamond, quản lý level, nhân vật, reset dữ liệu.
/// Mặc định chỉ hoạt động trong Editor và Development Build.
/// </summary>
public class AdminPanel : MonoBehaviour
{
    [Header("Bảo vệ")]
    [Tooltip("Bật: tự ẩn admin trong bản build Release (người chơi không thấy)")]
    [SerializeField] private bool onlyInDebugBuild = true;

    [Header("Mở / đóng panel")]
    [SerializeField] private Button toggleButton;   // Nút nhỏ để mở/đóng admin
    [SerializeField] private GameObject panelRoot;  // Panel chứa các nút admin

    [Header("Trạng thái")]
    [SerializeField] private TMP_Text statusText;

    [Header("Currency (+ / -)")]
    [SerializeField] private TMP_InputField amountInput;
    [SerializeField] private int defaultAmount = 100;
    [SerializeField] private Button addCoinButton;
    [SerializeField] private Button subCoinButton;
    [SerializeField] private Button addDiamondButton;
    [SerializeField] private Button subDiamondButton;

    [Header("Quản lý Level")]
    [SerializeField] private TMP_InputField levelIdInput;
    [Tooltip("Tổng số màn, dùng cho nút Mở tất cả / Khóa tất cả")]
    [SerializeField] private int totalLevels = 5;
    [SerializeField] private Button unlockLevelButton;
    [SerializeField] private Button lockLevelButton;
    [SerializeField] private Button unlockAllButton;
    [SerializeField] private Button lockAllButton;

    [Header("Quản lý Nhân vật")]
    [Tooltip("Nhập index nhân vật: 0 = nhân vật đầu tiên (luôn mở), 1, 2...")]
    [SerializeField] private TMP_InputField characterIndexInput;
    [Tooltip("Tổng số nhân vật, dùng cho nút Mở tất cả / Khóa tất cả")]
    [SerializeField] private int totalCharacters = 3;
    [SerializeField] private Button unlockCharacterButton;
    [SerializeField] private Button lockCharacterButton;
    [SerializeField] private Button unlockAllCharactersButton;
    [SerializeField] private Button lockAllCharactersButton;

    [Header("Reset dữ liệu")]
    [SerializeField] private Button resetAllButton;
    [SerializeField] private GameObject confirmPanel;   // Hộp xác nhận (có thể bỏ trống)
    [SerializeField] private Button confirmYesButton;
    [SerializeField] private Button confirmNoButton;

    private PlayerDataManager Manager => PlayerDataManager.Instance;

    private void Awake()
    {
        if (onlyInDebugBuild && !Debug.isDebugBuild)
        {
            if (toggleButton != null)
                toggleButton.gameObject.SetActive(false);

            if (panelRoot != null)
                panelRoot.SetActive(false);

            enabled = false;
        }
    }

    private void Start()
    {
        if (!enabled)
            return;

        Bind(toggleButton, TogglePanel);

        Bind(addCoinButton, () => ChangeCoin(+1));
        Bind(subCoinButton, () => ChangeCoin(-1));
        Bind(addDiamondButton, () => ChangeDiamond(+1));
        Bind(subDiamondButton, () => ChangeDiamond(-1));

        Bind(unlockLevelButton, UnlockLevel);
        Bind(lockLevelButton, LockLevel);
        Bind(unlockAllButton, UnlockAllLevels);
        Bind(lockAllButton, LockAllLevels);

        Bind(unlockCharacterButton, UnlockCharacter);
        Bind(lockCharacterButton, LockCharacter);
        Bind(unlockAllCharactersButton, UnlockAllCharacters);
        Bind(lockAllCharactersButton, LockAllCharacters);

        Bind(resetAllButton, AskResetAll);
        Bind(confirmYesButton, ConfirmReset);
        Bind(confirmNoButton, CancelReset);

        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        if (Manager != null)
        {
            Manager.OnCurrencyChanged += RefreshStatus;
            Manager.OnProgressionChanged += RefreshStatus;
        }

        RefreshStatus();
    }

    private void OnDestroy()
    {
        if (Manager != null)
        {
            Manager.OnCurrencyChanged -= RefreshStatus;
            Manager.OnProgressionChanged -= RefreshStatus;
        }
    }

    private void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    // ================= PANEL =================

    private void TogglePanel()
    {
        if (panelRoot == null)
            return;

        bool show = !panelRoot.activeSelf;
        panelRoot.SetActive(show);

        if (show)
            RefreshStatus();
    }

    // ================= CURRENCY =================

    private int ReadAmount()
    {
        if (amountInput != null &&
            int.TryParse(amountInput.text, out int value) &&
            value > 0)
        {
            return value;
        }

        return defaultAmount;
    }

    // sign = +1 để cộng, -1 để trừ
    private void ChangeCoin(int sign)
    {
        if (Manager == null) return;

        Manager.ChangeCoin(sign * ReadAmount());
    }

    private void ChangeDiamond(int sign)
    {
        if (Manager == null) return;

        Manager.ChangeDiamond(sign * ReadAmount());
    }

    // ================= LEVEL =================

    private bool TryReadLevelId(out int levelId)
    {
        levelId = 0;

        if (levelIdInput == null ||
            !int.TryParse(levelIdInput.text, out levelId) ||
            levelId <= 0)
        {
            Debug.LogWarning("Admin: nhập ID màn hợp lệ (số nguyên > 0).");
            return false;
        }

        return true;
    }

    private void UnlockLevel()
    {
        if (Manager == null) return;

        if (TryReadLevelId(out int levelId))
            Manager.UnlockLevel(levelId);
    }

    private void LockLevel()
    {
        if (Manager == null) return;

        if (TryReadLevelId(out int levelId))
            Manager.LockLevel(levelId);
    }

    private void UnlockAllLevels()
    {
        if (Manager == null) return;

        for (int id = 1; id <= totalLevels; id++)
            Manager.UnlockLevel(id);
    }

    // Khóa tất cả trừ màn 1
    private void LockAllLevels()
    {
        if (Manager == null) return;

        for (int id = 2; id <= totalLevels; id++)
            Manager.LockLevel(id);
    }

    // ================= NHÂN VẬT =================

    private bool TryReadCharacterIndex(out int index)
    {
        index = -1;

        if (characterIndexInput == null ||
            !int.TryParse(characterIndexInput.text, out index) ||
            index < 0)
        {
            Debug.LogWarning("Admin: nhập index nhân vật hợp lệ (số nguyên >= 0).");
            return false;
        }

        return true;
    }

    private void UnlockCharacter()
    {
        if (Manager == null) return;

        if (TryReadCharacterIndex(out int index))
            Manager.UnlockCharacter(index);
    }

    private void LockCharacter()
    {
        if (Manager == null) return;

        if (TryReadCharacterIndex(out int index))
        {
            if (index == 0)
                Debug.LogWarning("Admin: nhân vật 0 luôn mở, không thể khóa.");

            Manager.LockCharacter(index);
        }
    }

    private void UnlockAllCharacters()
    {
        if (Manager == null) return;

        Manager.UnlockAllCharacters(totalCharacters);
    }

    // Khóa tất cả trừ nhân vật 0
    private void LockAllCharacters()
    {
        if (Manager == null) return;

        for (int i = 1; i < totalCharacters; i++)
            Manager.LockCharacter(i);
    }

    // ================= RESET =================

    private void AskResetAll()
    {
        // Không có hộp xác nhận thì reset luôn
        if (confirmPanel == null)
        {
            ConfirmReset();
            return;
        }

        confirmPanel.SetActive(true);
    }

    private void ConfirmReset()
    {
        if (Manager != null)
            Manager.ResetAllData();

        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    private void CancelReset()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);
    }

    // ================= STATUS =================

    private void RefreshStatus()
    {
        if (statusText == null || Manager == null || Manager.Data == null)
            return;

        var data = Manager.Data;

        List<int> unlocked = new List<int>(data.progression.unlockedLevels);
        unlocked.Sort();

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Coin: " + data.currency.coin.ToString("N0"));
        sb.AppendLine("Diamond: " + data.currency.diamond.ToString("N0"));
        sb.AppendLine("Level đã mở: " +
                  (unlocked.Count > 0 ? string.Join(", ", unlocked) : "(không có)"));

        List<int> chars = data.unlockedCharacters != null
            ? new List<int>(data.unlockedCharacters)
            : new List<int>();
        chars.Sort();

        sb.Append("Nhân vật đã mở: " +
                  (chars.Count > 0 ? string.Join(", ", chars) : "(không có)"));

        statusText.text = sb.ToString();
    }
}