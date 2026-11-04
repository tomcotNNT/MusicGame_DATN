using UnityEngine;

public class BossProjectile : MonoBehaviour
{
    public enum ProjectileKind { Item, Fire }

    [Header("Loại")]
    [Tooltip("Item = người chơi nhặt. Fire = người chơi né")]
    [SerializeField] private ProjectileKind kind = ProjectileKind.Item;
    private bool hitPlayer;

    [Header("Item (chỉ dùng khi Kind = Item)")]
    [Tooltip("Mỗi loại item một id riêng. Nhặt item khác id thì thanh fever tính lại từ đầu")]
    [SerializeField] private string itemId = "item_default";
    [SerializeField] private Sprite itemIcon;
    [Tooltip("Bỏ lỡ item thì reset luôn thanh fever của item")]
    [SerializeField] private bool resetItemBarOnMiss = false;

    [Header("Fire (chỉ dùng khi Kind = Fire)")]
    [Tooltip("Số máu mất khi dính fire (cần nối với hệ thống máu, xem TODO)")]
    [SerializeField] private int fireDamage = 1;
    [Tooltip("Lưới an toàn: sau targetTime bao nhiêu giây thì tự huỷ nếu chưa chạm tường")]
    [SerializeField, Min(0.5f)] private float fireMaxLifetime = 5f;

    [Header("Visual")]
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private float vfxLifetime = 1f;

    [Header("Timing")]
    [SerializeField] private float missTolerance = 0.12f;

    private PianoKey.KeyType keyType;

    private Lanee Lane =>
        keyType == PianoKey.KeyType.Left
            ? Lanee.Left
            : Lanee.Right;

    private Vector3 startPosition;
    private Vector3 targetPosition;

    private float targetTime;
    private float launchSongTime;
    private float travelTime;

    private bool playerOnKey;
    private bool completed;      // Item: đã nhặt/miss. Fire: đã bị dọn (chạm tường)
    private bool fireResolved;   // Fire: đã kiểm tra dính/né, đạn vẫn bay tiếp
    private bool initialized;

    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;

    public ProjectileKind Kind => kind;
    public string ItemId => itemId;

    private void Awake()
    {
        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }

    public void Init(
        PianoKey.KeyType type,
        Vector3 start,
        Vector3 target,
        float hitTime,
        float duration)
    {
        keyType = type;

        startPosition = start;
        targetPosition = target;
        targetTime = hitTime;

        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        launchSongTime = spawner != null
            ? spawner.SongTime
            : hitTime - duration;

        // Ưu tiên thời gian thực còn lại đến nốt MIDI
        float remaining = targetTime - launchSongTime;

        travelTime = remaining > 0f
            ? remaining
            : Mathf.Max(0.01f, duration);

        transform.position = startPosition;

        completed = false;
        fireResolved = false;
        playerOnKey = false;
        initialized = true;

        // Đăng ký để PianoKeySpawner quản lý vòng đời projectile
        if (spawner != null)
            spawner.RegisterActiveKey(gameObject);

        if (remaining <= 0f)
        {
            Debug.LogWarning(
                "BossProjectile: Đạn được bắn sau thời điểm nốt MIDI. " +
                "Hãy đặt Animation Event sớm hơn.",
                this);
        }
    }

    private void Update()
    {
        if (!initialized || completed)
            return;

        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null)
            return;

        if (!spawner.IsPlaying || spawner.IsPaused)
            return;

        float now = spawner.SongTime;

        // Vị trí theo thời gian bài nhạc (không clamp -> đạn đi xuyên qua hit point)
        float progress = (now - launchSongTime) / travelTime;

        transform.position = Vector3.LerpUnclamped(
            startPosition,
            targetPosition,
            progress);

