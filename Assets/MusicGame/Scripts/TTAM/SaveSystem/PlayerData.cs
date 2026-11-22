
using System;
using System.Collections.Generic;

[Serializable]
public class PlayerData
{
    public int version = 1;

    public List<string> claimedAchievements = new List<string>();
    public ComboStatsData comboStats = new ComboStatsData();

    public CurrencyData currency = new CurrencyData();

    public List<LevelRecordData> levelRecords =
        new List<LevelRecordData>();

    public ProgressionData progression =
        new ProgressionData();

    public SettingsData settings =
        new SettingsData();
}
