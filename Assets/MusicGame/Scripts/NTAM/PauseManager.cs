using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;       
    [SerializeField] private GameObject pauseButton;      

    [Header("Lane Buttons to Lock")]
    [SerializeField] private Button buttonL; // Kéo nút L vào đây trên Inspector
    [SerializeField] private Button buttonR; // Kéo nút R vào đây trên Inspector

    [Header("Note / Piano Runner Script")]
    [SerializeField] private MonoBehaviour pianoRunnerScript; // Kéo thả script chạy nốt/piano của bạn vào đây

    [Header("Scene Navigation")]
    [SerializeField] private string selectSongSceneName = "SelectSong1"; 

    // Biến toàn cục để nhận diện trạng thái
    public static bool IsPaused { get; private set; } = false;

    private void Start()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    public void PauseGame()
    {
        IsPaused = true; 
        Time.timeScale = 0f; // Đóng băng thời gian game

        // 1. Hiện bảng Pause Panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        // 2. Tạm dừng toàn bộ âm thanh đang phát
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudioSources)
        {
            if (audio.isPlaying)
            {
                audio.Pause();
            }
        }

        // 3. Ẩn nút Pause chính
        if (pauseButton != null)
        {
            Button btn = pauseButton.GetComponent<Button>();
            if (btn != null) btn.interactable = false;

            Image img = pauseButton.GetComponent<Image>();
            if (img != null) img.enabled = false;
        }

        // 4. Khóa tương tác 2 nút L và R ngay lập tức
        if (buttonL != null) buttonL.interactable = false;
        if (buttonR != null) buttonR.interactable = false;

        // 5. Khóa script chạy nốt/piano một cách an toàn tuyệt đối (Không lo đơ màn hình)
        if (pianoRunnerScript != null)
        {
            pianoRunnerScript.enabled = false;
        }
    }

    public void ResumeGame()
    {
        IsPaused = false; 
        Time.timeScale = 1f; // Khôi phục thời gian

        // 1. Tắt bảng Pause Panel
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // 2. Tiếp tục âm thanh
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudioSources)
        {
            audio.UnPause();
        }

        // 3. Khôi phục nút Pause chính
        if (pauseButton != null)
        {
            Button btn = pauseButton.GetComponent<Button>();
            if (btn != null) btn.interactable = true;

            Image img = pauseButton.GetComponent<Image>();
            if (img != null) img.enabled = true;
        }

        // 4. Mở lại tương tác 2 nút L và R
        if (buttonL != null) buttonL.interactable = true;
        if (buttonR != null) buttonR.interactable = true;

        // 5. Bật lại script chạy nốt/piano để tiếp tục game
        if (pianoRunnerScript != null)
        {
            pianoRunnerScript.enabled = true;
        }
    }

    public void ReplayGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitToMenu()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(selectSongSceneName);
    }
}