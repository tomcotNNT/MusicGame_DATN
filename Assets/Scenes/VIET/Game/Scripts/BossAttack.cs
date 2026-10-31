
using System.Collections.Generic;
using UnityEngine;

public class BossMusicAttack : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;
    [SerializeField] private BossProjectile projectilePrefab;
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Chọn nốt để ra đòn")]
    [SerializeField, Min(1)] private int attackEveryN = 4;
    [SerializeField] private bool onlyLongNotes;
    [SerializeField, Min(0f)] private float longNoteMinDuration = 0.5f;
    [SerializeField, Min(0f)] private float minInterval = 0.8f;

    [Header("Đạn")]
    [SerializeField, Min(0.1f)] private float travelTime = 0.9f;

    private struct AttackRequest
    {
        public float hitTime;
        public bool isLeft;

        public AttackRequest(float time, bool left)
        {
            hitTime = time;
            isLeft = left;
        }
    }

    private readonly Queue<AttackRequest> queue =
        new Queue<AttackRequest>();

    private int acceptedNoteCount;
    private float lastAttackNoteTime = -999f;

    private bool pendingShot;
    private float pendingHitTime;
    private bool pendingIsLeft;

    public bool TryScheduleBossNote(
        float noteTime,
        float noteDuration,
        bool isLeft)
    {
        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null ||
            !spawner.IsPlaying ||
            spawner.IsPaused ||
            spawner.IsTutorial)
        {
            return false;
        }

        acceptedNoteCount++;

        bool shouldAttack = onlyLongNotes
            ? noteDuration >= longNoteMinDuration
            : acceptedNoteCount % Mathf.Max(1, attackEveryN) == 0;

        if (!shouldAttack)
            return false;

        if (noteTime - lastAttackNoteTime < minInterval)
            return false;

        if (projectilePrefab == null || firePoint == null)
            return false;

        if (spawner.GetBossHitPoint(isLeft) == null)
            return false;

        lastAttackNoteTime = noteTime;

        queue.Enqueue(new AttackRequest(noteTime, isLeft));

        return true;
    }

    private void Update()
    {
        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null ||
            !spawner.IsPlaying ||
            spawner.IsPaused ||
            spawner.IsTutorial ||
            pendingShot ||
            queue.Count == 0)
        {
            return;
        }

        AttackRequest request = queue.Peek();

        // Bắt đầu animation trước thời điểm hitTime.
        if (spawner.SongTime < request.hitTime - travelTime)
            return;

        queue.Dequeue();

        pendingHitTime = request.hitTime;
        pendingIsLeft = request.isLeft;
        pendingShot = true;

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
        else
        {
            // Không có Animator thì bắn trực tiếp.
            AnimationEvent_FireProjectile();
        }
    }

    // Gắn vào Animation Event tại frame bắn đạn.
    public void AnimationEvent_FireProjectile()
    {
        if (!pendingShot)
            return;

        pendingShot = false;

        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null || firePoint == null ||
            projectilePrefab == null)
        {
            return;
        }

        Transform target = spawner.GetBossHitPoint(pendingIsLeft);

        if (target == null)
            return;

        // Tính thời gian còn lại để đạn vẫn đến đúng nhịp MIDI,
        // kể cả Animation Event xảy ra sau lúc animation bắt đầu.
        float remainingTravel =
            pendingHitTime - spawner.SongTime;

        if (remainingTravel <= 0f)
        {
            Debug.LogWarning(
                "Boss projectile bắn quá trễ. " +
                "Hãy đưa Animation Event lên sớm hơn trong clip.");

            return;
        }

        BossProjectile projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity);

        projectile.Init(
            pendingIsLeft
                ? PianoKey.KeyType.Left
                : PianoKey.KeyType.Right,
            firePoint.position,
            target.position,
            pendingHitTime,
            remainingTravel);
    }

    public void CancelPendingAttack()
    {
        pendingShot = false;
        queue.Clear();
    }

    public void ResetAttackState()
    {
        CancelPendingAttack();

        acceptedNoteCount = 0;
        lastAttackNoteTime = -999f;
    }
}
