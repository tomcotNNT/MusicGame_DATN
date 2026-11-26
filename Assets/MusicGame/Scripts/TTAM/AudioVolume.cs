using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Âm lượng nhạc / tiếng hiệu ứng (0..1) và bật/tắt hiệu ứng chạm, lưu trong PlayerPrefs.
/// Tự áp dụng cho mọi AudioSource mỗi khi vào scene và khi người chơi kéo thanh trượt.
///   - Nhạc: AudioSource có MidiReader, hoặc clip dài hơn 20 giây (nhạc bài hát)
///   - Nhạc menu (BackgroundMusic): gọi SetVolume
///   - Còn lại: tiếng hiệu ứng
/// </summary>
public static class AudioVolume
{
    private const string MusicKey = "SETTING_MUSIC_VOL";
    private const string SfxKey = "SETTING_SFX_VOL";
    private const string FxKey = "SETTING_CHAR_FX";

    // âm lượng gốc của từng nguồn (để nhân với thanh trượt, không bị nhân dồn)
    private static readonly Dictionary<AudioSource, float> baseVolume = new Dictionary<AudioSource, float>();

    public static float Music
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); Apply(); }
    }

    public static float Sfx
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); Apply(); }
    }

    /// <summary>Hiệu ứng chạm màn hình (TouchVFXController).</summary>
    public static bool EffectsOn
    {
        get => PlayerPrefs.GetInt(FxKey, 1) == 1;
        set { PlayerPrefs.SetInt(FxKey, value ? 1 : 0); ApplyEffects(); }
    }

    public static void Save() => PlayerPrefs.Save();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        baseVolume.Clear();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Apply();
        ApplyEffects();
    }

    public static void Apply()
    {
        // bỏ nguồn đã bị huỷ
        var dead = new List<AudioSource>();
        foreach (var k in baseVolume.Keys) if (k == null) dead.Add(k);
        foreach (var k in dead) baseVolume.Remove(k);

        float music = Music, sfx = Sfx;
        foreach (AudioSource src in Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (src.gameObject.name.StartsWith("GameplaySfx")) continue;   // GameplaySfx tự nhân âm lượng

            var bgm = src.GetComponent<BackgroundMusic>();
            if (bgm != null) { bgm.SetVolume(music); continue; }

            if (!baseVolume.TryGetValue(src, out float b))
            {
                b = src.volume;
                baseVolume[src] = b;
            }
            src.volume = b * (IsMusic(src) ? music : sfx);
        }
    }

    private static bool IsMusic(AudioSource s) =>
        s.GetComponent<MidiReader>() != null || (s.clip != null && s.clip.length > 20f);

    public static void ApplyEffects()
    {
        bool on = EffectsOn;
        foreach (var t in Object.FindObjectsByType<TouchVFXController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            t.enabled = on;
    }
}
