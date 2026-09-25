using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    // Đảm bảo chỉ có 1 GameManager duy nhất tồn tại (Singleton)
    public static GameManager Instance { get; private set; }

    [Header("UI Chuyển Cảnh")]
    public Slider loadingSlider; 

    [Header("Dữ Liệu Hệ Thống")]
    public float audioOffset = 0f;
    public float gameVolume = 1f;
    public int totalCoins = 0;

    [Header("Audio Wake-up")]
    public AudioSource wakeUpAudioSource;

    private void Awake()
    {
        // Xóa bản sao nếu lỡ quay lại Boot_Scene
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Bất tử khi chuyển Scene
    }

    private void Start()
    {
        // 0. Đánh thức chip âm thanh thiết bị di động
        if (wakeUpAudioSource != null)
        {
            wakeUpAudioSource.Play();
        }

        // Bắt đầu chuỗi sự kiện khởi động
        StartCoroutine(BootSequenceRoutine());
    }

    private IEnumerator BootSequenceRoutine()
    {
        // 1. Xử lý Dữ liệu Cấu hình (Settings)
        LoadSettingsData();

        // 2. Xử lý Dữ liệu Người chơi (Save Data JSON)
        LoadPlayerData();

        // 3. Bắt đầu chuyển sang Scene MainMenu
        yield return StartCoroutine(LoadSceneAsyncCoroutine());
    }

    private void LoadSettingsData()
    {
        // Đọc PlayerPrefs (Lưu trữ bộ nhớ tạm của điện thoại)
        audioOffset = PlayerPrefs.GetFloat("AudioOffset", 0f); // Mặc định là 0
        gameVolume = PlayerPrefs.GetFloat("GameVolume", 1f);   // Mặc định là 1 (100%)
        Debug.Log($"[GameManager] Đã nạp Cấu hình: Offset = {audioOffset}, Volume = {gameVolume}");
    }

    private void LoadPlayerData()
    {
        // Đường dẫn tới file JSON trên điện thoại Android/iOS
        string savePath = Application.persistentDataPath + "/SaveData.json";

        if (File.Exists(savePath))
        {
            string json = File.ReadAllText(savePath);
            Debug.Log("[GameManager] Đã tìm thấy dữ liệu Save Game.");
            // JsonUtility.FromJsonOverwrite(json, this); 
        }
        else
        {
            totalCoins = 0;
            Debug.Log("[GameManager] Người chơi mới, tạo file Save trống.");
        }
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        // Bắt đầu tải Scene số 1 (MainMenu) ở chế độ chạy ngầm
        AsyncOperation operation = SceneManager.LoadSceneAsync(1);
        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            // Unity load từ 0 đến 0.9 là hoàn tất tải dữ liệu
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (loadingSlider != null) 
            {
                loadingSlider.value = progress;
            }

            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(1f); // Đợi 1 giây để người chơi nhìn UI

                // 1. Cắt đứt tham chiếu UI để tránh lỗi Memory Leak
                loadingSlider = null;

                // 2. Ép Unity quét và xóa sạch hình ảnh Boot Scene khỏi RAM
                yield return Resources.UnloadUnusedAssets();

                // 3. Cho phép hiển thị Scene MainMenu
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}