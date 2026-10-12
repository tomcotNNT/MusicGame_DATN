using System;
using UnityEngine;

/// <summary>
/// Đếm coin trong màn (session) và tổng coin đã lưu (PlayerPrefs).
/// Đặt 1 object trong mỗi scene màn chơi.
/// Cuối màn (thắng/thua) gọi CoinSystem.Instance.CommitSession() để cộng vào tổng.
/// </summary>
public class CoinSystem : MonoBehaviour
{
    public static CoinSystem Instance { get; private set; }

    private const string TotalKey = "TotalCoins";

    public int SessionCoins { get; private set; }

    public static int TotalCoins => PlayerPrefs.GetInt(TotalKey, 0);

    /// <summary>Số coin trong màn hiện tại thay đổi.</summary>
    public static event Action<int> OnSessionCoinsChanged;

    private void Awake()
    {
        Instance = this;
        SessionCoins = 0;
    }

    private void Start()
    {
        // Hết bài thì tự cộng coin của màn vào tổng
        if (PianoKeySpawner.Instance != null)
            PianoKeySpawner.Instance.SongEnded += CommitSession;
    }

    private void OnDestroy()
    {
        if (PianoKeySpawner.Instance != null)
            PianoKeySpawner.Instance.SongEnded -= CommitSession;

        if (Instance == this)
            Instance = null;
    }

    public void AddCoin(int amount = 1)
    {
        if (amount <= 0)
            return;

        SessionCoins += amount;
        OnSessionCoinsChanged?.Invoke(SessionCoins);
    }

    /// <summary>Cộng coin của màn này vào tổng và lưu. Gọi 1 lần khi kết thúc màn.</summary>
    public void CommitSession()
    {
        if (SessionCoins <= 0)
            return;

        PlayerPrefs.SetInt(TotalKey, TotalCoins + SessionCoins);
        PlayerPrefs.Save();

        SessionCoins = 0;
        OnSessionCoinsChanged?.Invoke(SessionCoins);
    }

    /// <summary>Tiêu coin (shop...). Trả về false nếu không đủ.</summary>
    public static bool Spend(int amount)
    {
        if (amount <= 0 || TotalCoins < amount)
            return false;

        PlayerPrefs.SetInt(TotalKey, TotalCoins - amount);
        PlayerPrefs.Save();
        return true;
    }
}