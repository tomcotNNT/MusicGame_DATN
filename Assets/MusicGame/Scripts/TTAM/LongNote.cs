using System.Collections.Generic;
using UnityEngine;

public class LongNote : MonoBehaviour
{
    private enum State
    {
        Waiting,
        Holding,
        Completed,
        Missed,
        Broken
    }

    [Header("Timing")]
    [Tooltip("Quá noteTime bao nhiêu giây mà chưa giữ được thì Miss")]
    [SerializeField] private float hitWindow = 0.22f;
    [SerializeField] private float releaseGrace = 0.12f;
    [SerializeField] private float endTolerance = 0.1f;

    [Header("VFX")]
    [Tooltip("Prefab VFX loop (ParticleSystem) chạy suốt lúc giữ nốt")]
    [SerializeField] private GameObject holdVfxPrefab;

    [Tooltip("VFX nổ 1 lần khi hoàn thành nốt (tuỳ chọn)")]
    [SerializeField] private GameObject completeVfxPrefab;

    [SerializeField] private float completeVfxLifetime = 1f;

    [Tooltip("Offset so với vị trí Player")]
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    private GameObject holdVfx;
    private ParticleSystem[] holdVfxParticles;

    [Header("Score")]
    [SerializeField] private int startScore = 10;
    [SerializeField] private int tickScore = 10;
    [SerializeField] private float tickInterval = 0.1f;
    [SerializeField] private int completeBonus = 100;

    [Header("Visual")]
    [SerializeField] private Transform body;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer headRenderer;

    [Header("Body Layout")]
    [Tooltip("Khoảng cách (unit thế giới) từ tâm Head tới điểm bắt đầu Body. " +
             "Hở thì giảm (có thể âm), chồng lên Head thì tăng.")]
    [SerializeField] private float bodyStartOffset = 0f;

    [Header("Ride Player")]
    [Tooltip("Giữ nốt TRÊN (Right): Player lướt lên và bám theo nốt đến hết, rồi rơi xuống đất.")]
    [SerializeField] private bool ridePlayer = true;
    [Tooltip("Player cao hơn/thấp hơn điểm nốt bao nhiêu khi bám.")]
    [SerializeField] private float rideYOffset = 0f;

    [Header("Head Sprites")]
    [SerializeField] private Sprite leftHeadSprite;
    [SerializeField] private Sprite rightHeadSprite;

    [Header("Body Sprites")]
    [SerializeField] private Sprite leftBodySprite;
    [SerializeField] private Sprite rightBodySprite;

    private float fullLength;

    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;
    private static PlayerController playerCtrl;

    private static int activeHoldCount = 0;
    private bool registeredHold = false;
    private bool riding;

    // true khi đầu nốt đang chồng lên hit point CÙNG lane
    private bool onHitPoint;

    // Giữ lại cho TutorialManager (không còn ảnh hưởng vì đã bỏ cơ chế bám nốt)
    private bool tutorialAssist;
    public void SetTutorialAssist(bool value) { tutorialAssist = value; }
    public bool IsWaiting() { return state == State.Waiting; }

    // Danh sách nốt dài đang tồn tại (để KeyButtonController biết lane nào đang bận).
    public static readonly List<LongNote> Active = new List<LongNote>();

    private Lanee lane;

    private float noteTime;
    private float endTime;
    private float duration;

    private float speed;
    private float playerX;
    private float dir;

    private PianoKeySpawner spawner;

    private State state = State.Waiting;

