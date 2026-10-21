using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [Header("Kênh Âm Thanh (Bắt buộc dùng 2 AudioSource riêng)")]
    public AudioSource bgmSource;
    public AudioSource sfxSource;

    [Header("Nhạc Nền Sảnh (Lobby BGM)")]
    [SerializeField] private AudioClip lobbyBgmClip;

    [Header("Thư Viện UI SFX")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip confirmClip;
    [SerializeField] private AudioClip backClip;
    [SerializeField] private AudioClip popupOpenClip;
    [SerializeField] private AudioClip popupCloseClip;
    [SerializeField] private AudioClip buySuccessClip;
    [SerializeField] private AudioClip buyDeniedClip; // Tiếng khi chưa đủ tiền
    [SerializeField] private AudioClip gachaSpinClip;
    [SerializeField] private AudioClip gachaJackpotClip;

    // =========================================================================
    // TRẠNG THÁI ÂM THANH (DÙNG CHO UI NÚT BẤM KIỂM TRA)
    // =========================================================================
    public bool IsBGMActive => bgmSource != null && !bgmSource.mute;
    public bool IsSFXActive => sfxSource != null && !sfxSource.mute;

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

        // Cấu hình tính chất 2 kênh phát
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        // Khôi phục trạng thái Bật/Tắt đã lưu từ lần chơi trước
        InitAudioSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayLobbyBGM();
    }

    // =========================================================================
    // HỆ THỐNG MUTE & LƯU CÀI ĐẶT
    // =========================================================================
    private void InitAudioSettings()
    {
        // 1. LUÔN LUÔN BẬT NHẠC NỀN (BGM) KHI MỞ GAME
        if (bgmSource != null)
        {
            bgmSource.mute = false;
            bgmSource.volume = 1f;
        }
        PlayerPrefs.SetInt("SETTING_BGM", 1); // Ghi đè lại trạng thái bật để UI nút bấm luôn sáng

        // 2. KHỞI TẠO HIỆU ỨNG ÂM THANH (SFX)
        // (Nếu muốn SFX cũng luôn bật khi mở game, đổi dòng dưới thành sfxEnabled = true;)
        bool sfxEnabled = PlayerPrefs.GetInt("SETTING_SFX", 1) == 1;

        if (sfxSource != null)
        {
            sfxSource.mute = !sfxEnabled;
            sfxSource.volume = sfxEnabled ? 1f : 0f;
        }

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Đảo trạng thái Mute của BGM và lưu lại cài đặt
    /// </summary>
    public void ToggleBGM()
    {
        if (bgmSource == null) return;

        bgmSource.mute = !bgmSource.mute;
        PlayerPrefs.SetInt("SETTING_BGM", bgmSource.mute ? 0 : 1);
        PlayerPrefs.Save();

        Debug.Log(">>> [AUDIO] BGM: " + (!bgmSource.mute ? "ON" : "OFF"));
    }

    /// <summary>
    /// Đảo trạng thái Mute của SFX và lưu lại cài đặt
    /// </summary>
    public void ToggleSFX()
    {
        if (sfxSource == null) return;

        sfxSource.mute = !sfxSource.mute;
        PlayerPrefs.SetInt("SETTING_SFX", sfxSource.mute ? 0 : 1);
        PlayerPrefs.Save();

        Debug.Log(">>> [AUDIO] SFX: " + (!sfxSource.mute ? "ON" : "OFF"));
    }

    // =========================================================================
    // QUẢN LÝ THEO DÕI SCENE & PHÁT NHẠC NỀN
    // =========================================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        string sceneName = scene.name;

        if (IsMenuScene(sceneName))
        {
            PlayLobbyBGM();
        }
        else if (IsGameplayScene(sceneName))
        {
            StopBGM();
        }
    }

    public void PlayLobbyBGM()
    {
        if (bgmSource == null) return;

        if (lobbyBgmClip != null && bgmSource.clip != lobbyBgmClip)
        {
            bgmSource.clip = lobbyBgmClip;
        }

        // Chặn tua lại nếu bài nhạc đang chạy
        if (!bgmSource.isPlaying && bgmSource.clip != null)
        {
            bgmSource.Play();
            Debug.Log(">>> [AUDIO] BGM Sảnh đang phát tại: " + SceneManager.GetActiveScene().name);
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }

    // =========================================================================
    // PHÁT HIỆU ỨNG ÂM THANH UI
    // =========================================================================
    public void PlaySFX(UISfxType sfxType)
    {
        AudioClip clipToPlay = null;

        switch (sfxType)
        {
            case UISfxType.Click: clipToPlay = clickClip; break;
            case UISfxType.Confirm: clipToPlay = confirmClip; break;
            case UISfxType.Back: clipToPlay = backClip; break;
            case UISfxType.PopupOpen: clipToPlay = popupOpenClip; break;
            case UISfxType.PopupClose: clipToPlay = popupCloseClip; break;
            case UISfxType.BuySuccess: clipToPlay = buySuccessClip; break;
            case UISfxType.BuyDenied: clipToPlay = buyDeniedClip; break;
            case UISfxType.GachaSpin: clipToPlay = gachaSpinClip; break;
            case UISfxType.GachaJackpot: clipToPlay = gachaJackpotClip; break;
        }

        if (clipToPlay != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        return sceneName == "Lobby" || sceneName == "Character" || sceneName == "Story" || sceneName == "MainMenu2";
    }

    private bool IsGameplayScene(string sceneName)
    {
        return sceneName == "Tutorial" || sceneName.StartsWith("Man");
    }
}

public enum UISfxType
{
    Click,
    Confirm,
    Back,
    PopupOpen,
    PopupClose,
    BuySuccess,
    BuyDenied,
    GachaSpin,
    GachaJackpot
}