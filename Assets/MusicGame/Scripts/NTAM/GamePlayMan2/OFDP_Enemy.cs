using UnityEngine;

public class OFDP_Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public bool isFromLeft = true;
    public bool isHighLane = false;

    [Header("Sprite Direction")]
    [Tooltip("Tích chọn nếu hình vẽ gốc trong ảnh sprite của con này quay mặt sang TRÁI (như Bat). Bỏ tích nếu gốc quay sang PHẢI.")]
    public bool defaultFacingLeft = true;

    [Header("State")]
    [HideInInspector] public bool isInAttackRange = false;
    private bool isMissedAndAttackingPlayer = false;

    private SpriteRenderer sr;
    private Transform playerTransform;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) playerTransform = p.transform;
    }

    public void Init(bool fromLeft, bool isHigh, float speed)
    {
        isFromLeft = fromLeft;
        isHighLane = isHigh;
        moveSpeed = speed;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        UpdateFacing();
    }

    void UpdateFacing()
    {
        if (sr == null) return;
        // Đảm bảo mặt luôn hướng về tâm (X = 0)
        if (isFromLeft)
        {
            // Đi từ Trái sang Phải: Cần nhìn sang Phải
            sr.flipX = defaultFacingLeft;
        }
        else
        {
            // Đi từ Phải sang Trái: Cần nhìn sang Trái
            sr.flipX = !defaultFacingLeft;
        }
    }

    void Update()
    {
        if (isMissedAndAttackingPlayer && playerTransform != null)
        {
            // Lao thẳng vào người Player khi người chơi bấm trượt / bỏ lỡ
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, (moveSpeed * 1.5f) * Time.deltaTime);
            return;
        }

        // Di chuyển bình thường về tâm
        float dir = isFromLeft ? 1f : -1f;
        transform.position += Vector3.right * dir * moveSpeed * Time.deltaTime;

        // Tự hủy nếu bay quá xa
        if (Mathf.Abs(transform.position.x) > 16f)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Kích hoạt khi người chơi bỏ lỡ nhịp (Miss): Quái lao thẳng vào cắn Player
    /// </summary>
    public void TriggerMissRush()
    {
        if (isMissedAndAttackingPlayer) return;
        isMissedAndAttackingPlayer = true;
    }

    public void OnHit()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Chạm vào thân Player
        if (collision.CompareTag("Player") || collision.name == "Hitbox")
        {
            PlayerDefenseController player = collision.GetComponentInParent<PlayerDefenseController>();
            if (player != null)
            {
                player.TakeDamage();
            }
            Destroy(gameObject);
        }
    }
}