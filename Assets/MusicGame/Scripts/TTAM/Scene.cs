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

    /// <summary>Bật/tắt panel, luôn đọc trạng thái thật từ panel nên không bị lệch.</summary>
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
    }

    /// <summary>Chuyển sang scene được chỉ định (phải có trong Build Settings).</summary>
    public void GetScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}