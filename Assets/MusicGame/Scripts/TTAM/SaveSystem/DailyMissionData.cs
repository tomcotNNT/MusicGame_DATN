using System;
using System.Collections.Generic;

/// <summary>Tiến độ nhiệm vụ ngày (lưu trong PlayerData.json, tự làm mới khi sang ngày).</summary>
[Serializable]
public class DailyMissionData
{
    /// <summary>Ngày của tiến độ hiện tại (yyyy-MM-dd, giờ máy).</summary>
    public string day = "";

    public int levelsCompleted;
    public int perfectHits;
    public int bestCombo;

    /// <summary>Index các nhiệm vụ đã nhận thưởng hôm nay.</summary>
    public List<int> claimed = new List<int>();
}

/// <summary>Danh sách nhiệm vụ ngày. Thứ tự = thứ tự Mission1, Mission2, Mission3 trên UI.</summary>
public static class DailyMissions
{
    public class Def
    {
        public string title;
        public string descriptionFormat;   // {0} = tiến độ, {1} = mục tiêu
        public int target;
        public int coinReward;
        public Func<DailyMissionData, int> progress;
    }

    public static readonly Def[] All =
    {
        new Def
        {
            title = "WARM UP",
            descriptionFormat = "- FINISH LEVELS: {0}/{1}",
            target = 3,
            coinReward = 500,
            progress = d => d.levelsCompleted
        },
        new Def
        {
            title = "PERFECT TIMING",
            descriptionFormat = "- PERFECT HITS: {0}/{1}",
            target = 100,
            coinReward = 500,
            progress = d => d.perfectHits
        },
        new Def
        {
            title = "COMBO MASTER",
            descriptionFormat = "- BEST COMBO: {0}/{1}",
            target = 50,
            coinReward = 500,
            progress = d => d.bestCombo
        }
    };

    public static string Today => DateTime.Now.ToString("yyyy-MM-dd");
}
