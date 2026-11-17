
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

    [Header("7 Reward Images")]
    public Image[] dayImages = new Image[7];

    [Header("Lobby Currency Display")]
    public LobbyCoinDisplay coinUI;
    public LobbyDiamondDisplay diamondUI;
    public LobbyHeartDisplay heartUI;

    [Header("Reward Amounts")]
    public int[] rewardAmounts =
    {
        50, 50, 500, 10, 2, 2, 3
    };

    [Header("Fade Settings")]
    [Range(0f, 1f)]
    public float claimedAlpha = 0.4f;

    private const string LastClaimKey = "Daily_LastClaim";
    private const string DayKey = "Daily_CurrentDay";
    private const string ClaimedKey = "Daily_Claimed";

    private int currentDay = 1;
    private bool claimedToday;

    private void Start()
    {
        if (claimButton != null)
            claimButton.onClick.AddListener(ClaimReward);

        LoadData();
        RefreshUI();
    }

    private void OnEnable()
    {
        LoadData();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (claimButton != null)
            claimButton.onClick.RemoveListener(ClaimReward);
    }

    private void LoadData()
    {
        currentDay = Mathf.Clamp(
            PlayerPrefs.GetInt(DayKey, 1), 1, 7
        );

        string lastClaim =
            PlayerPrefs.GetString(LastClaimKey, "");

        bool wasClaimed =
            PlayerPrefs.GetInt(ClaimedKey, 0) == 1;

        claimedToday = wasClaimed;

        DateTime lastDate;

        if (wasClaimed &&
            DateTime.TryParseExact(
                lastClaim, "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out lastDate))
        {
            if (DateTime.Today > lastDate.Date)
            {
                currentDay = currentDay % 7 + 1;
                claimedToday = false;

                PlayerPrefs.SetInt(DayKey, currentDay);
                PlayerPrefs.SetInt(ClaimedKey, 0);
                PlayerPrefs.Save();
            }
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

        string lastClaim =
            PlayerPrefs.GetString(LastClaimKey, "");

        DateTime lastDate;

        if (DateTime.TryParseExact(
            lastClaim, "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out lastDate))
        {
            if (DateTime.Today < lastDate.Date)
            {
                ShowStatus("Invalid device date!");
                return;
            }
        }

        if (rewardAmounts == null ||
            rewardAmounts.Length < 7)
        {
            Debug.LogError(
                "Reward Amounts must contain 7 values!"
            );
            return;
        }

        // Give the reward
        if (!GiveReward(currentDay))
        {
            ShowStatus("Could not grant reward!");
            return;
        }

        // Save claim data
        PlayerPrefs.SetString(
            LastClaimKey,
            DateTime.Today.ToString("yyyy-MM-dd")
        );

        PlayerPrefs.SetInt(DayKey, currentDay);
        PlayerPrefs.SetInt(ClaimedKey, 1);
        PlayerPrefs.Save();

        claimedToday = true;

        // Update coin text immediately
        // Cập nhật cả Coin và Diamond trên Lobby
if (coinUI != null)
    coinUI.UpdateCoinUI();

if (diamondUI != null)
    diamondUI.UpdateDiamondUI();

if (heartUI != null)
    heartUI.UpdateHeartUI();

        // Fade the claimed image
        RefreshUI();

        ShowStatus(
            "Day " + currentDay + " reward claimed!"
        );
    }

    private bool GiveReward(int day)
    {
        int amount = rewardAmounts[day - 1];
        if (amount <= 0) return false;

        if (day >= 1 && day <= 3)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("Daily Gift: GameManager not found!");
                return false;
            }

            if (day == 1) GameManager.Instance.AddCoins(amount);
            else GameManager.Instance.AddDiamonds(amount);
        }
        else
        {
            string rewardKey = day == 4 ? "Heart" :
                               day == 5 ? "Headphone" :
                               day == 6 ? "SpecialItem" :
                               day == 7 ? "RareItem" : "";
            if (string.IsNullOrEmpty(rewardKey)) return false;
            PlayerPrefs.SetInt(rewardKey, PlayerPrefs.GetInt(rewardKey, 0) + amount);
            PlayerPrefs.Save();
        }

        Debug.Log("Daily Gift: Day " + day + " +" + amount);
        return true;
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

    private void RefreshUI()
    {
        for (int i = 0; i < 7; i++)
        {
            bool isClaimed =
                i < currentDay - 1 ||
                (i == currentDay - 1 && claimedToday);

            if (dayImages != null &&
                i < dayImages.Length &&
                dayImages[i] != null)
            {
                Color imageColor = dayImages[i].color;

                // Claimed image becomes transparent
                imageColor.a = isClaimed
                    ? claimedAlpha
                    : 1f;

                dayImages[i].color = imageColor;
            }
        }

        if (claimButton != null)
            claimButton.interactable = !claimedToday;

        if (claimButtonText != null)
            claimButtonText.text =
                claimedToday ? "CLAIMED" : "CLAIM";
    }

    private void ShowStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }

    public void OpenPanel()
    {
        LoadData();
        RefreshUI();

        if (dailyRewardPanel != null)
            dailyRewardPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (dailyRewardPanel != null)
            dailyRewardPanel.SetActive(false);
    }
}
