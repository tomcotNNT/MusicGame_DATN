
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
        Nothing,
        Ticket
    }

    
[Serializable]
public class GachaReward
{
    public string rewardName;
    public RewardType type;
    public int amount = 1;

    public Sprite rewardIcon;

    [Min(0f)]
    public float weight = 10f;
}
[Header("Reward Result UI")]
[SerializeField] private Image iconItemReceive;
[SerializeField] private TMP_Text quantityText;

    [Header("Wheel")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minFullRotations = 4;
    [SerializeField] private int maxFullRotations = 7;

    [Header("Wheel Alignment")]
    [Tooltip("Angle of the first slot center in the unrotated wheel")]
    [SerializeField] private float firstSlotAngle = 90f;

    [Tooltip("Left pointer = 180, top pointer = 90")]
    [SerializeField] private float pointerAngle = 180f;

    [SerializeField] private bool clockwise = true;

    [Header("Rewards - Clockwise Order")]
    [SerializeField] private GachaReward[] rewards = new GachaReward[8];

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

    [Header("ADS Counter")]
    [SerializeField] private TMP_Text adsNotice;

    private const int MaxAdsSpins = 5;
    private const long CooldownSeconds = 300;
    private const long RefillSeconds = 86400;

    private const string UsesKey = "Gacha_Ads_Uses";
    private const string CooldownKey = "Gacha_Ads_Cooldown";
    private const string RefillKey = "Gacha_Ads_Refill";

    private bool isSpinning;
    private bool isWaitingForAd;

    private int adsUses;
    private long cooldownUntil;
    private long refillUntil;

    private long Now =>
        DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private void Start()
    {
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
               (noticePanel != null &&
                noticePanel.activeSelf);
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
            Debug.LogError("Gacha requires 8 reward slots!");
            return false;
        }

        float totalWeight = 0f;

        foreach (GachaReward reward in rewards)
        {
            if (reward == null)
            {
                Debug.LogError("A reward slot is null!");
                return false;
            }

            if (reward.type != RewardType.Nothing &&
                reward.amount <= 0)
            {
                Debug.LogError("Reward amount must be positive!");
                return false;
            }

            totalWeight += Mathf.Max(0f, reward.weight);
        }

        if (totalWeight <= 0f)
        {
            Debug.LogError("All reward weights are zero!");
            return false;
        }

        return true;
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

        if (cost > 0 &&
            !GameManager.Instance.SpendDiamonds(cost))
        {
            ShowNotice("Không đủ kim cương!");
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
        float totalWeight = 0f;

        foreach (GachaReward reward in rewards)
            totalWeight += Mathf.Max(0f, reward.weight);

        float randomValue = UnityEngine.Random.value * totalWeight;

        float cumulative = 0f;
        int lastValidIndex = 0;

        for (int i = 0; i < rewards.Length; i++)
        {
            float weight = Mathf.Max(0f, rewards[i].weight);

            if (weight <= 0f)
                continue;

            lastValidIndex = i;
            cumulative += weight;

            if (randomValue < cumulative)
                return i;
        }

        return lastValidIndex;
    }

    private IEnumerator SpinSequence(int count)
    {
        isSpinning = true;
        RefreshUI();

        StringBuilder results = new StringBuilder();

        for (int i = 0; i < count; i++)
        {
            int selectedIndex = ChooseReward();

            yield return StartCoroutine(
                SpinToReward(selectedIndex)
            );

            GachaReward reward = rewards[selectedIndex];

            bool granted = GiveReward(reward);
            if (granted && count == 1)
{
    ShowRewardResult(reward);
}

            if (!granted)
            {
                results.AppendLine(
                    "Không thể cộng phần thưởng!"
                );
                break;
            }

            if (reward.type == RewardType.Nothing)
            {
                results.AppendLine(
                    "Chúc bạn may mắn lần sau!"
                );
            }
            else
            {
                string name = GetRewardName(reward);

                results.AppendLine(
                    "Chúc mừng! Bạn nhận được " +
                    reward.amount + " " + name
                );
            }

            if (count > 1 && i < count - 1)
                yield return new WaitForSeconds(0.3f);
        }

        isSpinning = false;

        ShowNotice(results.ToString().TrimEnd());
        RefreshUI();
    }

    private IEnumerator SpinToReward(int index)
    {
        float slotAngle = 360f / rewards.Length;

        // Each next slot is clockwise from slot 0.
        float selectedAngle =
            firstSlotAngle - index * slotAngle;

        // Align selected slot with the stationary pointer.
        float targetAngle = Mathf.Repeat(
            pointerAngle - selectedAngle,
            360f
        );

        float startAngle = wheel.localEulerAngles.z;

        float remaining;

        if (clockwise)
        {
            remaining = Mathf.Repeat(
                startAngle - targetAngle,
                360f
            );
        }
        else
        {
            remaining = Mathf.Repeat(
                targetAngle - startAngle,
                360f
            );
        }

        int minRotations = Mathf.Max(1, minFullRotations);
        int maxRotations = Mathf.Max(
            minRotations, maxFullRotations
        );

        int fullRotations = UnityEngine.Random.Range(
            minRotations,
            maxRotations + 1
        );

        float totalRotation =
            fullRotations * 360f + remaining;

        float direction = clockwise ? -1f : 1f;

        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, spinDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);

            // Fast start, slow finish.
            float eased =
                1f - Mathf.Pow(1f - t, 4f);

            float angle =
                startAngle +
                direction * totalRotation * eased;

            wheel.localRotation =
                Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // Exact final alignment.
        wheel.localRotation = Quaternion.Euler(
            0f, 0f, targetAngle
        );
    }

    private bool GiveReward(GachaReward reward)
    {
        if (reward.type == RewardType.Nothing)
            return true;

        switch (reward.type)
        {
            case RewardType.Coin:
                if (GameManager.Instance == null)
                    return false;

                GameManager.Instance.AddCoins(reward.amount);
                return true;

            case RewardType.Diamond:
                if (GameManager.Instance == null)
                    return false;

                GameManager.Instance.AddDiamonds(reward.amount);
                return true;

            case RewardType.Heart:
                AddPlayerPrefsReward("Heart", reward.amount);
                return true;

            case RewardType.Ticket:
                AddPlayerPrefsReward("Ticket", reward.amount);
                return true;
        }

        return false;
    }

    private void AddPlayerPrefsReward(string key, int amount)
    {
        int current = PlayerPrefs.GetInt(key, 0);

        PlayerPrefs.SetInt(key, current + amount);
        PlayerPrefs.Save();
    }

    private string GetRewardName(GachaReward reward)
    {
        if (!string.IsNullOrWhiteSpace(reward.rewardName))
            return reward.rewardName;

        switch (reward.type)
        {
            case RewardType.Coin:
                return "Xu";

            case RewardType.Diamond:
                return "Kim cương";

            case RewardType.Heart:
                return "Heart";

            case RewardType.Ticket:
                return "Ticket";

            default:
                return "";
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
            CooldownKey,
            cooldownUntil.ToString()
        );

        PlayerPrefs.SetString(
            RefillKey,
            refillUntil.ToString()
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
                   "/5\nNext in " +
                   FormatTime(remaining);
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

private void ShowRewardResult(GachaReward reward)
{
    bool isNothing = reward.type == RewardType.Nothing;

    if (iconItemReceive != null)
    {
        iconItemReceive.sprite = reward.rewardIcon;
        iconItemReceive.enabled = reward.rewardIcon != null;
        iconItemReceive.preserveAspect = true;
    }

    if (quantityText != null)
    {
        quantityText.gameObject.SetActive(!isNothing);

        if (!isNothing)
            quantityText.text = "x" + reward.amount;
    }

    if (noticeText != null)
    {
        noticeText.text = isNothing
            ? "Chúc bạn may mắn lần sau!"
            : "Chúc mừng! Bạn nhận được " +
              GetRewardName(reward);
    }

    if (noticePanel != null)
        noticePanel.SetActive(true);
}

}
