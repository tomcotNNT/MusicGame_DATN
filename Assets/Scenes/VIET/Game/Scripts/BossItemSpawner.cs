using System.Collections;
using UnityEngine;

public class BossItemSpawner : MonoBehaviour
{
    [Header("Prefab order: Blue, Purple, Gold")]
    [SerializeField] private BossCollectible[] itemPrefabs = new BossCollectible[3];
    [Header("Player and two lanes")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform hitPointBottom;
    [SerializeField] private Transform hitPointTop;
    [SerializeField] private float bottomYOffset = 0f;
    [SerializeField] private float topYOffset = 0f;
    [SerializeField, Min(0.5f)] private float spawnDistanceAhead = 8f;
    [SerializeField, Min(0.1f)] private float itemMoveSpeed = 4f;
    [SerializeField, Min(0.1f)] private float spawnInterval = 1.5f;
    [SerializeField, Min(1)] private int maxAlive = 12;
    [SerializeField, Min(0.1f)] private float itemLifetime = 12f;
    [SerializeField, Min(0.2f)] private float firstWaveSpacing = 2.5f;

    private BossItemBattleManager manager;
    private Coroutine routine;
    private int sequence;

    public void StartSpawning(BossItemBattleManager owner)
    {
        StopSpawning();
        if (player == null || hitPointBottom == null || hitPointTop == null)
        {
            Debug.LogError("BossItemSpawner: gắn Player, HitPointBottom và HitPointTop!", this);
            return;
        }
        if (itemPrefabs == null || itemPrefabs.Length < 3 ||
            itemPrefabs[0] == null || itemPrefabs[1] == null || itemPrefabs[2] == null)
        {
            Debug.LogError("BossItemSpawner: cần đủ Blue, Purple, Gold prefabs!", this);
            return;
        }
        manager = owner;
        sequence = 0;
        // Tạo đủ ba loại ngay khi Boss.Begin() được gọi.
        for (int i = 0; i < 3; i++)
            SpawnOne(i, spawnDistanceAhead + i * firstWaveSpacing, i % 2 == 0);
        sequence = 3;
        routine = StartCoroutine(SpawnLoop());
    }

    public void StopSpawning()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    private IEnumerator SpawnLoop()
    {
        // Đợi một nhịp để không đè lên ba item đầu.
        yield return new WaitForSeconds(spawnInterval);
        while (manager != null && manager.IsActive)
        {
            if (CountAlive() < maxAlive)
            {
                int index = sequence % 3;
                sequence++;
                SpawnOne(index, spawnDistanceAhead, Random.value >= 0.5f);
            }
            yield return new WaitForSeconds(spawnInterval);
        }
        routine = null;
    }

    private void SpawnOne(int index, float distance, bool upper)
    {
        if (CountAlive() >= maxAlive) return;
        float y = upper ? hitPointTop.position.y + topYOffset : hitPointBottom.position.y + bottomYOffset;
        Vector3 pos = new Vector3(player.position.x + distance, y, player.position.z);
        BossCollectible item = Instantiate(itemPrefabs[index], pos, Quaternion.identity, transform);
        item.Initialize((BossItemType)index, manager);

        // Prefab cũ có BossItemMovement sẽ làm item di chuyển gấp đôi.
        BossItemMovement oldMove = item.GetComponent<BossItemMovement>();
        if (oldMove != null) oldMove.enabled = false;
        BossItemLaneMovement movement = item.GetComponent<BossItemLaneMovement>();
        if (movement == null) movement = item.gameObject.AddComponent<BossItemLaneMovement>();
        movement.Initialize(player, itemMoveSpeed);
        Destroy(item.gameObject, itemLifetime);
    }

    private int CountAlive() => GetComponentsInChildren<BossCollectible>().Length;
}
