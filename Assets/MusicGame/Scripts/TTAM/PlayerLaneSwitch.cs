using System;
using UnityEngine;

// Kiểu Muse Dash: trúng nốt dưới (Left) => Player xuống lane dưới,
// trúng nốt trên (Right) => Player lên lane trên. Không còn nhảy.
// Gắn vào GameObject Player.
public class PlayerLaneSwitcher : MonoBehaviour
{
    public static PlayerLaneSwitcher Instance { get; private set; }

    [Header("Lane Reference")]
    [Tooltip("Hit point dưới (Left)")]
    [SerializeField] private Transform hitPointBottom;
    [Tooltip("Hit point trên (Right)")]
    [SerializeField] private Transform hitPointTop;
    [Tooltip("Player cao hơn/thấp hơn hit point dưới bao nhiêu (unit). VD: pivot ở chân thì chỉnh cho đứng đúng mặt đường")]
    [SerializeField] private float bottomYOffset = 0f;
    [Tooltip("Player cao hơn/thấp hơn hit point trên bao nhiêu (unit)")]
    [SerializeField] private float topYOffset = 0f;

    [Header("Move")]
    [Tooltip("Tốc độ chuyển lane (unit/giây). Lớn = lướt nhanh, rất lớn = dịch chuyển tức thì")]
    [SerializeField] private float switchSpeed = 40f;
    [SerializeField] private Lanee startLane = Lanee.Left;

    [Header("Physics")]
    [Tooltip("Tắt trọng lực: Rigidbody2D của Player chuyển sang Kinematic để không rơi / không bị đẩy Y")]
    [SerializeField] private bool makeKinematic = true;

    public Lanee CurrentLane { get; private set; }

    // UI/Animator có thể lắng nghe để chạy animation lên/xuống
    public event Action<Lanee> LaneChanged;

    private float targetY;

    private void Awake()
    {
        Instance = this;

        if (makeKinematic && TryGetComponent(out Rigidbody2D rb))
            rb.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        // Đặt Player đúng lane ban đầu ngay lập tức
        CurrentLane = startLane;
        targetY = GetLaneY(startLane);

        Vector3 p = transform.position;
        p.y = targetY;
        transform.position = p;
    }

    private void Update()
    {
        Vector3 p = transform.position;

        if (Mathf.Approximately(p.y, targetY))
            return;

        p.y = Mathf.MoveTowards(p.y, targetY, switchSpeed * Time.deltaTime);
        transform.position = p;
    }

    // Gọi từ PianoKey / LongNote
    public static void GoTo(Lanee lane)
    {
        if (Instance != null)
            Instance.SwitchTo(lane);
    }

    public void SwitchTo(Lanee lane)
    {
        targetY = GetLaneY(lane);

        if (CurrentLane == lane)
            return;

        CurrentLane = lane;
        LaneChanged?.Invoke(lane);
    }

    private float GetLaneY(Lanee lane)
    {
        Transform hp = lane == Lanee.Left ? hitPointBottom : hitPointTop;

        if (hp == null)
        {
            Debug.LogWarning("PlayerLaneSwitcher: chưa gán hit point cho lane " + lane, this);
            return transform.position.y;
        }

        float offset = lane == Lanee.Left ? bottomYOffset : topYOffset;
        return hp.position.y + offset;
    }
}