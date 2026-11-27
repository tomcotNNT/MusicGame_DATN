
using System;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;

public class RewardedAdsManager : MonoBehaviour
{
    // Google Rewarded Test Ad ID dành cho Android
    private const string AndroidTestAdUnitId =
        "ca-app-pub-3940256099942544/5224354917";

    private RewardedAd rewardedAd;
    private bool isLoading;

    private Action pendingRewardAction;
    private Action pendingFailedAction;

    private bool hasEarnedReward;
    private bool hasAdClosed;

    // Callback của AdMob trên Android có thể chạy ở thread khác (không phải main thread của Unity).
    // Gọi API Unity (SetActive, ghi file, event UI) từ đó sẽ lỗi/crash -> chỉ bật cờ, xử lý trong Update().
    private volatile bool pendingEarned;
    private volatile bool pendingClosed;
    private volatile bool pendingShowFailed;
    private volatile bool pendingReload;

    public bool IsReady =>
        rewardedAd != null && rewardedAd.CanShowAd();

    private void Update()
    {
        if (pendingShowFailed)
        {
            pendingShowFailed = false;
            pendingEarned = false;
            pendingClosed = false;

            Action failed = pendingFailedAction;
            pendingRewardAction = null;
            pendingFailedAction = null;
            hasEarnedReward = false;
            hasAdClosed = false;

            failed?.Invoke();
        }

        if (pendingEarned)
        {
            pendingEarned = false;
            hasEarnedReward = true;
        }

        if (pendingClosed)
        {
            pendingClosed = false;
            hasAdClosed = true;

            if (!hasEarnedReward)
            {
                Action failed = pendingFailedAction;
                pendingFailedAction = null;
                pendingRewardAction = null;
                failed?.Invoke();
            }
        }

        CheckRewardComplete();

        if (pendingReload)
        {
            pendingReload = false;
            LoadRewardedAd();
        }
    }

    private void Start()
    {
        MobileAds.Initialize((InitializationStatus status) =>
        {
            if (status == null)
            {
                Debug.LogError("AdMob initialization failed.");
                return;
            }

            Debug.Log("AdMob initialized successfully.");

            LoadRewardedAd();
        });
    }

    // ==============================
    // LOAD REWARDED AD
    // ==============================

    public void LoadRewardedAd()
    {
        if (isLoading || IsReady)
            return;

        isLoading = true;

        RewardedAd.Load(
            AndroidTestAdUnitId,
            new AdRequest(),
            (RewardedAd ad, LoadAdError error) =>
            {
                isLoading = false;

                if (error != null || ad == null)
                {
                    Debug.LogWarning(
                        "Rewarded ad failed to load: " + error
                    );

                    return;
                }

                rewardedAd = ad;

                Debug.Log("Rewarded ad is ready.");

                // Quảng cáo đóng (có thể ở thread khác -> chỉ bật cờ)
                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    pendingClosed = true;
                    pendingReload = true;   // tải quảng cáo tiếp theo
                };

                // Quảng cáo gặp lỗi khi hiển thị
                rewardedAd.OnAdFullScreenContentFailed +=
                    (AdError adError) =>
                    {
                        pendingShowFailed = true;
                        pendingReload = true;
                    };
            }
        );
    }

    // ==============================
    // SHOW REWARDED AD
    // ==============================

    public bool ShowRewarded(
        Action onReward,
        Action onFailed = null
    )
    {
        if (!IsReady)
        {
            Debug.LogWarning(
                "Rewarded ad is not ready yet."
            );

            LoadRewardedAd();

            return false;
        }

        RewardedAd adToShow = rewardedAd;

        // Không cho hiển thị lại cùng một quảng cáo
        rewardedAd = null;

        pendingRewardAction = onReward;
        pendingFailedAction = onFailed;

        hasEarnedReward = false;
        hasAdClosed = false;

        // Callback nhận thưởng có thể ở thread khác -> chỉ bật cờ, Update() trả thưởng
        adToShow.Show((Reward reward) =>
        {
            pendingEarned = true;
        });

        return true;
    }

    // ==============================
    // CHECK REWARD
    // ==============================

    private void CheckRewardComplete()
    {
        if (hasEarnedReward && hasAdClosed)
        {
            Debug.Log("Rewarded ad completed successfully.");

            pendingRewardAction?.Invoke();

            pendingRewardAction = null;
            pendingFailedAction = null;

            hasEarnedReward = false;
            hasAdClosed = false;
        }
    }
}
