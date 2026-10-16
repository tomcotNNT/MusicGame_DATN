using System;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private Boss bossPrefab;      // kéo PREFAB từ Project
    [SerializeField] private Transform spawnPoint; // ngoài màn hình, nơi boss xuất hiện
    [SerializeField] private Transform stopPoint;  // vị trí boss đứng đánh
    [SerializeField] private int damagePerAttack = 1;

    private Boss boss;

    public void StartBossFight(Action onDefeated)
    {
        if (bossPrefab == null || spawnPoint == null || stopPoint == null)
        {
            Debug.LogWarning("BossSpawner: thiếu reference -> bỏ qua boss.");
            onDefeated?.Invoke();
            return;
        }

        boss = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);
        boss.Begin(stopPoint.position);
        boss.Defeated += () => onDefeated?.Invoke();
    }

    // Nối vào Button "Attack" trên UI, hoặc gọi từ cơ chế tấn công của bạn.
    public void Attack()
    {
        if (boss != null)
            boss.TakeDamage(damagePerAttack);
    }
}