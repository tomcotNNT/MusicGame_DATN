using System;

[Serializable]
public class LevelRecordData
{
    public int levelId;
    public int highScore;
    public int maxCombo;

    // Số sao cao nhất từng đạt (0-3). File save cũ chưa có field này -> mặc định 0
    public int bestStars;
}