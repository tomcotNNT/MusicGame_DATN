using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Hiệu ứng UI")]
    public TextMeshProUGUI tapToStartText; 
    public Color clickedColor = Color.red; 

    [Header("UI Cài Đặt")]
    public GameObject settingsPopup; 

    [Header("UI Đặt Tên Lần Đầu")]
    public GameObject nameInputPopup;
    public TMP_InputField nameInputField;

    [Header("UI Reset Dữ Liệu")]
    public GameObject resetConfirmPopup;

    [Header("Hệ thống Âm thanh")]
    public AudioSource bgmSource;
    public AudioSource sfxClickSource;
    public AudioSource sfxStartSource;

    [Header("Cài đặt Chuyển cảnh")]
    public float transitionDelay = 1.5f; 
    private bool isStarting = false;

    // ==========================================
    // CÁC HÀM XỬ LÝ ÂM THANH CHUNG
    // ==========================================

    public void PlayClickSound()
    {
        if (sfxClickSource != null)
        {
            sfxClickSource.Play();
        }
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ BẢNG CÀI ĐẶT (SETTINGS)
    // ==========================================

    public void OnSettingsOpenClicked()
    {
        if (isStarting) return; 
        PlayClickSound(); 
        
        if (settingsPopup != null)
            settingsPopup.SetActive(true); 
    }

    public void OnSettingsCloseClicked()
    {
        PlayClickSound(); 
        
        if (settingsPopup != null)
            settingsPopup.SetActive(false); 
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ RESET DỮ LIỆU
    // ==========================================

    public void OnResetButtonClicked()
    {
        if (isStarting) return;
        PlayClickSound();

        if (resetConfirmPopup != null) 
            resetConfirmPopup.SetActive(true);
    }

    public void OnCancelResetClicked()
    {
        PlayClickSound();

        if (resetConfirmPopup != null) 
            resetConfirmPopup.SetActive(false);
    }

    public void OnConfirmResetClicked()
    {
        PlayClickSound();

        // 1. Xóa thông tin đã lưu (Có thể mở rộng xóa nhiều thứ khác sau này)
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save(); 
        
        Debug.Log("Hệ thống: Đã xóa toàn bộ dữ liệu người chơi!");

        // 2. Đóng Popup lại
        if (resetConfirmPopup != null) 
            resetConfirmPopup.SetActive(false);
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ VÀO GAME & ĐẶT TÊN
    // ==========================================

    public void OnTapToStartClicked()
    {
        if (isStarting) return; 

        // CHỐT AN TOÀN TOÀN DIỆN: Chặn click "Tap To Start" nếu BẤT KỲ popup nào đang mở
        if ((settingsPopup != null && settingsPopup.activeInHierarchy) || 
            (nameInputPopup != null && nameInputPopup.activeInHierarchy) ||
            (resetConfirmPopup != null && resetConfirmPopup.activeInHierarchy)) 
        {
            return;
        }

        // KIỂM TRA NGƯỜI CHƠI LẦN ĐẦU (HOẶC VỪA RESET)
        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "");
        
        if (string.IsNullOrEmpty(savedPlayerName))
        {
            // Chưa có tên -> Phát tiếng chạm nhỏ và MỞ BẢNG ĐẶT TÊN
            PlayClickSound();
            if (nameInputPopup != null) nameInputPopup.SetActive(true);
            return; 
        }
        else
        {
            // Đã có tên -> Tiến hành vào game 
            ExecuteGameStart();
        }
    }

    public void OnConfirmNameClicked()
    {
        string inputName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(inputName))
        {
            Debug.Log("Lỗi: Tên không hợp lệ hoặc để trống!");
            return; 
        }

        // 1. Lưu tên xuống máy
        PlayerPrefs.SetString("PlayerName", inputName);
        PlayerPrefs.Save();
        PlayClickSound();

        // 2. Ẩn bảng đặt tên
        if (nameInputPopup != null) nameInputPopup.SetActive(false);

        // 3. Tiến hành vào game 
        ExecuteGameStart();
    }

    // ==========================================
    // LOGIC CHUYỂN CẢNH (ANIMATION & LOAD SCENE)
    // ==========================================

    private void ExecuteGameStart()
    {
        isStarting = true;

        if (tapToStartText != null)
        {
            tapToStartText.color = clickedColor;
            StartCoroutine(FlickerTextEffect());
        }

        if (sfxStartSource != null) sfxStartSource.Play();
        if (bgmSource != null) StartCoroutine(FadeOutBGM());

        StartCoroutine(LoadLobbyScene());
    }

    private IEnumerator FlickerTextEffect()
    {
        while (true) 
        {
            tapToStartText.enabled = !tapToStartText.enabled; 
            yield return new WaitForSeconds(0.1f); 
        }
    }

    private IEnumerator FadeOutBGM()
    {
        if (bgmSource == null) yield break;

        float startVolume = bgmSource.volume;
        while (bgmSource.volume > 0)
        {
            bgmSource.volume -= startVolume * (Time.deltaTime / transitionDelay);
            yield return null;
        }
    }

    private IEnumerator LoadLobbyScene()
    {
        yield return new WaitForSeconds(transitionDelay);
        SceneManager.LoadScene(2);
    }
}