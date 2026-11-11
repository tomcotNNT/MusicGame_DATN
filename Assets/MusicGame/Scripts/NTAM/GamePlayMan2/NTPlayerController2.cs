using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class NTPlayerController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Hit Points & Detection")]
    [Tooltip("Kéo GameObject HitPointL vào")]
    [SerializeField] private Transform hitPointL;
    [Tooltip("Kéo GameObject HitPointR vào")]
    [SerializeField] private Transform hitPointR;
    [SerializeField] private float hitRadius = 0.8f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Attack Animations")]
    [Tooltip("Danh sách tên các Trigger hoặc tên State trong Animator (Hit, Hit2, Swing Hand, Kick)")]
    [SerializeField] private string[] attackAnimationNames = { "Hit", "Hit2", "Swing Hand", "Kick" };
    [Tooltip("Tên State chạy bộ mặc định")]
    [SerializeField] private string runStateName = "Run";
    [Tooltip("Nếu bạn dùng 1 Trigger chung tên là 'Attack' trong Animator thì tích vào đây")]
    [SerializeField] private bool useSingleAttackTrigger = false;
    [SerializeField] private string singleTriggerName = "Attack";

    [Header("Hit VFX & Audio")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip missSound;
    [SerializeField] private float vfxDestroyTime = 0.5f;

    [Header("Score & Combo Integration")]
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private int scorePerEnemy = 100;

    private float baseScaleX = 1f;
    private bool facingLeft = false;

    private void Reset()
    {
        AutoFindReferences();
    }

    private void Awake()
    {
        AutoFindReferences();

        baseScaleX = Mathf.Abs(transform.localScale.x);
        if (baseScaleX <= 0.001f) baseScaleX = 1f;

        // Giữ Player đứng vững trên sàn
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Đảm bảo SpriteRenderer không bị tích flipX gây ngược hướng scale
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = false;
        }
    }

    private void AutoFindReferences()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
        if (comboSystem == null) comboSystem = FindFirstObjectByType<ComboSystem>();
    }

    private void Update()
    {
        // 1. Phím PC: A / Mũi tên Trái (Đánh Trái)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            PerformAttack(isLeft: true);
        }
        // 2. Phím PC: D / Mũi tên Phải (Đánh Phải)
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            PerformAttack(isLeft: false);
        }

        // 3. Cảm ứng Mobile
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (touch.position.x < Screen.width / 2f)
                    PerformAttack(isLeft: true);
                else
                    PerformAttack(isLeft: false);
            }
        }
    }

    /// <summary>
    /// Kích hoạt đòn đánh và quét quái theo hướng
    /// </summary>
    public void PerformAttack(bool isLeft)
    {
        // 1. Quay toàn bộ nhân vật và các HitPoint con theo hướng đánh
        Flip(isLeft);

        // 2. Kích hoạt Animation vung đòn ngẫu nhiên
        PlayRandomAttackAnimation();

        // 3. Quét va chạm quái vật
        Transform targetHitPoint = isLeft ? hitPointL : hitPointR;
        Vector2 checkPos = targetHitPoint != null ? (Vector2)targetHitPoint.position : (Vector2)transform.position;

        Collider2D[] hits = Physics2D.OverlapCircleAll(checkPos, hitRadius, enemyLayer);
        bool hasHitEnemy = false;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // Kiểm tra quái mới NTEnemyMan2
            NTEnemyMan2 ntEnemy = hit.GetComponentInParent<NTEnemyMan2>();
            if (ntEnemy != null)
            {
                if (ntEnemy.TryReceiveHit())
                {
                    OnHitSuccess(hit.transform.position);
                    hasHitEnemy = true;
                }
                continue;
            }

            // Dự phòng quái cũ EnemyHit
            EnemyHit oldEnemy = hit.GetComponentInParent<EnemyHit>();
            if (oldEnemy != null)
            {
                if (oldEnemy.TryReceiveHit())
                {
                    OnHitSuccess(hit.transform.position);
                    hasHitEnemy = true;
                }
            }
        }

        // 4. Nếu chém gió (không trúng quái nào)
        if (!hasHitEnemy && missSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(missSound);
        }
    }

    private void Flip(bool isLeft)
    {
        facingLeft = isLeft;

        // Chỉ lật trục scale.x, tuyệt đối không đụng vào spriteRenderer.flipX
        Vector3 scale = transform.localScale;
        scale.x = isLeft ? -baseScaleX : baseScaleX;
        transform.localScale = scale;
    }

    private void PlayRandomAttackAnimation()
    {
        if (animator == null) return;

        // Cách 1: Nếu trong Animator bạn dùng 1 Trigger chung "Attack"
        if (useSingleAttackTrigger && !string.IsNullOrEmpty(singleTriggerName))
        {
            animator.ResetTrigger(singleTriggerName);
            animator.SetTrigger(singleTriggerName);
            return;
        }

        // Cách 2: Phát ngẫu nhiên 1 trong các đòn đánh
        if (attackAnimationNames != null && attackAnimationNames.Length > 0)
        {
            int index = Random.Range(0, attackAnimationNames.Length);
            string chosenAnim = attackAnimationNames[index];

            // Thử kích hoạt bằng Trigger trước
            animator.ResetTrigger(chosenAnim);
            animator.SetTrigger(chosenAnim);

            // Đồng thời Play trực tiếp State đề phòng trường hợp bạn chưa tạo kịp Trigger cùng tên
            animator.Play(chosenAnim, 0, 0f);
        }
    }

    private void OnHitSuccess(Vector3 enemyPosition)
    {
        // 1. Hiệu ứng chém / nổ quái
        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(hitVfxPrefab, enemyPosition, Quaternion.identity);
            Destroy(vfx, vfxDestroyTime);
        }

        // 2. Âm thanh đập trúng quái
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }

        // 3. Khựng hình & Rung Camera
        if (NTHitStopAndShake.Instance != null)
        {
            NTHitStopAndShake.Instance.TriggerHitImpact(0.08f, 0.12f, 0.03f);
        }

        // 4. Cộng điểm & Tăng Combo
        if (scoreSystem != null) scoreSystem.AddScore(scorePerEnemy);
        if (comboSystem != null) comboSystem.HitNote();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (hitPointL != null) Gizmos.DrawWireSphere(hitPointL.position, hitRadius);
        if (hitPointR != null) Gizmos.DrawWireSphere(hitPointR.position, hitRadius);
    }
}