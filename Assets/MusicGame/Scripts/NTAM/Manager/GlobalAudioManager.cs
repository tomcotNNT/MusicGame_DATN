using UnityEngine;
using UnityEngine.SceneManagement;

public class GlobalAudioManager : MonoBehaviour
{
    public static GlobalAudioManager Instance { get; private set; }

    [Header("Kênh Âm Thanh (Bắt buộc dùng 2 AudioSource riêng)")]
    public AudioSource bgmSource; // ĐÃ ĐỔI THÀNH public
    public AudioSource sfxSource; // ĐÃ ĐỔI THÀNH public

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

        // Đảm bảo cấu hình đúng tính chất 2 kênh
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

    /// <summary>
    /// Tự động bật/tắt nhạc sảnh theo từng Scene mà không làm mất GameObject
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Hủy trạng thái câm tiếng và đóng băng thời gian phòng khi thoát từ bảng Pause
        Time.timeScale = 1f;
        AudioListener.pause = false;

        string sceneName = scene.name;

        if (IsMenuScene(sceneName))
        {
            PlayLobbyBGM();
        }
        else if (IsGameplayScene(sceneName))
        {
            // Vào màn chơi: Chỉ dừng phát BGM sảnh, giữ nguyên hệ thống SFX
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

    /// <summary>
    /// Phát âm thanh hiệu ứng UI qua PlayOneShot
    /// </summary>
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

// ĐẶT ENUM Ở CUỐI FILE ĐỂ TRÁNH LỖI NHẬN DIỆN CLASS CỦA UNITY
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