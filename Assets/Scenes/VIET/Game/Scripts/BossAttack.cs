using System.Collections.Generic;
using UnityEngine;

public class BossMusicAttack : MonoBehaviour
{
    public enum PickMode { Random, Sequential }

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;
    [SerializeField] private string attackTrigger = "Attack";

    [Header("Nhịp bắn")]
    [Tooltip("Cứ N nốt MIDI thì bắn 1 phát. 1 = mọi nốt, 2 = cách 1 nốt...")]
    [SerializeField, Min(1)] private int attackEveryN = 2;
    [Tooltip("Khoảng cách tối thiểu (giây) giữa 2 phát bắn")]
    [SerializeField, Min(0f)] private float minInterval = 1.0f;

    [Header("Fire / Item")]
    [Tooltip("Prefab BossProjectile có Kind = Fire (người chơi phải né)")]
    [SerializeField] private List<BossProjectile> firePrefabs = new List<BossProjectile>();
    [Tooltip("Prefab BossProjectile có Kind = Item (người chơi phải nhặt)")]
    [SerializeField] private List<BossProjectile> itemPrefabs = new List<BossProjectile>();
    [Tooltip("Xác suất bắn Fire (0.5 = 50% fire, 50% item)")]
    [SerializeField, Range(0f, 1f)] private float fireChance = 0.5f;
    [Tooltip("Không bắn cùng loại (fire/item) quá số lần này liên tiếp. 0 = tắt")]
    [SerializeField, Min(0)] private int maxSameKindInRow = 2;
    [SerializeField] private PickMode firePickMode = PickMode.Random;
    [Tooltip("Sequential = lần lượt từng item trong list, Random = ngẫu nhiên")]
    [SerializeField] private PickMode itemPickMode = PickMode.Random;

    [Header("Lane ngược")]
    [Tooltip("Fire bay sang lane NGƯỢC với nốt piano (tạo cảm giác né)")]
    [SerializeField] private bool fireOppositeLane = true;
    [Tooltip("Item bay sang lane ngược với nốt piano. Tắt = cùng lane với nốt")]
    [SerializeField] private bool itemOppositeLane = false;
    [Tooltip("Xác suất đảo lane (1 = luôn đảo, 0.7 = 70% đảo, 30% cùng lane cho khó đoán)")]
    [SerializeField, Range(0f, 1f)] private float oppositeChance = 1f;

    [Header("Đạn")]
    [Tooltip("Boss bắt đầu animation trước nhịp bao nhiêu giây. " +
             "Càng lớn thì đạn bay càng chậm/dài. Nên 1.5 - 2.")]
    [SerializeField, Min(0.1f)] private float travelTime = 1.6f;
    [Tooltip("Nếu Animation Event chưa kịp chạy, tự bắn khi còn ít hơn số giây này tới nhịp")]
    [SerializeField, Min(0f)] private float forceFireBeforeHit = 0.25f;

    private struct AttackRequest
    {
        public float hitTime;
        public bool isLeft;
        public bool isFire;

        public AttackRequest(float time, bool left, bool fire)
        {
            hitTime = time;
            isLeft = left;
            isFire = fire;
        }
    }

    private readonly Queue<AttackRequest> queue = new Queue<AttackRequest>();

    private bool pendingShot;
    private float pendingHitTime;
    private bool pendingIsLeft;
    private bool pendingIsFire;

    private int beatCount;
    private float lastBeatTime = -999f;

    private bool lastWasFire;
    private int sameKindCount;

    private int fireIndex;
    private int itemIndex;

    private bool HasFire => firePrefabs != null && firePrefabs.Count > 0;
    private bool HasItem => itemPrefabs != null && itemPrefabs.Count > 0;

    // PianoKeySpawner gọi cho MỖI nốt MIDI khi boss đang đánh
    public void ScheduleBeat(float noteTime, bool isLeft)
    {
        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null || !spawner.IsPlaying || spawner.IsTutorial)
            return;

        if ((!HasFire && !HasItem) || firePoint == null)
            return;

        // Lọc theo số nốt và khoảng cách thời gian
        beatCount++;

        if (beatCount % Mathf.Max(1, attackEveryN) != 0)
            return;

        if (noteTime - lastBeatTime < minInterval)
            return;

        lastBeatTime = noteTime;

        // Chọn Fire hay Item
        bool isFire = Random.value < fireChance;

