using UnityEngine;

// Gắn vào 2 hit point: dưới = Left, trên = Right.
// Cần Collider2D (Is Trigger) + Rigidbody2D (Kinematic).
[RequireComponent(typeof(Collider2D))]
public class HitPoint : MonoBehaviour
{
    [Tooltip("Lane của hit point này. Dưới = Left, Trên = Right")]
    [SerializeField] private Lanee lane = Lanee.Left;

    public Lanee Lane => lane;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;

        if (!TryGetComponent(out Rigidbody2D rb))
            rb = gameObject.AddComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Notify(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        Notify(other, false);
    }

    private void Notify(Collider2D other, bool inside)
    {
        PianoKey key = other.GetComponentInParent<PianoKey>();
        if (key != null)
            key.SetHitPoint(this, inside);

        LongNote longNote = other.GetComponentInParent<LongNote>();
        if (longNote != null)
            longNote.SetHitPoint(this, inside);

        // Boss projectile cũng nhận trạng thái đang ở hit point.
        BossProjectile projectile =
            other.GetComponentInParent<BossProjectile>();

        if (projectile != null)
            projectile.SetHitPoint(this, inside);
    }
}
