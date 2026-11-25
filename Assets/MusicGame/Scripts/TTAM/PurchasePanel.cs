using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào bảng Purchase (1 script duy nhất cho cả bảng).
/// Thêm từng gói vào List "Packs": kéo Button vào, chọn loại tiền, ghi số lượng.
/// Bấm nút nào sẽ cộng đúng số của gói đó vào currency (PlayerData.json).
/// </summary>
public class PurchasePanel : MonoBehaviour
{
    [Serializable]
    public class PurchasePack
    {
        [Tooltip("Tên gói (chỉ để dễ nhìn trong Inspector)")]
        public string name = "Pack";

        [Tooltip("Nút bấm của gói")]
        public Button button;

        [Tooltip("Loại tiền được cộng")]
        public CurrencyManager.CurrencyType currencyType = CurrencyManager.CurrencyType.Coin;

        [Tooltip("Số lượng được cộng khi bấm")]
        [Min(1)] public int amount = 100;

        [Tooltip("(Tùy chọn) Text hiển thị số lượng của gói")]
        public TMP_Text amountText;
    }

    [Header("Danh sách gói nạp")]
    [SerializeField] private List<PurchasePack> packs = new List<PurchasePack>();

    [Header("Hiển thị số lượng (tùy chọn)")]
    [Tooltip("Chữ phía trước số, VD: \"+\"")]
    [SerializeField] private string prefix = "";
    [Tooltip("Chữ phía sau số, VD: \" Coin\"")]
    [SerializeField] private string suffix = "";

    private void Awake()
    {
        foreach (PurchasePack pack in packs)
        {
            if (pack == null || pack.button == null)
                continue;

            PurchasePack p = pack; // tránh lỗi closure trong vòng lặp
            pack.button.onClick.AddListener(() => Buy(p));
        }
    }

    private void Start()
    {
        RefreshTexts();
    }

    private void OnValidate()
    {
        // Cập nhật text ngay trong Editor khi sửa số
        RefreshTexts();
    }

    private void OnDestroy()
    {
        foreach (PurchasePack pack in packs)
        {
            if (pack != null && pack.button != null)
                pack.button.onClick.RemoveAllListeners();
        }
    }

    private void RefreshTexts()
    {
        if (packs == null) return;

        foreach (PurchasePack pack in packs)
        {
            if (pack != null && pack.amountText != null)
                pack.amountText.text = prefix + pack.amount.ToString("N0") + suffix;
        }
    }

    private void Buy(PurchasePack pack)
    {
        AddCurrency(pack.currencyType, pack.amount);
    }

    /// <summary>Mua gói theo vị trí trong List (có thể gán vào OnClick nếu muốn).</summary>
    public void BuyByIndex(int index)
    {
        if (index < 0 || index >= packs.Count)
        {
            Debug.LogWarning("PurchasePanel: index gói không hợp lệ: " + index);
            return;
        }

        Buy(packs[index]);
    }

    /// <summary>Cộng tiền với loại và số lượng tùy ý (có thể gọi từ code khác).</summary>
    public static bool AddCurrency(CurrencyManager.CurrencyType type, int value)
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("PurchasePanel: Không tìm thấy PlayerDataManager trong scene!");
            return false;
        }

        if (value <= 0) return false;

        switch (type)
        {
            case CurrencyManager.CurrencyType.Coin:
                PlayerDataManager.Instance.AddCoin(value);
                break;

            case CurrencyManager.CurrencyType.Diamond:
                PlayerDataManager.Instance.AddDiamond(value);
                break;
        }

        Debug.Log($"Đã cộng {value} {type}.");
        return true;
    }
}
