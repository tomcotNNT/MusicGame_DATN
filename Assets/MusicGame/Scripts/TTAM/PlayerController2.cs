
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Attack")]
    [SerializeField] private string[] attackTriggers =
    {
        "Attack1",
        "Attack2",
        "Attack3"
    };

    [SerializeField] private bool faceEnemyWhenAttack = true;

    [Header("Score & Combo")]
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;
    [SerializeField] private int scorePerEnemy = 100;

    private bool isAttacking;
    private int facingDirection = 1; // 1 = phải, -1 = trái

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Khóa di chuyển vật lý
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void Update()
    {
        // PC: A = trái, D = phải
        if (Input.GetKeyDown(KeyCode.A))
            AttackDirection(-1);

        if (Input.GetKeyDown(KeyCode.D))
            AttackDirection(1);

        // Mobile: chạm nửa trái/phải màn hình
        if (Input.touchCount > 0)
        {
            foreach (Touch touch in Input.touches)
            {
                if (touch.phase != TouchPhase.Began)
                    continue;

                int direction = touch.position.x < Screen.width / 2f
                    ? -1 : 1;

                AttackDirection(direction);
            }
        }
    }

    private void AttackDirection(int direction)
    {
        facingDirection = direction;

        // Lật Sprite
        Flip(direction);

        // Chạy animation Attack ngẫu nhiên
        PlayAttack();
    }

    private void Flip(int direction)
    {
        if (spriteRenderer == null)
            return;

        // Sprite mặc định quay phải
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryAttack(other.transform);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryAttack(collision.transform);
    }

    private void TryAttack(Transform target)
    {
        if (target == null)
            return;

        EnemyHit enemy = target.GetComponentInParent<EnemyHit>();

        if (enemy == null || !enemy.CompareTag("Enemy"))
            return;

        // Kiểm tra hướng đánh
        if (faceEnemyWhenAttack)
        {
            float direction = enemy.transform.position.x - transform.position.x;

            if (Mathf.Sign(direction) != facingDirection)
                return;
        }

        // Enemy chỉ được tính điểm một lần
        if (!enemy.TryReceiveHit())
            return;

        if (scoreSystem != null)
            scoreSystem.AddScore(scorePerEnemy);

        if (comboSystem != null)
            comboSystem.HitNote();

        Debug.Log("Enemy Hit! Score +" + scorePerEnemy);
    }

    // Gọi bằng Animation Event nếu cần
    public void StartAttack()
    {
        isAttacking = true;
    }

    public void EndAttack()
    {
        isAttacking = false;
    }
}
