using UnityEngine;

/// <summary>Bảng kết thúc màn 4 (boss đứng yên). Sao 3 = hạ được boss. Logic chung ở EndGameBase.</summary>
public class EndGameManMan4 : EndGameBase
{
    [Header("Spawner màn 4")]
    [SerializeField] private Boss4RhythmSpawner spawner;

    private bool bossDefeated;

    protected override void Awake()
    {
        base.Awake();
        if (spawner == null) spawner = FindFirstObjectByType<Boss4RhythmSpawner>();
    }

    protected override void SubscribeSongEnded(bool subscribe)
    {
        if (subscribe)
        {
            bossDefeated = false;
            Boss4Controller.AnyBossDefeated += OnBossDefeated;
            if (spawner != null) spawner.SongEnded += OnSongEnded;
        }
        else
        {
            Boss4Controller.AnyBossDefeated -= OnBossDefeated;
            if (spawner != null) spawner.SongEnded -= OnSongEnded;
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
