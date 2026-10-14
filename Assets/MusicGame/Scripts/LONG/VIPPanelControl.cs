using UnityEngine;

public class VIPPanelControl : MonoBehaviour
{
    public GameObject vipPanel;

    public void OpenVipPanel()
    {
        vipPanel.SetActive(true);
    }

    public void CloseVipPanel()
    {
        vipPanel.SetActive(false);
    }
}