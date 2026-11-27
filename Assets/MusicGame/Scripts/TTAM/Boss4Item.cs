using System;
using UnityEngine;

/// <summary>
/// Item màn 4 (Boss4RhythmSpawner). Bay giống FallingItem.
/// - Bấm trúng: dừng lại, chạy anim Hit rồi tự huỷ.
/// - Miss: spawner Destroy luôn.
/// </summary>
public class Boss4Item : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float spawnTime;
    private Func<float> songTime;

    [Tooltip("Càng lớn item càng sớm trượt về đúng làn (1 = chéo thẳng)")]
    [SerializeField] private float xSnapPower = 2f;

    [Header("Animator")]
    [SerializeField] private Animator animator;
    [Tooltip("Tên trigger trong Animator Controller")]
    [SerializeField] private string hitTrigger = "Hit";
    [Tooltip("Huỷ item sau khi chạy anim Hit (giây) - đặt bằng độ dài clip Hit")]
    [SerializeField] private float destroyAfterHit = 0.5f;

    [Header("Âm thanh (tuỳ chọn)")]
    [Tooltip("Tiếng khi người chơi đánh trúng item này")]
    [SerializeField] private AudioClip collectClip;
    [SerializeField, Range(0f, 1f)] private float collectVolume = 1f;

    public bool IsLeft { get; private set; }
    public float HitTime { get; private set; }

    /// <summary>true = đã bị đánh trúng, đang chạy anim Hit.</summary>
    public bool IsHit { get; private set; }

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // Đã trúng: đứng yên tại chỗ chạy anim Hit
        if (songTime == null || IsHit) return;

        float t = Mathf.Clamp01(Mathf.InverseLerp(spawnTime, HitTime, songTime()));

        // Trục Y: rơi nhanh dần cho cảm giác trọng lực
        float ky = t * t * 0.5f + t * 0.5f;

        // Trục X: trượt về làn sớm hơn (ease-out)
        float kx = 1f - Mathf.Pow(1f - t, xSnapPower);

        Vector3 p;
        p.x = Mathf.Lerp(startPos.x, targetPos.x, kx);
        p.y = Mathf.Lerp(startPos.y, targetPos.y, ky);
        p.z = Mathf.Lerp(startPos.z, targetPos.z, ky);

        transform.position = p;
    }

    public void Init(bool isLeft, Vector3 start, Vector3 target,
                     float spawnTime, float hitTime, Func<float> songTime)
    {
        IsLeft = isLeft;
        startPos = start;
        targetPos = target;
        this.spawnTime = spawnTime;
        HitTime = hitTime;
        this.songTime = songTime;
        IsHit = false;

        transform.position = start;
    }

    // Spawner gọi khi bấm trúng: âm thanh + anim Hit, rồi tự huỷ
    public void PlayHit()
    {
        if (IsHit) return;
        IsHit = true;

        GameplaySfx.Play(collectClip, collectVolume);

        // Tắt collider (nếu có) để không bị tính thêm lần nào nữa
        foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            c.enabled = false;

        if (animator != null && !string.IsNullOrEmpty(hitTrigger))
        {
            animator.SetTrigger(hitTrigger);
            Destroy(gameObject, Mathf.Max(0f, destroyAfterHit));
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Sai lệch (giây) giữa thời điểm hiện tại và thời điểm nốt
    public float TimingError()
    {
        return songTime == null ? float.MaxValue : Mathf.Abs(songTime() - HitTime);
    }

    public float Now()
    {
        return songTime == null ? 0f : songTime();
    }
}
