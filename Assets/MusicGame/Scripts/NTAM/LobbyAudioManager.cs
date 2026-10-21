using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class LobbyAudioManager : MonoBehaviour
{
    public static LobbyAudioManager Instance { get; private set; }

    private AudioSource audioSource;

    private void Awake()
    {
        // =========================================================================
        // BƯỚC 1: KIỂM TRA CHỐNG TRÙNG LẶP VỚI BGM BAN ĐẦU (TỪ LOADING SCENE)
        // =========================================================================
        GameManager gm = FindAnyObjectByType<GameManager>();
        if (gm != null)
        {
            AudioSource gmAudio = gm.GetComponent<AudioSource>();
            // Nếu GameManager vẫn còn sống và đang phát bài BGM từ trước -> HỦY NGAY object này để tránh đè nhạc
            if (gmAudio != null && gmAudio.isPlaying)
            {
                Debug.Log(">>> [LOBBY AUDIO] Đã có BGM ban đầu đang phát từ GameManager. Hủy Audio dự phòng sảnh!");
                Destroy(gameObject);
                return;
            }
        }

        // =========================================================================
        // BƯỚC 2: NẾU BGM BAN ĐẦU ĐÃ BỊ MẤT (KHI TỪ TUTORIAL VỀ) -> ĐỨNG RA THAY THẾ
        // =========================================================================
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject); // Giữ bài BGM3 này đi xuyên qua Character và Story

        audioSource = GetComponent<AudioSource>();
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
        PlayBGM();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        string sceneName = scene.name;

        // Nếu ở các scene Menu -> Đảm bảo BGM3 tiếp tục phát liền mạch
        if (IsMenuScene(sceneName))
        {
            PlayBGM();
        }
        // Nếu vào màn gameplay (Tutorial, Man1, Man2...) -> Tự hủy để nhường sân cho nhạc màn chơi
        else if (IsGameplayScene(sceneName))
        {
            Instance = null;
            Destroy(gameObject);
        }
    }

    private void PlayBGM()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.mute = false;
            audioSource.volume = 1f;
            audioSource.loop = true;

            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($">>> [LOBBY AUDIO] Đang phát BGM ({audioSource.clip.name}) tại: {SceneManager.GetActiveScene().name}");
            }
        }
    }

    private bool IsMenuScene(string sceneName)
    {
        return sceneName == "Lobby" || 
               sceneName == "Character" || 
               sceneName == "Story" || 
               sceneName == "MainMenu2";
    }

    private bool IsGameplayScene(string sceneName)
    {
        return sceneName == "Tutorial" || 
               sceneName.StartsWith("Man");
    }
}