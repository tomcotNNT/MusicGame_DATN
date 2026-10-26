
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GachaWheel : MonoBehaviour
{
    [Header("Wheel")]
    [SerializeField] private RectTransform wheel;
    [SerializeField] private float spinDuration = 4f;
    [SerializeField] private int minFullRotations = 4;
    [SerializeField] private int maxFullRotations = 7;

    [Header("Buttons")]
    [SerializeField] private Button x1Button;
    [SerializeField] private Button x5Button;
    [SerializeField] private Button adsFreeButton;
    [SerializeField] private Button okButton;

    [Header("Notice Panel")]
    [SerializeField] private GameObject noticePanel;
    [SerializeField] private TMP_Text noticeText;

    [Header("Rewarded Ads")]
    [SerializeField] private RewardedAdsManager adsManager;

    // TMP tên Notice nằm dưới ADSFreeButton trong Hierarchy.
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
        x1Button.onClick.AddListener(SpinX1);
        x5Button.onClick.AddListener(SpinX5);
        adsFreeButton.onClick.AddListener(SpinWithRewardedAd);
        okButton.onClick.AddListener(CloseNotice);

        noticePanel.SetActive(false);

        currentAngle = wheel.localEulerAngles.z;

        LoadAdsState();
        RefreshUI();
    }

    private void Update()
    {
        // Nhấn F6 để reset lượt quay ADS
        if (Input.GetKeyDown(KeyCode.F6))
        {
            adsUses = MaxAdsSpins;
            cooldownUntil = 0;
            refillUntil = 0;

            SaveAdsState();
            RefreshUI();

            Debug.Log("ADS spins reset to 5/5!");
        }

        // Hết 24 giờ: khôi phục 5 lượt.
        if (adsUses <= 0 && refillUntil > 0
            && Now >= refillUntil)
        {
            adsUses = MaxAdsSpins;
            cooldownUntil = 0;
            refillUntil = 0;
            SaveAdsState();
        }

        RefreshUI();
    }

    private void LoadAdsState()
    {
        adsUses = PlayerPrefs.GetInt(UsesKey, MaxAdsSpins);
        cooldownUntil = long.Parse(
            PlayerPrefs.GetString(CooldownKey, "0")
        );
        refillUntil = long.Parse(
            PlayerPrefs.GetString(RefillKey, "0")
        );

        adsUses = Mathf.Clamp(adsUses, 0, MaxAdsSpins);
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

    private bool IsBusy()
    {
        return isSpinning
            || isWaitingForAd
            || noticePanel.activeSelf;
    }

    public void SpinX1()
    {
        if (IsBusy())
            return;

        StartCoroutine(SpinRoutine());
    }

    public void SpinX5()
    {
        if (IsBusy())
            return;

        // Giữ nguyên cơ chế quay hiện tại của bạn:
        // x5 cũng kích hoạt một lần quay.
        StartCoroutine(SpinRoutine());
    }

    public void SpinWithRewardedAd()
    {
        if (IsBusy())
            return;

        if (adsUses <= 0)
        {
            RefreshUI();
            return;
        }

        if (Now < cooldownUntil)
        {
            RefreshUI();
            return;
        }

        if (adsManager == null)
        {
            Debug.LogError(
                "RewardedAdsManager is not assigned."
            );
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

        // Chỉ trừ lượt sau khi xác nhận nhận thưởng.
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
        RefreshUI();

        // Sau khi nhận thưởng mới bắt đầu quay.
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        RefreshUI();

        int fullRotations = UnityEngine.Random.Range(
            minFullRotations,
            maxFullRotations + 1
        );

        float finalAngle = UnityEngine.Random.Range(0f, 360f);
        float startAngle = currentAngle;
        float totalRotation =
            fullRotations * 360f + finalAngle;

        float elapsed = 0f;

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / spinDuration);
            float easedT = 1f - Mathf.Pow(1f - t, 4f);

            currentAngle =
                startAngle + totalRotation * easedT;

            wheel.localRotation = Quaternion.Euler(
                0f, 0f, currentAngle
            );

            yield return null;
        }

        currentAngle = startAngle + totalRotation;

        wheel.localRotation = Quaternion.Euler(
            0f, 0f, currentAngle
        );

        isSpinning = false;

        noticeText.text = "You received ...";
        noticePanel.SetActive(true);

        RefreshUI();
    }

    public void CloseNotice()
    {
        if (isSpinning)
            return;

        noticePanel.SetActive(false);
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool busy = isSpinning
            || isWaitingForAd
            || (noticePanel != null && noticePanel.activeSelf);

        if (x1Button != null)
            x1Button.interactable = !busy;

        if (x5Button != null)
            x5Button.interactable = !busy;

        if (okButton != null && noticePanel != null)
            okButton.interactable = noticePanel.activeSelf
                && !isSpinning;

        if (adsFreeButton != null)
        {
            bool canUseAds = adsUses > 0
                && Now >= cooldownUntil
                && !busy
                && adsManager != null;

            adsFreeButton.interactable = canUseAds;
        }

        if (adsNotice != null)
            adsNotice.text = GetAdsNotice();
    }

    private string GetAdsNotice()
    {
        if (adsUses <= 0)
        {
            long remaining = Math.Max(0, refillUntil - Now);
            return "ADS: 0/5\nRefill in " + FormatTime(remaining);
        }

        if (Now < cooldownUntil)
        {
            long remaining = cooldownUntil - Now;
            return "ADS: " + adsUses + "/5\nNext in "
                + FormatTime(remaining);
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
