using System;
using TMPro;
using UnityEngine;

/// <summary>
/// Gắn vào 1 GameObject trong scene Menu (ví dụ Canvas hoặc TopBar).
/// Gán các TMP_Text vào list, chọn loại tài nguyên tương ứng.
/// UI tự cập nhật khi tài nguyên thay đổi.
/// </summary>
public class ResourceUI : MonoBehaviour
{
    [Serializable]
    public class Entry
    {
        public ResourceType type;
        public TMP_Text label;
    }

    [SerializeField] private Entry[] entries;

    private void OnEnable()
    {
        ResourceManager.OnResourceChanged += HandleChanged;
        RefreshAll();
    }

    private void OnDisable()
    {
        ResourceManager.OnResourceChanged -= HandleChanged;
    }

    private void HandleChanged(ResourceType type, int value)
    {
        foreach (var e in entries)
            if (e.type == type) SetText(e, value);
    }

    private void RefreshAll()
    {
        if (ResourceManager.Instance == null) return;

        foreach (var e in entries)
            SetText(e, ResourceManager.Instance.Get(e.type));
    }

    private static void SetText(Entry e, int value)
    {
        if (e.label != null)
            e.label.text = value.ToString();
    }

    // ---------- Hàm test: gán vào Button OnClick ----------
    public void AddGold100() => ResourceManager.Instance.Add(ResourceType.Gold, 100);
    public void AddGems10() => ResourceManager.Instance.Add(ResourceType.Gems, 10);
    public void SpendEnergy1() => ResourceManager.Instance.Spend(ResourceType.Energy, 1);
    public void ResetAll() => ResourceManager.Instance.ResetAll();
}