
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class BossBullet : MonoBehaviour
{
    private enum BulletState
    {
        FlyingToPlayer,
        WaitingForParry,
        ReturningToBoss
    }

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float waitTime = 2f;
    [SerializeField] private int damage = 1;

    [Header("VFX")]
    [SerializeField] private GameObject parryVFX;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private Rigidbody2D rb;
    private Transform playerTarget;
    private BossController2 bossOwner;

    private BulletState state;
    private float waitTimer;

    private bool initialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    public void Initialize(
        Transform target,
        float speed,
        bool left,
        BossController2 owner)
    {
        playerTarget = target;
        moveSpeed = speed;
        bossOwner = owner;

        state = BulletState.FlyingToPlayer;
        waitTimer = 0f;
        initialized = true;

        if (showDebug)
        {
            Debug.Log(
                "[BULLET] Initialize SUCCESS | Target: " +
                (target != null ? target.name : "NULL") +
                " | Speed: " + moveSpeed +
                " | Start: " + transform.position +
                " | Target Position: " +
                (target != null ? target.position.ToString() : "NULL")
            );
        }

        if (playerTarget == null)
            Debug.LogError("[BULLET] Target is NULL!");

        if (moveSpeed <= 0)
            Debug.LogError("[BULLET] Move Speed must be greater than 0!");
    }

    private void FixedUpdate()
    {
        if (!initialized)
            return;

        switch (state)
        {
            case BulletState.FlyingToPlayer:
                MoveToPlayer();
                break;

            case BulletState.ReturningToBoss:
                ReturnToBoss();
                break;
        }
    }

    private void Update()
    {
        if (!initialized)
            return;

        if (state == BulletState.WaitingForParry)
        {
            waitTimer += Time.deltaTime;

            if (waitTimer >= waitTime)
            {
                if (showDebug)
                    Debug.Log("[BULLET] Parry timeout!");

                Destroy(gameObject);
            }
        }
    }

    private void MoveToPlayer()
    {
        if (playerTarget == null)
        {
            Debug.LogError("[BULLET] Player Target missing!");
            Destroy(gameObject);
            return;
        }

        Vector2 targetPosition = playerTarget.position;

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        float distance = Vector2.Distance(
            nextPosition,
            targetPosition
        );

        if (distance <= 0.05f)
        {
            rb.position = targetPosition;

            state = BulletState.WaitingForParry;
            waitTimer = 0f;

            if (showDebug)
                Debug.Log("[BULLET] Reached Player HitPoint!");
        }
    }

    public bool TryReflect()
    {
        if (state == BulletState.ReturningToBoss)
            return false;

        if (!initialized || playerTarget == null)
            return false;

        float distance = Vector2.Distance(
            rb.position,
            playerTarget.position
        );

        if (distance > 0.6f)
            return false;

        state = BulletState.ReturningToBoss;

        if (parryVFX != null)
        {
            Instantiate(
                parryVFX,
                transform.position,
                Quaternion.identity
            );
        }

        Debug.Log("[BULLET] REFLECTED!");

        return true;
    }

    private void ReturnToBoss()
    {
        if (bossOwner == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 bossPosition = bossOwner.transform.position;

        Vector2 nextPosition = Vector2.MoveTowards(
            rb.position,
            bossPosition,
            moveSpeed * 1.5f * Time.fixedDeltaTime
        );

        rb.MovePosition(nextPosition);

        if (Vector2.Distance(nextPosition, bossPosition) <= 0.2f)
        {
            bossOwner.ReceiveReflectedHit(damage);

            Debug.Log("[BULLET] HIT BOSS!");

            Destroy(gameObject);
        }
    }
}
