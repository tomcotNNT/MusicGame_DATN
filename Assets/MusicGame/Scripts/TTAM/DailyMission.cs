using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyMission : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject missionPanel;

    [Header("Mission Buttons")]
    [SerializeField] private Button claimButton1;
    [SerializeField] private Button claimButton2;
    [SerializeField] private Button claimButton3;

    [Header("UI Display")]
    [SerializeField] private TextMeshProUGUI coinTextUI;

    [Header("Reward Settings")]
    [SerializeField] private int mission1Reward = 500;

    private bool isMission1Claimed = false;

    private void Start()
    {
        isMission1Claimed = PlayerPrefs.GetInt("Mission1Claimed", 0) == 1;
        UpdateMissionUI();
    }

    public void OpenMissionPanel()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(true);
            UpdateMissionUI();

            if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        }
    }

    public void CloseMissionPanel()
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        }
    }

    public void ClaimMission1()
    {
        Debug.Log("--- Nút Claim 1 đã được bấm thành công! ---"); // Thêm dòng này để test

        if (isMission1Claimed) return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.playerData.totalCoins += mission1Reward;
            GameManager.Instance.SaveGameData();
            GameManager.Instance.PlayClickSound();
        }

        isMission1Claimed = true;
        PlayerPrefs.SetInt("Mission1Claimed", 1);
        PlayerPrefs.Save();

        UpdateMissionUI();

        Debug.Log($"Đã nhận thành công {mission1Reward} coins từ Mission 1!");
    }

    private void UpdateMissionUI()
    {
        if (GameManager.Instance != null && coinTextUI != null)
        {
            coinTextUI.text = GameManager.Instance.playerData.totalCoins.ToString();
        }

        if (claimButton1 != null)
        {
            TextMeshProUGUI btnText1 = claimButton1.GetComponentInChildren<TextMeshProUGUI>();

            if (isMission1Claimed)
            {
                claimButton1.interactable = false;
                if (btnText1 != null) btnText1.text = "DONE"; // Đã chuyển thành DONE để test
            }
            else
            {
                claimButton1.interactable = true;
                if (btnText1 != null) btnText1.text = "CLAIM";
            }
        }

        if (claimButton2 != null)
        {
            claimButton2.interactable = false;
            TextMeshProUGUI btnText2 = claimButton2.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText2 != null) btnText2.text = "LOCKED";
        }

        if (claimButton3 != null)
        {
            claimButton3.interactable = false;
            TextMeshProUGUI btnText3 = claimButton3.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText3 != null) btnText3.text = "LOCKED";
        }
    }
}