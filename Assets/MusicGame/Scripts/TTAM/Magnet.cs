using System.Collections;
using UnityEngine;

public class Magnet : MonoBehaviour
{
    [SerializeField] private float radius = 6f;
    [SerializeField] private float pullSpeed = 18f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private Sprite icon;

    private float targetTime;
    private float moveSpeed;
    private float hitX;
    private float direction;
    private bool configured;

    [SerializeField] private float destroyBehindDistance = 5f;

    private bool used;
    

    private void Update()
    {
        // Đã nhặt rồi thì đứng yên để coroutine chạy.
        if (used || !configured || PianoKeySpawner.Instance == null)
            return;

        float songTime = PianoKeySpawner.Instance.SongTime;

        Vector3 pos = transform.position;
        pos.x = hitX + direction * moveSpeed * (targetTime - songTime);
        transform.position = pos;

        // Bay qua Player mà không nhặt thì hủy.
        if ((hitX - pos.x) * direction > destroyBehindDistance)
            Destroy(gameObject);
    }

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

    public void Setup(float time, float speed, float playerX, float dir)
    {
        targetTime = time;
        moveSpeed = speed;
        hitX = playerX;
        direction = dir;
        configured = true;
    }
}