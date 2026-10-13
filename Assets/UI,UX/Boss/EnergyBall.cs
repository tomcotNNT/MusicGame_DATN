using UnityEngine;

public class EnergyBall : MonoBehaviour
{
    public float speed = 8f;
    public float damage = 10f;
    public float lifetime = 5f;

    private Vector2 direction = Vector2.left;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    public void SetDirection(Vector2 dir)
    {
        // Always travel to the LEFT regardless of the player's position.
        direction = Vector2.left;
        // Set this to false if the source sprite already faces left.
        if (spriteRenderer != null) spriteRenderer.flipX = true;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Debug.Log("Energy Ball hit Player, damage: " + damage);
        // Connect to your PlayerHealth script here, e.g.:
        // other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        Destroy(gameObject);
    }
}
