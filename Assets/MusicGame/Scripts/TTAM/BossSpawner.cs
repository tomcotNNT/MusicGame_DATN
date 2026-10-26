using System;
using System.Collections;
using UnityEngine;

public class BossSpawner : MonoBehaviour
{
    [SerializeField] private Boss bossPrefab;      // kéo PREFAB từ Project
    [SerializeField] private Transform spawnPoint; // ngoài màn hình, nơi boss xuất hiện
    [SerializeField] private Transform stopPoint;  // vị trí boss đứng đánh
    [SerializeField] private int damagePerAttack = 1;
    private bool hasSpawnedBoss;

    [Header("Retreat")]
    [Tooltip("Tốc độ boss đi lùi về spawn point khi hết nhạc boss (unit/giây)")]
    [SerializeField] private float retreatSpeed = 4f;

    [Header("Fever")]
    [SerializeField] private FeverSystem feverSystem;
    [Tooltip("Thanh Fever đầy thì tự gây sát thương lên boss")]
    [SerializeField] private bool attackWhenFeverFull = true;

    [Tooltip("Thanh Fever đầy thì ngoài Attack() còn trừ máu boss (tạm tắt để test log)")]
    [SerializeField] private bool feverAlsoDamagesBoss = false;

    private Boss boss;
    private Coroutine retreatRoutine;

    private bool IsRetreating => retreatRoutine != null;

    public bool IsFighting => boss != null && !IsRetreating;
    public BossMusicAttack CurrentMusicAttack =>
        boss != null ? boss.GetComponent<BossMusicAttack>() : null;

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
        if (boss == null || IsRetreating)
            return;

        boss.Attack();   // hiện tại chỉ Debug.Log

        if (feverAlsoDamagesBoss)
            boss.TakeDamage(damagePerAttack);
    }

    // Gọi khi hết nhạc đầu (PianoKeySpawner.NextRound): boss xuất hiện và đi vào vị trí đánh.
    public void StartBossFight(Action onDefeated)
    {
        // Không spawn lại trong cùng lượt chơi.
        if (hasSpawnedBoss)
            return;

        if (bossPrefab == null || spawnPoint == null || stopPoint == null)
        {
            Debug.LogWarning("BossSpawner: Thiếu reference -> bỏ qua Boss.");
            onDefeated?.Invoke();
            return;
        }

        hasSpawnedBoss = true;

        Boss spawned = Instantiate(
            bossPrefab,
            spawnPoint.position,
            Quaternion.identity
        );

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
        if (boss != null && !IsRetreating)
            boss.TakeDamage(damagePerAttack);
    }

    // =====================================================
    // RETREAT: hết nhạc boss => boss đi LÙI về spawn point rồi biến mất
    // =====================================================

    public void RetreatBoss()
    {
        if (feverSystem != null)
            feverSystem.Show(false);

        if (boss == null)
        {
            hasSpawnedBoss = false;
            return;
        }

        if (IsRetreating)
            return;

        if (spawnPoint == null)
        {
            ClearBoss();
            return;
        }

        retreatRoutine = StartCoroutine(RetreatRoutine(boss));
    }

    private IEnumerator RetreatRoutine(Boss target)
    {
        // Tắt script Boss để nó không giành quyền di chuyển (animation vẫn chạy)
        target.enabled = false;

        Vector3 destination = spawnPoint.position;

        // Đi lùi: chỉ đổi vị trí, không lật hướng nhìn
        while (target != null &&
               Vector3.Distance(target.transform.position, destination) > 0.05f)
        {
            target.transform.position = Vector3.MoveTowards(
                target.transform.position,
                destination,
                retreatSpeed * Time.deltaTime
            );

            yield return null;
        }

        if (target != null)
            Destroy(target.gameObject);

        if (boss == target)
            boss = null;

        hasSpawnedBoss = false;
        retreatRoutine = null;
    }

    // Gọi khi thoát level để xoá boss còn sót ngay lập tức (không lùi)
    public void ClearBoss()
    {
        if (retreatRoutine != null)
        {
            StopCoroutine(retreatRoutine);
            retreatRoutine = null;
        }

        if (boss != null)
            Destroy(boss.gameObject);

        boss = null;
        hasSpawnedBoss = false;

        if (feverSystem != null)
            feverSystem.Show(false);
    }
}