using UnityEngine;

/// <summary>Đếm ngược màn 4 (hang động, né quái). Logic chung nằm ở RhythmCountdown.</summary>
public class CountdownCave : RhythmCountdown
{
    [SerializeField] private CaveMawSpawner caveSpawner;

    protected override IRhythmSongClock Clock => caveSpawner;

    protected override void Start()
    {
        if (caveSpawner == null)
            caveSpawner = FindFirstObjectByType<CaveMawSpawner>();

        base.Start();
    }
}
