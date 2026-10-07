using UnityEngine;
using UnityEngine.UI;

public class AboutPanelControl : MonoBehaviour
{
    [Header("About Button")]
    public Button aboutButton;

    [Header("About Panel")]
    public GameObject aboutPanel;

    [Header("Panel Back Button")]
    public Button backButton;

    private void Start()
    {
        // Bấm nút About trong SettingPanel
        aboutButton.onClick.AddListener(OpenAboutPanel);

        // Bấm nút Back trong AboutPanel
        backButton.onClick.AddListener(CloseAboutPanel);

        // AboutPanel ban đầu tắt
        aboutPanel.SetActive(false);
    }

    private void OpenAboutPanel()
    {
        aboutPanel.SetActive(true);
    }

    private void CloseAboutPanel()
    {
        aboutPanel.SetActive(false);
    }
}