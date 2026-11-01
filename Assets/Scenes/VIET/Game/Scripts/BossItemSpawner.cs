using System.Collections;
using UnityEngine;

public class BossItemSpawner : MonoBehaviour
{
    [SerializeField] private BossCollectible[] itemPrefabs = new BossCollectible[3]; // Blue, Purple, Gold
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1)] private int maxAlive = 12;
    [SerializeField, Min(0.1f)] private float itemLifetime = 12f;

    private BossItemBattleManager manager;
    private Coroutine routine;
    private int sequence;

    public void StartSpawning(BossItemBattleManager owner)
    {
        StopSpawning();
        manager = owner;
        sequence = 0;
        routine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (manager != null && manager.IsActive)
        {
            if (spawnPoints.Length > 0 && CountAlive() < maxAlive)
            {
                // Luân phiên bảo đảm cả ba loại xuất hiện, không phụ thuộc random.
                int index = sequence % 3;
                sequence++;
                if (itemPrefabs.Length > index && itemPrefabs[index] != null)
                {
                    Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    if (point != null)
                    {
                        BossCollectible item = Instantiate(itemPrefabs[index], point.position, Quaternion.identity, transform);
                        item.Initialize((BossItemType)index, manager);
                        Destroy(item.gameObject, itemLifetime);
                    }
                }
            }
            yield return new WaitForSeconds(spawnInterval);
        }
        routine = null;
    }

    private int CountAlive() => GetComponentsInChildren<BossCollectible>().Length;
}
