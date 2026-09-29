using UnityEngine;
using UnityEngine.InputSystem; // 1. Thêm namespace này

public class PlayerRunner : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float jumpForce = 12f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float checkRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, checkRadius, groundLayer);
        }

        // 2. Đọc phím Space hoặc Click chuột trái / Chạm màn hình bằng New Input System
        bool jumpPressed = false;

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpPressed = true;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            jumpPressed = true;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            jumpPressed = true;

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(runSpeed, rb.linearVelocity.y);
    }
}