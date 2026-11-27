using System;
using UnityEngine;

/// <summary>
/// Cài đặt âm thanh dùng chung cho cả game (1 bộ key duy nhất).
///   SETTING_BGM: nhạc nền menu (BackgroundMusic)
///   SETTING_SFX: tiếng nút + tiếng đánh trúng nốt
/// Nhạc của bài trong màn chơi luôn phát (game nhịp điệu cần nghe nhạc).
/// </summary>
public static class AudioPrefs
{
    public const string MusicKey = "SETTING_BGM";
    public const string SfxKey = "SETTING_SFX";

    /// <summary>Phát ra khi bật/tắt nhạc hoặc âm thanh.</summary>
    public static event Action Changed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Changed = null;

    public static bool MusicOn
    {
        get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
        set => Set(MusicKey, value);
    }

    public static bool SfxOn
    {
        get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
        set => Set(SfxKey, value);
    }

    private static void Set(string key, bool on)
    {
        PlayerPrefs.SetInt(key, on ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
