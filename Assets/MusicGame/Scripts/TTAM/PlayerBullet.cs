using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField, Min(1)] private int damage = 10;

    [Header("Va chạm")]
    [Tooltip("Bật = đạn biến mất khi trúng boss. Tắt = bay xuyên (chỉ trừ máu 1 lần)")]
    [SerializeField] private bool destroyOnHit = true;
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private float vfxLifetime = 1f;

    [Header("Vòng đời")]
    [Tooltip("Tự huỷ sau bao nhiêu giây (lưới an toàn nếu đạn không chạm gì)")]
    [SerializeField, Min(0.5f)] private float maxLifetime = 5f;

    private bool hasHit;

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, maxLifetime);
    }

    // Cho PlayerFeverShooter đổi damage khi cần (tuỳ chọn)
    public void SetDamage(int value)
    {
        damage = Mathf.Max(1, value);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit)
            return;

        BossHealth boss = other.GetComponentInParent<BossHealth>();

        if (boss == null || boss.IsDead)
            return;

        hasHit = true;   // mỗi đạn chỉ trừ máu một lần

        boss.TakeDamage(damage);

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                hitVfxPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(vfx, vfxLifetime);
        }

        if (destroyOnHit)
            Destroy(gameObject);
    }
}