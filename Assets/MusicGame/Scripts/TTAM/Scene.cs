using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes : MonoBehaviour
{
    public GameObject optionPanel;

    private void Start()
    {
        SetOptionPanel(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleOptionPanel();
    }

    // Điện thoại chuyển app / tắt màn hình -> tự mở menu pause để không lệch nhịp
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && optionPanel != null && !optionPanel.activeSelf && Time.timeScale > 0f)
            SetOptionPanel(true);
    }

    private void OnDestroy()
    {
        // Phòng trường hợp object bị huỷ khi đang pause
        ResumeAll();
    }

    public void ToggleOptionPanel()
    {
        if (optionPanel == null)
            return;

        // Game đã bị dừng bởi script khác (Game Over...) -> không cho mở/đóng pause,
        // tránh đóng panel làm timeScale = 1 và game chạy tiếp sau bảng Game Over
        if (!optionPanel.activeSelf && Time.timeScale == 0f)
            return;

        SetOptionPanel(!optionPanel.activeSelf);
    }

    public void ShowOptionPanel() => SetOptionPanel(true);

    public void HideOptionPanel() => SetOptionPanel(false);

    private void SetOptionPanel(bool active)
    {
        if (optionPanel != null)
            optionPanel.SetActive(active);

        if (active) PauseAll();
        else ResumeAll();
    }

    private bool songPaused;

    private void PauseAll()
    {
        Time.timeScale = 0f;          // dừng vật lý, animation, Time.deltaTime
        AudioListener.pause = true;   // dừng toàn bộ nhạc và âm thanh

        // Đóng băng đồng hồ bài hát (dspTime vẫn chạy khi pause -> không làm thì lệch nhịp)
        SongPause.PauseAll();
        songPaused = true;
    }

    private void ResumeAll()
    {
        if (songPaused)
        {
            SongPause.ResumeAll();
            songPaused = false;
        }

        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    // ===== Nút điều hướng trong màn chơi (dùng chung luật của LevelFlow) =====

    /// <summary>Nút Replay: trừ 1 lượt, chơi lại màn hiện tại.</summary>
    public void RetryLevel() => LevelFlow.Retry();

    /// <summary>Nút Next: màn kế phải đã mở khóa, trừ 1 lượt.</summary>
    public void NextLevel() => LevelFlow.NextLevel();

    /// <summary>Nút Home: về màn chọn level.</summary>
    public void BackToSelect() => LevelFlow.BackToSelect();

    /// <summary>Chuyển thẳng sang scene chỉ định, KHÔNG trừ lượt / kiểm tra khóa.
    /// Không dùng cho nút Replay / Next trong màn chơi.</summary>
    public void GetScene(string sceneName)
    {
        // Không bỏ pause trước: SceneFader tự trả timeScale/âm thanh khi scene mới load
        SceneFader.Load(sceneName);   // tối dần rồi chuyển scene
    }
}