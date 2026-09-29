using UnityEngine;

public class Note : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private ComboSystem Combo;
    private ScoreSystem ScoreSystem;
    [SerializeField] private GameObject collectEffect;
    private FeverSystem feverSystem;

    private void Start()
    {
        Combo = FindFirstObjectByType<ComboSystem>();
        ScoreSystem = FindFirstObjectByType<ScoreSystem>();
        feverSystem = FindFirstObjectByType<FeverSystem>();
    }

    private void Update()
    {   
        float speedMultiplier = 1f;
         if (feverSystem != null)
        {
            speedMultiplier = feverSystem.GetSpeedMultiplier();
        }


        transform.position += Vector3.left * moveSpeed * speedMultiplier * Time.deltaTime;
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

        // Spawn hiệu ứng vòng tròn
        if (collectEffect != null)
        {
            GameObject effect = Instantiate(collectEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f); // Hủy hiệu ứng sau 1 giây
        }

        // Combo
        if (Combo != null)
        {
            Combo.HitNote();
        }

        // Score
        if (ScoreSystem != null)
        {
            ScoreSystem.AddScore();
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