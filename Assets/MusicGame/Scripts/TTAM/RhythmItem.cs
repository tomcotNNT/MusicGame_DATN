using UnityEngine;
using UnityEngine.Events;

// Item xen kẽ với PianoKey: bay zigzag tới hit point, bấm đúng lane khi item nằm trên hit point để nhặt.
public class RhythmItemm : MonoBehaviour
{
    [Header("Zigzag")]
    [Tooltip("Biên độ lên xuống (unit)")]
    [SerializeField] private float amplitude = 1.2f;
    [Tooltip("Quãng đường (unit) cho 1 chu kỳ lên-xuống")]
    [SerializeField] private float wavelength = 3f;
    [Tooltip("Cách hit point bao xa (unit) thì zigzag thu nhỏ dần về 0 để vào đúng hit point")]
    [SerializeField] private float settleDistance = 2.5f;

    [Header("Hit point")]
    [Tooltip("Bật = tự nhặt khi chạm hit point. Tắt = phải bấm đúng lane như PianoKey")]
    [SerializeField] private bool autoCollect = false;
    [Tooltip("Vùng trúng quanh hit point (unit). Nên bằng nửa chiều rộng collider của HitPoint")]
    [SerializeField] private float hitWindow = 0.8f;
    [Tooltip("Bay qua hit point thêm bao nhiêu unit thì tự hủy")]
    [SerializeField] private float destroyAfterDistance = 8f;

    [Header("Khi nhặt")]
    [SerializeField] private bool addScore = false;
    [SerializeField] private bool addCombo = false;
    [Tooltip("Giống PianoKey: nốt trên (Right) => Player lướt lên, nốt dưới (Left) => rơi xuống")]
    [SerializeField] private bool movePlayer = false;
    [SerializeField] private GameObject collectVfxPrefab;
    [SerializeField] private float vfxLifetime = 1f;
    [SerializeField] private AudioClip collectClip;
    [Range(0f, 1f)] [SerializeField] private float collectVolume = 1f;
    [Tooltip("Gán hiệu ứng riêng: hồi máu, buff, tăng fever...")]
    [SerializeField] private UnityEvent onCollected;

    [Header("Khi bỏ lỡ")]
    [Tooltip("Bật = bỏ lỡ item làm mất combo như nốt thường")]
    [SerializeField] private bool missBreaksCombo = false;
    [SerializeField] private UnityEvent onMissed;

    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;
    private static PlayerController playerCtrl;

    private bool isLeft;
    private float targetTime;
    private float moveSpeed;
    private float hitX;
    private float hitY;
    private float direction;   // +1: bay từ phải sang trái, -1: ngược lại

    public bool IsCompleted { get; private set; }
    private Lanee Lane => isLeft ? Lanee.Left : Lanee.Right;

    private void Awake()
    {
        if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
        if (comboSystem == null) comboSystem = FindFirstObjectByType<ComboSystem>();
        if (playerCtrl == null) playerCtrl = FindFirstObjectByType<PlayerController>();
    }

    // Giống PianoKey.Setup: hitPoint là hit point của lane
    public void Setup(bool left, float time, Transform hitPoint, float speed)
    {
        isLeft = left;
        targetTime = time;
        moveSpeed = speed;
        hitX = hitPoint.position.x;
        hitY = hitPoint.position.y;
        direction = transform.position.x >= hitX ? 1f : -1f;
        IsCompleted = false;
    }

    private void Update()
    {
        if (PianoKeySpawner.Instance == null)
            return;

        // Vị trí tính từ SongTime => khớp nhạc, tự đứng yên khi pause
        float remaining = targetTime - PianoKeySpawner.Instance.SongTime;
        float distToHit = moveSpeed * remaining;           // >0: chưa tới, <0: đã qua

        Vector3 pos = transform.position;
        pos.x = hitX + direction * distToHit;

        float ahead = Mathf.Max(distToHit, 0f);
        float fade = Mathf.Clamp01(ahead / Mathf.Max(settleDistance, 0.01f));
        float phase = ahead / Mathf.Max(wavelength, 0.01f) * Mathf.PI * 2f;
        pos.y = hitY + Mathf.Sin(phase) * amplitude * fade;

        transform.position = pos;

        if (!IsCompleted)
        {
            bool onHitPoint = Mathf.Abs(distToHit) <= hitWindow;

            if (onHitPoint && (autoCollect || HoldInput.WasPressed(Lane)))
            {
                Collect();
                return;
            }

            if (distToHit < -hitWindow)
                Miss();
        }

        if (distToHit < -destroyAfterDistance)
            Destroy(gameObject);
    }

    private void Collect()
    {
        if (IsCompleted) return;
        IsCompleted = true;

        if (collectVfxPrefab != null)
        {
            GameObject vfx = Instantiate(collectVfxPrefab, transform.position, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }

        if (collectClip != null)
        {
            Vector3 at = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(collectClip, at, collectVolume);
        }

        if (addScore && scoreSystem != null) scoreSystem.AddScore();
        if (addCombo && comboSystem != null) comboSystem.HitNote();

        if (playerCtrl != null)
        {
            if (movePlayer)
            {
                if (!isLeft) playerCtrl.DashUp(transform.position.y);
                else         playerCtrl.Drop();
            }
            playerCtrl.PlayAttack();
        }

        onCollected?.Invoke();
        Destroy(gameObject);
    }

    private void Miss()
    {
        if (IsCompleted) return;
        IsCompleted = true;

        if (missBreaksCombo && comboSystem != null)
            comboSystem.MissNote();

        onMissed?.Invoke();
        // Item vẫn bay tiếp ra khỏi màn hình rồi tự hủy ở Update
    }
}