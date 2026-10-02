using UnityEngine;

public class PlayerLaneController : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private bool isGround = true;
    [SerializeField] private float jumpForce = 2f;

    [Header("Component")]
    private Animator animator;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Cập nhật Animator
        animator.SetBool("IsGround", isGround);

        // Player tự động chạy
        Move();
    }

    public void Jump()
    {
        if (isGround)
        {
            rb.AddForce(
                Vector2.up * jumpForce,
                ForceMode2D.Impulse
            );

            isGround = false;

            animator.SetBool("IsJump", true);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;

            animator.SetBool("IsJump", false);
            animator.SetBool("IsGround", true);
        }
    }

    public void Move()
    {
        rb.linearVelocity = new Vector2(
            moveSpeed,
            rb.linearVelocity.y
        );
    }
}