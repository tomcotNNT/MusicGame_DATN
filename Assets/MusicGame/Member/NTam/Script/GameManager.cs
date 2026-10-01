using System.IO;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Dữ Liệu Hệ Thống")]
    public float audioOffset = 0f;
    public float gameVolume = 1f;
    public int totalCoins = 0;

    [Header("Âm thanh xuyên Scene")]
    public AudioSource persistentBGM; // Đổi tên cho chuẩn ý nghĩa nhạc nền xuyên suốt

    private void Awake()
    {
        // Đảm bảo chỉ có 1 GameManager tồn tại
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Bùa hộ mệnh giúp sống sót qua mọi Scene

        // Khởi tạo dữ liệu ngay từ lúc game vừa mở lên
        LoadSettingsData();
        LoadPlayerData();
    }

    private void Start()
    {
        // Phát nhạc nền. Vì có DontDestroyOnLoad, nhạc sẽ kêu mượt mà từ Boot sang Lobby
        if (persistentBGM != null && !persistentBGM.isPlaying)
        {
            persistentBGM.Play();
        }
    }

    private void LoadSettingsData()
    {
        audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f);
        gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f); 
        Debug.Log($"[GameManager] Đã nạp Cấu hình: Offset = {audioOffset}, Volume = {gameVolume}");
    }

    private void LoadPlayerData()
    {
        string savePath = Application.persistentDataPath + "/SaveData.json";
        if (File.Exists(savePath))
        {
            // Sau này bạn sẽ dùng JsonUtility để parse dữ liệu thực tế ở đây
            string json = File.ReadAllText(savePath);
            Debug.Log("[GameManager] Đã tìm thấy dữ liệu Save Game.");
        }
        else
        {
            totalCoins = 0;
            Debug.Log("[GameManager] Người chơi mới, tạo file Save trống.");
        }
    }
}