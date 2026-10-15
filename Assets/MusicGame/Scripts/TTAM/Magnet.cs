using System.Collections;
using UnityEngine;

public class Magnet : MonoBehaviour
{
    [SerializeField] private float radius = 6f;
    [SerializeField] private float pullSpeed = 18f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private Sprite icon;

    private bool used;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (used || !collision.CompareTag("Player"))
            return;

        used = true;

        // Ẩn magnet nhưng giữ object sống để chạy coroutine.
        foreach (var r in GetComponentsInChildren<SpriteRenderer>())
            r.enabled = false;
        GetComponent<Collider2D>().enabled = false;

        bool active = true;

        if (ItemDurationSystem.Instance != null)
            ItemDurationSystem.Instance.StartEffect("Magnet", icon, duration, () => active = false);

        StartCoroutine(PullRoutine(collision.transform, () => active));
    }

    private IEnumerator PullRoutine(Transform player, System.Func<bool> isActive)
    {
        var wait = new WaitForSeconds(0.1f);

        while (isActive())
        {
            foreach (Collider2D c in Physics2D.OverlapCircleAll(player.position, radius))
                if (c.TryGetComponent(out Coin coin))
                    coin.Attract(player, pullSpeed);

            yield return wait;
        }

        Destroy(gameObject);
    }
}