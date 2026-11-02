using UnityEngine;

public enum BossItemType { Blue, Purple, Gold }

[RequireComponent(typeof(Collider2D))]
public class BossCollectible : MonoBehaviour
{
    [SerializeField] private BossItemType itemType;
    [Header("Collect effects (can reuse PianoKey VFX prefabs)")]
    [SerializeField] private GameObject collectVfxPrefab;
    [SerializeField] private GameObject collectVfxPrefab2;
    [SerializeField] private AudioClip collectSfx;
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;
    [SerializeField, Min(0.1f)] private float vfxLifetime = 1f;

    private BossItemBattleManager manager;
    private bool collected;

    private void Awake()
    {
        Collider2D collider2d = GetComponent<Collider2D>();
        collider2d.isTrigger = true;
    }

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
        PlayCollectEffects();
        manager.Collect(itemType);
        Destroy(gameObject);
    }

    private void PlayCollectEffects()
    {
        SpawnVfx(collectVfxPrefab);
        SpawnVfx(collectVfxPrefab2);
        if (collectSfx != null)
            AudioSource.PlayClipAtPoint(collectSfx, transform.position, collectVolume);
    }

    private void SpawnVfx(GameObject prefab)
    {
        if (prefab == null) return;
        GameObject vfx = Instantiate(prefab, transform.position, Quaternion.identity);
        Destroy(vfx, vfxLifetime);
    }
}
