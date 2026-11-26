using UnityEngine;

/// <summary>Spawner nhịp điệu có thể tạm dừng / tiếp tục bài hát mà không lệch nhịp.</summary>
public interface ISongPausable
{
    void PauseSong();
    void ResumeSong();
}

/// <summary>
/// Đồng hồ bài hát theo dspTime dùng chung cho các spawner.
/// dspTime vẫn chạy khi Time.timeScale = 0 hoặc AudioListener.pause = true,
/// nên khi pause phải "đóng băng" SongTime và dời mốc bắt đầu bài khi tiếp tục.
///
/// Tự cứu: nếu dspTime đứng yên quá lâu (thiết bị âm thanh lỗi / Editor mất audio)
/// thì chuyển sang đếm theo thời gian thật để nốt vẫn chạy, và ghi cảnh báo 1 lần.
/// </summary>
public class SongClock
{
    private const float StallSeconds = 0.5f;

    private double songStartDsp;
    private double pauseStartDsp;
    private bool pausedBeforeAudioStart;

    // Phát hiện dspTime đứng yên
    private double lastDsp = -1;
    private float lastDspChangeAt;
    private bool fallback;
    private double fallbackBase;
    private float fallbackBaseUnscaled;

    public bool IsPaused { get; private set; }

    /// <summary>true = dspTime không chạy, đang đếm theo thời gian thật.</summary>
    public bool UsingFallback => fallback;

    /// <summary>Giây tính từ lúc nhạc bắt đầu (âm khi đang đếm ngược). Đứng yên khi pause.</summary>
    public float SongTime => (float)((IsPaused ? pauseStartDsp : Now()) - songStartDsp);

    // Thời điểm hiện tại của đồng hồ bài hát (dspTime, hoặc thời gian thật nếu dspTime bị kẹt)
    private double Now()
    {
        double dsp = AudioSettings.dspTime;
        float u = Time.unscaledTime;

        if (fallback)
            return fallbackBase + (u - fallbackBaseUnscaled);

        if (dsp != lastDsp)
        {
            lastDsp = dsp;
            lastDspChangeAt = u;
            return dsp;
        }

        if (u - lastDspChangeAt > StallSeconds)
        {
            fallback = true;
            fallbackBase = dsp;
            fallbackBaseUnscaled = lastDspChangeAt;
            Debug.LogWarning("[SongClock] AudioSettings.dspTime không chạy (thiết bị âm thanh?) -> chuyển sang đếm theo thời gian thật. " +
                             "Kiểm tra loa/tai nghe hoặc Project Settings > Audio.");
            return fallbackBase + (u - fallbackBaseUnscaled);
        }

        return dsp;
    }

    /// <summary>Lên lịch phát nhạc sau <paramref name="leadIn"/> giây.</summary>
    public void Start(AudioSource source, float leadIn)
    {
        IsPaused = false;
        fallback = false;
        lastDsp = AudioSettings.dspTime;
        lastDspChangeAt = Time.unscaledTime;

        songStartDsp = AudioSettings.dspTime + leadIn;

        if (source != null)
        {
            source.Stop();
            source.PlayScheduled(songStartDsp);
        }
    }

    public void Pause(AudioSource source)
    {
        if (IsPaused) return;

        pauseStartDsp = Now();
        IsPaused = true;
        pausedBeforeAudioStart = pauseStartDsp < songStartDsp;

        if (source == null) return;

        if (pausedBeforeAudioStart)
            source.Stop();   // nhạc chưa phát: huỷ lịch phát, lúc tiếp tục sẽ lên lịch lại
        else
            source.Pause();
    }

    public void Resume(AudioSource source)
    {
        if (!IsPaused) return;

        IsPaused = false;
        songStartDsp += Now() - pauseStartDsp;

        if (source == null) return;

        if (pausedBeforeAudioStart)
            source.PlayScheduled(fallback ? AudioSettings.dspTime + (songStartDsp - Now()) : songStartDsp);
        else
            source.UnPause();
    }
}


/// <summary>Dừng / tiếp tục tất cả spawner trong scene (dùng cho menu pause, Game Over).</summary>
public static class SongPause
{
    public static void PauseAll()
    {
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is ISongPausable p)
                p.PauseSong();
        }
    }

    public static void ResumeAll()
    {
        foreach (MonoBehaviour mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (mb is ISongPausable p)
                p.ResumeSong();
        }
    }
}
