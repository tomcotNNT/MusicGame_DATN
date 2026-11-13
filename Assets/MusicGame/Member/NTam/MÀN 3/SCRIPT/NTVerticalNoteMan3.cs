using System;
using UnityEngine;

public enum Man3NoteType
{
    Normal, // Nốt đơn thường
    Hold,   // Nốt kéo dài (giữ ngón tay)
    Double, // Nốt đôi bấm 2 bên cùng lúc
    Bomb,   // Bẫy Bom (né bằng cách không bấm)
    Buff    // Nốt vàng kích hoạt x2 điểm
}

public class NTVerticalNoteMan3 : MonoBehaviour
{
    [Header("Tùy chọn hiển thị đuôi nốt Hold (Giai đoạn 3)")]
    [SerializeField] private Transform holdTailTransform;

    [HideInInspector] public bool isLeftLane;
    [HideInInspector] public Man3NoteType noteType;
    [HideInInspector] public float spawnTime;
    [HideInInspector] public float hitTime;
    [HideInInspector] public float holdDuration;
    [HideInInspector] public bool isBeingHeld = false;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float fallSpeed;
    private Func<float> getSongTime;
    private Action<NTVerticalNoteMan3> onNoteMissed;
    private bool isResolved = false;

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

        // Kiểm tra nếu nốt trôi quá tâm quá 0.22s mà chưa bấm
        if (now > hitTime + 0.22f)
        {
            isResolved = true;

            if (noteType == Man3NoteType.Bomb)
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