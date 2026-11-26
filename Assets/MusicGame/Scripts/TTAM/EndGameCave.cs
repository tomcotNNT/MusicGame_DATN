using UnityEngine;

/// <summary>Bảng kết thúc màn 4 (hang động). Sao 3 = hạ được quái mẹ Mother Maw. Logic chung ở EndGameBase.</summary>
public class EndGameCave : EndGameBase
{
    [Header("Spawner màn 4")]
    [SerializeField] private CaveMawSpawner caveSpawner;

    private bool bossDefeated;

    protected override void Awake()
    {
        base.Awake();
        if (caveSpawner == null) caveSpawner = FindFirstObjectByType<CaveMawSpawner>();
    }

    protected override void SubscribeSongEnded(bool subscribe)
    {
        if (subscribe)
        {
            bossDefeated = false;
            CaveMawSpawner.AnyBossDefeated += OnBossDefeated;
            if (caveSpawner != null) caveSpawner.SongEnded += OnSongEnded;
        }
        else
        {
            CaveMawSpawner.AnyBossDefeated -= OnBossDefeated;
            if (caveSpawner != null) caveSpawner.SongEnded -= OnSongEnded;
        }
    }

    private void OnBossDefeated() => bossDefeated = true;

    protected override bool EvaluateThirdStar(out object arg0, out object arg1)
    {
        arg0 = "";
        arg1 = "";
        return bossDefeated;
    }
}
