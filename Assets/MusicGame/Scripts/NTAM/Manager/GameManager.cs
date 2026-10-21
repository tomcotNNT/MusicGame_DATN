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

    private string savePath;

    // =========================================================================
    // CẦU NỐI ÂM THANH (TƯƠNG THÍCH VỚI TẤT CẢ SCRIPT CŨ CỦA ĐỒNG ĐỘI)
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
    // VÒNG ĐỜI VÀ QUẢN TRỊ DỮ LIỆU JSON
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

        savePath = Application.persistentDataPath + "/PlayerSaveData.json";

        LoadGameData();
        LoadSettingsData();
    }

    public void SaveGameData()
    {
        string json = JsonUtility.ToJson(playerData, true);
        File.WriteAllText(savePath, json);
    }

    public void LoadGameData()
    {
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            JsonUtility.FromJsonOverwrite(json, playerData);
        }
        else
        {
            // Lần đầu chơi: Khởi tạo dữ liệu sạch (0 Coin, 0 Diamond)
            ResetNewPlayerData();
        }
    }

    private void LoadSettingsData()
    {
        playerData.audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
        playerData.gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        AudioListener.volume = playerData.gameVolume;
    }

    // =========================================================================
    // HỆ THỐNG KINH TẾ: QUẢN LÝ TIỀN TỆ AN TOÀN VÀ ĐỒNG BỘ UI
    // =========================================================================
    public bool SpendCoins(int amount)
    {
        if (playerData.totalCoins >= amount)
        {
            playerData.totalCoins -= amount;
            SaveGameData();
            OnCurrencyChanged?.Invoke(); // Bắn tín hiệu để Text UI tự nhảy số mới
            return true;
        }

        PlayErrorSound(); // Báo âm thanh từ chối khi không đủ tiền
        return false;
    }

    public void AddCoins(int amount)
    {
        playerData.totalCoins += amount;
        SaveGameData();
        OnCurrencyChanged?.Invoke(); // Bắn tín hiệu cập nhật Text UI
    }

    public bool SpendDiamonds(int amount)
    {
        if (playerData.totalDiamonds >= amount)
        {
            playerData.totalDiamonds -= amount;
            SaveGameData();
            OnCurrencyChanged?.Invoke(); // Bắn tín hiệu cập nhật Text UI
            return true;
        }

        PlayErrorSound();
        return false;
    }

    public void AddDiamonds(int amount)
    {
        playerData.totalDiamonds += amount;
        SaveGameData();
        OnCurrencyChanged?.Invoke(); // Bắn tín hiệu cập nhật Text UI
    }

    // =========================================================================
    // CÔNG CỤ TEST NHANH TRONG EDITOR
    // =========================================================================
    [ContextMenu("Test: Xóa Toàn Bộ File Save Về 0")]
    public void ResetNewPlayerData()
    {
        playerData = new PlayerGameData();
        SaveGameData();
        OnCurrencyChanged?.Invoke();
        Debug.Log("<color=cyan>>>> [DATA] Đã khởi tạo lại dữ liệu: Coin = 0, Diamond = 0!</color>");
    }
}