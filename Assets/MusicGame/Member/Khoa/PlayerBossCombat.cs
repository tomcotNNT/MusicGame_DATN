// using UnityEngine;

// public class PlayerBossCombat : MonoBehaviour
// {
//     [Header("Combat Settings")]
//     public float attackDamage = 25f;
//     public float attackRange = 2.5f;
//     public float attackCooldown = 1.2f;

//     [Header("References")]
//     public Animator animator;
//     public Transform attackPoint;
//     public LayerMask bossLayer;

//     private float nextAttackTime;
//     private bool isAttacking;

//     void Update()
//     {
//         if (Input.GetKeyDown(KeyCode.J))
//         {
//             AttackBoss();
//         }
//     }

//     public void AttackBoss()
//     {
//         if (Time.time < nextAttackTime || isAttacking)
//             return;

//         nextAttackTime = Time.time + attackCooldown;
//         isAttacking = true;

//         if (animator != null)
//         {
//             animator.SetTrigger("Attack");
//         }

//         Collider2D[] hitObjects = Physics2D.OverlapCircleAll(
//             attackPoint.position,
//             attackRange,
//             bossLayer
//         );

//         foreach (Collider2D target in hitObjects)
//         {
//             BossHealth boss = target.GetComponent<BossHealth>();

//             if (boss != null)
//             {
//                 boss.TakeDamage(attackDamage);
//             }
//         }

//         isAttacking = false;
//     }

//     private void OnDrawGizmosSelected()
//     {
//         if (attackPoint == null) return;

//         Gizmos.color = Color.red;
//         Gizmos.DrawWireSphere(attackPoint.position, attackRange);
//     }
// }