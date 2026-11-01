using System;
using UnityEngine;
using UnityEngine.UI;

public class BossItemBattleManager : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField] private BossItemSpawner itemSpawner;
    [SerializeField, Min(1)] private int requiredCombo = 10;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private Text comboText; // Legacy UI Text; replace with TMP_Text if using TextMeshPro
    [SerializeField] private Text selectedText;
    [SerializeField] private Image comboFill;

    public event Action Won;
    public event Action Lost;
    public bool IsActive { get; private set; }
    public int Combo { get; private set; }
    public BossItemType? SelectedType { get; private set; }
    private bool finished;

    private void Start()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
    }

    private void Update()
    {
        // Boss.IsReady được bật khi boss đến vị trí do Boss.Begin(...) đặt.
        if (!finished && !IsActive && boss != null && boss.IsReady)
            BeginBattle();
    }

    public void BeginBattle()
    {
        if (finished || IsActive) return;
        IsActive = true;
        Combo = 0;
        SelectedType = null;
        if (itemPanel != null) itemPanel.SetActive(true);
        RefreshUI();
        if (itemSpawner != null) itemSpawner.StartSpawning(this);
    }

    public void Collect(BossItemType type)
    {
        if (!IsActive) return;
        if (!SelectedType.HasValue)
        {
            SelectedType = type;
            Combo = 1;
        }
        else if (SelectedType.Value == type)
        {
            Combo++;
        }
        else
        {
            Combo = 0;
            SelectedType = null; // Nhặt item tiếp theo để chọn lại.
        }
        RefreshUI();
        if (Combo >= requiredCombo) Win();
    }

    // Gọi từ sự kiện kết thúc bài hát của hệ thống phát nhạc.
    public void OnSongFinished()
    {
        if (IsActive && Combo < requiredCombo) Lose();
    }

    private void Win()
    {
        if (!IsActive) return;
        EndBattle();
        if (boss != null) boss.FinishByItems();
        Won?.Invoke();
    }

    private void Lose()
    {
        if (!IsActive) return;
        EndBattle();
        Lost?.Invoke(); // Kết nối Game Over UI tại đây.
    }

    private void EndBattle()
    {
        IsActive = false;
        finished = true;
        if (itemSpawner != null) itemSpawner.StopSpawning();
        if (itemPanel != null) itemPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        if (comboText != null) comboText.text = $"{Combo}/{requiredCombo}";
        if (selectedText != null) selectedText.text = SelectedType.HasValue ? SelectedType.Value.ToString() : "Chưa chọn";
        if (comboFill != null) comboFill.fillAmount = Mathf.Clamp01((float)Combo / requiredCombo);
    }
}
