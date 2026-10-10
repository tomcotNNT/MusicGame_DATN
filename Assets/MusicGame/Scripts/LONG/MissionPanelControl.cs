using UnityEngine;

public class MissionPanelControl : MonoBehaviour
{
    public GameObject missionPanel;

    public void OpenMissionPanel()
    {
        missionPanel.SetActive(true);
    }

    public void CloseMissionPanel()
    {
        missionPanel.SetActive(false);
    }
}