using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyRewardManager : MonoBehaviour
{
    [Header("Daily Reward UI")]
    public GameObject dailyRewardPanel;
    public Button claimButton;
    public TMP_Text claimButtonText;
    public TMP_Text statusText;

    [Header("7 Reward Images (Day 1 to Day 7)")]
    public Image[] dayImages = new Image[7];

    [Header("Lobby Currency Display (optional)")]
    public LobbyCoinDisplay coinUI;
    public LobbyDiamondDisplay diamondUI;
    public LobbyHeartDisplay heartUI;

    [Header("Rewards: Coin, Diamond, Diamond, Heart, TIME, TIME, Character")]
    public int[] rewardAmounts = { 50, 50, 100, 5, 1, 2, 1 };

    [Header("Day 7 Character")]
    [Tooltip("Unique character ID used by your character selection system")]
    public string day7CharacterId = "DailyGiftCharacter";

    [Header("Fade Settings")]
    [Range(0f, 1f)] public float claimedAlpha = 0.4f;

    private const string LastClaimKey = "Daily_LastClaim";
    private const string DayKey = "Daily_CurrentDay";
    private const string ClaimedKey = "Daily_Claimed";
    private const string CharacterKeyPrefix = "CharacterUnlocked_";

    private int currentDay = 1;
    private bool claimedToday;

    private void Awake()
    {
        LoadData();
    }

    private void OnEnable()
    {
        if (claimButton != null)
        {
            claimButton.onClick.RemoveListener(ClaimReward);
            claimButton.onClick.AddListener(ClaimReward);
        }
        LoadData();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(ClaimReward);
    }

    private static DateTime TodayUtc => DateTime.UtcNow.Date;

    private void LoadData()
    {
        currentDay = Mathf.Clamp(PlayerPrefs.GetInt(DayKey, 1), 1, 7);
        claimedToday = PlayerPrefs.GetInt(ClaimedKey, 0) == 1;
        string storedDate = PlayerPrefs.GetString(LastClaimKey, "");

        if (string.IsNullOrEmpty(storedDate))
        {
            // Legacy data without a claim date should not block claiming.
            claimedToday = false;
            return;
        }

        if (!DateTime.TryParseExact(storedDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastDate))
        {
            Debug.LogWarning("Daily Gift: invalid saved claim date.");
            return;
        }

        if (TodayUtc > lastDate.Date && claimedToday)
        {
            currentDay = currentDay % 7 + 1;
            claimedToday = false;
            PlayerPrefs.SetInt(DayKey, currentDay);
            PlayerPrefs.SetInt(ClaimedKey, 0);
            PlayerPrefs.Save();
        }
    }

    public void ClaimReward()
    {
        LoadData();
        if (claimedToday)
        {
            ShowStatus("Already claimed today!");
            RefreshUI();
            return;
        }

        string storedDate = PlayerPrefs.GetString(LastClaimKey, "");
        if (DateTime.TryParseExact(storedDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime lastDate))
        {
            if (TodayUtc < lastDate.Date)
            {
                ShowStatus("Invalid device date!");
                return;
            }
            if (TodayUtc == lastDate.Date)
            {
                ShowStatus("Already claimed today!");
                return;
            }
        }

        if (rewardAmounts == null || rewardAmounts.Length < 7)
        {
            Debug.LogError("Daily Gift: rewardAmounts needs 7 entries.");
            return;
        }

        if (!GiveReward(currentDay))
        {
            ShowStatus("Could not grant reward!");
            return;
        }

        PlayerPrefs.SetString(LastClaimKey, TodayUtc.ToString("yyyy-MM-dd"));
        PlayerPrefs.SetInt(DayKey, currentDay);
        PlayerPrefs.SetInt(ClaimedKey, 1);
        PlayerPrefs.Save();
        claimedToday = true;

        if (coinUI != null) coinUI.UpdateCoinUI();
        if (diamondUI != null) diamondUI.UpdateDiamondUI();
        if (heartUI != null) heartUI.UpdateHeartUI();
        RefreshUI();
        ShowStatus("Day " + currentDay + " reward claimed!");
    }

    private bool GiveReward(int day)
    {
        int amount = rewardAmounts[day - 1];
        if (amount <= 0) return false;

        switch (day)
        {
            case 1:
                if (GameManager.Instance == null) return Missing("GameManager");
                GameManager.Instance.AddCoins(amount);
                break;
            case 2:
            case 3:
                if (GameManager.Instance == null) return Missing("GameManager");
                GameManager.Instance.AddDiamonds(amount);
                break;
            case 4:
                PlayerPrefs.SetInt("Heart", PlayerPrefs.GetInt("Heart", 0) + amount);
                PlayerPrefs.Save();
                break;
            case 5:
            case 6:
                if (GachaTicketManager.Instance == null) return Missing("GachaTicketManager");
                GachaTicketManager.Instance.AddTickets(amount);
                break;
            case 7:
                if (string.IsNullOrWhiteSpace(day7CharacterId))
                    return Missing("Day 7 Character ID");
                // Unlock flag: connect this same key to your character-selection UI.
                PlayerPrefs.SetInt(CharacterKeyPrefix + day7CharacterId, 1);
                PlayerPrefs.Save();
                break;
            default:
                return false;
        }

        Debug.Log("Daily Gift: Day " + day + " +" + amount);
        return true;
    }

    private bool Missing(string component)
    {
        Debug.LogError("Daily Gift: missing " + component);
        return false;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < 7; i++)
        {
            bool wasClaimed = i < currentDay - 1 ||
                              (i == currentDay - 1 && claimedToday);
            if (dayImages != null && i < dayImages.Length && dayImages[i] != null)
            {
                Color color = dayImages[i].color;
                color.a = wasClaimed ? claimedAlpha : 1f;
                dayImages[i].color = color;
            }
        }

        if (claimButton != null) claimButton.interactable = !claimedToday;
        if (claimButtonText != null)
            claimButtonText.text = claimedToday ? "CLAIMED" : "CLAIM";
    }

    private void ShowStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    public void OpenPanel()
    {
        if (dailyRewardPanel != null) dailyRewardPanel.SetActive(true);
        LoadData();
        RefreshUI();
    }

    public void ClosePanel()
    {
        if (dailyRewardPanel != null) dailyRewardPanel.SetActive(false);
    }

    [ContextMenu("Test: Reset Daily Gift")]
    public void ResetDailyGift()
    {
        PlayerPrefs.DeleteKey(LastClaimKey);
        PlayerPrefs.DeleteKey(DayKey);
        PlayerPrefs.DeleteKey(ClaimedKey);
        PlayerPrefs.Save();
        currentDay = 1;
        claimedToday = false;
        RefreshUI();
        ShowStatus("Daily Gift reset to Day 1");
    }
}
