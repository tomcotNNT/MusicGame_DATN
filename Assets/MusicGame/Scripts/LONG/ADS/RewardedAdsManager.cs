
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

    public bool IsReady =>
        rewardedAd != null && rewardedAd.CanShowAd();

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

                // Quảng cáo đóng
                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    hasAdClosed = true;

                    // Nếu chưa nhận được thưởng
                    if (!hasEarnedReward)
                    {
                        pendingFailedAction?.Invoke();

                        pendingFailedAction = null;
                        pendingRewardAction = null;
                    }

                    CheckRewardComplete();

                    rewardedAd = null;

                    // Tải quảng cáo tiếp theo
                    LoadRewardedAd();
                };

                // Quảng cáo gặp lỗi khi hiển thị
                rewardedAd.OnAdFullScreenContentFailed +=
                    (AdError adError) =>
                    {
                        Debug.LogWarning(
                            "Rewarded ad failed to show: " + adError
                        );

                        pendingFailedAction?.Invoke();

                        rewardedAd = null;

                        pendingRewardAction = null;
                        pendingFailedAction = null;

                        hasEarnedReward = false;
                        hasAdClosed = false;

                        LoadRewardedAd();
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

        adToShow.Show((Reward reward) =>
        {
            Debug.Log("Rewarded ad reward received.");

            hasEarnedReward = true;

            CheckRewardComplete();
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
