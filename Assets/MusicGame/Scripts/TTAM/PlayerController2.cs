
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Hit Points")]
    [SerializeField] private Transform hitPointL;
    [SerializeField] private Transform hitPointR;
    [SerializeField] private float hitRadius = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Attack")]
    [SerializeField] private string[] attackTriggers =
    {
        "Attack1",
        "Attack2",
        "Attack3"
    };

    [Header("Score & Combo")]
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private int scorePerEnemy = 100;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Player đứng yên
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        // PC
        if (Input.GetKeyDown(KeyCode.A))
            AttackLeft();

        if (Input.GetKeyDown(KeyCode.D))
            AttackRight();

        // Mobile
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

    // Attack bên trái
    private void AttackLeft()
    {
        Flip(-1);
        PlayAttack();
        CheckHitPoint(hitPointL);
    }

    // Attack bên phải
    private void AttackRight()
    {
        Flip(1);
        PlayAttack();
        CheckHitPoint(hitPointR);
    }

    private void Flip(int direction)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = direction < 0;
    }

    private void PlayAttack()
    {
        if (animator == null || attackTriggers.Length == 0)
            return;

        int randomIndex = Random.Range(0, attackTriggers.Length);

        animator.ResetTrigger(attackTriggers[randomIndex]);
        animator.SetTrigger(attackTriggers[randomIndex]);
    }

    // Kiểm tra Enemy có đang chạm HitPoint không
    private void CheckHitPoint(Transform hitPoint)
    {
        if (hitPoint == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            hitPoint.position,
            hitRadius,
            enemyLayer
        );

        foreach (Collider2D hit in hits)
        {
            EnemyHit enemy = hit.GetComponentInParent<EnemyHit>();

            if (enemy == null)
                continue;

            // Chỉ nhận một lần trên mỗi Enemy
            if (!enemy.CompareTag("Enemy"))
                continue;

            if (!enemy.TryReceiveHit())
                continue;

            // Cộng điểm
            if (scoreSystem != null)
                scoreSystem.AddScore(scorePerEnemy);

            // Tăng Combo
            if (comboSystem != null)
                comboSystem.HitNote();

            Debug.Log("Hit Enemy! +" + scorePerEnemy);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (hitPointL != null)
            Gizmos.DrawWireSphere(hitPointL.position, hitRadius);

        if (hitPointR != null)
            Gizmos.DrawWireSphere(hitPointR.position, hitRadius);
    }
}
