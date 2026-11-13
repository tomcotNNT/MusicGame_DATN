using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemDurationSystem : MonoBehaviour
{
    public static ItemDurationSystem Instance { get; private set; }

    [SerializeField] private ItemDurationBar barPrefab;
    [SerializeField] private Transform container;   // có Vertical/Horizontal Layout Group

    private readonly Dictionary<string, ItemDurationBar> bars = new Dictionary<string, ItemDurationBar>();
    private readonly Dictionary<string, Action> endCallbacks = new Dictionary<string, Action>();

    private void Awake()
    {
        Instance = this;
    }

    public void StartEffect(string id, Sprite icon, float duration, Action onEnd = null)
    {
        endCallbacks[id] = onEnd;

        if (bars.TryGetValue(id, out ItemDurationBar existing) && existing != null)
        {
            existing.Restart(duration);
            return;
        }

        ItemDurationBar bar = Instantiate(barPrefab, container);
        bar.Begin(id, icon, duration, OnBarEnd);
        bars[id] = bar;
    }

    public bool IsActive(string id)
    {
        return bars.TryGetValue(id, out ItemDurationBar b) && b != null;
    }

    // Hủy thanh mà không gọi onEnd.
    public void StopEffect(string id)
    {
        if (bars.TryGetValue(id, out ItemDurationBar b) && b != null)
            b.Finish(false);

        bars.Remove(id);
        endCallbacks.Remove(id);
    }

    public void ClearAll()
    {
        foreach (var kv in bars)
            if (kv.Value != null)
                kv.Value.Finish(false);

        bars.Clear();
        endCallbacks.Clear();
    }

    private void OnBarEnd(string id)
    {
        bars.Remove(id);

        if (endCallbacks.TryGetValue(id, out Action cb))
        {
            endCallbacks.Remove(id);
            cb?.Invoke();
        }
    }
}