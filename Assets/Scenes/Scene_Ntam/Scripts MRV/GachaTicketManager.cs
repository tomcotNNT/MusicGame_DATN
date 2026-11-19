
using System;
using UnityEngine;
using TMPro;

public class GachaTicketManager : MonoBehaviour
{
    public static GachaTicketManager Instance { get; private set; }

    [Header("Ticket Settings")]
    [SerializeField] private int maxTickets = 5;
    [SerializeField] private int regenHours = 6;

    [Header("Ticket UI")]
    [SerializeField] private TMP_Text ticketAmountText;
    [SerializeField] private TMP_Text ticketTimerText;

    private const string TicketKey = "Gacha_Tickets";
    private const string NextRegenKey = "Gacha_NextRegen";

    private int tickets;
    private long nextRegenTime;

    private long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Load();
        RefreshTickets();
    }

    private void OnEnable()
    {
        if (Instance == this)
        {
            RefreshTickets();
            UpdateUI();
        }
    }

    private void Update()
    {
        RefreshTickets();
        UpdateUI();
    }

    private void Load()
    {
        tickets = PlayerPrefs.GetInt(TicketKey, maxTickets);
        long.TryParse(PlayerPrefs.GetString(NextRegenKey, "0"),
            out nextRegenTime);

        tickets = Mathf.Clamp(tickets, 0, maxTickets);
    }

    private void Save()
    {
        PlayerPrefs.SetInt(TicketKey, tickets);
        PlayerPrefs.SetString(NextRegenKey, nextRegenTime.ToString());
        PlayerPrefs.Save();
    }

    private void RefreshTickets()
    {
        if (tickets >= maxTickets)
        {
            if (nextRegenTime != 0)
            {
                nextRegenTime = 0;
                Save();
            }
            return;
        }

        long interval = regenHours * 3600L;

        if (nextRegenTime <= 0)
        {
            nextRegenTime = Now + interval;
            Save();
            return;
        }

        if (Now < nextRegenTime)
            return;

        long recovered = 1 + (Now - nextRegenTime) / interval;

        tickets = (int)Math.Min(maxTickets, tickets + recovered);

        if (tickets >= maxTickets)
            nextRegenTime = 0;
        else
            nextRegenTime += recovered * interval;

        Save();
    }

    public bool SpendTickets(int amount)
    {
        RefreshTickets();

        if (amount <= 0 || tickets < amount)
            return false;

        bool wasFull = tickets == maxTickets;

        tickets -= amount;

        if (wasFull || nextRegenTime <= 0)
            nextRegenTime = Now + regenHours * 3600L;

        Save();
        UpdateUI();
        return true;
    }

    public void AddTickets(int amount)
    {
        if (amount <= 0)
            return;

        RefreshTickets();

        tickets = Mathf.Min(maxTickets, tickets + amount);

        if (tickets >= maxTickets)
            nextRegenTime = 0;

        Save();
        UpdateUI();
    }

    public int GetTickets()
    {
        RefreshTickets();
        return tickets;
    }

    private void UpdateUI()
    {
        if (ticketAmountText != null)
            ticketAmountText.text = tickets + "/" + maxTickets;

        if (ticketTimerText == null)
            return;

        if (tickets >= maxTickets)
        {
            ticketTimerText.text = "FULL";
            return;
        }

        long seconds = Math.Max(0, nextRegenTime - Now);

        TimeSpan time = TimeSpan.FromSeconds(seconds);

        ticketTimerText.text =
            string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)time.TotalHours, time.Minutes, time.Seconds);
    }
}
