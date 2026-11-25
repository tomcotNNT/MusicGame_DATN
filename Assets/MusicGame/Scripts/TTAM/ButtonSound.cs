using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gắn vào Button -> bấm là phát tiếng.
/// Âm thanh phát qua 1 AudioSource dùng chung không bị huỷ khi đổi scene,
/// nên nút chuyển scene (Play, Back...) vẫn nghe đủ tiếng.
/// Tôn trọng cài đặt tắt SFX (PlayerPrefs "SETTING_SFX").
/// </summary>
[RequireComponent(typeof(Button))]
public class ButtonSound : MonoBehaviour
{
    [Tooltip("Tiếng khi bấm nút")]
    [SerializeField] private AudioClip clickClip;

    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClick);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }

    private void PlayClick()
    {
        UISoundPlayer.Play(clickClip, volume);
    }

    /// <summary>Dùng khi thêm ButtonSound bằng code (ButtonSoundGroup).</summary>
    public void Setup(AudioClip clip, float vol)
    {
        clickClip = clip;
        volume = vol;
    }
}

/// <summary>
/// Bộ phát âm thanh UI dùng chung, tự tạo khi cần, sống qua mọi scene.
/// Gọi từ bất cứ đâu: UISoundPlayer.Play(clip);
/// </summary>
public static class UISoundPlayer
{
    private static AudioSource source;

    public static bool IsSfxOn => PlayerPrefs.GetInt("SETTING_SFX", 1) == 1;

    public static void Play(AudioClip clip, float volume = 1f)
    {
        if (clip == null || !IsSfxOn) return;

        if (source == null)
        {
            var go = new GameObject("UISoundPlayer");
            Object.DontDestroyOnLoad(go);
            source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.ignoreListenerPause = true; // Vẫn kêu khi game đang pause (AudioListener.pause)
        }

        source.PlayOneShot(clip, volume);
    }
}
