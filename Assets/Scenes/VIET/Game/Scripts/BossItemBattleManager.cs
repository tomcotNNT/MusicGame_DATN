using System;
using UnityEngine;
using UnityEngine.UI;

public class BossItemBattleManager : MonoBehaviour
{
    [SerializeField] private Boss boss;
    [SerializeField] private BossItemSpawner itemSpawner;
    [SerializeField, Min(1)] private int requiredCombo = 10;
    [SerializeField] private GameObject itemPanel;
    [SerializeField] private Text comboText;
    [SerializeField] private Text selectedText;
    [SerializeField] private Image comboFill;
    [Header("Optional end panels")]
    [SerializeField] private GameObject gameOverPanel;

    public event Action Won;
    public event Action Lost;
    public bool IsActive { get; private set; }
    public bool HasWon { get; private set; }
    public bool HasLost { get; private set; }
    public int Combo { get; private set; }
    public BossItemType? SelectedType { get; private set; }
    private bool finished;

    private void OnEnable()
    {
        if (boss != null) boss.Appeared += OnBossAppeared;
    }

    private void OnDisable()
    {
        if (boss != null) boss.Appeared -= OnBossAppeared;
    }

    private void Start()
    {
        if (itemPanel != null) itemPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (boss != null && boss.HasBegun) BeginBattle();
    }

    private void OnBossAppeared() => BeginBattle();

    public void BeginBattle()
    {
        if (finished || IsActive) return;
        if (itemSpawner == null)
        {
            Debug.LogError("BossItemBattleManager: chưa gắn ItemSpawner!", this);
            return;
        }
        IsActive = true;
        Combo = 0;
        SelectedType = null;
        if (itemPanel != null) itemPanel.SetActive(true);
        RefreshUI();
        itemSpawner.StartSpawning(this);
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
            Combo++;
        else
        {
            Combo = 0;
            SelectedType = null;
        }
        RefreshUI();
        if (Combo >= requiredCombo) Win();
    }

    // Gọi từ PlayerController.EndGame() hoặc sự kiện kết thúc bài nhạc.
    public void OnSongFinished()
    {
        if (IsActive && Combo < requiredCombo) Lose();
    }

    private void Win()
    {
        if (!IsActive) return;
        HasWon = true;
        EndBattle();
        if (boss != null) boss.FinishByItems();
        Won?.Invoke();
    }

    private void Lose()
    {
        if (!IsActive) return;
        HasLost = true;
        EndBattle();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Lost?.Invoke();
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
