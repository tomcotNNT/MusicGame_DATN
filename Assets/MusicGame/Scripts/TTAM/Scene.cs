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

    private void OnDestroy()
    {
        // Phòng trường hợp object bị huỷ khi đang pause
        ResumeAll();
    }

    public void ToggleOptionPanel()
    {
        if (optionPanel != null)
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

    private void PauseAll()
    {
        Time.timeScale = 0f;          // dừng vật lý, animation, Time.deltaTime
        AudioListener.pause = true;   // dừng toàn bộ nhạc và âm thanh
    }

    private void ResumeAll()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    /// <summary>Chuyển sang scene được chỉ định (phải có trong Build Settings).</summary>
    public void GetScene(string sceneName)
    {
        ResumeAll();
        SceneManager.LoadScene(sceneName);
    }
}