
using UnityEngine;

public class BossProjectile : MonoBehaviour
{
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
    private bool completed;
    private bool initialized;

    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;

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

        // Ưu tiên thời gian thực còn lại đến nốt MIDI.
        float remaining = targetTime - launchSongTime;

        travelTime = remaining > 0f
            ? remaining
            : Mathf.Max(0.01f, duration);

        transform.position = startPosition;

        completed = false;
        playerOnKey = false;
        initialized = true;

        // Đăng ký để PianoKeySpawner quản lý vòng đời projectile.
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

        // Tính vị trí dựa theo thời gian bài nhạc.
        float progress = Mathf.Clamp01(
            (now - launchSongTime) / travelTime);

        transform.position = Vector3.Lerp(
            startPosition,
            targetPosition,
            progress);

        // Nhặt đạn bằng input đúng lane.
        if (playerOnKey && HoldInput.WasPressed(Lane))
        {
            Complete();
            return;
        }

        // Bỏ lỡ nếu đã qua thời điểm đánh nốt.
        if (now > targetTime + missTolerance)
            Miss();
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

        Debug.Log(
            $"Projectile lane: {Lane} | " +
            $"HitPoint: {point.name} | " +
            $"Inside: {inside}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Wall"))
            Miss();
    }

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

        Destroy(gameObject);
    }
}
