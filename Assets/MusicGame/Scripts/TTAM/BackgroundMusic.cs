using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Nhạc nền phát liên tục qua các scene (LoadingScene -> MainMenu -> SelectSong -> SelectChar ...).
/// Khi vào scene màn chơi (Man1, Man2, Man3, Tutorial...) thì dừng nhạc.
/// Quay lại scene menu thì nhạc tự phát lại.
///
/// Cách dùng: tạo 1 GameObject trong scene ĐẦU TIÊN (LoadingScene), gắn script này,
/// kéo AudioClip nhạc nền vào ô "Bgm Clip". Không cần đặt ở các scene khác.
/// (Nếu có đặt ở scene khác cũng không sao, bản trùng sẽ tự hủy.)
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance { get; private set; }

    [Header("Nhạc nền")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    [Header("Scene màn chơi (vào đây sẽ TẮT nhạc)")]
    [Tooltip("Tên chính xác của scene màn chơi")]
    [SerializeField] private string[] gameplayScenes = { "StoryMan1", "StoryMan2", "StoryMan3", "Tutorial", "Man1", "Man2", "Man3", "Man4", "Man5", "StoryMan4", "StoryMan5" };

    [Header("Fade (giây)")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.5f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.5f;

    private AudioSource source;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = true;
        source.clip = bgmClip;
        source.volume = volume;
        source.mute = !AudioPrefs.MusicOn;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        AudioPrefs.Changed += ApplyAudioPrefs;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        AudioPrefs.Changed -= ApplyAudioPrefs;
    }

    // Nút Music ở menu bật/tắt -> tắt tiếng nhạc nền ngay
    private void ApplyAudioPrefs()
    {
        if (source != null)
            source.mute = !AudioPrefs.MusicOn;
    }

    private void Start()
    {
        // Áp dụng cho scene hiện tại (sceneLoaded không gọi cho scene đầu tiên trước khi đăng ký)
        ApplyForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;
        ApplyForScene(scene.name);
    }

    private void ApplyForScene(string sceneName)
    {
        if (IsGameplayScene(sceneName))
            Stop();
        else
            Play();
    }

    private bool IsGameplayScene(string sceneName)
    {
        foreach (var s in gameplayScenes)
        {
            // "Man4_Test" (scene thử nghiệm) cũng tính là màn chơi của "Man4"
            if (!string.IsNullOrEmpty(s) && (s == sceneName || sceneName.StartsWith(s + "_")))
                return true;
        }
        return false;
    }

    // ===================== API công khai =====================

    /// <summary>Phát nhạc nền (không tua lại nếu đang phát).</summary>
    public void Play()
    {
        if (source == null || bgmClip == null) return;

        if (source.clip != bgmClip)
            source.clip = bgmClip;

        if (source.isPlaying && fadeRoutine == null) return;

        StartFade(volume, fadeInDuration, stopAtEnd: false);
    }

    /// <summary>Dừng nhạc nền (có fade).</summary>
    public void Stop()
    {
        if (source == null || !source.isPlaying) return;
        StartFade(0f, fadeOutDuration, stopAtEnd: true);
    }

    /// <summary>Đổi bài nhạc nền.</summary>
    public void ChangeClip(AudioClip newClip)
    {
        if (newClip == null || newClip == bgmClip) return;
        bgmClip = newClip;
        source.Stop();
        source.clip = bgmClip;
        Play();
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (fadeRoutine == null && source != null)
            source.volume = volume;
    }

    public void SetMute(bool mute)
    {
        if (source != null) source.mute = mute;
    }

    // ===================== Fade =====================

    private void StartFade(float target, float duration, bool stopAtEnd)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(target, duration, stopAtEnd));
    }

    private IEnumerator FadeTo(float target, float duration, bool stopAtEnd)
    {
        if (!stopAtEnd && !source.isPlaying)
        {
            source.volume = duration > 0f ? 0f : target;
            source.Play();
        }

        float start = source.volume;
        float t = 0f;

        while (t < duration)
        {
            // unscaled để vẫn chạy khi timeScale = 0
            t += Time.unscaledDeltaTime;
            source.volume = Mathf.Lerp(start, target, t / duration);
            yield return null;
        }

        source.volume = target;
        if (stopAtEnd) source.Stop();

        fadeRoutine = null;
    }
}
