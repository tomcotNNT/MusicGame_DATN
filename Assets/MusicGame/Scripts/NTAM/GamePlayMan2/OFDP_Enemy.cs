using UnityEngine;

public class OFDP_Enemy : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public bool isFromLeft = true; // true: từ trái sang, false: từ phải sang

    [Header("Lane & State")]
    public bool isHighLane = false; // false: Làn dưới, true: Làn trên
    [HideInInspector] public bool isInAttackRange = false;

    private SpriteRenderer sr;
    private Animator anim;
    private Collider2D col;
    private bool isDead = false;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    public void Init(bool fromLeft, bool isHigh, float speed)
    {
        isFromLeft = fromLeft;
        isHighLane = isHigh;
        moveSpeed = speed;

        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // Sprite gốc nhìn sang trái: nếu từ trái qua thì flipX = true, từ phải qua thì flipX = false
            sr.flipX = fromLeft; 
        }
    }

    void Update()
    {
        // Khi đã bị chém trúng thì đứng yên tại chỗ để rơi/chết, không trượt tới trước nữa
        if (isDead) return;

        float direction = isFromLeft ? 1f : -1f;
        transform.position += Vector3.right * direction * moveSpeed * Time.deltaTime;

        // Tự hủy nếu bay quá xa màn hình (ngoài phạm vi X = +-16)
        if (Mathf.Abs(transform.position.x) > 16f)
        {
            Destroy(gameObject);
        }
    }

    // Được gọi khi người chơi bấm nút chém trúng quái
    public void OnHit()
    {
        if (isDead) return;
        isDead = true;

        // 1. Tắt Collider để không còn va chạm hay gây sát thương cho player
        if (col != null) col.enabled = false;

        // 2. Kích hoạt Animation chết
        if (anim != null)
        {
            anim.SetTrigger("Die");
        }
        else
        {
            // Đề phòng quái chưa gán Animator thì hủy luôn
            Destroy(gameObject);
        }
    }

    // Hàm này sẽ được Animation Event gọi ở cuối frame 0:08
    public void OnDeathComplete()
    {
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDead) return;

        // Chạm vào Player hoặc Hitbox khi người chơi không bấm kịp
        if (collision.CompareTag("Player") || collision.name == "Hitbox" || collision.GetComponent<PlayerDefenseController>() != null)
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