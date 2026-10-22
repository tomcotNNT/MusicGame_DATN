using UnityEngine;
using System.Collections.Generic;

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

    private KeyType keyType;
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

        if (scoreSystem != null) scoreSystem.AddScore();
        if (comboSystem != null) comboSystem.HitNote();
        if (FeverSystem.Instance != null)
            FeverSystem.Instance.AddNote();

        TutorialEvents.NormalNoteHit?.Invoke();

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

        Destroy(gameObject);
    }

    private void SpawnHitVfx()
    {
        if (hitVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(
            hitVfxPrefab,
            transform.position + vfxOffset,
            Quaternion.identity);

        Destroy(vfx, vfxLifetime);
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