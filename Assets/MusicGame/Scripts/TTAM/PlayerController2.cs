
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Rhythm System")]
    [SerializeField] private EnemySpawnerMan2 enemySpawner;

    [Header("Score & Combo")]
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointL;
    [SerializeField] private Transform hitPointR;
    [SerializeField] private float hitRadius = 0.6f;

    [Header("Boss Bullet")]
    [SerializeField] private LayerMask bossBulletLayer;

    [Header("Hit VFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private float vfxDestroyTime = 1.5f;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Attack Sound")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip attackHitSound;

    [Header("Attack Animation")]
    [SerializeField] private string[] attackTriggers =
    {
        "Attack1",
        "Attack2",
        "Attack3"
    };

    [Header("Score Settings")]
    [SerializeField] private int perfectScore = 100;
    [SerializeField] private int goodScore = 50;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawnerMan2>();

        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
            AttackLeft();

        if (Input.GetKeyDown(KeyCode.D))
            AttackRight();

        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began)
                    continue;

                if (touch.position.x < Screen.width / 2f)
                    AttackLeft();
                else
                    AttackRight();
            }
        }
    }

    // =========================================
    // PLAYER ATTACK
    // =========================================

    private void AttackLeft()
    {
        Flip(-1);
        PlayAttackAnimation();

        HandleAttack(true, hitPointL);
    }

    private void AttackRight()
    {
        Flip(1);
        PlayAttackAnimation();

        HandleAttack(false, hitPointR);
    }

    private void HandleAttack(bool isLeft, Transform hitPoint)
    {
        if (hitPoint == null)
        {
            Debug.LogError(
                "[PLAYER] Missing HitPoint " +
                (isLeft ? "LEFT" : "RIGHT")
            );
            return;
        }

        // Ưu tiên phản đạn Boss
        if (TryReflectBossBullet(hitPoint))
            return;

        // Bắt nốt Enemy
        TryHitEnemyNote(isLeft);
    }

    // =========================================
    // HIT ENEMY BY MIDI
    // =========================================

    private void TryHitEnemyNote(bool isLeft)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("[PLAYER] EnemySpawner is missing!");
            return;
        }

        bool success = enemySpawner.TryHit(isLeft);

        if (!success)
        {
            if (showDebug)
            {
                Debug.Log(
                    "[PLAYER] MISS TIMING: " +
                    (isLeft ? "LEFT" : "RIGHT")
                );
            }

            return;
        }

        if (showDebug)
        {
            Debug.Log(
                "[PLAYER] HIT MIDI NOTE: " +
                (isLeft ? "LEFT" : "RIGHT")
            );
        }

        PlayHitFeedback(transform.position);
    }

    // =========================================
    // REFLECT BOSS BULLET
    // =========================================

    private bool TryReflectBossBullet(Transform hitPoint)
    {
        Collider2D[] bullets = Physics2D.OverlapCircleAll(
            hitPoint.position,
            hitRadius,
            bossBulletLayer
        );

        foreach (Collider2D col in bullets)
        {
            if (col == null || !col.CompareTag("BossBullet"))
                continue;

            BossBullet bullet =
                col.GetComponentInParent<BossBullet>();

            if (bullet == null)
                continue;

            if (!bullet.TryReflect())
                continue;

            if (showDebug)
                Debug.Log("[PLAYER] REFLECTED BOSS BULLET!");

            PlayHitFeedback(bullet.transform.position);

            return true;
        }

        return false;
    }

    // =========================================
    // SCORE & COMBO
    // =========================================

    // Gọi từ EnemyRhythmController thông qua event
    public void AddScore(int score)
    {
        if (scoreSystem != null)
            scoreSystem.AddScore(score);
    }

    // Gọi từ EnemyRhythmController thông qua event
    public void AddCombo()
    {
        if (comboSystem != null)
            comboSystem.HitNote();
    }

    // =========================================
    // ANIMATION
    // =========================================

    private void Flip(int direction)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;
    }

    private void PlayAttackAnimation()
    {
        if (animator == null ||
            attackTriggers == null ||
            attackTriggers.Length == 0)
            return;

        int index = Random.Range(0, attackTriggers.Length);
        animator.SetTrigger(attackTriggers[index]);
    }

    // =========================================
    // HIT FEEDBACK
    // =========================================

    private void PlayHitFeedback(Vector3 position)
    {
        PlayAttackSound();
        SpawnHitVFX(position);
    }

    private void PlayAttackSound()
    {
        if (attackAudioSource != null && attackHitSound != null)
            attackAudioSource.PlayOneShot(attackHitSound);
    }

    private void SpawnHitVFX(Vector3 position)
    {
        if (hitVFXPrefab == null)
            return;

        GameObject vfx = Instantiate(
            hitVFXPrefab,
            position + vfxOffset,
            Quaternion.identity
        );

        Destroy(vfx, vfxDestroyTime);
    }

    // =========================================
    // DEBUG HIT POINT
    // =========================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (hitPointL != null)
            Gizmos.DrawWireSphere(hitPointL.position, hitRadius);

        if (hitPointR != null)
            Gizmos.DrawWireSphere(hitPointR.position, hitRadius);
    }
}
