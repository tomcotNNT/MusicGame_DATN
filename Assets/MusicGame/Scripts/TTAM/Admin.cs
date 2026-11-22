using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bộ nút admin để test: +/- coin, diamond, quản lý level, reset dữ liệu.
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
        sb.Append("Level đã mở: " +
                  (unlocked.Count > 0 ? string.Join(", ", unlocked) : "(không có)"));

        statusText.text = sb.ToString();
    }
}