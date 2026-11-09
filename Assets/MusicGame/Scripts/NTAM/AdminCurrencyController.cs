using UnityEngine;
using TMPro;

public class AdminCurrencyController : MonoBehaviour
{
    [Header("UI Hiển Thị Trên Sảnh (Kéo thả từ Buttons)")]
    [SerializeField] private TextMeshProUGUI goldText;    // Kéo Buttons > Gold > SoLuong
    [SerializeField] private TextMeshProUGUI diamondText; // Kéo Buttons > Diamond > SoLuong

    [Header("Can Thiệp Bằng Số Tùy Chọn")]
    [Tooltip("Nhập số vàng bạn muốn gán rồi bấm chuột phải vào script chọn 'Gán Giá Trị Đã Nhập'")]
    [SerializeField] private int targetGold = 1000;
    [SerializeField] private int targetDiamond = 100;

    [Header("Bật/Tắt Phím Tắt Cheat Trong Khi Play")]
    [SerializeField] private bool enableHotkeys = true;

    private void Start()
    {
        // Đồng bộ dữ liệu thực tế từ GameManager lên màn hình ngay khi mở Lobby
        UpdateCurrencyUI();
    }

    private void Update()
    {
        if (!enableHotkeys) return;

        // Phím G: Thêm nhanh 1,000 Vàng
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddGold(1000);
        }

        // Phím H: Thêm nhanh 100 Kim Cương
        if (Input.GetKeyDown(KeyCode.H))
        {
            AddDiamond(100);
        }

        // Phím R: Reset cả Vàng và Kim Cương về 0
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllToZero();
        }
    }

    // =========================================================================
    // CÁC HÀM CAN THIỆP DỮ LIỆU
    // =========================================================================

    public void AddGold(int amount)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddCoins(amount);
        UpdateCurrencyUI();
        Debug.Log($"<color=yellow>>>> [ADMIN] +{amount} Vàng | Tổng: {GameManager.Instance.playerData.totalCoins}</color>");
    }

    public void AddDiamond(int amount)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.AddDiamonds(amount);
        UpdateCurrencyUI();
        Debug.Log($"<color=cyan>>>> [ADMIN] +{amount} Kim Cương | Tổng: {GameManager.Instance.playerData.totalDiamonds}</color>");
    }

    [ContextMenu("1. Gán Giá Trị Đã Nhập (Target Gold & Diamond)")]
    public void SetCustomCurrency()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError(">>> [ADMIN] Không tìm thấy GameManager! Hãy chạy từ LoadingScene 1.");
            return;
        }

        GameManager.Instance.playerData.totalCoins = Mathf.Max(0, targetGold);
        GameManager.Instance.playerData.totalDiamonds = Mathf.Max(0, targetDiamond);
        GameManager.Instance.SaveGameData();

        UpdateCurrencyUI();
        Debug.Log($"<color=green>>>> [ADMIN] Đã gán: {targetGold} Vàng | {targetDiamond} Kim Cương</color>");
    }

    [ContextMenu("2. Thêm Nhanh 10,000 Vàng")]
    public void QuickAdd10KGold() => AddGold(10000);

    [ContextMenu("3. Thêm Nhanh 500 Kim Cương")]
    public void QuickAdd500Diamond() => AddDiamond(500);

    [ContextMenu("4. Đưa Toàn Bộ Về 0")]
    public void ResetAllToZero()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.playerData.totalCoins = 0;
        GameManager.Instance.playerData.totalDiamonds = 0;
        GameManager.Instance.SaveGameData();

        UpdateCurrencyUI();
        Debug.Log("<color=red>>>> [ADMIN] Đã đưa Vàng và Kim Cương về 0!</color>");
    }

    /// <summary>
    /// Đồng bộ dữ liệu từ GameManager vào component TextMeshPro
    /// </summary>
    public void UpdateCurrencyUI()
    {
        if (GameManager.Instance == null) return;

        if (goldText != null)
        {
            goldText.text = GameManager.Instance.playerData.totalCoins.ToString();
        }

        if (diamondText != null)
        {
            diamondText.text = GameManager.Instance.playerData.totalDiamonds.ToString();
        }
    }
}