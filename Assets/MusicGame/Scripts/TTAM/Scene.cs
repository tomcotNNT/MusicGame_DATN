using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scenes : MonoBehaviour
{
    [Tooltip("Panel option cũ trong scene. Khi bật 'Use New Pause Menu' thì panel này không còn được hiện")]
    public GameObject optionPanel;

    [Header("Menu tạm dừng mới")]
    [Tooltip("Dùng menu tạm dừng dựng bằng code (RESUME / REPLAY / HOME, âm lượng, hiệu ứng). Tắt = dùng panel cũ")]
    [SerializeField] private bool useNewPauseMenu = true;
    [Tooltip("Mỗi số của đếm ngược 3-2-1 khi chơi tiếp (giây thật). 0.5 = đúng 1 phách ở 120 BPM")]
    [SerializeField, Min(0f)] private float resumeCountdownStep = 0.5f;

    private PauseMenuUI menu;
    private bool isPaused;
    private bool countingDown;
    private Coroutine countdownRoutine;

    private void Start()
    {
        if (optionPanel != null) optionPanel.SetActive(false);
        isPaused = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleOptionPanel();
    }

    // Điện thoại chuyển app / tắt màn hình / rời cửa sổ Unity -> tự mở menu pause để không lệch nhịp
    private void OnApplicationPause(bool pauseStatus)
    {
        if (!pauseStatus) return;

        if (countingDown)
        {
            // đang đếm ngược mà rời app -> dừng lại, mở menu (vẫn đang pause)
            StopCountdown();
            ShowMenuVisual(true);
            return;
        }

        if (!isPaused && Time.timeScale > 0f && !EndGameBase.ResultShowing)
            SetOptionPanel(true);
    }

    private void OnDestroy()
    {
        // Phòng trường hợp object bị huỷ khi đang pause
        ResumeAll();
    }

    public void ToggleOptionPanel()
    {
        if (countingDown) return;   // đang đếm 3-2-1 thì bỏ qua
        if (!isPaused && EndGameBase.ResultShowing) return;   // đã hết bài, đang hiện bảng kết quả

        // Game đã bị dừng bởi script khác (Game Over, bảng kết quả...) -> không mở/đóng pause,
        // tránh đóng panel làm timeScale = 1 và game chạy tiếp sau bảng Game Over
        if (!isPaused && Time.timeScale == 0f)
            return;

        SetOptionPanel(!isPaused);
    }

    public void ShowOptionPanel() => SetOptionPanel(true);

    public void HideOptionPanel() => SetOptionPanel(false);

    /// <summary>Nút RESUME (gắn được vào Button cũ): đóng menu, đếm ngược rồi chơi tiếp.</summary>
    public void ResumeGame() => SetOptionPanel(false);

    private void SetOptionPanel(bool active)
    {
        if (active)
        {
            if (isPaused) return;
            isPaused = true;
            PauseAll();
            ShowMenuVisual(true);
        }
        else
        {
            if (!isPaused) return;
            ShowMenuVisual(false);

            if (useNewPauseMenu && resumeCountdownStep > 0f && isActiveAndEnabled)
                countdownRoutine = StartCoroutine(ResumeAfterCountdown());
            else
                FinishResume();
        }
    }

    private void ShowMenuVisual(bool show)
    {
        if (useNewPauseMenu)
        {
            if (optionPanel != null) optionPanel.SetActive(false);
            if (show)
            {
                EnsureMenu();
                menu.Show();
            }
            else if (menu != null) menu.Hide();
        }
        else if (optionPanel != null)
        {
            optionPanel.SetActive(show);
        }
    }

    private void EnsureMenu()
    {
        if (menu != null) return;
        menu = PauseMenuUI.Create();
        // KHÔNG gắn vào object của Scenes: nếu nằm trong Canvas khác sẽ thành Canvas lồng, mất thứ tự hiển thị
        menu.OnResume = ResumeGame;
        menu.OnReplay = RetryLevel;
        menu.OnHome = BackToSelect;
    }

    private IEnumerator ResumeAfterCountdown()
    {
        countingDown = true;
        yield return menu.StartCoroutine(menu.Countdown(resumeCountdownStep, FinishResume));
        countingDown = false;
        countdownRoutine = null;
    }

    private void StopCountdown()
    {
        if (countdownRoutine != null) StopCoroutine(countdownRoutine);
        if (menu != null)
        {
            menu.StopAllCoroutines();
            menu.CancelCountdown();
        }
        countdownRoutine = null;
        countingDown = false;
    }

    private void FinishResume()
    {
        isPaused = false;
        ResumeAll();
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
