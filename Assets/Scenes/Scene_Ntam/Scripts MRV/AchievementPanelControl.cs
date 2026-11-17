using UnityEngine;

public class AchievementPanelControl : MonoBehaviour
{
    public GameObject archivePanel;
    public AchievementItemUI[] items;

    public void OpenPanel()
    {
        if (archivePanel != null) archivePanel.SetActive(true);
        RefreshAll();
    }

    public void ClosePanel()
    {
        if (archivePanel != null) archivePanel.SetActive(false);
    }

    public void RefreshAll()
    {
        if (items == null) return;
        foreach (var item in items) if (item != null) item.Refresh();
    }
}
