using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bộ phát tiếng hiệu ứng trong màn chơi (đánh nốt, nhặt item/coin...).
///   - Âm 2D, không nhỏ dần theo khoảng cách camera.
///   - Tôn trọng nút tắt Âm thanh (AudioPrefs.SfxOn).
///   - Dừng cùng game khi Pause / Game Over (AudioListener.pause) và khi chuyển app.
///   - Chống chồng tiếng: cùng 1 clip phát lại trong &lt; minRepeatInterval giây thì bỏ qua.
/// Object phát nằm trong scene hiện tại, tự mất khi đổi scene.
/// </summary>
public static class GameplaySfx
{
    private const float MinRepeatInterval = 0.03f;
    private const int MaxVoices = 8;

    private static AudioSource[] voices;
    private static int nextVoice;
    private static readonly Dictionary<AudioClip, float> lastPlayed = new Dictionary<AudioClip, float>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        voices = null;
        nextVoice = 0;
        lastPlayed.Clear();
    }

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || !AudioPrefs.SfxOn)
            return;

        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(clip, out float last) && now >= last && now - last < MinRepeatInterval)
            return;
        lastPlayed[clip] = now;

        EnsureVoices();

        // Xoay vòng nhiều AudioSource: tiếng mới không cắt tiếng cũ, nhưng tổng số tiếng có giới hạn
        AudioSource src = voices[nextVoice];
        nextVoice = (nextVoice + 1) % voices.Length;

        src.PlayOneShot(clip, Mathf.Clamp01(volume));
    }

    private static void EnsureVoices()
    {
        if (voices != null && voices[0] != null)
            return;

        GameObject go = new GameObject("GameplaySfx (auto)");
        voices = new AudioSource[MaxVoices];

        for (int i = 0; i < MaxVoices; i++)
        {
            AudioSource s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D
            voices[i] = s;
        }

        nextVoice = 0;
        lastPlayed.Clear();
    }
}
