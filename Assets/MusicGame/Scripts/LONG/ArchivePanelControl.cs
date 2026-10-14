using UnityEngine;

public class ArchivePanelControl : MonoBehaviour
{
    public GameObject archivePanel;

    public void OpenArchivePanel()
    {
        archivePanel.SetActive(true);
    }

    public void CloseArchivePanel()
    {
        archivePanel.SetActive(false);
    }
}