using System;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NTEnemyMan2 : MonoBehaviour
{
    public static event Action<NTEnemyMan2> AnyEnemyHit;
    public static event Action<NTEnemyMan2> AnyEnemyMissed;

    [Header("Sprite & Facing")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool facePlayer = true;
    [Tooltip("Tích vào nếu sprite gốc vốn dĩ đã vẽ quay mặt sang TRÁI")]
    [SerializeField] private bool defaultFacingLeft = false;

    [Header("Lane Settings")]
    [Tooltip("True là quái bay (giữ độ cao trên không), False là quái đất (chạy sát sàn gỗ)")]
    public bool isHighLane = false;

    [Header("Timing Tolerance")]
    [SerializeField] private float missTolerance = 0.2f;

    [Header("VFX & Feedback")]
    [SerializeField] private GameObject hitVfxPrefab;

    protected Transform player;
    protected Transform checkpoint;
    protected Vector3 startPos;
    protected Vector3 targetHitPos;
    protected Func<float> getSongTime;

    protected float spawnTime;
    protected float hitTime;
    protected bool isHit = false;
    protected bool isMissed = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public virtual void Init(
        Transform playerTransform,
        Transform targetCheckpoint,
        float spawnTime,
        float hitTime,
        Func<float> songTimeProvider)
    {
        player = playerTransform;
        checkpoint = targetCheckpoint;
        this.spawnTime = spawnTime;
        this.hitTime = hitTime;
        getSongTime = songTimeProvider;

        startPos = transform.position;

        if (checkpoint != null)
        {
            targetHitPos = checkpoint.position;
            if (!isHighLane) targetHitPos.y = startPos.y;
            targetHitPos.z = startPos.z;
        }
        else if (player != null)
        {
            targetHitPos = player.position;
            if (!isHighLane) targetHitPos.y = startPos.y;
        }

        UpdateFacing();
    }

    // ĐỔI SANG protected virtual ĐỂ CLASS ĐẠN CỦA BOSS GỌI ĐƯỢC
    protected virtual void Update()
    {
        if (isHit || isMissed || getSongTime == null) return;

        float songTime = getSongTime();
        float t = Mathf.InverseLerp(spawnTime, hitTime, songTime);

        if (t <= 1.0f)
        {
            transform.position = Vector3.Lerp(startPos, targetHitPos, t);
        }
        else
        {
            Vector3 passTarget = player != null ? player.position : targetHitPos;
            if (!isHighLane) passTarget.y = startPos.y;

            transform.position = Vector3.MoveTowards(transform.position, passTarget, 5f * Time.deltaTime);

            if (songTime >= (hitTime + missTolerance))
            {
                OnMiss();
            }
        }
    }

    private void UpdateFacing()
    {
        if (!facePlayer || player == null) return;

        bool isAtRightSide = transform.position.x > player.position.x;
        if (spriteRenderer != null)
        {
            bool shouldFlip = isAtRightSide ? !defaultFacingLeft : defaultFacingLeft;
            spriteRenderer.flipX = shouldFlip;
        }
    }

    // ĐỔI SANG virtual ĐỂ TÍNH ĐA HÌNH HOẠT ĐỘNG
    public virtual bool TryReceiveHit()
    {
        if (isHit || isMissed) return false;
        OnHit();
        return true;
    }

    public virtual void OnHit()
    {
        if (isHit) return;
        isHit = true;

        AnyEnemyHit?.Invoke(this);

        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols) col.enabled = false;

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        Destroy(gameObject);
    }

    protected virtual void OnMiss()
    {
        if (isMissed) return;
        isMissed = true;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        AnyEnemyMissed?.Invoke(this);

        ComboSystem combo = FindFirstObjectByType<ComboSystem>();
        if (combo != null) combo.MissNote();

        Destroy(gameObject);
    }
}