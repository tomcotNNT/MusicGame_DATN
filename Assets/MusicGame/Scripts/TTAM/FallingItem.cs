using System;
using UnityEngine;

public class FallingItem : MonoBehaviour
{
    private Vector3 startPos;
    private Vector3 targetPos;
    private float spawnTime;
    private Func<float> songTime;

    [Tooltip("Càng lớn item càng sớm trượt về đúng làn (1 = chéo thẳng)")]
    [SerializeField] private float xSnapPower = 2f;

    public bool IsLeft { get; private set; }
    public float HitTime { get; private set; }

    private void Update()
    {
        if (songTime == null) return;

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

        transform.position = start;
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