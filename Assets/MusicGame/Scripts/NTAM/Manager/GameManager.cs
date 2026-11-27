using System;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerGameData
{
    public int totalCoins = 0;
    public int totalDiamonds = 0;
    public int highestScore = 0;
    public float gameVolume = 1f;
    public float audioOffset = 0f;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // Sự kiện toàn cục báo hiệu số tiền vừa thay đổi (để UI ở mọi Scene tự động cập nhật)
    public static event Action OnCurrencyChanged;

    [Header("Dữ Liệu Người Chơi")]
    public PlayerGameData playerData = new PlayerGameData();

    // PlayerPrefs keys (giu co dinh de khong mat du lieu)
    private const string CoinsKey = "Player_TotalCoins";
    private const string DiamondsKey = "Player_TotalDiamonds";
    private const string ScoreKey = "Player_HighestScore";
    private const string MigrationKey = "Player_DataMigrated";

    // =========================================================================
    // CẦU NỐI ÂM THANH (TƯƠNG THÍCH VỚI SCRIPT CỦA NHÁNH TTAM & LONG)
    // =========================================================================
    public AudioSource bgmSource
    {
        get => GlobalAudioManager.Instance != null ? GlobalAudioManager.Instance.bgmSource : null;
        set
        {
            if (GlobalAudioManager.Instance != null)
                GlobalAudioManager.Instance.bgmSource = value;
        }
    }

    public void PlayClickSound()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Click);
    }

    public void PlayStartSound()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.PlaySFX(UISfxType.Confirm);
    }

    public void PlayPopupOpenSound()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.PlaySFX(UISfxType.PopupOpen);
    }

    public void PlayErrorSound()
    {
        if (GlobalAudioManager.Instance != null)
            GlobalAudioManager.Instance.PlaySFX(UISfxType.BuyDenied);
    }

    // =========================================================================
    // PLAYERPREFS: SAVE / LOAD + CHUYEN DU LIEU JSON CU MOT LAN
    // =========================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        LoadGameData();
        LoadSettingsData();
    }

    public void SaveGameData()
    {
        PlayerPrefs.SetInt(CoinsKey, playerData.totalCoins);
        PlayerPrefs.SetInt(DiamondsKey, playerData.totalDiamonds);
        PlayerPrefs.SetInt(ScoreKey, playerData.highestScore);
        PlayerPrefs.SetFloat("GameVolume", playerData.gameVolume);
        PlayerPrefs.SetFloat("AudioOffset", playerData.audioOffset);
        PlayerPrefs.Save();
    }

    public void LoadGameData()
    {
        // Neu da co PlayerPrefs, khong ghi de bang file JSON cu.
        bool hasPrefs = PlayerPrefs.HasKey(CoinsKey) ||
                        PlayerPrefs.HasKey(DiamondsKey) ||
                        PlayerPrefs.HasKey(ScoreKey);

        if (!PlayerPrefs.HasKey(MigrationKey))
        {
            if (!hasPrefs)
            {
                // Chi doc JSON mot lan de giu tien va diem cu.
                // Tu sau lan nay, game chi SAVE vao PlayerPrefs.
                string oldPath = Path.Combine(Application.persistentDataPath,
                                              "PlayerSaveData.json");
                if (File.Exists(oldPath))
                {
                    try
                    {
                        string json = File.ReadAllText(oldPath);
                        JsonUtility.FromJsonOverwrite(json, playerData);

                        // Cai dat am thanh da co san trong PlayerPrefs duoc uu tien.
                        playerData.gameVolume = PlayerPrefs.GetFloat("GameVolume", playerData.gameVolume);
                        playerData.audioOffset = PlayerPrefs.GetFloat("AudioOffset", playerData.audioOffset);
                        SaveGameData();
                        Debug.Log("[GameManager] Da chuyen JSON cu sang PlayerPrefs.");
                    }
                    catch (Exception ex)
                    {
                        // Khong danh dau migration neu doc JSON that bai.
                        Debug.LogError("[GameManager] Khong the chuyen JSON cu: " + ex.Message);
                        return;
                    }
                }
            }

            PlayerPrefs.SetInt(MigrationKey, 1);
            PlayerPrefs.Save();
        }

        playerData.totalCoins = PlayerPrefs.GetInt(CoinsKey, 0);
        playerData.totalDiamonds = PlayerPrefs.GetInt(DiamondsKey, 0);
        playerData.highestScore = PlayerPrefs.GetInt(ScoreKey, 0);
        playerData.gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        playerData.audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
    }

    private void LoadSettingsData()
    {
        playerData.audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
        playerData.gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        AudioListener.volume = playerData.gameVolume;
    }

    // =========================================================================
    // HỆ THỐNG KINH TẾ (COINS & DIAMONDS)
    // =========================================================================
    public bool SpendCoins(int amount)
    {
        if (playerData.totalCoins >= amount)
        {
            playerData.totalCoins -= amount;
            SaveGameData();
            OnCurrencyChanged?.Invoke();
            return true;
        }

        PlayErrorSound();
        return false;
    }

    public void AddCoins(int amount)
    {
        playerData.totalCoins += amount;
        SaveGameData();
        OnCurrencyChanged?.Invoke();
    }

    public bool SpendDiamonds(int amount)
    {
        if (playerData.totalDiamonds >= amount)
        {
            playerData.totalDiamonds -= amount;
            SaveGameData();
            OnCurrencyChanged?.Invoke();
            return true;
        }

        PlayErrorSound();
        return false;
    }

    public void AddDiamonds(int amount)
    {
        playerData.totalDiamonds += amount;
        SaveGameData();
        OnCurrencyChanged?.Invoke();
    }

    // =========================================================================
    // CẦU NỐI ĐỒNG BỘ TÊN GỌI (GOLD / DIAMOND ALIASES CHO SCRIPT CŨ)
    // =========================================================================
    public int GetGold() => playerData.totalCoins;
    public int GetDiamond() => playerData.totalDiamonds;

    public void AddGold(int amount) => AddCoins(amount);
    public bool SpendGold(int amount) => SpendCoins(amount);

    public void AddDiamond(int amount) => AddDiamonds(amount);
    public bool SpendDiamond(int amount) => SpendDiamonds(amount);

    // =========================================================================
    // CÔNG CỤ TEST NHANH TRONG EDITOR
    // =========================================================================
    [ContextMenu("Test: Reset Player Data (PlayerPrefs)")]
    public void ResetNewPlayerData()
    {
        playerData = new PlayerGameData();
        SaveGameData();
        OnCurrencyChanged?.Invoke();
        Debug.Log("<color=cyan>>>> [DATA] Đã khởi tạo lại dữ liệu: Coin = 0, Diamond = 0!</color>");
    }
}