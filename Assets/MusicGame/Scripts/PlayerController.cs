using System;
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

    [Header("Class")]
    private PlayerAttack playerAttack;

    [Header("Touch Effect")]
    [SerializeField] private TouchEffectManager touchEffectManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        playerAttack = GetComponent<PlayerAttack>();
    }

    private void Update()
    {   
        HandleTouch();
        // Cập nhật trạng thái Animator
        animator.SetBool("IsGround", isGround);
    }

    private void HandleTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                // Hiệu ứng vòng tròn
                touchEffectManager.ShowTouch(touch.position);

                // Chia màn hình trái / phải
                if (touch.position.x < Screen.width / 2f)
                {
                    Jump();
                }
                else
                {
                    playerAttack.Attack();
                }
            }
        }

        // Test bằng chuột trên PC
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Input.mousePosition;

            // Hiệu ứng vòng tròn
            touchEffectManager.ShowTouch(mousePosition);

            if (mousePosition.x < Screen.width / 2f)
            {
                Jump();
            }
            else
            {
                playerAttack.Attack();
            }
        }
    }

    // Gọi hàm này khi bấm nút Jump
    public void Jump()
    {
        if (isGround)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

            isGround = false;

            // Bắt đầu Jump
            animator.SetBool("IsJump", true);
        }
    }

    // Gọi khi va chạm với mặt đất
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;

            // Đã chạm đất
            animator.SetBool("IsJump", false);
            animator.SetBool("IsGround", true);
        }
    }
}