using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NTBossProjectileMan2 : NTEnemyMan2
{
    [Header("Reflect Properties")]
    [SerializeField] private float returnSpeed = 24f; // Tốc độ đạn dội ngược lại Boss
    private NTBossFightDirector bossDirector;
    private bool isParried = false;

    public void InitBossProjectile(
        NTBossFightDirector director,
        Transform targetCheckpoint,
        float spawnTime,
        float hitTime,
        Func<float> songTimeProvider)
    {
        bossDirector = director;
        isHighLane = true;

        base.Init(
            playerTransform: (targetCheckpoint != null) ? targetCheckpoint : director.transform,
            targetCheckpoint: targetCheckpoint,
            spawnTime: spawnTime,
            hitTime: hitTime,
            songTimeProvider: songTimeProvider
        );
    }

    protected override void Update()
    {
        // GIAI ĐOẠN 2: Người chơi đã đánh trúng -> Đạn dội ngược lên Boss
        if (isParried)
        {
            if (bossDirector == null)
            {
                Destroy(gameObject);
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                bossDirector.transform.position,
                returnSpeed * Time.deltaTime
            );

            // Chạm trúng ngực Boss
            if (Vector3.Distance(transform.position, bossDirector.transform.position) < 0.6f)
            {
                bossDirector.OnReflectedHitLanded();
                Destroy(gameObject);
            }
            return;
        }

        // GIAI ĐOẠN 1: Bay tới Checkpoint của Player theo nhịp
        base.Update();
    }

    // OVERRIDE CHUẨN XÁC: Player chém trúng sẽ phản đạn thay vì làm vỡ nốt
    public override bool TryReceiveHit()
    {
        if (isParried || isHit || isMissed) return false;

        isParried = true;

        // Vô hiệu hóa Collider để Player không chém trúng lần 2
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // Xoay đầu mũi đạn hướng ngược lên Boss
        if (bossDirector != null)
        {
            Vector3 diff = bossDirector.transform.position - transform.position;
            float rotZ = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, rotZ);
        }

        return true;
    }
}