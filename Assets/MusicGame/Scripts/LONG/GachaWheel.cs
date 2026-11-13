
using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaWheel : MonoBehaviour
{
    public enum RewardType
    {
        Coin,
        Diamond,
        Heart,
        Nothing
    }

    [Serializable]
    public class GachaReward
    {
        public RewardType type;
        public int amount = 50;

        [Min(0)]
        public float weight = 10f;
    }

    [Header("Wheel")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minFullRotations = 4;
    [SerializeField] private int maxFullRotations = 7;

    [Header("Wheel Alignment")]
    [Tooltip("8 equal slots, clockwise from slot 1")]
    [SerializeField] private float firstSlotAngle = 90f;
    [SerializeField] private float pointerAngle = 180f;
    [SerializeField] private bool clockwise = true;

    [Header("Rewards - Clockwise Order")]
    [SerializeField] private GachaReward[] rewards =
    {
        new GachaReward { type = RewardType.Coin, amount = 100, weight = 20 },
        new GachaReward { type = RewardType.Diamond, amount = 50, weight = 10 },
        new GachaReward { type = RewardType.Heart, amount = 5, weight = 15 },
        new GachaReward { type = RewardType.Nothing, amount = 0, weight = 25 },
        new GachaReward { type = RewardType.Coin, amount = 200, weight = 15 },
        new GachaReward { type = RewardType.Diamond, amount = 100, weight = 5 },
        new GachaReward { type = RewardType.Heart, amount = 10, weight = 5 },
        new GachaReward { type = RewardType.Coin, amount = 500, weight = 5 }
    };

    [Header("Buttons")]
    [SerializeField] private Button x1Button;
    [SerializeField] private Button x5Button;
    [SerializeField] private Button adsFreeButton;
    [SerializeField] private Button okButton;

    [Header("Spin Costs")]
    [SerializeField] private int x1DiamondCost = 50;
    [SerializeField] private int x5DiamondCost = 200;

    [Header("Notice Panel")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeText;

    [Header("Rewarded Ads")]
    [SerializeField] private RewardedAdsManager adsManager;

    [Header("ADS Button Counter")]
    [SerializeField] private TMP_Text adsNotice;

    private const int MaxAdsSpins = 5;
    private const long CooldownSeconds = 5 * 60;
    private const long RefillSeconds = 24 * 60 * 60;

    private const string UsesKey = "Gacha_Ads_Uses";
    private const string CooldownKey = "Gacha_Ads_Cooldown";
    private const string RefillKey = "Gacha_Ads_Refill";

    private bool isSpinning;
    private bool isWaitingForAd;
    private float currentAngle;

    private int adsUses;
    private long cooldownUntil;
    private long refillUntil;

    private long Now =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private void Start()
    {
        Debug.Log(
        "GameManager Instance: " +
        (GameManager.Instance == null
            ? "NULL"
            : "FOUND")
    );
        if (x1Button != null)
            x1Button.onClick.AddListener(SpinX1);

        if (x5Button != null)
            x5Button.onClick.AddListener(SpinX5);

        if (adsFreeButton != null)
            adsFreeButton.onClick.AddListener(SpinWithRewardedAd);

        if (okButton != null)
            okButton.onClick.AddListener(CloseNotice);

        if (noticePanel != null)
            noticePanel.SetActive(false);

        if (wheel != null)
            currentAngle = wheel.localEulerAngles.z;

        LoadAdsState();
        RefreshUI();
    }

    private void OnDestroy()
    {
        if (x1Button != null)
            x1Button.onClick.RemoveListener(SpinX1);

        if (x5Button != null)
            x5Button.onClick.RemoveListener(SpinX5);

        if (adsFreeButton != null)
            adsFreeButton.onClick.RemoveListener(SpinWithRewardedAd);

        if (okButton != null)
            okButton.onClick.RemoveListener(CloseNotice);
    }

    private void Update()
    {
        // Test ADS reset in Editor only.
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F6))
        {
            adsUses = MaxAdsSpins;
            cooldownUntil = 0;
            refillUntil = 0;
            SaveAdsState();
        }
#endif

        if (adsUses <= 0 &&
            refillUntil > 0 &&
            Now >= refillUntil)
        {
            adsUses = MaxAdsSpins;
            cooldownUntil = 0;
            refillUntil = 0;
            SaveAdsState();
        }

        RefreshUI();
    }

    private bool IsBusy()
    {
        return isSpinning ||
               isWaitingForAd ||
               (noticePanel != null && noticePanel.activeSelf);
    }

    private bool ValidateWheel()
    {
        if (wheel == null)
        {
            Debug.LogError("Wheel is not assigned!");
            return false;
        }

        if (rewards == null || rewards.Length != 8)
        {
            Debug.LogError("Gacha requires exactly 8 rewards!");
            return false;
        }

        float totalWeight = 0f;

        foreach (GachaReward reward in rewards)
        {
            if (reward == null)
                return false;

            if (reward.type != RewardType.Nothing &&
                reward.amount <= 0)
                return false;

            totalWeight += Mathf.Max(0f, reward.weight);
        }

        return totalWeight > 0f;
    }

    public void SpinX1()
    {
        StartPaidSpin(1, x1DiamondCost);
    }

    public void SpinX5()
    {
        StartPaidSpin(5, x5DiamondCost);
    }

    private void StartPaidSpin(int count, int cost)
    {
        if (IsBusy() || !ValidateWheel())
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found!");
            return;
        }

        if (cost < 0)
            return;

        // SpendDiamonds must exist in your GameManager.
        if (cost > 0 &&
            !GameManager.Instance.SpendDiamonds(cost))
        {
            ShowNotice("Not enough Diamonds!");
            return;
        }

        StartCoroutine(SpinSequence(count));
    }

    public void SpinWithRewardedAd()
    {
        if (IsBusy() || !ValidateWheel())
            return;

        if (adsUses <= 0 || Now < cooldownUntil)
            return;

        if (adsManager == null)
        {
            Debug.LogError("RewardedAdsManager is missing!");
            return;
        }

        isWaitingForAd = true;
        RefreshUI();

        bool started = adsManager.ShowRewarded(
            OnRewardedAdCompleted
        );

        if (!started)
        {
            isWaitingForAd = false;
            RefreshUI();
        }
    }

    private void OnRewardedAdCompleted()
    {
        if (!isWaitingForAd)
            return;

        isWaitingForAd = false;
        adsUses--;

        if (adsUses <= 0)
        {
            adsUses = 0;
            cooldownUntil = 0;
            refillUntil = Now + RefillSeconds;
        }
        else
        {
            cooldownUntil = Now + CooldownSeconds;
        }

        SaveAdsState();
        StartCoroutine(SpinSequence(1));
    }

    private int ChooseReward()
    {
        float total = 0f;

        foreach (GachaReward reward in rewards)
            total += Mathf.Max(0f, reward.weight);

        float random = UnityEngine.Random.Range(0f, total);

        for (int i = 0; i < rewards.Length; i++)
        {
            random -= Mathf.Max(0f, rewards[i].weight);

            if (random < 0f && rewards[i].weight > 0f)
                return i;
        }

        for (int i = rewards.Length - 1; i >= 0; i--)
        {
            if (rewards[i].weight > 0f)
                return i;
        }

        return 0;
    }

    private IEnumerator SpinSequence(int count)
    {
        isSpinning = true;
        RefreshUI();

        StringBuilder result = new StringBuilder();

        for (int i = 0; i < count; i++)
        {
            int selectedIndex = ChooseReward();

            yield return StartCoroutine(
                SpinToReward(selectedIndex)
            );

            GachaReward reward = rewards[selectedIndex];

            GiveReward(reward);

            if (reward.type == RewardType.Nothing)
            {
                result.AppendLine(
                    "Chúc bạn may mắn lần sau!"
                );
            }
            else
            {
                result.AppendLine(
                    "Nhận được " + reward.amount +
                    " " + reward.type
                );
            }
        }

        isSpinning = false;
        ShowNotice(result.ToString().TrimEnd());
        RefreshUI();
    }

    private IEnumerator SpinToReward(int index)
    {
        float slotAngle = 360f / rewards.Length;

        float selectedSlotAngle =
            firstSlotAngle - index * slotAngle;

        float targetAngle = Mathf.Repeat(
            pointerAngle - selectedSlotAngle,
            360f
        );

        float startAngle = currentAngle;
        float startNormalized = Mathf.Repeat(startAngle, 360f);

        float remaining;

        if (clockwise)
        {
            remaining = Mathf.Repeat(
                startNormalized - targetAngle, 360f
            );
        }
        else
        {
            remaining = Mathf.Repeat(
                targetAngle - startNormalized, 360f
            );
        }

        int rotations = UnityEngine.Random.Range(
            minFullRotations,
            maxFullRotations + 1
        );

        float totalRotation =
            rotations * 360f + remaining;

        float direction = clockwise ? -1f : 1f;

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, spinDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 4f);

            currentAngle =
                startAngle + direction * totalRotation * eased;

            wheel.localRotation = Quaternion.Euler(
                0f, 0f, currentAngle
            );

            yield return null;
        }

        currentAngle =
            startAngle + direction * totalRotation;

        wheel.localRotation = Quaternion.Euler(
            0f, 0f, currentAngle
        );

        // Prevent angle values from growing indefinitely.
        currentAngle = Mathf.Repeat(currentAngle, 360f);
    }

    private void GiveReward(GachaReward reward)
    {
        if (reward.type == RewardType.Nothing)
            return;

        switch (reward.type)
        {
            case RewardType.Coin:
                GameManager.Instance.AddCoins(reward.amount);
                break;

            case RewardType.Diamond:
                GameManager.Instance.AddDiamonds(reward.amount);
                break;

            case RewardType.Heart:
                // Temporary PlayerPrefs storage.
                // Replace with GameManager.AddHearts when integrated.
                int hearts = PlayerPrefs.GetInt("Heart", 0);
                PlayerPrefs.SetInt(
                    "Heart",
                    hearts + reward.amount
                );
                PlayerPrefs.Save();
                break;
        }
    }

    private void ShowNotice(string message)
    {
        if (noticeText != null)
            noticeText.text = message;

        if (noticePanel != null)
            noticePanel.SetActive(true);
    }

    public void CloseNotice()
    {
        if (isSpinning)
            return;

        if (noticePanel != null)
            noticePanel.SetActive(false);

        RefreshUI();
    }

    private void LoadAdsState()
    {
        adsUses = PlayerPrefs.GetInt(
            UsesKey, MaxAdsSpins
        );

        long.TryParse(
            PlayerPrefs.GetString(CooldownKey, "0"),
            out cooldownUntil
        );

        long.TryParse(
            PlayerPrefs.GetString(RefillKey, "0"),
            out refillUntil
        );

        adsUses = Mathf.Clamp(
            adsUses, 0, MaxAdsSpins
        );
    }

    private void SaveAdsState()
    {
        PlayerPrefs.SetInt(UsesKey, adsUses);
        PlayerPrefs.SetString(
            CooldownKey, cooldownUntil.ToString()
        );
        PlayerPrefs.SetString(
            RefillKey, refillUntil.ToString()
        );
        PlayerPrefs.Save();
    }

    private void RefreshUI()
    {
        bool busy = IsBusy();

        if (x1Button != null)
            x1Button.interactable = !busy;

        if (x5Button != null)
            x5Button.interactable = !busy;

        if (okButton != null)
            okButton.interactable =
                noticePanel != null &&
                noticePanel.activeSelf &&
                !isSpinning;

        if (adsFreeButton != null)
        {
            adsFreeButton.interactable =
                !busy &&
                adsUses > 0 &&
                Now >= cooldownUntil &&
                adsManager != null;
        }

        if (adsNotice != null)
            adsNotice.text = GetAdsNotice();
    }

    private string GetAdsNotice()
    {
        if (adsUses <= 0)
        {
            long remaining = Math.Max(
                0, refillUntil - Now
            );

            return "ADS: 0/5\nRefill in " +
                   FormatTime(remaining);
        }

        if (Now < cooldownUntil)
        {
            long remaining = cooldownUntil - Now;

            return "ADS: " + adsUses +
                   "/5\nNext in " + FormatTime(remaining);
        }

        return "ADS: " + adsUses + "/5";
    }

    private string FormatTime(long seconds)
    {
        long hours = seconds / 3600;
        long minutes = (seconds % 3600) / 60;
        long secs = seconds % 60;

        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{secs:D2}";

        return $"{minutes:D2}:{secs:D2}";
    }
}
