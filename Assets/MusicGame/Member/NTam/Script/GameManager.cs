using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerGameData
{
    public int totalCoins = 0;
    public int highestScore = 0;
    public float gameVolume = 1f;
    public float audioOffset = 0f;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dữ Liệu Trò Chơi Hiện Tại")]
    public PlayerGameData playerData = new PlayerGameData();

    [Header("Kho Âm Thanh Toàn Cục (Global Audio)")]
    public AudioSource bgmSource;       // Nhạc nền xuyên suốt
    public AudioSource sfxClickSource;  // Tiếng click chung
    public AudioSource sfxStartSource;  // Tiếng bắt đầu / chuyển cảnh

    private string savePath;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);

        savePath = Application.persistentDataPath + "/PlayerSaveData.json";

        // Khôi phục dữ liệu ngay khi game khởi động ở Scene đầu tiên
        LoadGameData();
        LoadSettingsData();
    }

    private void Start()
    {
        PlayBGM();
    }

    // --- QUẢN LÝ LƯU & TẢI DỮ LIỆU ---
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
            // Nếu chưa có file (lần đầu chơi), khởi tạo mặc định và lưu lại
            playerData = new PlayerGameData();
            SaveGameData();
        }
    }

    private void LoadSettingsData()
    {
        playerData.audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
        playerData.gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        AudioListener.volume = playerData.gameVolume;
    }

    // --- QUẢN LÝ ÂM THANH ---
    public void PlayBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.volume = playerData.gameVolume;
            bgmSource.Play();
        }
    }

    public void PlayClickSound()
    {
        if (sfxClickSource != null) sfxClickSource.Play();
    }

    public void PlayStartSound()
    {
        if (sfxStartSource != null) sfxStartSource.Play();
    }
}