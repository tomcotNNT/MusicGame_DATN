using System;
using UnityEngine;

public enum AchievementStat { HighestCombo, HighestScore }
public enum AchievementRewardType { Coin, Diamond }

[Serializable]
public class AchievementDefinition
{
    public string id;
    public string title;
    public AchievementStat stat;
    public int target;
    public AchievementRewardType rewardType;
    public int rewardAmount;
}

[Serializable]
public class AchievementSaveData
{
    public int highestCombo;
    public int highestScore;
    public bool[] claimed = new bool[5];
}

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }
    public static event Action OnAchievementsChanged;
    private const string SaveKey = "BeatRun_Achievements_v1";

    [Header("Five achievements in Archive1-Archive5 order")]
    public AchievementDefinition[] definitions = new AchievementDefinition[]
    {
        new AchievementDefinition { id="combo10", title="First Combo", stat=AchievementStat.HighestCombo, target=10, rewardType=AchievementRewardType.Coin, rewardAmount=100 },
        new AchievementDefinition { id="combo50", title="Combo Master", stat=AchievementStat.HighestCombo, target=50, rewardType=AchievementRewardType.Coin, rewardAmount=200 },
        new AchievementDefinition { id="combo100", title="Rhythm Legend", stat=AchievementStat.HighestCombo, target=100, rewardType=AchievementRewardType.Diamond, rewardAmount=50 },
        new AchievementDefinition { id="score10000", title="Score Hunter", stat=AchievementStat.HighestScore, target=10000, rewardType=AchievementRewardType.Coin, rewardAmount=300 },
        new AchievementDefinition { id="score50000", title="Score Legend", stat=AchievementStat.HighestScore, target=50000, rewardType=AchievementRewardType.Diamond, rewardAmount=100 }
    };

    private AchievementSaveData data = new AchievementSaveData();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            try { data = JsonUtility.FromJson<AchievementSaveData>(json); }
            catch (Exception ex) { Debug.LogWarning("Achievement save invalid: " + ex.Message); }
        }
        if (data == null) data = new AchievementSaveData();
        if (data.claimed == null || data.claimed.Length != definitions.Length)
        {
            bool[] previous = data.claimed;
            data.claimed = new bool[definitions.Length];
            if (previous != null) Array.Copy(previous, data.claimed, Mathf.Min(previous.Length, data.claimed.Length));
        }
    }

    private void Save()
    {
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
        OnAchievementsChanged?.Invoke();
    }

    public void ReportCombo(int currentCombo)
    {
        if (currentCombo <= data.highestCombo) return;
        data.highestCombo = Mathf.Max(0, currentCombo);
        Save();
    }

    public void ReportScore(int currentScore)
    {
        if (currentScore <= data.highestScore) return;
        data.highestScore = Mathf.Max(0, currentScore);
        Save();
    }

    public int GetProgress(int index)
    {
        if (!IsValid(index)) return 0;
        return definitions[index].stat == AchievementStat.HighestCombo ? data.highestCombo : data.highestScore;
    }

    public bool IsCompleted(int index) => IsValid(index) && GetProgress(index) >= definitions[index].target;
    public bool IsClaimed(int index) => IsValid(index) && data.claimed[index];
    private bool IsValid(int index) => definitions != null && index >= 0 && index < definitions.Length;

    public bool Claim(int index)
    {
        if (!IsValid(index) || !IsCompleted(index) || IsClaimed(index)) return false;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("Achievement claim failed: GameManager is missing.");
            return false;
        }
        AchievementDefinition reward = definitions[index];
        if (reward.rewardAmount < 0) return false;
        // Save the claim before granting currency so the button cannot be used twice.
        data.claimed[index] = true;
        Save();
        if (reward.rewardType == AchievementRewardType.Coin) GameManager.Instance.AddCoins(reward.rewardAmount);
        else GameManager.Instance.AddDiamonds(reward.rewardAmount);
        return true;
    }

    [ContextMenu("Test: Reset Achievements")]
    public void ResetAchievements()
    {
        data = new AchievementSaveData();
        data.claimed = new bool[definitions.Length];
        Save();
        Debug.Log("[Achievements] Reset all progress and claims.");
    }

    // Use after a full PlayerPrefs.DeleteAll() while this manager is still alive.
    public void ReloadAfterFullReset()
    {
        data = new AchievementSaveData();
        Load();
        OnAchievementsChanged?.Invoke();
    }
}
