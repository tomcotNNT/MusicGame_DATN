
// using UnityEngine;

// public class BossMovement : MonoBehaviour
// {
//     [Header("Boss Movement Settings")]
//     public float moveSpeed = 3f;
//     public float verticalSpeed = 2f;
//     public float moveRange = 3f;
//     public float chaseSpeed = 4f;
//     public float rotationSpeed = 5f;

//     [Header("Player Settings")]
//     public Transform player;
//     public bool followPlayer = false;
//     public float detectionRange = 8f;

//     [Header("Boss State")]
//     public bool canMove = true;
//     public bool isDefeated = false;
//     public bool isAttacking = false;

//     private Vector3 startPosition;
//     private Vector3 targetPosition;
//     private float movementTimer;
//     private bool movingRight = true;

//     void Start()
//     {
//         startPosition = transform.position;
//         targetPosition = startPosition;
//         movementTimer = 0f;

//         if (moveSpeed <= 0f)
//         {
//             moveSpeed = 3f;
//         }

//         if (verticalSpeed <= 0f)
//         {
//             verticalSpeed = 2f;
//         }
//     }

//     void Update()
//     {
//         if (isDefeated || !canMove)
//         {
//             return;
//         }

//         movementTimer += Time.deltaTime;

//         if (followPlayer && player != null)
//         {
//             FollowPlayer();
//         }
//         else
//         {
//             MoveSideToSide();
//         }

//         CheckBossPosition();
//     }

//     void MoveSideToSide()
//     {
//         float newX = startPosition.x
//             + Mathf.Sin(Time.time * moveSpeed) * moveRange;

//         float newY = startPosition.y
//             + Mathf.Sin(Time.time * verticalSpeed) * 0.5f;

//         Vector3 newPosition = new Vector3(
//             newX,
//             newY,
//             startPosition.z
//         );

//         transform.position = Vector3.Lerp(
//             transform.position,
//             newPosition,
//             Time.deltaTime * moveSpeed
//         );
//     }

//     void FollowPlayer()
//     {
//         if (player == null)
//         {
//             return;
//         }

//         float distance = Vector2.Distance(
//             transform.position,
//             player.position
//         );

//         if (distance <= detectionRange)
//         {
//             Vector3 direction = player.position
//                 - transform.position;

//             direction.z = 0f;

//             if (direction.sqrMagnitude > 0.01f)
//             {
//                 Vector3 nextPosition = Vector3.MoveTowards(
//                     transform.position,
//                     player.position,
//                     chaseSpeed * Time.deltaTime
//                 );

//                 transform.position = nextPosition;
//             }
//         }
//         else
//         {
//             MoveSideToSide();
//         }
//     }

//     void CheckBossPosition()
//     {
//         if (transform.position.x > startPosition.x + moveRange)
//         {
//             movingRight = false;
//         }
//         else if (transform.position.x < startPosition.x - moveRange)
//         {
//             movingRight = true;
//         }
//     }

//     public void StartBossMovement()
//     {
//         canMove = true;
//         isDefeated = false;
//     }

//     public void StopBossMovement()
//     {
//         canMove = false;
//     }

//     public void EnableChaseMode(Transform target)
//     {
//         player = target;
//         followPlayer = true;
//         canMove = true;
//     }

//     public void DisableChaseMode()
//     {
//         followPlayer = false;
//     }

//     public void BossAttack()
//     {
//         if (isDefeated || !canMove)
//         {
//             return;
//         }

//         isAttacking = true;
//         Debug.Log("Boss is attacking the player!");
//     }

//     public void FinishBossAttack()
//     {
//         isAttacking = false;
//     }

//     public void DefeatBoss()
//     {
//         isDefeated = true;
//         canMove = false;
//         isAttacking = false;

//         Debug.Log("Boss defeated!");
//     }

//     public void ResetBoss()
//     {
//         isDefeated = false;
//         canMove = true;
//         isAttacking = false;
//         followPlayer = false;

//         transform.position = startPosition;
//     }
// }
