using UnityEngine;

public class BossItemLaneMovement : MonoBehaviour
{
    private Transform player;
    private float speed;
    private bool initialized;

    public void Initialize(Transform targetPlayer, float moveSpeed)
    {
        player = targetPlayer;
        speed = Mathf.Max(0.1f, moveSpeed);
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || player == null) return;
        transform.position += Vector3.left * speed * Time.deltaTime;
        if (transform.position.x < player.position.x - 2f)
            Destroy(gameObject);
    }
}
