using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NTBossNoteMan2 : NTEnemyMan2
{
    [Header("Note Type Visual")]
    public bool isLeftNote = true;
    [SerializeField] private SpriteRenderer noteIconRenderer;
    [SerializeField] private Sprite spriteNoteL;
    [SerializeField] private Sprite spriteNoteR;

    [Header("Fireball Spawner On Hit")]
    [Tooltip("Kéo Prefab Quả cầu lửa vào đây")]
    [SerializeField] private GameObject fireballPrefab;

    [Header("Rhythm Timing Settings")]
    [Tooltip("Dung sai thời gian cho phép bấm trúng nốt")]
    [SerializeField] private float customMissTolerance = 0.2f;

    [Tooltip("Thời gian nốt tiếp tục bay qua Checkpoint trước khi biến mất hoàn toàn")]
    [SerializeField] private float fadeOutDuration = 0.2f;

    private NTBossFightDirector bossDirector;
    private Vector3 moveDirection;
    private bool hasTriggeredMiss = false;

    public void InitBossNote(
        NTBossFightDirector director,
        bool isLeft,
        Transform targetCheckpoint,
        float spawnTime,
        float hitTime,
        Func<float> songTimeProvider)
    {
        bossDirector = director;
        isLeftNote = isLeft;
        isHighLane = true;

        if (noteIconRenderer == null)
            noteIconRenderer = GetComponentInChildren<SpriteRenderer>();

        if (noteIconRenderer != null)
        {
            noteIconRenderer.sprite = isLeft ? spriteNoteL : spriteNoteR;
        }

        base.Init(
            playerTransform: targetCheckpoint,
            targetCheckpoint: targetCheckpoint,
            spawnTime: spawnTime,
            hitTime: hitTime,
            songTimeProvider: songTimeProvider
        );

        if (targetCheckpoint != null)
        {
            moveDirection = (targetCheckpoint.position - transform.position).normalized;
        }
    }

    protected override void Update()
    {
        if (isHit || isMissed || getSongTime == null) return;

        float songTime = getSongTime();
        float t = Mathf.InverseLerp(spawnTime, hitTime, songTime);

        if (t <= 1.0f)
        {
            // Giai đoạn 1: Bay chính xác từ tay Boss tới Checkpoint theo nhịp
            transform.position = Vector3.Lerp(startPos, targetHitPos, t);
        }
        else
        {
            // Giai đoạn 2: Bay xuyên qua Checkpoint tiếp tục trôi đi
            transform.position += moveDirection * (6f * Time.deltaTime);

            // Bỏ lỡ nốt: Gọi trực tiếp base.OnMiss() để trừ combo, phát sự kiện và tự hủy
            if (!hasTriggeredMiss && songTime >= (hitTime + customMissTolerance))
            {
                hasTriggeredMiss = true;
                base.OnMiss();
            }

            // Tự hủy dọn dẹp bộ nhớ nếu trôi quá xa
            if (songTime >= (hitTime + customMissTolerance + fadeOutDuration))
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// Player chém trúng: Phóng cầu lửa và gọi base.OnHit() để hủy nốt ngay lập tức
    /// </summary>
    public override bool TryReceiveHit()
    {
        if (isHit || isMissed) return false;

        // 1. Phóng quả cầu lửa bay lên Cái Bóng trên cây
        SpawnFireballToBoss();

        // 2. Tắt Collider ngay để tránh Player chém trúng lần 2
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 3. Gọi base.OnHit() để class cha tự phát sự kiện AnyEnemyHit, sinh VFX và Destroy(gameObject)
        // Không gán isHit = true trước dòng này để tránh bị class cha chặn return sớm
        base.OnHit();
        return true;
    }

    private void SpawnFireballToBoss()
    {
        if (fireballPrefab != null && bossDirector != null)
        {
            GameObject fb = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
            NTFireballToBoss fireballScript = fb.GetComponent<NTFireballToBoss>();
            if (fireballScript != null)
            {
                fireballScript.InitFireball(bossDirector);
            }
        }
    }
}