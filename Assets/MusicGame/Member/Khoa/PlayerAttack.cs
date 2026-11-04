
// using UnityEngine;
// using System.Collections;

// public class PlayerAttack : MonoBehaviour
// {
//     [Header("Attack Settings")]
//     public float attackRange = 2.5f;
//     public float attackCooldown = 1.2f;
//     public int attackDamage = 10;

//     [Header("Attack Detection")]
//     public LayerMask enemyLayer;
//     public Transform attackPoint;

//     [Header("Attack Effects")]
//     public GameObject attackEffect;
//     public Animator animator;

//     private bool canAttack = true;
//     private bool isAttacking = false;

//     void Update()
//     {
//         HandleAttackInput();
//     }

//     void HandleAttackInput()
//     {
//         if (Input.GetKeyDown(KeyCode.Space) && canAttack)
//         {
//             StartCoroutine(PerformAttack());
//         }
//     }

//     IEnumerator PerformAttack()
//     {
//         canAttack = false;
//         isAttacking = true;

//         if (animator != null)
//         {
//             animator.SetTrigger("Attack");
//         }

//         if (attackEffect != null && attackPoint != null)
//         {
//             Instantiate(
//                 attackEffect,
//                 attackPoint.position,
//                 attackPoint.rotation
//             );
//         }

//         yield return new WaitForSeconds(0.2f);

//         DetectEnemies();

//         yield return new WaitForSeconds(0.3f);

//         isAttacking = false;

//         yield return new WaitForSeconds(attackCooldown);

//         canAttack = true;
//     }

//     void DetectEnemies()
//     {
//         if (attackPoint == null) return;

//         Collider2D[] enemies = Physics2D.OverlapCircleAll(
//             attackPoint.position,
//             attackRange,
//             enemyLayer
//         );

//         foreach (Collider2D enemy in enemies)
//         {
//             EnemyHealth health = enemy.GetComponent<EnemyHealth>();

//             if (health != null)
//             {
//                 health.TakeDamage(attackDamage);
//             }
//         }
//     }

//     void OnDrawGizmosSelected()
//     {
//         if (attackPoint == null) return;

//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(
//             attackPoint.position,
//             attackRange
//         );
//     }
// }
