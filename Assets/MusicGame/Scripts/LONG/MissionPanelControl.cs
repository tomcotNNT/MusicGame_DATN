using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel nhiệm vụ ngày (Daily Mission).
/// Tự tìm các dòng Mission1, Mission2, Mission3 trong panel (theo tên object), điền tên,
/// tiến độ, phần thưởng; nút CLAIM chỉ bấm được khi đã hoàn thành và chưa nhận.
/// Dữ liệu nằm trong PlayerData.json (PlayerDataManager), tự làm mới mỗi ngày.
/// </summary>
public class MissionPanelControl : MonoBehaviour
{
    public GameObject missionPanel;

    private class Row
    {
        public TMP_Text title;
        public TMP_Text description;
        public TMP_Text reward;
        public Button claimButton;
        public TMP_Text claimText;
    }

    private Row[] rows;
    private bool subscribed;

    private void Start()
    {
        BindRows();
        Subscribe();
        Refresh();
    }

    private void OnDestroy()
    {
        if (subscribed && PlayerDataManager.Instance != null)
            PlayerDataManager.Instance.OnDataChanged -= Refresh;
    }

    public void OpenMissionPanel()
    {
        missionPanel.SetActive(true);

        if (rows == null)
            BindRows();

        Refresh();
    }

    public void CloseMissionPanel()
    {
        missionPanel.SetActive(false);
    }

    private void Subscribe()
    {
        if (subscribed || PlayerDataManager.Instance == null) return;

        PlayerDataManager.Instance.OnDataChanged += Refresh;
        subscribed = true;
    }

    private void BindRows()
    {
        if (missionPanel == null) return;

        rows = new Row[DailyMissions.All.Length];

        for (int i = 0; i < rows.Length; i++)
        {
            Transform rowRoot = FindDeep(missionPanel.transform, "Mission" + (i + 1));
            if (rowRoot == null) continue;

            Row row = new Row
            {
                title = GetText(rowRoot, "MissionName"),
                description = GetText(rowRoot, "MissionDescription"),
                reward = GetText(rowRoot, "SoLuong")
            };

            Transform btn = FindDeep(rowRoot, "ClaimButton");
            if (btn != null)
            {
                row.claimButton = btn.GetComponent<Button>();
                row.claimText = btn.GetComponentInChildren<TMP_Text>(true);

                int index = i;
                if (row.claimButton != null)
                    row.claimButton.onClick.AddListener(() => Claim(index));
            }

            rows[i] = row;
        }
    }

    private void Claim(int index)
    {
        if (PlayerDataManager.Instance == null) return;

        PlayerDataManager.Instance.ClaimDailyMission(index);
        Refresh();
    }

    private void Refresh()
    {
        if (rows == null) return;

        Subscribe();

        PlayerDataManager data = PlayerDataManager.Instance;
        DailyMissionData progress = data != null ? data.GetDailyMissions() : null;

        for (int i = 0; i < rows.Length; i++)
        {
            Row row = rows[i];
            if (row == null) continue;

            DailyMissions.Def def = DailyMissions.All[i];
            int value = progress != null ? Mathf.Min(def.progress(progress), def.target) : 0;
            bool done = value >= def.target;
            bool claimed = data != null && data.IsDailyMissionClaimed(i);

            if (row.title != null) row.title.text = def.title;
            if (row.description != null) row.description.text = string.Format(def.descriptionFormat, value, def.target);
            if (row.reward != null) row.reward.text = def.coinReward.ToString();

            if (row.claimButton != null)
                row.claimButton.interactable = done && !claimed;

            if (row.claimText != null)
                row.claimText.text = claimed ? "DONE" : (done ? "CLAIM" : "GO");
        }
    }

    private static TMP_Text GetText(Transform root, string name)
    {
        Transform t = FindDeep(root, name);
        return t != null ? t.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;

        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }

        return null;
    }
}
