using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Rhythm System")]
    [SerializeField] private BossSpawner2 enemySpawner;

    [Header("Score & Combo")]
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointL;
    [SerializeField] private Transform hitPointR;
    [SerializeField] private float hitRadius = 0.6f;

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
            enemySpawner = FindFirstObjectByType<BossSpawner2>();

        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        // Cảm ứng: 2 nút LaneButton (Left / Right) trên màn hình
        LaneButtonInput.Pressed += OnLaneButtonPressed;
    }

    private void OnDisable()
    {
        LaneButtonInput.Pressed -= OnLaneButtonPressed;
    }

    private void OnLaneButtonPressed(bool isLeft)
    {
        if (isLeft) AttackLeft();
        else AttackRight();
    }

    private void Update()
    {
        // Bàn phím (test trong Editor)
        if (Input.GetKeyDown(KeyCode.A))
            AttackLeft();

        if (Input.GetKeyDown(KeyCode.D))
            AttackRight();
    }

    // =========================================
    // PLAYER ATTACK
    // =========================================

    private void AttackLeft()
    {
        Flip(-1);

        // đánh trước, biết trúng/trượt rồi mới chọn animation
        PlayAttackAnimation(HandleAttack(true, hitPointL));
    }

    private void AttackRight()
    {
        Flip(1);

        PlayAttackAnimation(HandleAttack(false, hitPointR));
    }

    private bool HandleAttack(bool isLeft, Transform hitPoint)
    {
        if (hitPoint == null)
        {
            Debug.LogError(
                "[PLAYER] Missing HitPoint " +
                (isLeft ? "LEFT" : "RIGHT")
            );
            return false;
        }

        return TryHitEnemyNote(isLeft);
    }

    // =========================================
    // HIT ENEMY BY MIDI
    // =========================================

    private bool TryHitEnemyNote(bool isLeft)
    {
        if (enemySpawner == null)
        {
            Debug.LogError("[PLAYER] EnemySpawner is missing!");
            return false;
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

            return false;
        }

        if (showDebug)
        {
            Debug.Log(
                "[PLAYER] HIT MIDI NOTE: " +
                (isLeft ? "LEFT" : "RIGHT")
            );
        }

        PlayHitFeedback(transform.position);
        return true;
    }

    // =========================================
    // SCORE & COMBO
    // =========================================

    // Gọi từ EnemyRhythmController thông qua event
    public void AddScore(EnemyRhythmController.HitGrade grade)
    {
        // if (scoreSystem == null) return;

        // switch (grade)
        // {
        //     case EnemyRhythmController.HitGrade.Perfect:
        //         scoreSystem.AddScore(ScoreSystem.ScoreGrade.Perfect); break;
        //     case EnemyRhythmController.HitGrade.Great:
        //         scoreSystem.AddScore(ScoreSystem.ScoreGrade.Great); break;
        //     case EnemyRhythmController.HitGrade.Good:
        //         scoreSystem.AddScore(ScoreSystem.ScoreGrade.Good); break;
        // }
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

    // Chọn đòn theo kết quả bắt beat (AttackPicker): trượt/Good = đòn nhẹ, Great = vừa,
    // Perfect = mạnh, mỗi 10 combo = chiêu kết. Controller cũ không có đòn mới thì dùng danh sách attackTriggers.
    private void PlayAttackAnimation(bool hit)
    {
        if (AttackPicker.Play(animator, hit, comboSystem))
            return;

        if (animator == null ||
            attackTriggers == null ||
            attackTriggers.Length == 0)
            return;

        int index = Random.Range(0, attackTriggers.Length);
        AnimatorSafe.SetTrigger(animator, attackTriggers[index]);
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
            if (AudioPrefs.SfxOn) attackAudioSource.PlayOneShot(attackHitSound);
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