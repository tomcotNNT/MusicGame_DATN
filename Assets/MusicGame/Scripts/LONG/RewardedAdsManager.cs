
using System;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;

public class RewardedAdsManager : MonoBehaviour
{
    // Google Rewarded test ID dành cho Android
    private const string AndroidTestAdUnitId =
        "ca-app-pub-3940256099942544/5224354917";

    private RewardedAd rewardedAd;
    private bool isLoading;

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

                rewardedAd.OnAdFullScreenContentClosed += () =>
                {
                    rewardedAd = null;
                    LoadRewardedAd();
                };

                rewardedAd.OnAdFullScreenContentFailed +=
                    (AdError adError) =>
                    {
                        Debug.LogWarning(
                            "Rewarded ad failed to show: " + adError
                        );

                        rewardedAd = null;
                        LoadRewardedAd();
                    };

                Debug.Log("Rewarded ad is ready.");
            }
        );
    }

    // Chỉ gọi onReward khi Google xác nhận người chơi được thưởng.
    public bool ShowRewarded(Action onReward)
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

        // Chặn việc hiển thị cùng một quảng cáo hai lần.
        rewardedAd = null;

        adToShow.Show((Reward reward) =>
        {
            // Chuyển xử lý thưởng về Unity main thread.
            onReward?.Invoke();
        });

        return true;
    }
}
