using UnityEngine;

/// <summary>Đếm ngược màn 2 (boss). Logic chung nằm ở RhythmCountdown.</summary>
public class CountdownMan2 : RhythmCountdown
{
    [SerializeField] private BossSpawner2 enemySpawner;

    protected override IRhythmSongClock Clock => enemySpawner;

    protected override void Start()
    {
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<BossSpawner2>();

        base.Start();
    }
}
