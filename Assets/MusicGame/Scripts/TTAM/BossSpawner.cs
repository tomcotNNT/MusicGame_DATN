using System;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private Boss bossPrefab;      // kéo PREFAB từ Project
    [SerializeField] private Transform spawnPoint; // ngoài màn hình, nơi boss xuất hiện
    [SerializeField] private Transform stopPoint;  // vị trí boss đứng đánh
    [SerializeField] private int damagePerAttack = 1;

    [Header("Fever")]
    [SerializeField] private FeverSystem feverSystem;
    [Tooltip("Thanh Fever đầy thì tự gây sát thương lên boss")]
    [SerializeField] private bool attackWhenFeverFull = true;

    [Tooltip("Thanh Fever đầy thì ngoài Attack() còn trừ máu boss (tạm tắt để test log)")]
    [SerializeField] private bool feverAlsoDamagesBoss = false;

    private Boss boss;

    public bool IsFighting => boss != null;

    private void OnEnable()
    {
        if (feverSystem != null)
            feverSystem.Filled += OnFeverFilled;
    }

    private void OnDisable()
    {
        if (feverSystem != null)
            feverSystem.Filled -= OnFeverFilled;
    }

    private void OnFeverFilled()
    {
        if (boss == null)
            return;

        boss.Attack();   // hiện tại chỉ Debug.Log

        if (feverAlsoDamagesBoss)
            boss.TakeDamage(damagePerAttack);
    }

    public void StartBossFight(Action onDefeated)
    {
        if (bossPrefab == null || spawnPoint == null || stopPoint == null)
        {
            Debug.LogWarning("BossSpawner: thiếu reference -> bỏ qua boss.");
            onDefeated?.Invoke();
            return;
        }

        // Boss cũ vẫn còn => không spawn thêm
        if (boss != null)
            return;

        Boss spawned = Instantiate(bossPrefab, spawnPoint.position, Quaternion.identity);
        boss = spawned;

        if (feverSystem != null)
            feverSystem.Show(true);

        spawned.Begin(stopPoint.position);
        spawned.Defeated += () =>
        {
            if (boss == spawned)
            {
                boss = null;
                if (feverSystem != null)
                    feverSystem.Show(false);
            }

            onDefeated?.Invoke();
        };
    }

    // Nối vào Button "Attack" trên UI, hoặc gọi từ cơ chế tấn công của bạn.
    public void Attack()
    {
        if (boss != null)
            boss.TakeDamage(damagePerAttack);
    }

    // Gọi khi hết game / thoát level để xoá boss còn sót
    public void ClearBoss()
    {
        if (boss != null)
            Destroy(boss.gameObject);

        boss = null;

        if (feverSystem != null)
            feverSystem.Show(false);
    }
}