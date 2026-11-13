
using System.Collections;
using UnityEngine;

public class BossController : MonoBehaviour
{
    [Header("Boss Stats")]
    public float maxHP = 100f;
    public float currentHP;
    public bool evolved = false;

    [Header("Attack 1 - Dash")]
    public float dashSpeed = 12f;
    public float dashStopDistance = 0.8f;
    public float dashDamage = 20f;
    public float dashInterval = 8f;

    [Header("Attack 2 - Energy Ball")]
    public GameObject energyBallPrefab;
    public Transform firePoint;
    public float shootInterval = 5f;

    [Header("References")]
    public Transform player;
    public Animator animator;

    private Vector3 homePosition;
    private Rigidbody2D rb;

    private bool isAttacking = false;
    private bool isEvolving = false;
    private bool isDead = false;
    private bool dashHitPlayer = false;

    private float nextDashTime;
    private float nextShootTime;

    void Start()
    {
        currentHP = maxHP;
        homePosition = transform.position;

        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.linearVelocity = Vector2.zero;
        }

        nextDashTime = Time.time + dashInterval;
        nextShootTime = Time.time + shootInterval;

        PlayIdle();
    }

    void Update()
    {
        if (isDead || isEvolving || player == null)
            return;

        if (isAttacking)
            return;

        // Attack 2 uu tien khi den gio ban
        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + shootInterval;
            StartCoroutine(ShootAttack());
        }
        else if (Time.time >= nextDashTime)
        {
            nextDashTime = Time.time + dashInterval;
            StartCoroutine(DashAttack());
        }
    }

    // ATTACK 1: LAO DEN PLAYER -> QUAY VE -> IDLE
    IEnumerator DashAttack()
    {
        isAttacking = true;
        dashHitPlayer = false;

        animator.SetTrigger("Dash");

        Vector3 target = player.position;
        target.y = homePosition.y;
        target.z = homePosition.z;

        // Lao den vi tri Player
        while (Vector3.Distance(transform.position, target)
               > dashStopDistance)
        {
            if (isEvolving || isDead)
                yield break;

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                target,
                dashSpeed * Time.fixedDeltaTime
            );

            MoveBoss(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        // Dung lai tan cong
        yield return new WaitForSeconds(0.3f);

        // Quay ve vi tri ban dau
        while (Vector3.Distance(transform.position, homePosition)
               > 0.05f)
        {
            if (isEvolving || isDead)
                yield break;

            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                homePosition,
                dashSpeed * Time.fixedDeltaTime
            );

            MoveBoss(nextPosition);

            yield return new WaitForFixedUpdate();
        }

        MoveBoss(homePosition);

        // BAT BUOC TRO VE IDLE
        PlayIdle();

        isAttacking = false;
    }

    // ATTACK 2: BAN 1 QUA CAU -> IDLE
    IEnumerator ShootAttack()
    {
        isAttacking = true;

        animator.SetTrigger("Shoot");

        // Cho animation chuan bi ban
        yield return new WaitForSeconds(0.4f);

        if (energyBallPrefab != null && firePoint != null)
        {
            GameObject ball = Instantiate(
                energyBallPrefab,
                firePoint.position,
                Quaternion.identity
            );

            EnergyBall projectile =
                ball.GetComponent<EnergyBall>();

            if (projectile != null && player != null)
            {
                Vector2 direction =
                    (player.position - firePoint.position).normalized;

                projectile.SetDirection(direction);
            }
        }
        else
        {
            Debug.LogError(
                "Boss: Energy Ball Prefab hoac Fire Point bi thieu!"
            );
        }

        yield return new WaitForSeconds(0.4f);

        // BAT BUOC TRO VE IDLE
        PlayIdle();

        isAttacking = false;
    }

    // DI CHUYEN BOSS
    void MoveBoss(Vector3 position)
    {
        if (rb != null)
        {
            rb.MovePosition(new Vector2(position.x, position.y));
        }
        else
        {
            transform.position = position;
        }
    }

    // RESET ANIMATION VA TRO VE IDLE
    void PlayIdle()
    {
        if (animator == null)
            return;

        animator.ResetTrigger("Dash");
        animator.ResetTrigger("Shoot");
        animator.ResetTrigger("Evolve");
        animator.ResetTrigger("Die");

        animator.Play("Idle", 0, 0f);
        animator.Update(0f);
    }

    // VA CHAM PLAYER KHI DASH
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isAttacking || dashHitPlayer)
            return;

        if (other.CompareTag("Player"))
        {
            dashHitPlayer = true;

            Debug.Log("Boss Dash hit Player: " + dashDamage);

            // Goi PlayerHealth.TakeDamage(dashDamage)
            // theo script mau Player cua ban.
        }
    }

    // BOSS NHAN DAMAGE
    public void TakeDamage(float damage)
    {
        if (isEvolving || isDead)
            return;

        currentHP = Mathf.Max(0, currentHP - damage);

        Debug.Log("Boss HP: " + currentHP);

        if (currentHP <= 0)
        {
            if (!evolved)
                StartCoroutine(Evolve());
            else
                Die();
        }
    }

    // TIEN HOA
    IEnumerator Evolve()
    {
        isEvolving = true;
        isAttacking = false;

        StopAllCoroutines();

        animator.SetTrigger("Evolve");

        yield return new WaitForSeconds(2f);

        evolved = true;
        maxHP = 150f;
        currentHP = maxHP;

        dashSpeed = 18f;
        dashDamage = 30f;

        nextDashTime = Time.time + dashInterval;
        nextShootTime = Time.time + shootInterval;

        PlayIdle();

        isEvolving = false;
    }

    // BOSS CHET
    void Die()
    {
        isDead = true;
        StopAllCoroutines();

        animator.SetTrigger("Die");
    }
}