        // ---------- FIRE: bay xuyên, chỉ kiểm tra dính/né một lần ----------
        if (kind == ProjectileKind.Fire)
        {
            if (!fireResolved && now >= targetTime)
                ResolveFire();   // chỉ kiểm tra dính hay không, KHÔNG Destroy

            // Lưới an toàn nếu thiếu tường ở phía xa của lane
            if (now > targetTime + fireMaxLifetime)
            {
                completed = true;
                Destroy(gameObject);
            }

            return;   // đạn vẫn tiếp tục bay, tường sẽ dọn nó
        }

        // ---------- ITEM: nhặt bằng input đúng lane ----------
        if (playerOnKey && HoldInput.WasPressed(Lane))
        {
            Complete();
            return;
        }

        // if (now > targetTime + missTolerance)
        //     Miss();
    }

    public void SetHitPoint(HitPoint point, bool inside)
    {
        if (point == null || completed)
            return;

        if (point.Lane != Lane)
        {
            if (inside)
            {
                Debug.LogWarning(
                    $"Sai lane! Projectile = {Lane}, " +
                    $"HitPoint = {point.Lane}, Object = {point.name}");
            }

            return;
        }

        playerOnKey = inside;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {

         if (kind == ProjectileKind.Fire && !hitPlayer && other.CompareTag("Player"))
        {
            hitPlayer = true;   // lửa bay xuyên nên chỉ tính một lần

            PlayerHealth health = other.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.Die();   // hoặc health.Die()

            if (hitVfxPrefab != null)
            {
                GameObject vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.identity);
                Destroy(vfx, vfxLifetime);
            }
            return;
        }


        if (!other.CompareTag("Wall"))
            return;

        // Chạm tường chỉ dọn rác, miss đã do Update xử lý theo timing
        if (kind == ProjectileKind.Fire)
        {
            completed = true;
            Destroy(gameObject);
        }
        else if (!completed)
        {
            Miss();   // chỉ còn là lưới an toàn khi đạn ra khỏi màn hình
        }
    }

    // ================= FIRE =================

    private void ResolveFire()
    {
        if (fireResolved)
            return;

        fireResolved = true;   // KHÔNG đặt completed, KHÔNG Destroy

        if (playerOnKey)
        {
            // Dính fire
            if (comboSystem != null)
                comboSystem.MissNote();

            // TODO: trừ máu player, ví dụ:
            // FindFirstObjectByType<PlayerController>()?.TakeDamage(fireDamage);

            // if (hitVfxPrefab != null)
            // {
            //     GameObject vfx = Instantiate(
            //         hitVfxPrefab,
            //         transform.position,
            //         Quaternion.identity);

            //     Destroy(vfx, vfxLifetime);
            // }
        }

        // Né được thì không bị phạt, đạn tiếp tục bay xuyên
    }

    // ================= ITEM =================

    private void Complete()
    {
        if (completed)
            return;

        completed = true;

        if (hitVfxPrefab != null)
        {
            GameObject vfx = Instantiate(
                hitVfxPrefab,
                transform.position,
                Quaternion.identity);

            Destroy(vfx, vfxLifetime);
        }

        if (scoreSystem != null)
            scoreSystem.AddScore();

        if (comboSystem != null)
            comboSystem.HitNote();

        if (FeverSystem.Instance != null)
            FeverSystem.Instance.AddNote();

        // Thanh fever riêng theo từng loại item
        if (ItemFeverBar.Instance != null)
            ItemFeverBar.Instance.AddItem(itemId, itemIcon);

        TutorialEvents.NormalNoteHit?.Invoke();

        PlayerController player =
            FindFirstObjectByType<PlayerController>();

        if (player != null)
        {
            if (keyType == PianoKey.KeyType.Right)
                player.DashUp(transform.position.y);
            else
                player.Drop();

            player.PlayAttack();
        }

        Destroy(gameObject);
    }

    private void Miss()
    {
        if (completed)
            return;

        completed = true;

        if (comboSystem != null)
            comboSystem.MissNote();

        if (resetItemBarOnMiss && ItemFeverBar.Instance != null)
            ItemFeverBar.Instance.ResetBar();

        Destroy(gameObject);
    }
}