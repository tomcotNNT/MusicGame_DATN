using UnityEngine;

public class NTLevelManager : MonoBehaviour
{
    [Header("Current Level Data (Cải Tiến)")]
    [SerializeField] private NTLevelData currentLevel;

    [Header("References (Hệ Thống Mới)")]
    [SerializeField] private NTMidiReader ntMidiReader;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        LoadLevel();
    }

    private void LoadLevel()
    {
        if (currentLevel == null)
        {
            Debug.LogError("NTLevelManager: Chưa gán NTLevelData!");
            return;
        }

        if (ntMidiReader == null)
        {
            Debug.LogError("NTLevelManager: Chưa gán NTMidiReader!");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("NTLevelManager: Chưa gán AudioSource!");
            return;
        }

        // 1. Nạp và xử lý file MIDI dựa hoàn toàn vào các thông số độc lập bên trong NTLevelData
        ntMidiReader.LoadMidi(currentLevel);

        // 2. Nạp và cấu hình Audio Clip nhạc nền của màn chơi
        if (currentLevel.music != null)
        {
            audioSource.clip = currentLevel.music;
            // audioSource.Play(); // Có thể bật sẵn hoặc để Director điều phối thời điểm phát
        }
        else
        {
            Debug.LogWarning("NTLevelManager: NTLevelData hiện tại chưa có AudioClip nhạc nền!");
        }

        Debug.Log("Loaded NT Level Successfully: " + currentLevel.levelName);
    }
}