using UnityEngine;
using UnityEngine.UI;

public class RatePanelControl : MonoBehaviour
{
    [Header("Rate Button")]
    public Button rateButton;

    [Header("Rate Panel")]
    public GameObject ratePanel;

    [Header("Panel Back Button")]
    public Button backButton;

    private void Start()
    {
        // Bấm nút Rate trong SettingPanel
        rateButton.onClick.AddListener(OpenRatePanel);

        // Bấm nút Back trong RatePanel
        backButton.onClick.AddListener(CloseRatePanel);

        // RatePanel ban đầu tắt
        ratePanel.SetActive(false);
    }

    private void OpenRatePanel()
    {
        ratePanel.SetActive(true);
    }

    private void CloseRatePanel()
    {
        ratePanel.SetActive(false);
    }
}