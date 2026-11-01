using UnityEngine;

public enum BossItemType { Blue, Purple, Gold }

[RequireComponent(typeof(Collider2D))]
public class BossCollectible : MonoBehaviour
{
    [SerializeField] private BossItemType itemType;
    private BossItemBattleManager manager;
    private bool collected;

    public void Initialize(BossItemType type, BossItemBattleManager owner)
    {
        itemType = type;
        manager = owner;
        collected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected || !other.CompareTag("Player")) return;
        if (manager == null || !manager.IsActive) return;
        collected = true;
        manager.Collect(itemType);
        Destroy(gameObject);
    }
}
