using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dữ Liệu Hệ Thống")]
    public float audioOffset = 0f;
    public float gameVolume = 1f;
    public int totalCoins = 0;

    [Header("Kho Âm Thanh Toàn Cục (Global Audio)")]
    public AudioSource bgmSource;       // Nhạc nền xuyên suốt
    public AudioSource sfxClickSource;  // Tiếng click chung toàn game
    public AudioSource sfxStartSource;  // Tiếng bắt đầu game / chuyển cảnh

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // Chống nhân bản GameManager khi sang Scene mới
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject); // Sống mãi xuyên các Scene

        LoadSettingsData();
        LoadPlayerData();
    }

    private void Start()
    {
        PlayBGM();
    }

    public void PlayBGM()
    {
        if (bgmSource != null && !bgmSource.isPlaying)
        {
            bgmSource.volume = gameVolume;
            bgmSource.Play();
        }
    }

    // Hàm chung để mọi Script khác gọi tiếng Click mà không cần khai báo lại
    public void PlayClickSound()
    {
        if (sfxClickSource != null)
        {
            sfxClickSource.Play();
        }
    }

    public void PlayStartSound()
    {
        if (sfxStartSource != null)
        {
            sfxStartSource.Play();
        }
    }

    private void LoadSettingsData()
    {
        audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
        gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);
        AudioListener.volume = gameVolume;
    }

    private void LoadPlayerData()
    {
        string savePath = Application.persistentDataPath + "/SaveData.json";
        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
        }
        else
        {
            totalCoins = 0;
        }
    }
}