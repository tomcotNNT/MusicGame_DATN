using UnityEngine;

/// <summary>Bảng kết thúc màn 1 (boss đứng yên). Sao 3 = hạ được boss. Logic chung ở EndGameBase.</summary>
public class EndGameManMan1 : EndGameBase
{
    [Header("Spawner màn 1")]
    [SerializeField] private Boss1RhythmSpawner spawner;

    private bool bossDefeated;

    protected override void Awake()
    {
        base.Awake();
        if (spawner == null) spawner = FindFirstObjectByType<Boss1RhythmSpawner>();
    }

    protected override void SubscribeSongEnded(bool subscribe)
    {
        if (subscribe)
        {
            bossDefeated = false;
            Boss1Controller.AnyBossDefeated += OnBossDefeated;
            if (spawner != null) spawner.SongEnded += OnSongEnded;
        }
        else
        {
            Boss1Controller.AnyBossDefeated -= OnBossDefeated;
            if (spawner != null) spawner.SongEnded -= OnSongEnded;
        }
    }

    private void OnBossDefeated() => bossDefeated = true;

    // Thanh sao lúc chơi
    protected override int LiveTotalNotes => spawner != null ? spawner.TotalNotes : 0;

    protected override float LiveThirdStarProgress
    {
        get
        {
            Boss1Controller b = spawner != null ? spawner.Boss : null;
            if (b == null) return bossDefeated ? 1f : -1f;
            return BossProgress(b.CurrentHp, b.MaxHp, bossDefeated || b.IsDefeated);
        }
    }

    protected override bool EvaluateThirdStar(out object arg0, out object arg1)
    {
        arg0 = "";
        arg1 = "";
        return bossDefeated;
    }
}
