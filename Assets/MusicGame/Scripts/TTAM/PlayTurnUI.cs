using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị lượt chơi (đọc từ PlayerData.json qua PlayerDataManager):
/// "3/5" + đồng hồ đếm ngược tới lượt tiếp theo + panel "Hết lượt".
/// Gắn vào TopBar ở Lobby / Level Select / Select Char.
/// </summary>
public class PlayTurnUI : MonoBehaviour
{
    [Header("Hiển thị")]
    [Tooltip("Text số lượt, VD: 3/5")]
    [SerializeField] private TMP_Text turnText;

    [Tooltip("Text đếm ngược hồi lượt, VD: 12:34")]
    [SerializeField] private TMP_Text timerText;

    [Tooltip("Chữ hiện khi đầy lượt")]
    [SerializeField] private string fullLabel = "FULL";

    [Header("Thông báo hết lượt (tùy chọn)")]
    [Tooltip("Panel bật lên khi muốn vào màn mà hết lượt (để tắt sẵn trong Hierarchy)")]
    [SerializeField] private GameObject notEnoughPanel;

    [Tooltip("Text trên panel hiện thời gian chờ (tùy chọn)")]
    [SerializeField] private TMP_Text notEnoughTimerText;

    private bool subscribed;

    private void OnEnable()
    {
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
        TrySubscribe();
        Refresh();
    }

    private void Start()
    {
        // Phòng trường hợp OnEnable chạy trước khi PlayerDataManager kịp khởi tạo
        TrySubscribe();
        Refresh();
    }

    private void OnDisable()
    {
        if (subscribed && PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.OnPlayTurnChanged -= Refresh;
            PlayerDataManager.Instance.OnNotEnoughTurns -= ShowNotEnough;
        }
        subscribed = false;
    }

    private void TrySubscribe()
    {
        if (subscribed || PlayerDataManager.Instance == null) return;

        PlayerDataManager.Instance.OnPlayTurnChanged += Refresh;
        PlayerDataManager.Instance.OnNotEnoughTurns += ShowNotEnough;
        subscribed = true;
    }

    private void Update()
    {
        RefreshTimer();
    }

    private void Refresh()
    {
        PlayerDataManager pdm = PlayerDataManager.Instance;
        if (pdm == null) return;

        if (turnText != null)
            turnText.text = $"{pdm.GetTurns()}/{pdm.MaxTurns}";

        RefreshTimer();
    }

    private void RefreshTimer()
    {
        PlayerDataManager pdm = PlayerDataManager.Instance;
        if (pdm == null) return;

        string label = pdm.IsTurnFull() ? fullLabel : FormatTime(pdm.GetTimeToNextTurn());

        if (timerText != null)
            timerText.text = label;

        if (notEnoughTimerText != null && notEnoughPanel != null && notEnoughPanel.activeSelf)
            notEnoughTimerText.text = $"Lượt tiếp theo sau {label}";
    }

    private static string FormatTime(TimeSpan t)
    {
        return t.TotalHours >= 1
            ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}"
            : $"{t.Minutes:00}:{t.Seconds:00}";
    }

    private void ShowNotEnough()
    {
        if (notEnoughPanel != null) notEnoughPanel.SetActive(true);
        RefreshTimer();
    }

    // ---------- Gán vào Button OnClick ----------
    public void CloseNotEnoughPanel()
    {
        if (notEnoughPanel != null) notEnoughPanel.SetActive(false);
    }

    public void AddOneTurn()
    {
        if (PlayerDataManager.Instance != null) PlayerDataManager.Instance.AddTurns(1);
    }

    public void RefillFull()
    {
        if (PlayerDataManager.Instance != null) PlayerDataManager.Instance.RefillTurns();
    }
}
