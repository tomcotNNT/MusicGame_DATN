using UnityEngine;
using UnityEngine.Serialization;

/// <summary>Bảng kết thúc màn 2 (boss). Sao 3 = hạ được boss. Logic chung ở EndGameBase.</summary>
public class EndGameManager : EndGameBase
{
    [Header("Spawner màn 2")]
    [FormerlySerializedAs("pianoKeySpawner")]
    [SerializeField] private BossSpawner2 spawner;

    private bool bossDefeated;

    protected override void Awake()
    {
        base.Awake();
        if (spawner == null) spawner = FindFirstObjectByType<BossSpawner2>();
    }

    protected override void SubscribeSongEnded(bool subscribe)
    {
        if (subscribe)
        {
            bossDefeated = false;
            BossController2.AnyBossDefeated += OnBossDefeated;
            if (spawner != null) spawner.SongEnded += OnSongEnded;
        }
        else
        {
            BossController2.AnyBossDefeated -= OnBossDefeated;
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
