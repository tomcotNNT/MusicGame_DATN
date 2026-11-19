using System;
using UnityEngine;

// Chỉ số trong JSON dùng để so sánh với mục tiêu
public enum AchievementStat
{
    Coin,               // currency.coin (số đang có, giảm khi tiêu)
    Diamond,            // currency.diamond (số đang có, giảm khi tiêu)
    TotalCoinEarned,    // currency.totalCoinEarned (tổng đã kiếm, không giảm khi tiêu)
    TotalDiamondEarned, // currency.totalDiamondEarned (tổng đã kiếm, không giảm khi tiêu)
    LevelsUnlocked,     // số level trong progression.unlockedLevels
    BestScoreAnyLevel,  // highScore cao nhất trong levelRecords
    BestComboAnyLevel,  // maxCombo cao nhất trong 1 lần chơi (chuỗi combo dài nhất)
    TotalGood,          // comboStats.good: tổng số lần Good đã đạt
    TotalGreat,         // comboStats.great: tổng số lần Great đã đạt
    TotalPerfect,       // comboStats.perfect: tổng số lần Perfect đã đạt
    TotalHighScore      // tổng highScore của mọi level
}

public enum AchievementState
{
    InProgress, // chưa đủ điều kiện: không có glow
    Claimable,  // đủ điều kiện, chưa nhận: có glow chạy quanh icon
    Claimed     // đã nhận: icon xám + chữ Claimed
}

[Serializable]
public class AchievementDefinition
{
    [Tooltip("ID duy nhất, dùng để lưu trạng thái đã nhận. KHÔNG đổi sau khi phát hành")]
    public string id = "collect_gold_1000";

    public string title = "Collect 1000 Gold";

    [Header("Điều kiện")]
    public AchievementStat stat = AchievementStat.Coin;
    [Tooltip("Đạt hoặc vượt giá trị này thì được nhận")]
    public int targetValue = 1000;

    [Header("UI")]
    public Sprite icon;

    [Header("Phần thưởng")]
    public CurrencyManager.CurrencyType rewardType = CurrencyManager.CurrencyType.Coin;
    public int rewardAmount = 100;
}