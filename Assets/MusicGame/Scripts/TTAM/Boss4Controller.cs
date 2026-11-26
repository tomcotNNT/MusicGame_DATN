using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boss màn 4: đứng yên 1 chỗ.
/// - Spawn nốt      -> anim Attack
/// - Player đánh trúng -> anim Hit
/// - Hết máu        -> anim Die
/// Kéo vào ô "Boss" của Boss4RhythmSpawner.
/// </summary>
public class Boss4Controller : MonoBehaviour
{
    [Header("Drop")]
    [Tooltip("Vị trí nốt xuất hiện (miệng / tay boss). Trống = vị trí boss")]
    [SerializeField] private Transform dropPoint;

    [Header("Idle Bobbing (0 = đứng im hẳn)")]
    [SerializeField] private float bobAmplitude = 0f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [Tooltip("Tên trigger trong Animator Controller")]
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";
    [Tooltip("Huỷ boss sau khi chạy xong animation Die (giây). 0 = không huỷ")]
    [SerializeField] private float destroyAfterDie = 0f;

    [Header("Camera Shake")]
    [SerializeField] private CameraShake cameraShake;
    [Tooltip("Rung nhẹ mỗi lần Attack (0 = không rung)")]
    [SerializeField] private float attackShakeDuration = 0.1f;
    [SerializeField] private float attackShakeMagnitude = 0.08f;
    [Tooltip("Rung mạnh khi boss bị hạ")]
    [SerializeField] private float defeatShakeDuration = 0.6f;
    [SerializeField] private float defeatShakeMagnitude = 0.4f;

    [Header("Boss HP")]
    [Tooltip("Image kiểu Filled (Image Type = Filled, Fill Method = Horizontal)")]
    [SerializeField] private Image hpFill;
    [SerializeField] private float hpSmoothSpeed = 8f;

    public static event Action AnyBossDefeated;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsDefeated { get; private set; }

    private int attackHash;
    private int hitHash;
    private int dieHash;

    private float displayedFill = 1f;
    private float baseY;

    // Awake (không phải Start): spawner/countdown gọi InitHp ngay ở Start của nó
    private void Awake()
    {
        baseY = transform.position.y;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        attackHash = Animator.StringToHash(attackTrigger);
        hitHash = Animator.StringToHash(hitTrigger);
        dieHash = Animator.StringToHash(dieTrigger);

        if (cameraShake == null && Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    private void Update()
    {
        UpdateHpBar();

        if (IsDefeated || bobAmplitude <= 0f)
            return;

        Vector3 pos = transform.position;
        pos.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = pos;
    }

    private void UpdateHpBar()
    {
        if (hpFill == null || MaxHp <= 0)
            return;

        float target = (float)CurrentHp / MaxHp;
        displayedFill = Mathf.MoveTowards(displayedFill, target, hpSmoothSpeed * Time.deltaTime);
        hpFill.fillAmount = displayedFill;
    }

    public Vector3 GetDropPosition()
    {
        return dropPoint != null ? dropPoint.position : transform.position;
    }

    // Spawner gọi khi thả nốt -> Attack
    public void Shake()
    {
        PlayAttack();

        if (cameraShake != null && attackShakeDuration > 0f)
            cameraShake.Shake(attackShakeDuration, attackShakeMagnitude);
    }

    public void InitHp(int maxHp)
    {
        MaxHp = Mathf.Max(1, maxHp);
        CurrentHp = MaxHp;
        IsDefeated = false;
        displayedFill = 1f;

        if (hpFill != null)
            hpFill.fillAmount = 1f;

        // Bài mới: đưa Animator về Idle
        if (animator != null)
        {
            ResetAllTriggers();
            animator.Rebind();
            animator.Update(0f);
        }
    }

    // Player bấm trúng nốt -> Hit, hết máu -> Die
    public void TakeHit()
    {
        if (IsDefeated) return;

        CurrentHp = Mathf.Max(0, CurrentHp - 1);

        if (CurrentHp > 0)
        {
            PlayHit();
            return;
        }

        IsDefeated = true;

        if (animator != null)
        {
            ResetAllTriggers();   // tránh Attack / Hit chen vào lúc đang chết
            animator.SetTrigger(dieHash);
        }

        if (cameraShake != null)
            cameraShake.Shake(defeatShakeDuration, defeatShakeMagnitude);

        AnyBossDefeated?.Invoke();

        if (destroyAfterDie > 0f)
            Destroy(gameObject, destroyAfterDie);
    }

    public void PlayAttack()
    {
        if (animator == null || IsDefeated) return;
        animator.ResetTrigger(hitHash);
        animator.SetTrigger(attackHash);
    }

    public void PlayHit()
    {
        if (animator == null || IsDefeated) return;
        animator.ResetTrigger(attackHash);
        animator.SetTrigger(hitHash);
    }

    private void ResetAllTriggers()
    {
        animator.ResetTrigger(attackHash);
        animator.ResetTrigger(hitHash);
        animator.ResetTrigger(dieHash);
    }
}
