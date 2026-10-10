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
    [SerializeField] private float hitWindow = 0.22f;
    [SerializeField] private float releaseGrace = 0.12f;
    [SerializeField] private float endTolerance = 0.1f;

    [Header("Score")]
    [SerializeField] private int startScore = 10;
    [SerializeField] private int tickScore = 10;
    [SerializeField] private float tickInterval = 0.1f;
    [SerializeField] private int completeBonus = 100;

    [Header("Visual")]
    [SerializeField] private Transform body;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer headRenderer;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color holdColor = Color.yellow;
    [SerializeField] private Color failColor = Color.gray;

    [Header("Body Layout")]
    [Tooltip("Khoảng cách (unit thế giới) từ tâm Head tới điểm bắt đầu Body. " +
             "Hở thì giảm (có thể âm), chồng lên Head thì tăng.")]
    [SerializeField] private float bodyStartOffset = 0f;

    [Header("Ride Player")]
    [Tooltip("Player đang nhảy và ở gần nốt khi bắt đầu giữ -> bám theo nốt đến hết.")]
    [SerializeField] private bool ridePlayer = true;
    [Tooltip("Chênh lệch độ cao tối đa (unit) giữa Player và nốt để tính là CHẠM nốt " +
             "(dùng cho cả bắt đầu giữ lẫn bám theo).")]
    [SerializeField] private float rideRange = 1.5f;
    [Tooltip("Player cao hơn/thấp hơn điểm nốt bao nhiêu khi bám.")]
    [SerializeField] private float rideYOffset = 0f;

    [Header("Head Sprites")]
    [SerializeField] private Sprite leftHeadSprite;
    [SerializeField] private Sprite rightHeadSprite;

    private float fullLength;

    private static ScoreSystem scoreSystem;
    private static ComboSystem comboSystem;
    private static FeverSystem feverSystem;
    private static PlayerLaneController playerCtrl;
    private bool riding;

    // Tutorial: bỏ điều kiện độ cao để người chơi mới vẫn giữ được nốt.
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
    private float breakTime;

    private void Awake()
    {
        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();

        if (comboSystem == null)
            comboSystem = FindFirstObjectByType<ComboSystem>();

        if (feverSystem == null)
            feverSystem = FindFirstObjectByType<FeverSystem>();

        if (playerCtrl == null)
            playerCtrl = FindFirstObjectByType<PlayerLaneController>();
    }

    private void OnEnable()
    {
        Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
        StopRide(); // bị Destroy giữa chừng cũng phải nhả Player
    }

    // =========================================================
    // RIDE PLAYER
    // =========================================================

    // Player có đang ở đúng độ cao của nốt (chạm nốt) không.
    private bool IsPlayerNear()
    {
        if (tutorialAssist || playerCtrl == null)
            return true; // tutorial hoặc không tìm thấy Player thì không chặn

        float dy = Mathf.Abs(
            playerCtrl.transform.position.y - (transform.position.y + rideYOffset)
        );

        return dy <= rideRange;
    }

    private void TryRide()
    {
        if (!ridePlayer || playerCtrl == null || playerCtrl.IsGround)
            return;

        if (!IsPlayerNear())
            return;

        playerCtrl.AttachToLongNote(transform, rideYOffset);
        riding = true;
    }

    private void StopRide()
    {
        if (!riding)
            return;

        riding = false;

        if (playerCtrl != null)
            playerCtrl.DetachFromLongNote(transform);
    }

    // =========================================================
    // LANE BUSY
    // true nếu lane đang giữ nốt dài, hoặc nốt dài sắp/đang tới Player.
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
                Mathf.Abs(songTime - n.noteTime) <= n.hitWindow * 2f)
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
        this.playerX = playerX;
        this.dir = dir;
        this.spawner = spawner;

        state = State.Waiting;

        fullLength = speed * duration;

        UpdateHeadSprite();

        SetBody(fullLength);
        SetColor(normalColor);
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

        if (songTime > endTime + 1f)
            Destroy(gameObject);
    }

    private void UpdatePosition(float songTime)
    {
        Vector3 position = transform.position;

        // Đang giữ: Head đứng tại Player, Body co dần.
        if (state == State.Holding)
        {
            position.x = playerX;
            transform.position = position;

            float remainingTime = Mathf.Max(0f, endTime - songTime);
            float remainingLength = Mathf.Min(speed * remainingTime, fullLength);

            SetBody(remainingLength);
            return;
        }

        // Thả sớm: nốt tiếp tục di chuyển.
        if (state == State.Broken)
        {
            position.x = playerX + dir * speed * (breakTime - songTime);
            transform.position = position;
            return;
        }

        // Đang chờ hoặc đã miss.
        position.x = playerX + dir * speed * (noteTime - songTime);
        transform.position = position;
    }

    // =========================================================
    // STATE
    // =========================================================

    private void UpdateState(float songTime)
    {
        bool autoHold = feverSystem != null && feverSystem.isFever;
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
        float timeDifference = Mathf.Abs(songTime - noteTime);

        // Vị trí nốt đã tính theo songTime nên chỉ cần đúng thời điểm + đang giữ nút.
        if (held && timeDifference <= hitWindow && IsPlayerNear())
        {
            StartHold(songTime);
            return;
        }

        if (songTime > noteTime + hitWindow)
            Miss();
    }

    private void StartHold(float songTime)
    {
        state = State.Holding;

        lastHeldTime = songTime;
        nextTickTime = songTime + tickInterval;

        SetColor(holdColor);
        TryRide();

        if (scoreSystem != null)
            scoreSystem.AddScore(startScore);

        if (comboSystem != null)
            comboSystem.HitNote();

        if (feverSystem != null)
            feverSystem.AddFever();
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
        if (state == State.Missed || state == State.Completed || state == State.Broken)
            return;

        state = State.Missed;
        SetColor(failColor);

        // Nếu ComboSystem có hàm Miss(), gọi tại đây.
    }

    private void Complete()
    {
        if (state == State.Completed || state == State.Missed || state == State.Broken)
            return;

        state = State.Completed;
        StopRide();

        if (scoreSystem != null)
            scoreSystem.AddScore(completeBonus);

        if (feverSystem != null)
            feverSystem.AddFever();

        Destroy(gameObject);
    }

    private void Break()
    {
        if (state == State.Broken || state == State.Completed || state == State.Missed)
            return;

        state = State.Broken;
        breakTime = spawner.SongTime;
        StopRide();

        SetColor(failColor);
    }

    private void SetColor(Color color)
    {
        if (bodyRenderer != null)
            bodyRenderer.color = color;
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