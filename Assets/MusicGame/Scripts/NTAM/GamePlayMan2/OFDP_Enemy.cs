using UnityEngine;

public class OFDP_Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public bool isFromLeft = true;
    public bool isHighLane = false;

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

    void Start()
    {
        // Tự động kiểm tra vị trí lúc vừa xuất hiện trên Scene
        ApplyFacing();
    }

    public void Init(bool fromLeft, bool isHigh, float speed)
    {
        isFromLeft = fromLeft;
        isHighLane = isHigh;
        moveSpeed = speed;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        ApplyFacing();
    }

    public void ApplyFacing()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        // Kiểm tra xem quái đang nằm ở bên Trái (X < 0) hay bên Phải (X > 0)
        // Nếu biến isFromLeft đã gán thì dùng isFromLeft, nếu chưa thì so sánh theo tọa độ X
        bool fromLeftSide = (transform.position.x < 0) || isFromLeft;

        // ANIMATION GỐC QUAY SANG PHẢI:
        // - Đi từ TRÁI qua: Nhìn sang phải -> flipX = FALSE (Không lật)
        // - Đi từ PHẢI qua: Cần nhìn sang trái -> flipX = TRUE (Lật sang trái)
        if (sr != null)
        {
            sr.flipX = !fromLeftSide;
        }

        // Cập nhật lại hướng di chuyển để quái luôn lao về tâm (X = 0)
        isFromLeft = fromLeftSide;
    }

    void Update()
    {
        if (isMissedAndAttackingPlayer && playerTransform != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, (moveSpeed * 1.5f) * Time.deltaTime);
            return;
        }

        // Di chuyển về tâm: từ trái thì +X, từ phải thì -X
        float dir = isFromLeft ? 1f : -1f;
        transform.position += Vector3.right * dir * moveSpeed * Time.deltaTime;

        if (Mathf.Abs(transform.position.x) > 16f)
        {
            Destroy(gameObject);
        }
    }

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