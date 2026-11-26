using UnityEngine;

/// <summary>Đếm ngược màn 3 (tàu ngầm). Logic chung nằm ở RhythmCountdown.</summary>
public class CountdownMan3 : RhythmCountdown
{
    [SerializeField] private SubmarineRhythmSpawner submarineSpawner;

    protected override IRhythmSongClock Clock => submarineSpawner;

    protected override void Start()
    {
        if (submarineSpawner == null)
            submarineSpawner = FindFirstObjectByType<SubmarineRhythmSpawner>();

        base.Start();
    }
}
