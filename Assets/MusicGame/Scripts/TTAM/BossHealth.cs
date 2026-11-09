using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField] private Image hpBar;

    [Header("Animation (để trống nếu không dùng)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Khi boss chết")]
    [Tooltip("Boss ngừng bắn khi chết")]
    [SerializeField] private BossMusicAttack attack;
    [Tooltip("Collider của boss, tắt khi chết để đạn bay xuyên qua")]
    [SerializeField] private Collider2D[] collidersToDisable;

    [Header("Lùi ra sau rồi huỷ")]
    [Tooltip("Tắt = huỷ boss ngay sau Destroy Delay, không lùi")]
    [SerializeField] private bool retreatOnDeath = true;
    [Tooltip("Object sẽ lùi và bị huỷ. Để trống = chính object này (đặt root của boss nếu script nằm ở object con)")]
    [SerializeField] private GameObject destroyTarget;
    [Tooltip("Điểm boss lùi tới. Để trống = lùi theo Retreat Direction x Retreat Distance")]
    [SerializeField] private Transform retreatPoint;
    [Tooltip("Hướng lùi (ra sau lưng boss). Ví dụ (1,0) = lùi sang phải, (-1,0) = sang trái")]
    [SerializeField] private Vector2 retreatDirection = Vector2.right;
    [SerializeField, Min(0f)] private float retreatDistance = 6f;
    [SerializeField, Min(0.1f)] private float retreatSpeed = 4f;
    [Tooltip("Chờ bao lâu (giây) cho animation Die chạy rồi mới lùi")]
    [SerializeField, Min(0f)] private float delayBeforeRetreat = 0.5f;
    [Tooltip("Mờ dần khi lùi (cần SpriteRenderer)")]
    [SerializeField] private bool fadeOut = true;
    [Tooltip("Chờ thêm bao lâu sau khi lùi xong rồi mới Destroy")]
    [SerializeField, Min(0f)] private float destroyDelay = 0.1f;

    private int currentHealth;
    private bool isDead;

    public bool IsDead => isDead;
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    // Gửi tín hiệu khi boss chết (PlayerController lắng nghe để tính thắng)
    public event Action Died;
    public event Action<int, int> HealthChanged;   // (hiện tại, tối đa)

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (attack == null) attack = GetComponent<BossMusicAttack>();
        if (destroyTarget == null) destroyTarget = gameObject;

        currentHealth = maxHealth;
        UpdateBar();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth = Mathf.Max(currentHealth - damage, 0);

        UpdateBar();
        HealthChanged?.Invoke(currentHealth, maxHealth);

        Debug.Log($"Boss Health: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
        else
            SetTrigger(hitTrigger);
    }

    private void Die()
    {
        if (isDead)
            return;

        isDead = true;
        currentHealth = 0;
        UpdateBar();

        // Dừng bắn
        if (attack != null)
        {
            attack.CancelPendingAttack();
            attack.enabled = false;
        }

        // Tắt va chạm
        if (collidersToDisable != null)
        {
            foreach (Collider2D c in collidersToDisable)
                if (c != null) c.enabled = false;
        }

        SetTrigger(dieTrigger);

        Debug.Log("Boss Die!");

        // Báo tín hiệu trước, việc lùi và huỷ chạy sau
        Died?.Invoke();

        StartCoroutine(RetreatAndDestroy());
    }

    private IEnumerator RetreatAndDestroy()
    {
        if (delayBeforeRetreat > 0f)
            yield return new WaitForSeconds(delayBeforeRetreat);

        Transform t = destroyTarget.transform;

        if (retreatOnDeath)
        {
            Vector3 start = t.position;
            Vector3 end = retreatPoint != null
                ? retreatPoint.position
                : start + (Vector3)(retreatDirection.normalized * retreatDistance);

            float duration = Mathf.Max(0.01f, Vector3.Distance(start, end) / retreatSpeed);

            // Lưu màu gốc để làm mờ dần
            SpriteRenderer[] sprites = fadeOut
                ? t.GetComponentsInChildren<SpriteRenderer>()
                : new SpriteRenderer[0];

            Color[] originals = new Color[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                originals[i] = sprites[i].color;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);

                t.position = Vector3.Lerp(start, end, k);

                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null) continue;

                    Color c = originals[i];
                    c.a = originals[i].a * (1f - k);
                    sprites[i].color = c;
                }

                yield return null;
            }

            t.position = end;
        }

        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        Destroy(destroyTarget);
    }

    private void UpdateBar()
    {
        if (hpBar != null)
            hpBar.fillAmount = (float)currentHealth / maxHealth;
    }

    private void SetTrigger(string trigger)
    {
        if (animator == null || string.IsNullOrEmpty(trigger))
            return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == trigger && p.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(trigger);
                return;
            }
        }
    }
}