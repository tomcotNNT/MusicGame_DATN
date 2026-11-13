using System.Collections;
using System.IO; // Thêm thư viện để thao tác xóa file JSON trực tiếp
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName = "Lobby";

    [Header("Hiệu ứng UI")]
    public TextMeshProUGUI tapToStartText;
    public Color clickedColor = Color.red;

    [Header("UI Đặt Tên Lần Đầu")]
    public GameObject nameInputPopup;
    public TMP_InputField nameInputField;
    public GameObject dimBackgroundPanel;
    public TextMeshProUGUI errorText;

    [Header("UI Reset Dữ Liệu")]
    public GameObject resetConfirmPopup;

    [Header("Tùy Chọn Reset")]
    [Tooltip("Nếu tích chọn: Xóa sạch toàn bộ (Tiền, Tutorial, Tên). Nếu bỏ tích: Chỉ xóa tên.")]
    [SerializeField] private bool resetAllGameData = true;

    [Header("Hiệu ứng Chuyển Cảnh")]
    public CanvasGroup fadeCanvasGroup;
    public float transitionDelay = 1.2f;
    private bool isStarting = false;

    private void Start()
    {
        // Khởi tạo trạng thái ban đầu của màn hình
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.blocksRaycasts = false;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

        CloseAllPopups();

        // Lắng nghe sự kiện nhấn Enter trên bàn phím khi nhập tên xong
        if (nameInputField != null)
        {
            nameInputField.onSubmit.AddListener(delegate { OnConfirmNameClicked(); });
        }
    }

    private void CloseAllPopups()
    {
        if (nameInputPopup != null) nameInputPopup.SetActive(false);
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(false);
        if (resetConfirmPopup != null) resetConfirmPopup.SetActive(false);
        if (errorText != null) errorText.text = "";
    }

    public void PlayClickSound()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayClickSound();
        }
    }

    #region XỬ LÝ NÚT RESET DỮ LIỆU (NÚT HOME GÓC PHẢI)

    public void OnResetButtonClicked()
    {
        if (isStarting) return;
        PlayClickSound();

        // Mở popup hỏi xác nhận và làm tối nền
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(true);
        if (resetConfirmPopup != null) resetConfirmPopup.SetActive(true);
    }

    public void OnCancelResetClicked()
    {
        PlayClickSound();
        CloseAllPopups();
    }

    /// <summary>
    /// Xóa tận gốc dữ liệu JSON và PlayerPrefs để đưa game về trạng thái tân thủ tuyệt đối
    /// </summary>
    public void OnConfirmResetClicked()
    {
        PlayClickSound();

        if (resetAllGameData)
        {
            // 1. Reset dữ liệu trong bộ nhớ RAM và ghi đè file lưu qua GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetNewPlayerData();
            }

            // 2. Xóa trực tiếp file JSON trên ổ đĩa (đảm bảo sạch hoàn toàn ngay cả khi chạy lẻ Scene này)
            string savePath = Application.persistentDataPath + "/PlayerSaveData.json";
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
                Debug.Log(">>> [RESET] Đã xóa file lưu JSON: " + savePath);
            }

            // 3. Xóa sạch PlayerPrefs (Tên nhân vật, cờ tân thủ TutorialCompleted, cài đặt tạm)
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();

            Debug.Log("<color=green>>>> [RESET] ĐÃ RESET TOÀN BỘ DỮ LIỆU GAME VỀ 0 (COIN = 0, DIAMOND = 0)!</color>");
        }
        else
        {
            // Chỉ xóa tên và tiến trình hướng dẫn
            PlayerPrefs.DeleteKey("PlayerName");
            PlayerPrefs.DeleteKey("TutorialCompleted");
            PlayerPrefs.Save();

            Debug.Log("<color=yellow>>>> [RESET] Chỉ xóa PlayerName và TutorialCompleted!</color>");
        }

        // Xóa sạch text trong ô input
        if (nameInputField != null) nameInputField.text = "";

        CloseAllPopups();
    }

    #endregion

    #region XỬ LÝ TAP TO START & ĐẶT TÊN

    public void OnTapToStartClicked()
    {
        if (isStarting) return;

        // Nếu đang mở popup nào thì không nhận click Start
        if ((nameInputPopup != null && nameInputPopup.activeInHierarchy) ||
            (resetConfirmPopup != null && resetConfirmPopup.activeInHierarchy))
        {
            return;
        }

        // Kiểm tra xem đã có tên người chơi chưa
        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "");

        if (string.IsNullOrEmpty(savedPlayerName))
        {
            // NGƯỜI CHƠI MỚI -> Mở bảng nhập tên
            PlayClickSound();
            OpenNameInputPopup();
        }
        else
        {
            // NGƯỜI CHƠI CŨ -> Vào game ngay
            ExecuteGameStart();
        }
    }

    private void OpenNameInputPopup()
    {
        if (nameInputPopup != null) nameInputPopup.SetActive(true);
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(true);
        if (errorText != null) errorText.text = "";

        if (nameInputField != null)
        {
            nameInputField.text = "";
            nameInputField.ActivateInputField(); // Tự động focus con trỏ soạn thảo
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayPopupOpenSound();
        }
    }

    public void OnReturnNameClicked()
    {
        PlayClickSound();
        CloseAllPopups();
    }

    public void OnConfirmNameClicked()
    {
        if (nameInputField == null) return;

        string inputName = nameInputField.text.Trim();

        // Kiểm tra độ dài từ 3 đến 12 ký tự
        if (inputName.Length < 3 || inputName.Length > 12)
        {
            ShowError("Tên phải từ 3 đến 12 ký tự!");
            return;
        }

        // Kiểm tra ký tự đặc biệt (cho phép tiếng Việt có dấu, chữ cái, số và khoảng trắng)
        if (!Regex.IsMatch(inputName, @"^[a-zA-Z0-9À-ỹ\s]+$"))
        {
            ShowError("Tên không được chứa ký tự đặc biệt!");
            return;
        }

        // Lưu tên người chơi mới
        PlayerPrefs.SetString("PlayerName", inputName);
        PlayerPrefs.Save();
        PlayClickSound();

        CloseAllPopups();

        // Bắt đầu vào game
        ExecuteGameStart();
    }

    private void ShowError(string message)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayErrorSound();
        }

        if (errorText != null)
        {
            errorText.text = message;
        }
    }

    #endregion

    #region HIỆU ỨNG VÀO GAME (TRANSITION)

    private void ExecuteGameStart()
    {
        isStarting = true;

        if (tapToStartText != null)
        {
            StartCoroutine(FlashRedTextEffect());
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayStartSound();
            StartCoroutine(FadeOutBGM());
        }

        StartCoroutine(FadeOutRoutine());
    }

    private IEnumerator FlashRedTextEffect()
    {
        float timer = 0f;
        Color startColor = tapToStartText.color;

        while (timer < transitionDelay)
        {
            float t = Mathf.PingPong(Time.time * 8f, 1f);
            tapToStartText.color = Color.Lerp(startColor, clickedColor, t);
            timer += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOutBGM()
    {
        if (GameManager.Instance == null || GameManager.Instance.bgmSource == null) yield break;

        AudioSource bgm = GameManager.Instance.bgmSource;
        float startVolume = bgm.volume;

        while (bgm.volume > 0)
        {
            bgm.volume -= startVolume * (Time.deltaTime / transitionDelay);
            yield return null;
        }
    }

    private IEnumerator FadeOutRoutine()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.gameObject.SetActive(true);
            fadeCanvasGroup.blocksRaycasts = true;
            float timer = 0f;
            while (timer < transitionDelay)
            {
                timer += Time.deltaTime;
                fadeCanvasGroup.alpha = Mathf.Clamp01(timer / transitionDelay);
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(transitionDelay);
        }

        if (GameManager.Instance != null && GameManager.Instance.bgmSource != null)
        {
            GameManager.Instance.bgmSource.volume = 1f;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    #endregion
}