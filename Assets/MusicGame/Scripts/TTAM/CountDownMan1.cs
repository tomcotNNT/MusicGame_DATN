using UnityEngine;

/// <summary>Đếm ngược màn 1 (boss đứng yên). Logic chung nằm ở RhythmCountdown.</summary>
public class CountdownMan1 : RhythmCountdown
{
    [SerializeField] private Boss1RhythmSpawner bossSpawner;

    protected override IRhythmSongClock Clock => bossSpawner;

    protected override void Start()
    {
        if (bossSpawner == null)
            bossSpawner = FindFirstObjectByType<Boss1RhythmSpawner>();

        base.Start();
    }
}
