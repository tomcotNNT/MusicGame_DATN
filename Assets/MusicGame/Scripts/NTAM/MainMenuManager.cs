using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName = "SelectSong"; // Cho phép nhập tên Scene tiếp theo trực tiếp trên Inspector

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

    [Header("Hiệu ứng Chuyển Cảnh")]
    public CanvasGroup fadeCanvasGroup; 
    public float transitionDelay = 1.5f; 
    private bool isStarting = false;

    private void Start()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 0f;
            fadeCanvasGroup.gameObject.SetActive(false);
        }

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

    public void OnResetButtonClicked()
    {
        if (isStarting) return;
        PlayClickSound();
        if (resetConfirmPopup != null) resetConfirmPopup.SetActive(true);
    }

    public void OnCancelResetClicked()
    {
        PlayClickSound();
        if (resetConfirmPopup != null) resetConfirmPopup.SetActive(false);
    }

    public void OnConfirmResetClicked()
    {
        PlayClickSound();
        PlayerPrefs.DeleteKey("PlayerName");
        PlayerPrefs.Save(); 
        if (resetConfirmPopup != null) resetConfirmPopup.SetActive(false);
    }

    public void OnTapToStartClicked()
    {
        if (isStarting) return; 

        if ((nameInputPopup != null && nameInputPopup.activeInHierarchy) ||
            (resetConfirmPopup != null && resetConfirmPopup.activeInHierarchy)) 
        {
            return;
        }

        string savedPlayerName = PlayerPrefs.GetString("PlayerName", "");
        
        if (string.IsNullOrEmpty(savedPlayerName))
        {
            PlayClickSound();
            OpenNameInputPopup(); 
            return; 
        }
        else
        {
            ExecuteGameStart();
        }
    }

    private void OpenNameInputPopup()
    {
        if (nameInputPopup != null) nameInputPopup.SetActive(true);
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(true); 
        if (errorText != null) errorText.text = ""; 
    }

    public void OnReturnNameClicked()
    {
        PlayClickSound();
        if (nameInputPopup != null) nameInputPopup.SetActive(false);
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(false); 
    }

    public void OnConfirmNameClicked()
    {
        if (nameInputField == null) return;

        string inputName = nameInputField.text.Trim();

        if (inputName.Length < 3 || inputName.Length > 12)
        {
            ShowError("The name must be between 3 and 12 characters long!");
            return; 
        }

        if (!Regex.IsMatch(inputName, @"^[a-zA-Z0-9À-ỹ\s]+$"))
        {
            ShowError("Must not contain special characters!");
            return;
        }

        PlayerPrefs.SetString("PlayerName", inputName);
        PlayerPrefs.Save();
        PlayClickSound();

        if (errorText != null) errorText.text = "";
        if (nameInputPopup != null) nameInputPopup.SetActive(false);
        if (dimBackgroundPanel != null) dimBackgroundPanel.SetActive(false);

        ExecuteGameStart();
    }

    private void ShowError(string message)
    {
        PlayClickSound();
        if (errorText != null)
        {
            errorText.text = message;
        }
    }

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
        Color targetColor = new Color(1f, 0.2f, 0.4f, 1f);

        while (timer < transitionDelay)
        {
            float t = Mathf.PingPong(Time.time * 8f, 1f); 
            tapToStartText.color = Color.Lerp(startColor, targetColor, t);
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

        // Chuyển sang Scene đích dựa vào chuỗi nhập trên Inspector (Thay vì dùng số Index cố định)
        SceneManager.LoadScene(nextSceneName);
    }
}