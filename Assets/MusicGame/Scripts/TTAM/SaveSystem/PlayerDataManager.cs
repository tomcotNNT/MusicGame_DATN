
using System;
using System.IO;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public PlayerData Data { get; private set; }

    private string SavePath =>
        Path.Combine(Application.persistentDataPath, "PlayerData.json");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    public void LoadData()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);

                Data = JsonUtility.FromJson<PlayerData>(json);

                if (Data == null)
                    Data = new PlayerData();

                if (Data.currency == null)
                    Data.currency = new CurrencyData();

                if (Data.levelRecords == null)
                    Data.levelRecords = new System.Collections.Generic.List<LevelRecordData>();

                if (Data.progression == null)
                    Data.progression = new ProgressionData();

                if (Data.settings == null)
                    Data.settings = new SettingsData();
            }
            catch (Exception e)
            {
                Debug.LogError("Load failed: " + e);
                Data = new PlayerData();
            }
        }
        else
        {
            Data = new PlayerData();
        }

        SaveData();
    }

    public void SaveData()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            string tempPath = SavePath + ".tmp";

            File.WriteAllText(tempPath, json);

            if (File.Exists(SavePath))
                File.Delete(SavePath);

            File.Move(tempPath, SavePath);

            Debug.Log("Player data saved: " + SavePath);
        }
        catch (Exception e)
        {
            Debug.LogError("Save failed: " + e);
        }
    }

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        Data.currency.coin += amount;
        SaveData();
    }

    public void AddDiamond(int amount)
    {
        if (amount <= 0) return;

        Data.currency.diamond += amount;
        SaveData();
    }

    public bool SpendCoin(int amount)
    {
        if (amount <= 0 || Data.currency.coin < amount)
            return false;

        Data.currency.coin -= amount;
        SaveData();
        return true;
    }

    public bool SpendDiamond(int amount)
    {
        if (amount <= 0 || Data.currency.diamond < amount)
            return false;

        Data.currency.diamond -= amount;
        SaveData();
        return true;
    }

    public void UpdateLevelRecord(int levelId, int score, int combo)
    {
        if (levelId <= 0) return;

        LevelRecordData record =
            Data.levelRecords.Find(x => x.levelId == levelId);

        if (record == null)
        {
            record = new LevelRecordData
            {
                levelId = levelId,
                highScore = score,
                maxCombo = combo
            };

            Data.levelRecords.Add(record);
        }
        else
        {
            record.highScore = Mathf.Max(record.highScore, score);
            record.maxCombo = Mathf.Max(record.maxCombo, combo);
        }

        SaveData();
    }

    public LevelRecordData GetLevelRecord(int levelId)
    {
        return Data.levelRecords.Find(x => x.levelId == levelId);
    }

    public string GetSavePath()
    {
        return SavePath;
    }

    public bool IsLevelUnlocked(int levelId)
    {
        if (Data == null || Data.progression == null)
            return false;

        return Data.progression.unlockedLevels.Contains(levelId);
    }

    public void UnlockLevel(int levelId)
    {
        if (Data == null || Data.progression == null)
            return;

        if (!Data.progression.unlockedLevels.Contains(levelId))
        {
            Data.progression.unlockedLevels.Add(levelId);
            SaveData();

            Debug.Log("Unlocked Level: " + levelId);
        }
    }
}
