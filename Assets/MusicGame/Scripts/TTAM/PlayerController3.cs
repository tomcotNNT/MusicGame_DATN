using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControllerSub : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;

    [Header("Rhythm System")]
    [SerializeField] private SubmarineRhythmSpawner spawner;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Input")]
    [SerializeField] private KeyCode leftKey = KeyCode.A;
    [SerializeField] private KeyCode rightKey = KeyCode.D;

    [Header("Bấm trượt")]
    [Tooltip("Bật: bấm khi không có item nào trong vùng chạm thì mất combo")]
    [SerializeField] private bool missOnEmptyPress = false;

    [Header("Hit VFX")]
    [SerializeField] private GameObject hitVFXPrefab;
    [SerializeField] private float vfxDestroyTime = 1.5f;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Attack Sound")]
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioClip attackHitSound;

    [Header("Attack Animation")]
    [SerializeField] private string[] attackTriggers = { "Attack1", "Attack2", "Attack3" };

    [Header("Hurt Animation")]
    [Tooltip("Trigger chạy khi để lọt / hụt nốt")]
    [SerializeField] private string hurtTrigger = "Hurt";

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (spawner == null)
            spawner = FindFirstObjectByType<SubmarineRhythmSpawner>();

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnEnable()
    {
        // Cảm ứng: 2 nút LaneButton (Left / Right) trên màn hình, hỗ trợ bấm 2 nút cùng lúc
        LaneButtonInput.Pressed += Attack;

        // Hụt nốt (item lọt qua hit point) -> Hurt
        RhythmEvents.NoteMissed += OnNoteMissed;
    }

    private void OnDisable()
    {
        LaneButtonInput.Pressed -= Attack;
        RhythmEvents.NoteMissed -= OnNoteMissed;
    }

    private void OnNoteMissed(Vector3 position, bool isLeft)
    {
        if (animator == null || string.IsNullOrEmpty(hurtTrigger))
            return;

        AnimatorSafe.SetTrigger(animator, hurtTrigger);
    }

    private void Update()
    {
        // Bàn phím (test trong Editor)
        if (Input.GetKeyDown(leftKey))
            Attack(true);

        if (Input.GetKeyDown(rightKey))
            Attack(false);
    }

    // =========================================
    // ATTACK
    // =========================================

    private void Attack(bool isLeft)
    {
        // Chỉ chạy khi nhạc đang chạy
        if (spawner == null || !spawner.IsPlaying)
            return;

        // Không lật sprite khi đánh trái / phải

        // đánh trước, biết trúng/trượt rồi mới chọn animation
        bool hit = spawner.TryHit(isLeft, out Vector3 hitPos);
        PlayAttackAnimation(hit);

        if (hit)
        {
            if (showDebug)
                Debug.Log("[PLAYER] HIT " + (isLeft ? "LEFT" : "RIGHT"));

            PlayHitFeedback(hitPos);
        }
        else
        {
            if (showDebug)
                Debug.Log("[PLAYER] NO ITEM " + (isLeft ? "LEFT" : "RIGHT"));

            if (missOnEmptyPress && comboSystem != null)
                comboSystem.MissNote();
        }
    }

    // =========================================
    // ANIMATION
    // =========================================

    // Chọn đòn theo kết quả bắt beat (AttackPicker): trượt/Good = đòn nhẹ, Great = vừa,
    // Perfect = mạnh, mỗi 10 combo = chiêu kết. Controller cũ không có đòn mới thì dùng danh sách attackTriggers.
    private void PlayAttackAnimation(bool hit)
    {
        if (AttackPicker.Play(animator, hit, comboSystem))
            return;

        if (animator == null ||
            attackTriggers == null ||
            attackTriggers.Length == 0)
            return;

        int index = Random.Range(0, attackTriggers.Length);
        AnimatorSafe.SetTrigger(animator, attackTriggers[index]);
    }

    // =========================================
    // FEEDBACK
    // =========================================

    private void PlayHitFeedback(Vector3 position)
    {
        if (attackAudioSource != null && attackHitSound != null)
            if (AudioPrefs.SfxOn) attackAudioSource.PlayOneShot(attackHitSound);

        if (hitVFXPrefab != null)
        {
            GameObject vfx = Instantiate(hitVFXPrefab, position + vfxOffset, Quaternion.identity);
            Destroy(vfx, vfxDestroyTime);
        }
    }
}