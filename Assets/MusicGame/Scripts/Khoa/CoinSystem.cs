using System;
using UnityEngine;

/// <summary>
/// Đếm coin trong màn (session). Tổng coin được lưu qua ResourceManager (ResourceType.Gold),
/// nên UI ở scene Menu (ResourceUI) tự cập nhật.
/// Đặt 1 object trong mỗi scene màn chơi.
/// Cuối màn (thắng/thua) gọi CoinSystem.Instance.CommitSession() để cộng vào tổng.
/// </summary>
public class CoinSystem : MonoBehaviour
{
    public static CoinSystem Instance { get; private set; }

    private const string LegacyKey = "TotalCoins"; // key cũ, dùng để chuyển dữ liệu

    public int SessionCoins { get; private set; }

    /// <summary>Tổng coin đã lưu (lấy từ ResourceManager).</summary>
    public static int TotalCoins => ResourceManager.Instance.Get(ResourceType.Gold);

    /// <summary>Số coin trong màn hiện tại thay đổi.</summary>
    public static event Action<int> OnSessionCoinsChanged;

    private void Awake()
    {
        Instance = this;
        SessionCoins = 0;
        MigrateLegacyData();
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

        ResourceManager.Instance.Add(ResourceType.Gold, SessionCoins);

        SessionCoins = 0;
        OnSessionCoinsChanged?.Invoke(SessionCoins);
    }

    /// <summary>Tiêu coin (shop...). Trả về false nếu không đủ.</summary>
    public static bool Spend(int amount)
    {
        return ResourceManager.Instance.Spend(ResourceType.Gold, amount);
    }

    /// <summary>Chuyển số coin cũ (key "TotalCoins") sang hệ thống mới, chỉ chạy 1 lần.</summary>
    private static void MigrateLegacyData()
    {
        if (!PlayerPrefs.HasKey(LegacyKey))
            return;

        int old = PlayerPrefs.GetInt(LegacyKey, 0);
        PlayerPrefs.DeleteKey(LegacyKey);

        if (old > 0)
            ResourceManager.Instance.Add(ResourceType.Gold, old);
        else
            PlayerPrefs.Save();
    }
}