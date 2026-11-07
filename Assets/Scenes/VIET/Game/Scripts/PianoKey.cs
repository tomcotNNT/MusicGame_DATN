using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class PianoKey : MonoBehaviour
{
    public enum KeyType
    {
        Left,
        Right
    }

    public static List<PianoKey> ActiveKeys = new List<PianoKey>();
    public static event System.Action<KeyType> NoteHit;

    [Header("VFX")]
    [Tooltip("Prefab VFX (ParticleSystem) spawn khi hit đúng")]
    [SerializeField] private GameObject hitVfxPrefab;
    //vfx 2
    [SerializeField] private GameObject hitVfxPrefab2;

    [Tooltip("Thời gian tự hủy VFX sau khi spawn")]
    [SerializeField] private float vfxLifetime = 1f;

    [Tooltip("Offset so với vị trí key")]
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("SFX")]
    [Tooltip("Tiếng khi chạm nốt lane Left (ví dụ bass)")]
    [SerializeField] private AudioClip leftHitClip;

    [Tooltip("Tiếng khi chạm nốt lane Right")]
    [SerializeField] private AudioClip rightHitClip;

    [Range(0f, 1f)]
    [SerializeField] private float hitVolume = 1f;

    private static AudioSource sfxSource;


    [Header("Player")]
    [Tooltip("Trúng nốt TRÊN: Player lướt lên lane trên rồi rơi xuống. Trúng nốt DƯỚI: Player rơi xuống nếu đang lơ lửng")]
    [SerializeField] private bool movePlayer = true;

    [Header("Visual")]
    [SerializeField] private GameObject keyLeft;
    [SerializeField] private GameObject keyRight;

    [Header("Kick Effect")]
    [Tooltip("Bật = trúng nốt thì nốt bị đá văng ra thay vì biến mất ngay")]
    [SerializeField] private bool kickOnHit = true;
    [Tooltip("Vận tốc đá. X là hướng văng ra phía ngược với người chơi, Y là hướng lên")]
    [SerializeField] private Vector2 kickVelocity = new Vector2(6f, 7f);
    [Tooltip("Ngẫu nhiên thêm bớt vận tốc cho tự nhiên (0.2 = ±20%)")]
    [SerializeField, Range(0f, 0.5f)] private float kickRandomness = 0.2f;
    [SerializeField] private float kickGravity = 20f;
    [Tooltip("Tốc độ xoay (độ/giây)")]
    [SerializeField] private float kickSpin = 720f;
    [SerializeField] private float kickDuration = 0.6f;

    [Header("Random Sprite")]
    [Tooltip("Bật = mỗi nốt lấy 1 sprite ngẫu nhiên trong list")]
    [SerializeField] private bool randomSprite = true;
    [SerializeField] private List<Sprite> sprites = new List<Sprite>();
    [Tooltip("Bật = 2 nốt liên tiếp không trùng sprite")]
    [SerializeField] private bool avoidRepeat = true;

    private static int lastSpriteIndex = -1;

    private KeyType keyType;
    private BossItemBattleManager bossItemManager;
    private BossItemType? bossItemType;

    public void ConfigureBossItem(BossItemType type, BossItemBattleManager manager)
    {
        bossItemType = type;
        bossItemManager = manager;
    }
    private float targetTime;
    private float moveSpeed;

    // Đích của key: hit point của lane này
    private Transform player;
    private float hitX;        // vị trí X nơi key phải chạm đúng lúc targetTime
    private float direction;   // +1: key bay từ phải sang trái, -1: ngược lại

    // true khi key đang chồng lên hit point CÙNG lane
    public bool PlayerOnKey { get; private set; }
    public bool IsCompleted { get; private set; }

    // Cache dùng chung, tránh FindObjectOfType cho từng key
    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;
    private static PlayerController playerCtrl;

    private Lanee Lane => keyType == KeyType.Left ? Lanee.Left : Lanee.Right;

    private void Awake()
    {
        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();
        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();
        if (playerCtrl == null)
            playerCtrl = FindFirstObjectByType<PlayerController>();
    }

    private void OnEnable()
    {
        if (!ActiveKeys.Contains(this))
            ActiveKeys.Add(this);
    }

    private void OnDisable()
    {
        ActiveKeys.Remove(this);
    }

    // =====================================================
    // SETUP
    // =====================================================

    // playerTransform: truyền HIT POINT của lane (spawner đã làm vậy)
    public void Setup(
        KeyType type,
        float time,
        Transform playerTransform,
        float speed)
    {
        bossItemType = null;
        bossItemManager = null;
        keyType = type;
        targetTime = time;
        player = playerTransform;
        moveSpeed = speed;

        PlayerOnKey = false;
        IsCompleted = false;

        hitX = player.position.x;
        direction = transform.position.x >= hitX ? 1f : -1f;

        if (keyLeft != null)
            keyLeft.SetActive(type == KeyType.Left);

        if (keyRight != null)
            keyRight.SetActive(type == KeyType.Right);

        ApplyRandomSprite();      
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (IsCompleted)
            return;

        if (PianoKeySpawner.Instance == null)
            return;

        float songTime = PianoKeySpawner.Instance.SongTime;

        Move(songTime);
        CheckPress();
    }

    // Vị trí tính từ thời gian bài hát -> luôn khớp nhạc, không bị trôi.
    // Chỉ đổi X, Y giữ nguyên độ cao lúc spawn (= độ cao hit point).
    private void Move(float songTime)
    {
        float remaining = targetTime - songTime;

        Vector3 pos = transform.position;
        pos.x = hitX + direction * moveSpeed * remaining;
        transform.position = pos;
    }

    // Đang chồng lên hit point đúng lane + vừa bấm đúng nút => trúng
    private void CheckPress()
    {
        if (PlayerOnKey && HoldInput.WasPressed(Lane))
            Complete();
    }

    private void StartKick()
{
    // Tắt collider để không còn dính Wall / HitPoint nữa
    foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
        c.enabled = false;

    // Không còn là key đang chờ bấm
    PlayerOnKey = false;
    ActiveKeys.Remove(this);

    StartCoroutine(KickRoutine());
}

    private IEnumerator KickRoutine()
    {
        float rand = 1f + Random.Range(-kickRandomness, kickRandomness);

        // direction: +1 nghĩa là key đến từ bên phải => bị đá ngược về bên phải
        Vector2 vel = new Vector2(direction * kickVelocity.x, kickVelocity.y) * rand;
        float spin = kickSpin * (Random.value < 0.5f ? -1f : 1f) * rand;

        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        Color[] startColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            startColors[i] = renderers[i].color;

        Vector3 startScale = transform.localScale;
        float t = 0f;

        while (t < kickDuration)
        {
            float dt = Time.deltaTime;
            t += dt;
            float k = Mathf.Clamp01(t / kickDuration);

            vel.y -= kickGravity * dt;
            transform.position += (Vector3)(vel * dt);
            transform.Rotate(0f, 0f, spin * dt);
            transform.localScale = startScale * Mathf.Lerp(1f, 0.6f, k);

            // Mờ dần, bắt đầu mờ từ giữa hiệu ứng
            float alpha = 1f - Mathf.Clamp01((k - 0.4f) / 0.6f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                Color c = startColors[i];
                c.a = startColors[i].a * alpha;
                renderers[i].color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    // =====================================================
    // GET DATA
    // =====================================================

    public KeyType GetKeyType() => keyType;

    public float GetTargetTime() => targetTime;

    // =====================================================
    // HIT POINT (HitPoint.cs gọi vào đây)
    // =====================================================

    public void SetHitPoint(HitPoint hitPoint, bool inside)
    {
        // Chỉ hit point cùng type mới tính
        if (hitPoint.Lane != Lane)
            return;

        PlayerOnKey = inside;
    }

    private void ApplyRandomSprite()
    {
        if (!randomSprite || sprites == null || sprites.Count == 0)
            return;

        int index = Random.Range(0, sprites.Count);

        if (avoidRepeat && sprites.Count > 1)
        {
            while (index == lastSpriteIndex)
                index = Random.Range(0, sprites.Count);
        }
        lastSpriteIndex = index;

        if (sprites[index] == null)
            return;

        // Chỉ đổi sprite của visual đang bật (Left hoặc Right)
        GameObject visual = keyType == KeyType.Left ? keyLeft : keyRight;

        SpriteRenderer sr = visual != null
            ? visual.GetComponentInChildren<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
            sr.sprite = sprites[index];
    }

    // =====================================================
    // TRIGGERS
    // =====================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Đi qua hit point mà không bấm đúng => chạm Wall => Miss
        if (collision.CompareTag("Wall"))
            Miss();
    }

    // =====================================================
    // COMPLETE / MISS
    // =====================================================

    // KeyButtonController có gọi Complete() cũng không sao, đã có chặn IsCompleted
    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        SpawnHitVfx();
        PlayHitSfx();
        NoteHit?.Invoke(keyType);

        if (bossItemType.HasValue)
{
    if (bossItemManager != null)
        bossItemManager.Collect(bossItemType.Value);

    if (scoreSystem != null)
        scoreSystem.AddScore();
}
else
{
    if (scoreSystem != null) scoreSystem.AddScore();
    if (comboSystem != null) comboSystem.HitNote();
    if (FeverSystem.Instance != null)
        FeverSystem.Instance.AddNote();

    TutorialEvents.NormalNoteHit?.Invoke();
}

        // Trúng nốt trên => Player lướt lên rồi rơi xuống đất; nốt dưới => rơi xuống nếu đang lơ lửng
        if (playerCtrl != null)
        {
            if (movePlayer)
            {
                if (keyType == KeyType.Right)
                    playerCtrl.DashUp(transform.position.y);
                else
                    playerCtrl.Drop();
            }

            playerCtrl.PlayAttack();
        }

        if (kickOnHit)
            StartKick();
        else
            Destroy(gameObject);
    }

    private void SpawnHitVfx()
    {
        if (hitVfxPrefab == null && hitVfxPrefab2 == null)
            return;

        GameObject vfx = hitVfxPrefab != null ? Instantiate(
            hitVfxPrefab,
            transform.position + vfxOffset,
            Quaternion.identity) : null;

        //vfx 2
        if (hitVfxPrefab2 != null)
        {
            GameObject vfx2 = Instantiate(
                hitVfxPrefab2,
                transform.position + vfxOffset,
                Quaternion.identity);
            Destroy(vfx2, vfxLifetime);
        }    

        if (vfx != null) Destroy(vfx, vfxLifetime);
    }

    private void PlayHitSfx()
    {
        AudioClip clip = keyType == KeyType.Left ? leftHitClip : rightHitClip;

        if (clip == null)
            return;

        if (sfxSource == null)
        {
            GameObject go = new GameObject("PianoKeySfx");
            sfxSource = go.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;   // 2D, không bị nhỏ dần theo khoảng cách
        }

        sfxSource.PlayOneShot(clip, hitVolume);
    }

    private void Miss()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        // TODO: PlayerHealth.TakeDamage();
        if (comboSystem != null)
            comboSystem.MissNote();

        Destroy(gameObject);
    }
}