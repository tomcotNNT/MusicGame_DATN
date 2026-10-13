using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Các loại tài nguyên. Muốn thêm loại mới chỉ cần thêm vào đây.</summary>
public enum ResourceType
{
    Gold,
    Gems,
    Energy
}

/// <summary>
/// Quản lý tài nguyên, lưu/đọc bằng PlayerPrefs.
/// Tự tạo khi game chạy, không cần kéo vào scene.
/// </summary>
public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    /// <summary>Bắn ra mỗi khi một tài nguyên thay đổi (loại, giá trị mới).</summary>
    public static event Action<ResourceType, int> OnResourceChanged;

    private const string KeyPrefix = "RES_";

    private readonly Dictionary<ResourceType, int> _values = new Dictionary<ResourceType, int>();

    // Tự tạo ResourceManager trước khi scene đầu tiên load
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;
        var go = new GameObject("ResourceManager");
        go.AddComponent<ResourceManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAll();
    }

    // ---------- Giá trị khởi đầu khi người chơi mới ----------
    private static int GetDefault(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Gold: return 100;
            case ResourceType.Gems: return 10;
            case ResourceType.Energy: return 5;
            default: return 0;
        }
    }

    private static string Key(ResourceType type) => KeyPrefix + type;

    // ---------- Load / Save ----------
    private void LoadAll()
    {
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            _values[type] = PlayerPrefs.GetInt(Key(type), GetDefault(type));
    }

    private void Save(ResourceType type)
    {
        PlayerPrefs.SetInt(Key(type), _values[type]);
        PlayerPrefs.Save();
    }

    // ---------- API ----------
    public int Get(ResourceType type) => _values[type];

    public bool Has(ResourceType type, int amount) => _values[type] >= amount;

    public void Set(ResourceType type, int value)
    {
        value = Mathf.Max(0, value);
        if (_values[type] == value) return;

        _values[type] = value;
        Save(type);
        OnResourceChanged?.Invoke(type, value);
    }

    public void Add(ResourceType type, int amount)
    {
        if (amount <= 0) return;
        Set(type, _values[type] + amount);
    }

    /// <summary>Trừ tài nguyên. Trả về false nếu không đủ.</summary>
    public bool Spend(ResourceType type, int amount)
    {
        if (amount <= 0) return true;
        if (!Has(type, amount)) return false;

        Set(type, _values[type] - amount);
        return true;
    }

    /// <summary>Xoá toàn bộ dữ liệu tài nguyên, về giá trị mặc định.</summary>
    [ContextMenu("Reset All Resources")]
    public void ResetAll()
    {
        foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
        {
            PlayerPrefs.DeleteKey(Key(type));
            Set(type, GetDefault(type));
        }
    }
}