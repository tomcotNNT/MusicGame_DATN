
// using UnityEngine;
// using UnityEngine.UI;

// public class Stage3BossController : MonoBehaviour
// {
//     [Header("Boss Settings")]
//     public float maxHealth = 5000f;
//     public float currentHealth = 5000f;
//     public float moveSpeed = 2f;
//     public float moveDistance = 2f;

//     [Header("Attack Settings")]
//     public GameObject musicNotePrefab;
//     public Transform attackPoint;
//     public float attackInterval = 2f;
//     public float noteSpeed = 5f;

//     [Header("Health Bar")]
//     public Slider healthBar;

//     private Vector3 startPosition;
//     private float attackTimer;
//     private bool secondPhase;
//     private bool isDead;

//     void Start()
//     {
//         startPosition = transform.position;
//         currentHealth = maxHealth;

//         if (healthBar != null)
//         {
//             healthBar.maxValue = maxHealth;
//             healthBar.value = currentHealth;
//         }

//         attackTimer = attackInterval;
//     }

//     void Update()
//     {
//         if (isDead) return;

//         MoveBoss();
//         HandleAttack();
//         UpdatePhase();
//     }

//     void MoveBoss()
//     {
//         float distance = secondPhase ? moveDistance * 1.4f : moveDistance;
//         float speed = secondPhase ? moveSpeed * 1.5f : moveSpeed;

//         float offset = Mathf.Sin(Time.time * speed) * distance;

//         transform.position = new Vector3(
//             startPosition.x + offset,
//             startPosition.y,
//             startPosition.z
//         );
//     }

//     void HandleAttack()
//     {
//         attackTimer -= Time.deltaTime;

//         if (attackTimer <= 0f)
//         {
//             ShootMusicNote();

//             attackTimer = secondPhase
//                 ? attackInterval * 0.6f
//                 : attackInterval;
//         }
//     }

//     void ShootMusicNote()
//     {
//         if (musicNotePrefab == null || attackPoint == null)
//         {
//             return;
//         }

//         GameObject note = Instantiate(
//             musicNotePrefab,
//             attackPoint.position,
//             Quaternion.identity
//         );

//         Rigidbody2D rb = note.GetComponent<Rigidbody2D>();

//         if (rb != null)
//         {
//             rb.linearVelocity = Vector2.left * noteSpeed;
//         }
//     }

//     public void TakeDamage(float damage)
//     {
//         if (isDead || damage <= 0f) return;

//         currentHealth -= damage;
//         currentHealth = Mathf.Clamp(
//             currentHealth, 0f, maxHealth
//         );

//         if (healthBar != null)
//         {
//             healthBar.value = currentHealth;
//         }

//         if (currentHealth <= maxHealth * 0.5f)
//         {
//             secondPhase = true;
//         }

//         if (currentHealth <= 0f)
//         {
//             Die();
//         }
//     }

//     void UpdatePhase()
//     {
//         if (currentHealth <= maxHealth * 0.5f)
//         {
//             secondPhase = true;
//         }
//     }

//     void Die()
//     {
//         isDead = true;
//         Debug.Log("Boss màn 3 đã bị đánh bại!");

//         gameObject.SetActive(false);
//     }
// }
