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

    [Header("Visual")]
    [SerializeField] private GameObject keyLeft;
    [SerializeField] private GameObject keyRight;

    [Header("Miss")]
    [Tooltip("Quá targetTime bao nhiêu giây thì tính Miss")]
    [SerializeField] private float missWindow = 0.5f;

    private KeyType keyType;
    private float targetTime;
    private float moveSpeed;

    private Transform player;
    private float hitX;        // vị trí X nơi key phải chạm đúng lúc targetTime
    private float direction;   // +1: key bay từ phải sang trái, -1: ngược lại

    public bool PlayerOnKey { get; private set; }
    public bool IsCompleted { get; private set; }

    // Cache dùng chung, tránh FindObjectOfType cho từng key
    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;
    private static FeverSystem feverSystem;

    private void Awake()
    {
        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();
        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();
        if (feverSystem == null)
            feverSystem = FindFirstObjectByType<FeverSystem>();
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
        CheckMiss(songTime);
    }

    // Vị trí tính từ thời gian bài hát -> luôn khớp nhạc, không bị trôi
    private void Move(float songTime)
    {
        float remaining = targetTime - songTime;

        Vector3 pos = transform.position;
        pos.x = hitX + direction * moveSpeed * remaining;
        transform.position = pos;
    }

    private void CheckMiss(float songTime)
    {
        if (songTime > targetTime + missWindow)
            Miss();
    }

    // =====================================================
    // GET DATA
    // =====================================================

    public KeyType GetKeyType() => keyType;

    public float GetTargetTime() => targetTime;

    // =====================================================
    // TRIGGERS
    // =====================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            PlayerOnKey = true;

        if (collision.CompareTag("Wall"))
            Miss();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            PlayerOnKey = false;
    }

    // =====================================================
    // COMPLETE / MISS
    // =====================================================

    public void Complete()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        if (scoreSystem != null) scoreSystem.AddScore();
        if (comboSystem != null) comboSystem.HitNote();
        if (feverSystem != null) feverSystem.AddFever();

        Destroy(gameObject);
    }

    private void Miss()
    {
        if (IsCompleted)
            return;

        IsCompleted = true;

        // TODO: comboSystem.Miss(); PlayerHealth.TakeDamage();

        Destroy(gameObject);
    }
}