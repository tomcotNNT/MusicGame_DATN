using UnityEngine;

/// <summary>
/// Bảng kết thúc màn 1 (piano). Logic chung ở EndGameBase.
/// Sao 3 = hạ Boss (giống màn 2, màn 3). Màn tắt boss thì sao 3 = Miss không quá maxMissForStar.
/// </summary>
public class EndGameManagerMiss : EndGameBase
{
    [Header("Spawner màn 1")]
    [SerializeField] private PianoKeySpawner spawner;

    [Tooltip("Chỉ dùng khi màn KHÔNG có boss: Miss không quá số này thì được sao thứ 3")]
    [SerializeField] private int maxMissForStar = 5;

    protected override void Awake()
    {
        base.Awake();
        if (spawner == null) spawner = FindFirstObjectByType<PianoKeySpawner>();
    }

    protected override void SubscribeSongEnded(bool subscribe)
    {
        if (spawner == null) return;

        if (subscribe) spawner.SongEnded += OnSongEnded;
        else spawner.SongEnded -= OnSongEnded;
    }

    protected override bool EvaluateThirdStar(out object arg0, out object arg1)
    {
        if (spawner != null && spawner.BossActive)
        {
            arg0 = "";
            arg1 = "";
            return spawner.BossDefeated;
        }

        int missCount = scoreSystem != null ? scoreSystem.MissCount : 0;
        arg0 = maxMissForStar;
        arg1 = missCount;
        return missCount <= maxMissForStar;
    }
}
