using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (wakeUpAudioSource != null)
        {
            wakeUpAudioSource.Play();
        }

        StartCoroutine(BootSequenceRoutine());
    }

    private IEnumerator BootSequenceRoutine()
    {
        LoadSettingsData();
        LoadPlayerData();
        yield return StartCoroutine(LoadSceneAsyncCoroutine());
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
            string json = File.ReadAllText(savePath);
            Debug.Log("[GameManager] Đã tìm thấy dữ liệu Save Game.");
        }
        else
        {
            totalCoins = 0;
            Debug.Log("[GameManager] Người chơi mới, tạo file Save trống.");
        }
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        Debug.Log("[DEBUG] Bắt đầu gọi lệnh LoadScene số 1 ngầm...");
        AsyncOperation operation = SceneManager.LoadSceneAsync(1);
        
        if (operation == null)
        {
            Debug.LogError("[LỖI NẶNG] Unity không tìm thấy Scene số 1. Hãy kiểm tra lại Build Settings!");
            yield break; // Dừng code ngay lập tức
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            
            if (loadingSlider != null) 
            {
                loadingSlider.value = progress;
            }

            // In tiến độ ra Console
            Debug.Log($"[DEBUG] Đang load: {progress * 100}%");

           if (operation.progress >= 0.9f)
            {
                Debug.Log("[DEBUG] Đã load xong 90% dữ liệu. Đang chờ 1 giây ngắm UI...");
                yield return new WaitForSeconds(1f); 

                // 1. Cắt đứt tham chiếu UI để tránh lỗi Memory Leak
                loadingSlider = null;
                
                // (ĐÃ XÓA LỆNH DỌN RÁC RAM GÂY TREO GAME Ở ĐÂY)

                Debug.Log("[DEBUG] BẮT ĐẦU CHUYỂN SCENE!");
                operation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}