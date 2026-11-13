
using System;
using System.Collections;
using UnityEngine;

public class BossController2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;

    [Header("Animation")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string takeHitTrigger = "TakeHit";

    [Header("Teleport")]
    [SerializeField] private SpriteRenderer bossSprite;
    [SerializeField] private GameObject disappearVFX;
    [SerializeField] private GameObject appearVFX;
    [SerializeField] private float disappearDuration = 0.25f;
    [SerializeField] private float teleportCooldown = 4f;

    [Header("Sprite Direction")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    [Header("Lane Switch")]
    [SerializeField] private bool enableLaneSwitch = true;

    [Header("Attack")]
    [SerializeField] private float attackInterval = 2f;
    [SerializeField] private float bulletSpeed = 8f;

    [Header("Boss Health")]
    [SerializeField] private int maxHealth = 10;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private Transform laneLeft;
    private Transform laneRight;

    private Transform hitPointLeft;
    private Transform hitPointRight;

    private Transform currentTarget;
    private Func<float> getSongTime;

    private bool isLeft;
    private bool initialized;
    private bool isTeleporting;
    private bool isDead;

    private float nextSwitchTime;
    private float nextAttackTime;

    private int currentHealth;

    public bool IsLeft => isLeft;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    public void Initialize(
        bool startLeft,
        Transform left,
        Transform right,
        Transform leftHitPoint,
        Transform rightHitPoint,
        Func<float> songTimeProvider)
    {
        laneLeft = left;
        laneRight = right;

        hitPointLeft = leftHitPoint;
        hitPointRight = rightHitPoint;

        getSongTime = songTimeProvider;

        if (getSongTime == null)
        {
            Debug.LogError("[BOSS] SongTime Provider is null!");
            return;
        }

        isLeft = startLeft;

        currentTarget = isLeft ? laneLeft : laneRight;

        if (currentTarget == null)
        {
            Debug.LogError("[BOSS] Lane Transform is missing!");
            return;
        }

        transform.position = currentTarget.position;

        currentHealth = Mathf.Max(1, maxHealth);

        UpdateSpriteDirection();

        float now = getSongTime();

        nextSwitchTime = now + teleportCooldown;
        nextAttackTime = now + attackInterval;

        initialized = true;

        if (showDebug)
        {
            Debug.Log(
                "[BOSS] Initialized at " +
                (isLeft ? "LEFT" : "RIGHT") +
                " | HP: " + currentHealth
            );
        }
    }

    private void Update()
    {
        if (!initialized || isTeleporting || isDead)
            return;

        float now = getSongTime();

        // Teleport đổi lane
        if (enableLaneSwitch && now >= nextSwitchTime)
        {
            SwitchLane();
            nextSwitchTime = now + teleportCooldown;
        }

        // Bắn đạn
        if (now >= nextAttackTime)
        {
            Attack();
            nextAttackTime = now + attackInterval;
        }
    }

    private void SwitchLane()
    {
        if (isTeleporting || isDead)
            return;

        StartCoroutine(TeleportToLane());
    }

    private IEnumerator TeleportToLane()
    {
        isTeleporting = true;

        // VFX biến mất
        SpawnVFX(disappearVFX, transform.position);

        // Ẩn Sprite
        if (bossSprite != null)
            bossSprite.enabled = false;

        yield return new WaitForSeconds(disappearDuration);

        // Đổi lane
        isLeft = !isLeft;

        currentTarget = isLeft ? laneLeft : laneRight;

        if (currentTarget != null)
            transform.position = currentTarget.position;

        // Lật Sprite
        UpdateSpriteDirection();

        // VFX xuất hiện
        SpawnVFX(appearVFX, transform.position);

        // Hiện Sprite
        if (bossSprite != null)
            bossSprite.enabled = true;

        if (showDebug)
        {
            Debug.Log(
                "[BOSS] Teleported to " +
                (isLeft ? "LEFT" : "RIGHT")
            );
        }

        isTeleporting = false;
    }

    private void UpdateSpriteDirection()
    {
        if (bossSprite == null)
            return;

        bossSprite.flipX = spriteFacesRightByDefault
            ? !isLeft
            : isLeft;
    }

    private void SpawnVFX(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        Instantiate(
            prefab,
            position,
            Quaternion.identity
        );
    }

    private void Attack()
    {
        Transform target = isLeft
            ? hitPointLeft
            : hitPointRight;

        if (target == null || bulletPrefab == null)
        {
            if (showDebug)
                Debug.LogWarning("[BOSS] Missing target or Bullet Prefab!");

            return;
        }

        Vector3 spawnPosition = firePoint != null
            ? firePoint.position
            : transform.position;

        GameObject bullet = Instantiate(
            bulletPrefab,
            spawnPosition,
            Quaternion.identity
        );

        BossBullet projectile = bullet.GetComponent<BossBullet>();

        if (projectile == null)
        {
            Debug.LogError(
                "[BOSS] Bullet Prefab requires BossBullet!"
            );

            Destroy(bullet);
            return;
        }

        projectile.Initialize(
            target,
            bulletSpeed,
            isLeft,
            this
        );

        if (animator != null &&
            !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        if (showDebug)
        {
            Debug.Log(
                "[BOSS] Attacked " +
                (isLeft ? "LEFT" : "RIGHT")
            );
        }
    }

    private void PlayTakeHitAnimation()
    {
        if (animator == null || string.IsNullOrEmpty(takeHitTrigger))
            return;

        animator.ResetTrigger(attackTrigger);
        animator.SetTrigger(takeHitTrigger);
    }

    public void ReceiveReflectedHit(int damage)
    {
        if (isDead)
            return;

        currentHealth -= Mathf.Max(1, damage);
        currentHealth = Mathf.Max(0, currentHealth);

        if (showDebug)
        {
            Debug.Log(
                "[BOSS] HIT! HP: " +
                currentHealth + "/" + maxHealth
            );
        }

        // Phát animation khi Boss bị trúng đạn
        PlayTakeHitAnimation();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;

        StopAllCoroutines();

        if (showDebug)
            Debug.Log("[BOSS] DEFEATED!");

        Destroy(gameObject);
    }
}
