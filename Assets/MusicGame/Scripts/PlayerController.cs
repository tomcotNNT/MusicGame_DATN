using UnityEngine;

public class PlayerLaneController : MonoBehaviour
{


    [Header("Move")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private bool isGround = true;
    [SerializeField] private float jumpForce = 2f;

    [Header("Component")]
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
      
    }

    // Gọi hàm này khi bấm nút Jump
    public void Jump()
    {
        if (isGround)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGround = false;
        }
    }

    // Gọi hàm này khi va chạm với mặt đất
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGround = true;
        }
    }
}
