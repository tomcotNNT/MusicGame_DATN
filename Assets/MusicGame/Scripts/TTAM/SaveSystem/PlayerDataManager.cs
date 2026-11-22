using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public PlayerData Data { get; private set; }

    // Phát ra khi coin/diamond thay đổi -> UI tự cập nhật
    public event Action OnCurrencyChanged;

    // Phát ra khi trạng thái mở khóa level thay đổi (mở, khóa, reset)
    public event Action OnProgressionChanged;

    // Phát ra mỗi khi dữ liệu được lưu (tiền, level, record, thành tựu...)
    // Các UI dựa trên nhiều loại dữ liệu (như thành tựu) chỉ cần nghe event này
    public event Action OnDataChanged;

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

        EnsureDataStructure();
        SaveData();
    }

    // Đảm bảo không có field nào bị null sau khi load / reset
    private void EnsureDataStructure()
    {
        if (Data.currency == null)
            Data.currency = new CurrencyData();

        if (Data.levelRecords == null)
            Data.levelRecords = new List<LevelRecordData>();

        if (Data.progression == null)
            Data.progression = new ProgressionData();

        if (Data.progression.unlockedLevels == null)
            Data.progression.unlockedLevels = new List<int>();

        if (Data.settings == null)
            Data.settings = new SettingsData();

        // Danh sách id thành tựu đã nhận (file save cũ chưa có field này)
        if (Data.claimedAchievements == null)
            Data.claimedAchievements = new List<string>();

        // Danh sách index nhân vật đã mở (file save cũ chưa có field này)
        if (Data.unlockedCharacters == null)
            Data.unlockedCharacters = new List<int>();

        // Nhân vật đầu tiên (index 0) luôn mở
        if (!Data.unlockedCharacters.Contains(0))
            Data.unlockedCharacters.Add(0);

        // File save cũ chưa có thống kê combo
        if (Data.comboStats == null)
            Data.comboStats = new ComboStatsData();

        // File save cũ chưa có tổng đã kiếm: tổng đã kiếm không thể nhỏ hơn số đang có
        if (Data.currency.totalCoinEarned < Data.currency.coin)
            Data.currency.totalCoinEarned = Data.currency.coin;

        if (Data.currency.totalDiamondEarned < Data.currency.diamond)
            Data.currency.totalDiamondEarned = Data.currency.diamond;
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

        OnDataChanged?.Invoke();
    }

    // ================= CURRENCY =================

    public void AddCoin(int amount)
    {
        if (amount <= 0) return;

        Data.currency.coin += amount;
        Data.currency.totalCoinEarned += amount;
        SaveData();
        OnCurrencyChanged?.Invoke();
    }

    public void AddDiamond(int amount)
    {
        if (amount <= 0) return;

        Data.currency.diamond += amount;
        Data.currency.totalDiamondEarned += amount;
        SaveData();
        OnCurrencyChanged?.Invoke();
    }

    public bool SpendCoin(int amount)
    {
        if (amount <= 0 || Data.currency.coin < amount)
            return false;

        Data.currency.coin -= amount;
        SaveData();
        OnCurrencyChanged?.Invoke();
        return true;
    }

    public bool SpendDiamond(int amount)
    {
        if (amount <= 0 || Data.currency.diamond < amount)
            return false;

        Data.currency.diamond -= amount;
        SaveData();
        OnCurrencyChanged?.Invoke();
        return true;
    }

    // Dùng cho admin: cộng/trừ tự do, không cho xuống dưới 0
    public void ChangeCoin(int delta)
    {
        Data.currency.coin = Mathf.Max(0, Data.currency.coin + delta);

        if (delta > 0)
            Data.currency.totalCoinEarned += delta;
        SaveData();
        OnCurrencyChanged?.Invoke();
    }

    public void ChangeDiamond(int delta)
    {
        Data.currency.diamond = Mathf.Max(0, Data.currency.diamond + delta);

        if (delta > 0)
            Data.currency.totalDiamondEarned += delta;
        SaveData();
        OnCurrencyChanged?.Invoke();
    }

    // ================= LEVEL RECORD =================

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

    // ================= LEVEL UNLOCK =================

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
            OnProgressionChanged?.Invoke();

            Debug.Log("Unlocked Level: " + levelId);
        }
    }

    // Dùng cho admin: khóa lại 1 level (màn 1 luôn mở)
    public void LockLevel(int levelId)
    {
        if (Data == null || Data.progression == null)
            return;

        if (levelId == 1)
        {
            Debug.LogWarning("Màn 1 luôn mở, không thể khóa.");
            return;
        }

        if (Data.progression.unlockedLevels.Remove(levelId))
        {
            SaveData();
            OnProgressionChanged?.Invoke();

            Debug.Log("Locked Level: " + levelId);
        }
    }

    // ================= CHARACTER UNLOCK =================

    public bool IsCharacterUnlocked(int characterIndex)
    {
        if (Data == null || Data.unlockedCharacters == null)
            return characterIndex == 0;

        return Data.unlockedCharacters.Contains(characterIndex);
    }

    public void UnlockCharacter(int characterIndex)
    {
        if (Data == null || characterIndex < 0)
            return;

        if (!Data.unlockedCharacters.Contains(characterIndex))
        {
            Data.unlockedCharacters.Add(characterIndex);
            SaveData();
            OnProgressionChanged?.Invoke();

            Debug.Log("Unlocked Character: " + characterIndex);
        }
    }

    // Mở khóa toàn bộ nhân vật từ 0 đến count - 1, lưu JSON 1 lần duy nhất
    public void UnlockAllCharacters(int count)
    {
        if (Data == null || count <= 0)
            return;

        bool changed = false;

        for (int i = 0; i < count; i++)
        {
            if (!Data.unlockedCharacters.Contains(i))
            {
                Data.unlockedCharacters.Add(i);
                changed = true;
            }
        }

        if (changed)
        {
            SaveData();
            OnProgressionChanged?.Invoke();
            Debug.Log("Đã mở khóa toàn bộ " + count + " nhân vật.");
        }
    }

    // Dùng cho admin: khóa lại 1 nhân vật (nhân vật 0 luôn mở)
    public void LockCharacter(int characterIndex)
    {
        if (Data == null || characterIndex == 0)
            return;

        if (Data.unlockedCharacters.Remove(characterIndex))
        {
            SaveData();
            OnProgressionChanged?.Invoke();
            Debug.Log("Locked Character: " + characterIndex);
        }
    }

    // ================= COMBO STATS =================

    // Cộng dồn số lần good/great/perfect của 1 lượt chơi, lưu 1 lần duy nhất.
    // Nên gọi lúc kết thúc màn (cùng chỗ gọi UpdateLevelRecord), KHÔNG gọi mỗi lần đánh trúng
    // vì mỗi lần gọi sẽ ghi file JSON.
    public void AddComboCounts(int good, int great, int perfect)
    {
        if (good <= 0 && great <= 0 && perfect <= 0)
            return;

        Data.comboStats.Add(ComboType.Good, good);
        Data.comboStats.Add(ComboType.Great, great);
        Data.comboStats.Add(ComboType.Perfect, perfect);

        SaveData();
    }

    // Tiện lợi khi chỉ cộng 1 loại
    public void AddComboCount(ComboType type, int amount)
    {
        if (amount <= 0)
            return;

        Data.comboStats.Add(type, amount);
        SaveData();
    }

    public long GetComboCount(ComboType type)
    {
        if (Data == null || Data.comboStats == null)
            return 0;

        return Data.comboStats.Get(type);
    }

    // ================= ACHIEVEMENT =================

    public bool IsAchievementClaimed(string achievementId)
    {
        if (Data == null || Data.claimedAchievements == null)
            return false;

        return Data.claimedAchievements.Contains(achievementId);
    }

    // Đánh dấu đã nhận + cộng thưởng trong 1 lần lưu duy nhất.
    // Trả về false nếu id rỗng, đã nhận rồi hoặc số lượng thưởng không hợp lệ.
    public bool ClaimAchievementReward(string achievementId,
                                       CurrencyManager.CurrencyType rewardType,
                                       int rewardAmount)
    {
        if (string.IsNullOrEmpty(achievementId) || rewardAmount <= 0)
            return false;

        if (IsAchievementClaimed(achievementId))
            return false;

        Data.claimedAchievements.Add(achievementId);

        switch (rewardType)
        {
            case CurrencyManager.CurrencyType.Coin:
                Data.currency.coin += rewardAmount;
                Data.currency.totalCoinEarned += rewardAmount;
                break;

            case CurrencyManager.CurrencyType.Diamond:
                Data.currency.diamond += rewardAmount;
                Data.currency.totalDiamondEarned += rewardAmount;
                break;
        }

        SaveData();
        OnCurrencyChanged?.Invoke();

        Debug.Log("Claimed achievement: " + achievementId);
        return true;
    }

    // ================= RESET =================

    // Dùng cho admin: xóa toàn bộ dữ liệu về mặc định
    public void ResetAllData()
    {
        Data = new PlayerData();
        EnsureDataStructure();

        // Màn 1 luôn mở
        if (!Data.progression.unlockedLevels.Contains(1))
            Data.progression.unlockedLevels.Add(1);

        SaveData();

        OnCurrencyChanged?.Invoke();
        OnProgressionChanged?.Invoke();

        Debug.Log("Đã reset toàn bộ dữ liệu người chơi.");
    }
}