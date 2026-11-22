using System;

// Các loại combo/đánh giá cần tích lũy. Muốn thêm loại mới (vd Miss) thì thêm vào đây
public enum ComboType
{
    Good,
    Great,
    Perfect
}

/// <summary>
/// Tổng số lần đạt từng loại combo trong toàn bộ quá trình chơi (không bao giờ giảm).
/// Lưu trong JSON qua PlayerData.comboStats
/// </summary>
[Serializable]
public class ComboStatsData
{
    public long good;
    public long great;
    public long perfect;

    public long Get(ComboType type)
    {
        switch (type)
        {
            case ComboType.Good:    return good;
            case ComboType.Great:   return great;
            case ComboType.Perfect: return perfect;
            default:                return 0;
        }
    }

    public void Add(ComboType type, int amount)
    {
        if (amount <= 0)
            return;

        switch (type)
        {
            case ComboType.Good:    good += amount;    break;
            case ComboType.Great:   great += amount;   break;
            case ComboType.Perfect: perfect += amount; break;
        }
    }
}