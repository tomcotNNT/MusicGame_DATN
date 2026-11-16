using System;
using UnityEngine;

public class NTVerticalNoteMan3 : MonoBehaviour
{
    [Header("Tùy chọn hiển thị đuôi nốt Hold")]
    [SerializeField] private Transform holdTailTransform;

    [HideInInspector] public bool isLeftLane;
    [HideInInspector] public Man3NoteType noteType;
    [HideInInspector] public float spawnTime;
    [HideInInspector] public float hitTime;
    [HideInInspector] public float holdDuration;
    [HideInInspector] public bool isBeingHeld = false;

    private Vector3 startPos;
    private Vector3 targetPos;
    private Vector3 initialScale;
    private float fallSpeed;
    private Func<float> getSongTime;
    private Action<NTVerticalNoteMan3> onNoteMissed;
    private bool isResolved = false;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    public void Init(
        bool left,
        Man3NoteType type,
        Vector3 spawn,
        Vector3 target,
        float sTime,
        float hTime,
        float holdDur,
        Func<float> timeProvider,
        Action<NTVerticalNoteMan3> missCallback)
    {
        isLeftLane = left;
        noteType = type;
        startPos = spawn;
        targetPos = target;
        spawnTime = sTime;
        hitTime = hTime;
        holdDuration = holdDur;
        getSongTime = timeProvider;
        onNoteMissed = missCallback;

        float flightTime = Mathf.Max(0.01f, hitTime - spawnTime);
        float totalDistance = Vector3.Distance(startPos, targetPos);
        fallSpeed = totalDistance / flightTime;

        transform.position = startPos;
        UpdateHoldTailVisual(holdDuration);
    }

    private void Update()
    {
        if (isResolved || getSongTime == null) return;

        float now = getSongTime();

        // Trường hợp 1: Nốt Hold đang được người chơi giữ ngón tay tại tâm vòng tròn
        if (noteType == Man3NoteType.Hold && isBeingHeld)
        {
            transform.position = targetPos;
            float remainingHold = (hitTime + holdDuration) - now;
            UpdateHoldTailVisual(Mathf.Max(0f, remainingHold));
            return;
        }

        // Trường hợp 2: Nốt đang rơi thẳng đứng từ trên xuống
        float flightTime = Mathf.Max(0.01f, hitTime - spawnTime);
        float t = (now - spawnTime) / flightTime;

        Vector3 direction = (targetPos - startPos).normalized;
        float totalDistance = Vector3.Distance(startPos, targetPos);
        transform.position = startPos + direction * (totalDistance * t);

        // Hiệu ứng xoay tròn lấp lánh dành riêng cho Chuỗi Xu (Fever Coins)
        if (noteType == Man3NoteType.Coin)
        {
            float spinX = Mathf.Cos(Time.time * 10f);
            Vector3 s = initialScale;
            s.x = initialScale.x * (0.25f + 0.75f * Mathf.Abs(spinX));
            transform.localScale = s;
        }

        // Kiểm tra nếu nốt trôi quá tâm quá 0.22s mà chưa bấm (Tính là Miss)
        if (now > hitTime + 0.22f)
        {
            isResolved = true;

            // Bom né được và Xu bỏ lỡ đều tự hủy êm ái, KHÔNG trừ máu hay đứt Combo
            if (noteType == Man3NoteType.Bomb || noteType == Man3NoteType.Coin)
            {
                Destroy(gameObject);
                return;
            }

            onNoteMissed?.Invoke(this);
            Destroy(gameObject);
        }
    }

    public void StartHold()
    {
        isBeingHeld = true;
        transform.position = targetPos;
    }

    private void UpdateHoldTailVisual(float remainingDuration)
    {
        if (holdTailTransform == null) return;
        float tailLength = remainingDuration * fallSpeed;
        Vector3 s = holdTailTransform.localScale;
        s.y = tailLength;
        holdTailTransform.localScale = s;
    }

    public void ConsumeNote()
    {
        isResolved = true;
        Destroy(gameObject);
    }
}