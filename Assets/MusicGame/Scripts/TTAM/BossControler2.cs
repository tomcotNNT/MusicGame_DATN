using System;
using UnityEngine;
using UnityEngine.UI;

public class BossController2 : MonoBehaviour
{
    [Header("Dịch chuyển giữa 2 điểm")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [Tooltip("Cứ mỗi bao nhiêu giây thì Boss dịch chuyển sang điểm còn lại")]
    [SerializeField] private float teleportInterval = 3f;
    [Tooltip("Bật để Boss tự dịch chuyển theo thời gian")]
    [SerializeField] private bool autoTeleport = true;
    [Tooltip("Boss bắt đầu ở điểm A (tắt = bắt đầu ở điểm B)")]
    [SerializeField] private bool startAtA = true;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Bật nếu sprite Boss gốc đang quay mặt sang phải")]
    [SerializeField] private bool spriteFacesRight = true;
    [Tooltip("Tự lật mặt Boss về phía Player (nếu để trống thì lật theo điểm: A = trái, B = phải)")]
    [SerializeField] private Transform faceTarget;
    [Tooltip("Bật nếu Boss đang quay mặt ngược hướng mong muốn")]
    [SerializeField] private bool invertFacing = false;

    [Header("Teleport VFX")]
    [SerializeField] private GameObject teleportVFXPrefab;
    [Tooltip("Hiệu ứng tại chỗ Boss rời đi")]
    [SerializeField] private bool vfxAtOldPosition = true;
    [Tooltip("Hiệu ứng tại chỗ Boss xuất hiện")]
    [SerializeField] private bool vfxAtNewPosition = true;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;
    [SerializeField] private float vfxDestroyTime = 1.5f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack1";
    [Tooltip("Chạy trigger Attack1 mỗi lần dịch chuyển")]
    [SerializeField] private bool playAttackOnTeleport = true;

    private int attackHash;

    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Drop")]
    [Tooltip("Vị trí enemy xuất hiện (nòng bắn / miệng Boss)")]
    [SerializeField] private Transform dropPoint;

    [Header("Shake khi tấn công")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Boss HP")]
    [Tooltip("Image kiểu Filled (Image Type = Filled, Fill Method = Horizontal)")]
    [SerializeField] private Image hpFill;
    [SerializeField] private float hpSmoothSpeed = 8f;
    [Tooltip("Rung mạnh khi Boss bị hạ")]
    [SerializeField] private float defeatShakeDuration = 0.6f;
    [SerializeField] private float defeatShakeMagnitude = 0.4f;

    public static event Action AnyBossDefeated;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsDefeated { get; private set; }

    private float displayedFill = 1f;
    private bool atA;
    private float teleportTimer;
    private Vector3 basePosition;

    private void Start()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (cameraShake == null && Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();

        attackHash = Animator.StringToHash(attackTrigger);

        atA = startAtA;
        SnapToCurrentPoint(false); // lần đầu vào scene: không chạy hiệu ứng
    }

    private void Update()
    {
        // Tự dịch chuyển theo thời gian
        if (autoTeleport && !IsDefeated)
        {
            teleportTimer += Time.deltaTime;

            if (teleportTimer >= teleportInterval)
                TeleportToOtherPoint();
        }

        // Chỉ nhấp nhô nhẹ tại chỗ, không di chuyển
        Vector3 pos = basePosition;
        pos.y += Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = pos;

        if (hpFill != null && MaxHp > 0)
        {
            float target = (float)CurrentHp / MaxHp;
            displayedFill = Mathf.MoveTowards(displayedFill, target, hpSmoothSpeed * Time.deltaTime);
            hpFill.fillAmount = displayedFill;
        }
    }

    // =========================================
    // TELEPORT
    // =========================================

    private void UpdateFacing()
    {
        if (spriteRenderer == null)
            return;

        bool lookRight;

        if (faceTarget != null)
        {
            // Quay về phía Player
            lookRight = faceTarget.position.x > basePosition.x;
        }
        else
        {
            // Không có faceTarget: đứng ở A (bên trái) thì nhìn sang phải, ở B thì nhìn sang trái
            lookRight = atA;
        }

        if (invertFacing)
            lookRight = !lookRight;

        // Sprite gốc quay phải: nhìn phải thì không lật, nhìn trái thì lật
        // Sprite gốc quay trái: ngược lại
        spriteRenderer.flipX = spriteFacesRight ? !lookRight : lookRight;
    }

    // =========================================
    // DROP / SHAKE
    // =========================================

    public Vector3 GetDropPosition()
    {
        return dropPoint != null ? dropPoint.position : transform.position;
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
    // HP
    // =========================================

    // Spawner gọi khi bắt đầu bài: máu tối đa = số nốt
    public void InitHp(int maxHp)
    {
        MaxHp = Mathf.Max(1, maxHp);
        CurrentHp = MaxHp;
        IsDefeated = false;
        displayedFill = 1f;

        if (hpFill != null)
            hpFill.fillAmount = 1f;
    }

    // Spawner gọi mỗi lần người chơi đánh trúng 1 enemy
    public void TakeHit()
    {
        if (IsDefeated) return;

        CurrentHp = Mathf.Max(0, CurrentHp - 1);

        if (CurrentHp <= 0)
        {
            IsDefeated = true;
            Shake(defeatShakeDuration, defeatShakeMagnitude);
            AnyBossDefeated?.Invoke();
        }
    }

    public void TeleportToOtherPoint()
    {
        atA = !atA;
        SnapToCurrentPoint(true);
    }

    public void TeleportTo(bool toPointA)
    {
        // Đang ở đúng điểm đó rồi thì không dịch chuyển, không chạy hiệu ứng
        if (atA == toPointA)
            return;

        atA = toPointA;
        SnapToCurrentPoint(true);
    }

    private void SnapToCurrentPoint(bool playEffects)
    {
        teleportTimer = 0f;

        Vector3 oldPosition = transform.position;

        Transform target = atA ? pointA : pointB;
        basePosition = target != null ? target.position : transform.position;

        transform.position = basePosition;
        UpdateFacing();

        if (!playEffects)
            return;

        if (vfxAtOldPosition)
            SpawnTeleportVFX(oldPosition);

        if (vfxAtNewPosition)
            SpawnTeleportVFX(basePosition);

        if (playAttackOnTeleport)
            PlayAttack();
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

    public void PlayAttack()
    {
        if (animator == null)
            return;

        // Reset trước để các lần dịch chuyển sát nhau vẫn replay được animation
        animator.ResetTrigger(attackHash);
        animator.SetTrigger(attackHash);
    }
}