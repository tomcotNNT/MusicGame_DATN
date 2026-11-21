
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BossController2 : MonoBehaviour
{
    [Header("Dịch chuyển giữa 2 điểm")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float teleportInterval = 3f;
    [SerializeField] private bool autoTeleport = true;
    [SerializeField] private bool startAtA = true;
    public float TeleportTotalDuration => vanishDuration + appearDuration;

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool spriteFacesRight = true;
    [SerializeField] private Transform faceTarget;
    [SerializeField] private bool invertFacing = false;

    [Header("Teleport VFX")]
    [SerializeField] private GameObject teleportVFXPrefab;
    [SerializeField] private bool vfxAtOldPosition = true;
    [SerializeField] private bool vfxAtNewPosition = true;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    [SerializeField] private float vfxDestroyTime = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack1";
    [SerializeField] private string vanishTrigger = "Vanish";
    [SerializeField] private string appearTrigger = "Appear";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Animation Timing")]
    [Tooltip("Thời gian chạy animation Vanish trước khi đổi vị trí")]
    [SerializeField] private float vanishDuration = 0.4f;

    [Tooltip("Thời gian chạy animation Appear")]
    [SerializeField] private float appearDuration = 0.4f;

    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Drop")]
    [SerializeField] private Transform dropPoint;

    [Header("Shake khi tấn công")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Boss HP")]
    [SerializeField] private Image hpFill;
    [SerializeField] private float hpSmoothSpeed = 8f;
    [SerializeField] private float defeatShakeDuration = 0.6f;
    [SerializeField] private float defeatShakeMagnitude = 0.4f;

    public static event Action AnyBossDefeated;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsDefeated { get; private set; }

    // Spawner sử dụng để kiểm tra trạng thái dịch chuyển
    public bool IsTeleporting => isTeleporting;

    public bool IsAtPointA => atA;

    private int attackHash;
    private int vanishHash;
    private int appearHash;
    private int dieHash;

    private float displayedFill = 1f;
    private bool atA;
    private float teleportTimer;
    private Vector3 basePosition;

    private bool isTeleporting;
    private Coroutine teleportCoroutine;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraShake == null && Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();

        attackHash = Animator.StringToHash(attackTrigger);
        vanishHash = Animator.StringToHash(vanishTrigger);
        appearHash = Animator.StringToHash(appearTrigger);
        dieHash = Animator.StringToHash(dieTrigger);

        atA = startAtA;

        SnapToCurrentPoint();
    }

    private void Update()
    {
        // Tự động dịch chuyển nếu được bật
        if (autoTeleport && !IsDefeated && !isTeleporting)
        {
            teleportTimer += Time.deltaTime;

            if (teleportTimer >= teleportInterval)
                TeleportToOtherPoint();
        }

        // Bobbing
        if (!IsDefeated)
        {
            Vector3 pos = basePosition;
            pos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = pos;
        }

        // Cập nhật thanh HP
        if (hpFill != null && MaxHp > 0)
        {
            float target = (float)CurrentHp / MaxHp;

            displayedFill = Mathf.MoveTowards(
                displayedFill,
                target,
                hpSmoothSpeed * Time.deltaTime
            );

            hpFill.fillAmount = displayedFill;
        }
    }

    // =========================================
    // FACING
    // =========================================

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        bool lookRight;

        if (faceTarget != null)
            lookRight = faceTarget.position.x > basePosition.x;
        else
            lookRight = atA;

        if (invertFacing)
            lookRight = !lookRight;

        spriteRenderer.flipX = spriteFacesRight
            ? !lookRight
            : lookRight;
    }

    // =========================================
    // DROP / SHAKE
    // =========================================

    public Vector3 GetDropPosition()
    {
        return dropPoint != null
            ? dropPoint.position
            : transform.position;
    }

    public void Shake()
    {
        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);
    }

    public void Shake(float duration, float magnitude)
    {
        if (cameraShake != null)
            cameraShake.Shake(duration, magnitude);
    }

    // =========================================
    // ATTACK
    // =========================================

    // Spawner gọi sau khi Boss dịch chuyển xong
    public void PlayAttack()
    {
        if (IsDefeated || animator == null)
            return;

        animator.ResetTrigger(attackHash);
        animator.SetTrigger(attackHash);
    }

    // =========================================
    // HP
    // =========================================

    public void InitHp(int maxHp)
    {
        MaxHp = Mathf.Max(1, maxHp);
        CurrentHp = MaxHp;
        IsDefeated = false;
        displayedFill = 1f;

        if (hpFill != null)
            hpFill.fillAmount = 1f;
    }

    public void TakeHit()
    {
        if (IsDefeated)
            return;

        CurrentHp = Mathf.Max(0, CurrentHp - 1);

        if (CurrentHp <= 0)
        {
            IsDefeated = true;

            // Dừng dịch chuyển nếu Boss bị hạ
            if (teleportCoroutine != null)
            {
                StopCoroutine(teleportCoroutine);
                teleportCoroutine = null;
            }

            isTeleporting = false;

            // Chạy animation Die
            if (animator != null)
            {
                animator.ResetTrigger(dieHash);
                animator.SetTrigger(dieHash);
            }

            Shake(defeatShakeDuration, defeatShakeMagnitude);

            AnyBossDefeated?.Invoke();
        }
    }

    // =========================================
    // TELEPORT
    // =========================================

    public void TeleportToOtherPoint()
    {
        if (IsDefeated || isTeleporting)
            return;

        atA = !atA;

        BeginTeleport();
    }

    public void TeleportTo(bool toPointA)
    {
        if (IsDefeated || isTeleporting || atA == toPointA)
            return;

        atA = toPointA;

        BeginTeleport();
    }

    // Spawner gọi:
    // Lane 0 = Point A (trái)
    // Lane 1 = Point B (phải)
    public void MoveToLane(int lane)
    {
        if (IsDefeated || isTeleporting)
            return;

        bool targetPointA = lane == 0;

        TeleportTo(targetPointA);
    }

    private void BeginTeleport()
    {
        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);

        teleportCoroutine = StartCoroutine(TeleportRoutine());
    }

    private IEnumerator TeleportRoutine()
    {
        isTeleporting = true;
        teleportTimer = 0f;

        Vector3 oldPosition = transform.position;

        // 1. Vanish
        if (animator != null)
        {
            animator.ResetTrigger(vanishHash);
            animator.SetTrigger(vanishHash);
        }

        yield return new WaitForSeconds(vanishDuration);

        if (IsDefeated)
            yield break;

        // 2. VFX vị trí cũ
        if (vfxAtOldPosition)
            SpawnTeleportVFX(oldPosition);

        // 3. Di chuyển đến điểm mới
        Transform target = atA ? pointA : pointB;

        if (target != null)
            basePosition = target.position;

        transform.position = basePosition;

        UpdateFacing();

        // 4. VFX vị trí mới
        if (vfxAtNewPosition)
            SpawnTeleportVFX(basePosition);

        // 5. Appear
        if (animator != null)
        {
            animator.ResetTrigger(appearHash);
            animator.SetTrigger(appearHash);
        }

        yield return new WaitForSeconds(appearDuration);

        isTeleporting = false;
        teleportCoroutine = null;
    }

    private void SnapToCurrentPoint()
    {
        teleportTimer = 0f;

        Transform target = atA ? pointA : pointB;

        basePosition = target != null
            ? target.position
            : transform.position;

        transform.position = basePosition;

        UpdateFacing();
    }

    private void SpawnTeleportVFX(Vector3 position)
    {
        if (teleportVFXPrefab == null)
            return;

        GameObject vfx = Instantiate(
            teleportVFXPrefab,
            position + vfxOffset,
            Quaternion.identity
        );

        Destroy(vfx, vfxDestroyTime);
    }
}
