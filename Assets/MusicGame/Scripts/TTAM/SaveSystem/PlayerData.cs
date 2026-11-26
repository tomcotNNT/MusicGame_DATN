
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int version = 1;

    public List<string> claimedAchievements = new List<string>();
    public ComboStatsData comboStats = new ComboStatsData();
    public List<int> unlockedCharacters = new List<int>();

    public CurrencyData currency = new CurrencyData();

    public List<LevelRecordData> levelRecords =
        new List<LevelRecordData>();

    public ProgressionData progression =
        new ProgressionData();

    public SettingsData settings =
        new SettingsData();

    public PlayTurnData playTurn =
        new PlayTurnData();

    public DailyMissionData dailyMission =
        new DailyMissionData();
}
