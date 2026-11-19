using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CurrencyManager : MonoBehaviour
{
    public enum CurrencyType
    {
        Coin,
        Diamond
    }

    [Serializable]
    public class CurrencyUI
    {
        public CurrencyType type;
        public TMP_Text text;
    }

    [Header("UI References")]
    [Tooltip("Mỗi phần tử = 1 Text hiển thị 1 loại tiền. Có thể thêm nhiều Text cho cùng 1 loại.")]
    [SerializeField] private List<CurrencyUI> currencyList = new List<CurrencyUI>();

    private bool subscribed = false;

    private void Start()
    {
        // Phòng trường hợp OnEnable chạy trước khi PlayerDataManager kịp khởi tạo
        TrySubscribe();
        RefreshCurrencyUI();
    }

    private void OnEnable()
    {
        TrySubscribe();

        if (PlayerDataManager.Instance != null)
        {
            RefreshCurrencyUI();
        }
    }

    private void OnDisable()
    {
        if (subscribed && PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnCurrencyChanged -= RefreshCurrencyUI;

        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || PlayerDataManager.Instance == null)
            return;

        PlayerDataManager.Instance.OnCurrencyChanged += RefreshCurrencyUI;
        subscribed = true;
    }

    // Cập nhật tất cả loại tiền trong List lên UI
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

        foreach (CurrencyUI item in currencyList)
        {
            if (item == null || item.text == null)
                continue;

            item.text.text = GetAmount(data.currency, item.type).ToString("N0");
        }
    }

    private int GetAmount(CurrencyData currency, CurrencyType type)
    {
        switch (type)
        {
            case CurrencyType.Coin:
                return currency.coin;

            case CurrencyType.Diamond:
                return currency.diamond;

            default:
                return 0;
        }
    }
}