        if (maxSameKindInRow > 0 && sameKindCount >= maxSameKindInRow)
            isFire = !lastWasFire;

        // List loại được chọn rỗng thì dùng loại còn lại
        if (isFire && !HasFire) isFire = false;
        if (!isFire && !HasItem) isFire = true;

        if (sameKindCount > 0 && isFire == lastWasFire)
            sameKindCount++;
        else
            sameKindCount = 1;

        lastWasFire = isFire;

               // Đảo lane so với nốt piano nếu cần
        bool useOpposite = isFire ? fireOppositeLane : itemOppositeLane;
        bool targetLeft = isLeft;

        if (useOpposite && Random.value < oppositeChance)
            targetLeft = !isLeft;

        queue.Enqueue(new AttackRequest(noteTime, targetLeft, isFire));
    }

    private void Update()
    {
        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null || !spawner.IsPlaying || spawner.IsPaused ||
            spawner.IsTutorial)
            return;

        // Đang chờ Animation Event của phát trước:
        // nếu sắp trễ nhịp thì bắn luôn, không đợi animation nữa.
        if (pendingShot)
        {
            if (spawner.SongTime >= pendingHitTime - forceFireBeforeHit)
                AnimationEvent_FireProjectile();
            else
                return;
        }

        if (queue.Count == 0)
            return;

        AttackRequest request = queue.Peek();

        // Bắt đầu animation trước thời điểm hitTime
        if (spawner.SongTime < request.hitTime - travelTime)
            return;

        queue.Dequeue();

        pendingHitTime = request.hitTime;
        pendingIsLeft = request.isLeft;
        pendingIsFire = request.isFire;
        pendingShot = true;

        if (animator != null)
        {
            animator.ResetTrigger(attackTrigger);
            animator.SetTrigger(attackTrigger);
        }
        else
        {
            AnimationEvent_FireProjectile();
        }
    }

    // Chọn prefab đúng loại (Kind) theo list và chế độ chọn
    private BossProjectile PickPrefab(bool fire)
    {
        List<BossProjectile> list = fire ? firePrefabs : itemPrefabs;
        if (list == null || list.Count == 0)
            return null;

        BossProjectile.ProjectileKind wanted = fire
            ? BossProjectile.ProjectileKind.Fire
            : BossProjectile.ProjectileKind.Item;

        PickMode mode = fire ? firePickMode : itemPickMode;

        int start = mode == PickMode.Random
            ? Random.Range(0, list.Count)
            : (fire ? fireIndex : itemIndex) % list.Count;

        for (int i = 0; i < list.Count; i++)
        {
            int idx = (start + i) % list.Count;
            BossProjectile p = list[idx];

            if (p == null)
                continue;

            if (p.Kind != wanted)
            {
                Debug.LogWarning(
                    $"BossMusicAttack: '{p.name}' nằm trong list " +
                    $"{(fire ? "Fire" : "Item")} nhưng Kind = {p.Kind}. " +
                    "Hãy sửa Kind trên prefab hoặc chuyển sang đúng list.",
                    p);
                continue;
            }

            if (mode == PickMode.Sequential)
            {
                if (fire) fireIndex = idx + 1;
                else      itemIndex = idx + 1;
            }

            return p;
        }

        return null;
    }

    // Gắn vào Animation Event tại frame bắn
    public void AnimationEvent_FireProjectile()
    {
        if (!pendingShot)
            return;

        pendingShot = false;

        PianoKeySpawner spawner = PianoKeySpawner.Instance;

        if (spawner == null || firePoint == null)
            return;

        BossProjectile prefab = PickPrefab(pendingIsFire);
        if (prefab == null)
            return;

        Transform target = spawner.GetBossHitPoint(pendingIsLeft);
        if (target == null)
            return;

        float remainingTravel = pendingHitTime - spawner.SongTime;

        if (remainingTravel <= 0f)
        {
            Debug.LogWarning(
                "Boss bắn quá trễ. Hãy đưa Animation Event lên sớm hơn trong clip, " +
                "hoặc tăng Travel Time.");
            return;
        }

        BossProjectile projectile = Instantiate(
            prefab,
            firePoint.position,
            Quaternion.identity);

        projectile.Init(
            pendingIsLeft ? PianoKey.KeyType.Left : PianoKey.KeyType.Right,
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

        beatCount = 0;
        lastBeatTime = -999f;
        lastWasFire = false;
        sameKindCount = 0;
        fireIndex = 0;
        itemIndex = 0;
    }
}