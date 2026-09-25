using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    public ComboSystem Combo;

    private void Start()
    {
        Combo = FindFirstObjectByType<ComboSystem>();
    }

    private void Update()
    {
        transform.position += Vector3.left * moveSpeed * Time.deltaTime;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Collect();
        }

        //va chạm tường thì mất
        if (other.CompareTag("Wall"))
        {
            Miss();
            Destroy(gameObject,2f);

        }
    }

    private void Collect()
    {
        Debug.Log("Collect Note");
        if (Combo != null)
        {
            Combo.HitNote();
        }

        Destroy(gameObject);
    }

    private void Miss()
    {
        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.TakeMissDamage();
        }

        Debug.Log("Miss Note -10 HP");

        // Gọi hàm MissNote() từ ComboSystem
        if (Combo != null)
        {
            Combo.MissNote();
        }

        
    }
}