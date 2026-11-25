using System;

// Dữ liệu lượt chơi, lưu trong PlayerData.json
[Serializable]
public class PlayTurnData
{
    // Số lượt hiện có (có thể vượt max khi nhận quà / xem quảng cáo)
    public int turns = 5;

    // Mốc thời gian (UTC ticks) bắt đầu đếm hồi lượt. 0 = đang đầy, không đếm
    public long lastRegenTicks = 0;

    // Tổng số lần đã vào màn chơi (thống kê)
    public long totalPlays = 0;
}
