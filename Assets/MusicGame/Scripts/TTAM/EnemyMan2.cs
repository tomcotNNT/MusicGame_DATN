
using System;
using UnityEngine;

public class EnemyMan2 : MonoBehaviour
{
    public static event Action<EnemyMan2> AnyEnemyHit;

    private enum EnemyState
    {
        MovingToHitPoint,
        WaitingAtHitPoint,
        ChasingPlayer,
        Hit
    }

    [Header("Movement")]
    [SerializeField] private float chaseSpeed = 8f;
    [SerializeField] private float hitGraceTime = 0.25f;
    [SerializeField] private float hitDistance = 0.2f;
    [SerializeField] private float maxChaseDuration = 6f;

    [Header("Sprite Flip")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool facePlayer = true;

    private Transform target;
    private Transform hitPoint;

    private Vector3 startPos;

    private Func<float> getSongTime;

    private float spawnTime;
    private float hitTime;
    private float chaseStartTime;

    private EnemyState currentState;

    private bool damageTriggered;

    public void Init(
        Transform player,
        Transform hitPoint,
        float spawnTime,
        float hitTime,
        Func<float> songTimeProvider)
    {
        target = player;
        this.hitPoint = hitPoint;

        this.spawnTime = spawnTime;
        this.hitTime = hitTime;

        getSongTime = songTimeProvider;

        startPos = transform.position;

        currentState = EnemyState.MovingToHitPoint;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (target == null || hitPoint == null || getSongTime == null)
        {
            Debug.LogError("EnemyMan2: Missing Init references!");
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (currentState == EnemyState.Hit)
            return;

        if (target == null || getSongTime == null)
        {
            Destroy(gameObject);
            return;
        }

        UpdateSpriteDirection();

        float songTime = getSongTime();

        switch (currentState)
        {
            case EnemyState.MovingToHitPoint:
                MoveToHitPoint(songTime);
                break;

            case EnemyState.WaitingAtHitPoint:
                WaitAtHitPoint(songTime);
                break;

            case EnemyState.ChasingPlayer:
                ChasePlayer(songTime);
                break;
        }
    }

    // =========================================
    // MOVE TO HIT POINT
    // =========================================

    private void MoveToHitPoint(float songTime)
    {
        if (hitPoint == null)
        {
            Destroy(gameObject);
            return;
        }

        float duration = hitTime - spawnTime;

        float t = duration <= 0f
            ? 1f
            : Mathf.Clamp01((songTime - spawnTime) / duration);

        transform.position = Vector3.Lerp(
            startPos,
            hitPoint.position,
            t
        );

        if (songTime >= hitTime)
        {
            transform.position = hitPoint.position;

            currentState = EnemyState.WaitingAtHitPoint;
        }
    }

    // =========================================
    // WAIT FOR PLAYER INPUT
    // =========================================

    private void WaitAtHitPoint(float songTime)
    {
        if (hitPoint != null)
            transform.position = hitPoint.position;

        // Cho Player thêm thời gian để đánh đúng nhịp.
        if (songTime >= hitTime + hitGraceTime)
        {
            currentState = EnemyState.ChasingPlayer;
            chaseStartTime = songTime;
        }
    }

    // =========================================
    // CHASE PLAYER WHEN MISSED
    // =========================================

    private void ChasePlayer(float songTime)
    {
        Vector3 targetPosition = target.position;
        targetPosition.z = transform.position.z;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            chaseSpeed * Time.deltaTime
        );

        float distance = Vector2.Distance(
            transform.position,
            targetPosition
        );

        if (distance <= hitDistance)
        {
            TriggerPlayerHit();
            return;
        }

        // Tránh enemy tồn tại mãi nếu không đuổi kịp Player.
        if (songTime - chaseStartTime >= maxChaseDuration)
        {
            Destroy(gameObject);
        }
    }

    // =========================================
    // PLAYER GETS HIT
    // =========================================

    private void TriggerPlayerHit()
    {
        if (damageTriggered)
            return;

        damageTriggered = true;

        AnyEnemyHit?.Invoke(this);

        Destroy(gameObject);
    }

    // =========================================
    // ENEMY GETS HIT BY PLAYER
    // =========================================

    public void MarkHit()
    {
        if (currentState == EnemyState.Hit)
            return;

        currentState = EnemyState.Hit;

        // Cho hiệu ứng đánh có một khoảng thời gian hiển thị.
        Destroy(gameObject, 0.2f);
    }

    // =========================================
    // SPRITE DIRECTION
    // =========================================

    private void UpdateSpriteDirection()
    {
        if (!facePlayer || target == null || spriteRenderer == null)
            return;

        spriteRenderer.flipX =
            target.position.x < transform.position.x;
    }
}
