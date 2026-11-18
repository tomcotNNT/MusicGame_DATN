
using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text coinText;
    [SerializeField] private TMP_Text diamondText;

    private void Start()
    {
        RefreshCurrencyUI();
    }

    private void OnEnable()
    {
        if (PlayerDataManager.Instance != null)
        {
            RefreshCurrencyUI();
        }
    }

    // Cập nhật số Coin và Diamond lên UI
    public void RefreshCurrencyUI()
    {
        if (PlayerDataManager.Instance == null)
        {
            Debug.LogWarning("CurrencyManager: Không tìm thấy PlayerDataManager!");
            return;
        }

        var data = PlayerDataManager.Instance.Data;

        if (data == null || data.currency == null)
        {
            Debug.LogWarning("CurrencyManager: Dữ liệu Currency không tồn tại!");
            return;
        }

        if (coinText != null)
            coinText.text = data.currency.coin.ToString("N0");

        if (diamondText != null)
            diamondText.text = data.currency.diamond.ToString("N0");
    }
}
