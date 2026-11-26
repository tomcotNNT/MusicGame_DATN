
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class WatchAdsUI : MonoBehaviour
{
    [Header("Ads")]
    public RewardedAdsManager adsManager;

    [Header("Buttons")]
    public Button watchAdsButton;
    public Button okButton;

    [Header("UI")]
    public GameObject noticePanel;
    public TextMeshProUGUI noticeTime;

    [Header("Settings")]
    public int maxDailyAds = 5;
    public int cooldownMinutes = 5;

    [Header("Phần thưởng mỗi lần xem hết quảng cáo")]
    [Tooltip("Bảng thông báo đang ghi 'YOU RECEIVED 500' -> mặc định +500 coin")]
    public CurrencyManager.CurrencyType rewardType = CurrencyManager.CurrencyType.Coin;
    public int rewardAmount = 500;

    private int usedAds;
    private long nextAllowedTime;
    private string savedDate;
    private bool isWatching;

    private const string UsedKey = "WatchAds_Used";
    private const string DateKey = "WatchAds_Date";
    private const string CooldownKey = "WatchAds_Cooldown";

    private void Start()
    {
        LoadData();

        noticePanel.SetActive(false);

        watchAdsButton.onClick.AddListener(WatchAds);
        okButton.onClick.AddListener(CloseNotice);

        UpdateUI();
    }

    private void Update()
    {
        CheckNewDay();
        UpdateUI();

        // Phím R để reset khi debug
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        #if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetAds();
        }
        #elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAds();
        }
        #endif
        #endif
    }

    private void WatchAds()
    {
        CheckNewDay();

        if (isWatching || usedAds >= maxDailyAds)
            return;

        if (DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            < nextAllowedTime)
            return;

        isWatching = true;

        bool started = adsManager.ShowRewarded(
            OnReward,
            OnAdFailed
        );

        if (!started)
            isWatching = false;

        UpdateUI();
    }

    private void OnReward()
    {
        isWatching = false;

        usedAds++;

        nextAllowedTime =
            DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            + cooldownMinutes * 60;

        SaveData();

        // Trả thưởng vào dữ liệu người chơi (JSON) -> UI tiền tự cập nhật qua OnCurrencyChanged
        if (!PurchasePanel.AddCurrency(rewardType, rewardAmount))
            Debug.LogWarning("WatchAdsUI: không cộng được phần thưởng quảng cáo!");

        noticePanel.SetActive(true);

        UpdateUI();
    }

    private void OnAdFailed()
    {
        isWatching = false;
        UpdateUI();
    }

    private void CloseNotice()
    {
        noticePanel.SetActive(false);
    }

    private void UpdateUI()
    {
        long remaining =
            nextAllowedTime -
            DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        int availableAds = maxDailyAds - usedAds;

        if (availableAds <= 0)
        {
            noticeTime.text = "WATCH AD (0/5)";
            watchAdsButton.interactable = false;
        }
        else if (remaining > 0)
        {
            int minutes = (int)(remaining / 60);
            int seconds = (int)(remaining % 60);

            noticeTime.text =
                $"WATCH AD ({availableAds}/5)\n" +
                $"{minutes:00}:{seconds:00}";

            watchAdsButton.interactable = false;
        }
        else
        {
            noticeTime.text =
                $"WATCH AD ({availableAds}/5)";

            watchAdsButton.interactable = !isWatching;
        }
    }

    private void CheckNewDay()
    {
        string today = DateTime.Now.ToString("yyyyMMdd");

        if (savedDate != today)
        {
            savedDate = today;
            usedAds = 0;
            nextAllowedTime = 0;

            SaveData();
        }
    }

    private void LoadData()
    {
        savedDate = PlayerPrefs.GetString(DateKey, "");
        string today = DateTime.Now.ToString("yyyyMMdd");

        if (savedDate != today)
        {
            savedDate = today;
            usedAds = 0;
            nextAllowedTime = 0;
            SaveData();
        }
        else
        {
            usedAds = PlayerPrefs.GetInt(UsedKey, 0);

            long.TryParse(
                PlayerPrefs.GetString(CooldownKey, "0"),
                out nextAllowedTime
            );
        }
    }

    private void SaveData()
    {
        PlayerPrefs.SetInt(UsedKey, usedAds);
        PlayerPrefs.SetString(DateKey, savedDate);

        PlayerPrefs.SetString(
            CooldownKey,
            nextAllowedTime.ToString()
        );

        PlayerPrefs.Save();
    }

    private void ResetAds()
    {
        usedAds = 0;
        nextAllowedTime = 0;

        SaveData();
        UpdateUI();

        Debug.Log("DEBUG: Rewarded Ads reset to 5/5!");
    }
}