    private float lastHeldTime;
    private float nextTickTime;

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
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);


        UnregisterHold();
        StopRide();
        StopHoldVfx();
    }

    // =========================================================
    // HIT POINT (HitPoint.cs gọi vào đây)
    // =========================================================

    public void SetHitPoint(HitPoint hitPoint, bool inside)
    {
        // Chỉ hit point cùng type mới tính
        if (hitPoint.Lane != lane)
            return;

        onHitPoint = inside;
    }

    // =========================================================
    // RIDE PLAYER
    // =========================================================

    private void TryRide()
    {
        if (!ridePlayer || playerCtrl == null)
            return;

        riding = true;

        // Cho Player di chuyển lên và bám theo nốt dài.
        playerCtrl.AttachToLongNote(transform, rideYOffset);
    }

    private void StopRide()
    {
        if (!riding)
            return;

        riding = false;

        if (playerCtrl != null)
            playerCtrl.DetachFromLongNote(transform);
    }

    private void RegisterHold()
    {
        if (registeredHold)
            return;

        registeredHold = true;
        activeHoldCount++;

        if (activeHoldCount == 1 && playerCtrl != null)
            playerCtrl.StartHoldAttack();
    }

    private void UnregisterHold()
    {
        if (!registeredHold)
            return;

        registeredHold = false;
        activeHoldCount = Mathf.Max(0, activeHoldCount - 1);

        // Chỉ tắt animation khi không còn nốt hold nào đang chạy.
        if (activeHoldCount == 0 && playerCtrl != null)
            playerCtrl.StopHoldAttack();
    }

    // =========================================================
    // LANE BUSY
    // true nếu lane đang giữ nốt dài, hoặc nốt dài sắp/đang tới hit point.
    // =========================================================

    public static bool LaneBusy(Lanee lane, float songTime)
    {
        foreach (LongNote n in Active)
        {
            if (n == null || n.lane != lane)
                continue;

            if (n.state == State.Holding)
                return true;

            if (n.state == State.Waiting &&
                (n.onHitPoint ||
                 Mathf.Abs(songTime - n.noteTime) <= n.hitWindow * 2f))
                return true;
        }

        return false;
    }

    // =========================================================
    // INITIALIZE
    // =========================================================

    public void Init(
        Lanee lane,
        float noteTime,
        float duration,
        float speed,
        float playerX,
        float dir,
        PianoKeySpawner spawner)
    {
        this.lane = lane;
        this.noteTime = noteTime;
        this.duration = duration;
        this.endTime = noteTime + duration;
        this.speed = speed;
        this.playerX = playerX; // X của hit point (spawner truyền vào)
        this.dir = dir;
        this.spawner = spawner;

        state = State.Waiting;
        onHitPoint = false;

        fullLength = speed * duration;

        UpdateHeadSprite();
        UpdateBodySprite();

        SetBody(fullLength);
    }

    private void UpdateBodySprite()
    {
        if (bodyRenderer == null)
        {
            Debug.LogWarning("LongNote: Chưa gán Body Renderer!", this);
            return;
        }

        Sprite targetSprite = lane == Lanee.Left
            ? leftBodySprite
            : rightBodySprite;

        if (targetSprite != null)
            bodyRenderer.sprite = targetSprite;
        else
            Debug.LogWarning($"LongNote: Chưa gán body sprite cho lane {lane}!", this);
    }

    private void UpdateHeadSprite()
    {
        if (headRenderer == null)
        {
            Debug.LogWarning("LongNote: Chưa gán Head Renderer!", this);
            return;
        }

        Sprite targetSprite = lane == Lanee.Left
            ? leftHeadSprite
            : rightHeadSprite;

        if (targetSprite != null)
            headRenderer.sprite = targetSprite;
        else
            Debug.LogWarning($"LongNote: Chưa gán sprite cho lane {lane}!", this);
    }

    // =========================================================
    // BODY
    // =========================================================

    private void SetBody(float length)
    {
        if (body == null)
            return;

        length = Mathf.Max(0f, length);

        // Đổi từ đơn vị thế giới sang đơn vị local của parent.
        float parentScale = body.parent != null
            ? Mathf.Max(0.0001f, Mathf.Abs(body.parent.lossyScale.x))
            : 1f;

        float localLength = length / parentScale;
        float localOffset = bodyStartOffset / parentScale;

        // Độ rộng sprite (unit) và độ lệch pivot.
        float spriteWidth = 1f;
        float pivotShift = 0f;

        if (bodyRenderer != null && bodyRenderer.sprite != null)
        {
            Bounds b = bodyRenderer.sprite.bounds;
            spriteWidth = Mathf.Max(0.0001f, b.size.x);
            pivotShift = b.center.x; // 0 nếu pivot ở giữa
        }

        float scaleX = localLength / spriteWidth;

        Vector3 scale = body.localScale;
        scale.x = scaleX;
        body.localScale = scale;

        // Tâm body nằm giữa đoạn [offset, offset + length], bù pivot.
        float center = dir * (localOffset + localLength * 0.5f);

        Vector3 position = body.localPosition;
        position.x = center - pivotShift * scaleX;
        body.localPosition = position;
    }

    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (spawner == null)
            return;

        float songTime = spawner.SongTime;

        UpdatePosition(songTime);
        UpdateState(songTime);
        UpdateHoldVfx();

        if (songTime > endTime + 1f)
            Destroy(gameObject);
    }

    private void UpdatePosition(float songTime)
    {
        Vector3 position = transform.position;
        position.x = playerX + dir * speed * (noteTime - songTime);
        transform.position = position;
    }

    // =========================================================
    // STATE
    // =========================================================

    private void UpdateState(float songTime)
    {
        bool autoHold = ItemDurationSystem.Instance != null && ItemDurationSystem.Instance.IsActive("Fever");
        bool held = autoHold || HoldInput.IsHeld(lane);

        switch (state)
        {
            case State.Waiting:
                HandleWaiting(songTime, held);
                break;

            case State.Holding:
                HandleHolding(songTime, held);
                break;
        }
    }

    private void HandleWaiting(float songTime, bool held)
    {
        // Đầu nốt chồng lên hit point cùng type + đang giữ đúng nút => bắt đầu giữ, tính điểm
        if (held && onHitPoint)
        {
            StartHold(songTime);
            return;
        }

        // Đã qua hit point mà không giữ được
        if (songTime > noteTime + hitWindow)
            Miss();
    }

    private void StartHold(float songTime)
    {
        state = State.Holding;

        lastHeldTime = songTime;
        nextTickTime = songTime + tickInterval;

        // Nốt TRÊN: Player lướt lên và bám theo nốt đến hết
        if (lane == Lanee.Right)
            TryRide();

        RegisterHold();

        StartHoldVfx();

        if (scoreSystem != null)
            scoreSystem.AddScore(startScore);

        if (comboSystem != null)
            comboSystem.HitNote();

        TutorialEvents.LongNoteStarted?.Invoke();
    }

    private void HandleHolding(float songTime, bool held)
    {
        if (held)
            lastHeldTime = songTime;

        while (songTime >= nextTickTime && nextTickTime <= endTime)
        {
            bool stillHolding = songTime - lastHeldTime <= releaseGrace;

            if (stillHolding && scoreSystem != null)
                scoreSystem.AddScore(tickScore);

            nextTickTime += tickInterval;
        }

        float releaseTime = songTime - lastHeldTime;

        if (releaseTime > releaseGrace)
        {
            if (songTime >= endTime - endTolerance)
                Complete();
            else
                Break();

            return;
        }

        if (songTime >= endTime)
            Complete();
    }

    // =========================================================
    // MISS / COMPLETE / BREAK
    // =========================================================

    private void Miss()
    {
        if (state == State.Missed ||
            state == State.Completed ||
            state == State.Broken)
            return;

        state = State.Missed;

        UnregisterHold();
        StopHoldAttackSafe();
        StopRide();
        StopHoldVfx();
    }

    private void Complete()
    {
        if (state == State.Completed ||
            state == State.Missed ||
            state == State.Broken)
            return;

        state = State.Completed;

        UnregisterHold();
        StopRide();
        StopHoldVfx();
        PlayCompleteVfx();

        TutorialEvents.LongNoteCompleted?.Invoke();

        if (scoreSystem != null)
            scoreSystem.AddScore(completeBonus);

        if (FeverSystem.Instance != null)
            FeverSystem.Instance.AddNote();

        Destroy(gameObject);
    }

    private void Break()
    {
        if (state == State.Broken ||
            state == State.Completed ||
            state == State.Missed)
            return;

        state = State.Broken;

        UnregisterHold();
        StopHoldAttackSafe();
        StopRide();
        StopHoldVfx();
    }

    private void StopHoldAttackSafe()
    {
        if (playerCtrl != null)
            playerCtrl.StopHoldAttack();
    }

    // Vị trí VFX: bám Player nếu có, không thì bám đầu nốt
    private Vector3 GetVfxPosition()
    {
        Vector3 basePos = playerCtrl != null
            ? playerCtrl.transform.position
            : transform.position;

        return basePos + vfxOffset;
    }

    private void StartHoldVfx()
    {
        if (holdVfxPrefab == null || holdVfx != null)
            return;

        holdVfx = Instantiate(holdVfxPrefab, GetVfxPosition(), Quaternion.identity);
        holdVfxParticles = holdVfx.GetComponentsInChildren<ParticleSystem>();
    }

    private void UpdateHoldVfx()
    {
        if (holdVfx == null)
            return;

        holdVfx.transform.position = GetVfxPosition();
    }

    // Ngừng phát nhưng để hạt cũ tan dần rồi mới hủy
    private void StopHoldVfx()
    {
        if (holdVfx == null)
            return;

        if (holdVfxParticles != null)
        {
            foreach (var ps in holdVfxParticles)
                if (ps != null)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        Destroy(holdVfx, 1.5f);
        holdVfx = null;
        holdVfxParticles = null;
    }

    private void PlayCompleteVfx()
    {
        if (completeVfxPrefab == null)
            return;

        GameObject vfx = Instantiate(completeVfxPrefab, GetVfxPosition(), Quaternion.identity);
        Destroy(vfx, completeVfxLifetime);
    }

    // =========================================================
    // GETTERS
    // =========================================================

    public bool IsHolding() => state == State.Holding;
    public bool IsCompleted() => state == State.Completed;
    public bool IsBroken() => state == State.Broken;
    public float GetNoteTime() => noteTime;
    public float GetEndTime() => endTime;
    public float GetDuration() => duration;
    public Lanee GetLane() => lane;
}