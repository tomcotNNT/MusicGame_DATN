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
    [Tooltip("Thời gian dung sai cho phép bấm trễ sau HitTime trước khi tính là Miss")]
    [SerializeField] private float missTolerance = 0.2f;

    [Header("VFX & Feedback")]
    [SerializeField] private GameObject hitVfxPrefab;

    private Transform player;
    private Transform checkpoint;
    private Vector3 startPos;
    private Vector3 targetHitPos;
    private Func<float> getSongTime;

    private float spawnTime;
    private float hitTime;
    private bool isHit = false;
    private bool isMissed = false;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Tự động bật Trigger để quái không bị cản trở vật lý
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    public void Init(
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

        // CĂN TỌA ĐỘ ĐÍCH CHUẨN XÁC:
        if (checkpoint != null)
        {
            targetHitPos = checkpoint.position;

            // Nếu là quái đất (!isHighLane): Khóa cứng trục Y sàn nhà lúc xuất phát
            if (!isHighLane)
            {
                targetHitPos.y = startPos.y;
            }

            targetHitPos.z = startPos.z;
        }
        else if (player != null)
        {
            targetHitPos = player.position;
            if (!isHighLane) targetHitPos.y = startPos.y;
        }

        UpdateFacing();
    }

    private void Update()
    {
        if (isHit || isMissed || getSongTime == null) return;

        float songTime = getSongTime();
        float t = Mathf.InverseLerp(spawnTime, hitTime, songTime);

        if (t <= 1.0f)
        {
            // Quái di chuyển mượt mà tới Checkpoint
            transform.position = Vector3.Lerp(startPos, targetHitPos, t);
        }
        else
        {
            // Sau khi qua Checkpoint, quái tiếp tục trôi qua tâm về phía Player
            Vector3 passTarget = player != null ? player.position : targetHitPos;
            if (!isHighLane) passTarget.y = startPos.y;

            transform.position = Vector3.MoveTowards(transform.position, passTarget, 5f * Time.deltaTime);

            // Vượt quá thời gian dung sai mà người chơi không bấm -> Tính MISS
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

    /// <summary>
    /// Nhận tín hiệu chém trúng từ NTPlayerController2
    /// </summary>
    public bool TryReceiveHit()
    {
        if (isHit || isMissed) return false;
        OnHit();
        return true;
    }

    public void OnHit()
    {
        if (isHit) return;
        isHit = true;

        AnyEnemyHit?.Invoke(this);

        // Vô hiệu hóa va chạm ngay lập tức
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols) col.enabled = false;

        // Sinh hiệu ứng nổ / vỡ hạt VFX
        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, 0.5f);
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// Xử lý khi không ăn kịp nốt: Chỉ mất combo và hiện popup MISS (Không mất máu, không giật Hurt)
    /// </summary>
private void OnMiss()
    {
        if (isMissed) return;
        isMissed = true;

        // 1. Tắt va chạm lập tức
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 2. Bắn sự kiện Miss toàn cục (để UI tự động lắng nghe)
        AnyEnemyMissed?.Invoke(this);

        // 3. Reset chuỗi Combo về 0
        ComboSystem combo = FindFirstObjectByType<ComboSystem>();
        if (combo != null)
        {
            combo.MissNote();
        }

        // 4. Log kiểm tra trên Console (Tránh lỗi CS0246)
        Debug.Log("<color=red>[MISS]</color> Hụt nốt!");

        // 5. Tự hủy nốt quái
        Destroy(gameObject);
    }
}