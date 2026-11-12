
using UnityEngine;

public class EnemyHit : MonoBehaviour
{
    private bool hasBeenHit;

    public bool TryReceiveHit()
    {
        if (hasBeenHit)
            return false;

        hasBeenHit = true;

        // Tắt tất cả Collider để tránh va chạm lặp
        Collider2D[] colliders =
            GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
        {
            col.enabled = false;
        }

        Destroy(gameObject, 0.15f);

        return true;
    }
}
