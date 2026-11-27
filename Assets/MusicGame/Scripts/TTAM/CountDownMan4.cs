using UnityEngine;

/// <summary>Đếm ngược màn 4 (boss đứng yên). Logic chung nằm ở RhythmCountdown.</summary>
public class CountdownMan4 : RhythmCountdown
{
    [SerializeField] private Boss4RhythmSpawner bossSpawner;

    protected override IRhythmSongClock Clock => bossSpawner;

    protected override void Start()
    {
        if (bossSpawner == null)
            bossSpawner = FindFirstObjectByType<Boss4RhythmSpawner>();

        base.Start();
    }
}
