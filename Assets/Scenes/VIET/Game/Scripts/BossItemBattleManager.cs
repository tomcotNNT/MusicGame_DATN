using System;
using UnityEngine;
using UnityEngine.UI;

public class BossItemBattleManager : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField, Min(1)] private int requiredCombo = 10;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private Text comboText;
    [SerializeField] private Text selectedText;
    [SerializeField] private Image comboFill;

    public event Action Won;
    public event Action Lost;
    public bool IsActive { get; private set; }
    public int Combo { get; private set; }
    public BossItemType? SelectedType { get; private set; }
    private bool finished;

    private void Start() { if (itemPanel != null) itemPanel.SetActive(false); }

    // Chi duoc goi tu PianoKeySpawner khi Boss xuat hien
    public void BeginBattle()
    {
        if (finished || IsActive) return;
        IsActive = true;
        Combo = 0;
        SelectedType = null;
        if (itemPanel != null) itemPanel.SetActive(true);
        RefreshUI();
        Debug.Log("[BossItem] Battle started");
    }

    public void Collect(BossItemType type)
    {
        if (!IsActive) return;
        if (!SelectedType.HasValue) { SelectedType = type; Combo = 1; }
        else if (SelectedType.Value == type) Combo++;
        else { SelectedType = null; Combo = 0; }
        Debug.Log($"[BossItem] Hit {type} | Combo {Combo}/{requiredCombo}");
        RefreshUI();
        if (Combo >= requiredCombo) Win();
    }

    public void OnSongFinished() { if (IsActive && Combo < requiredCombo) Lose(); }

    private void Win()
    {
        if (!IsActive) return;
        IsActive = false; finished = true;
        if (itemPanel != null) itemPanel.SetActive(false);
        if (boss != null) boss.FinishByItems();
        Won?.Invoke();
        Debug.Log("[BossItem] WIN");
    }

    private void Lose()
    {
        if (!IsActive) return;
        IsActive = false; finished = true;
        if (itemPanel != null) itemPanel.SetActive(false);
        Lost?.Invoke();
        Debug.Log("[BossItem] LOSE - chua du combo");
    }

    private void RefreshUI()
    {
        if (comboText != null) comboText.text = $"{Combo}/{requiredCombo}";
        if (selectedText != null) selectedText.text = SelectedType.HasValue ? SelectedType.Value.ToString() : "Chua chon";
        if (comboFill != null) comboFill.fillAmount = Mathf.Clamp01((float)Combo / requiredCombo);
    }
}
