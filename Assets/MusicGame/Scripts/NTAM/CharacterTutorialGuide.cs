using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterTutorialGuide : MonoBehaviour
{
    [Header("Cài đặt Nhân vật mặc định")]
    [Tooltip("Chỉ số của Ramona (Vị trí thứ 3 trong hàng tính từ 0 là Index 2)")]
    [SerializeField] private int ramonaIndex = 2; 
    [SerializeField] private GameObject ramonaGlowEffect; // Kéo GlowEffect của Ramona vào đây

    [Header("UI Hướng Dẫn Tân Thủ")]
    [SerializeField] private GameObject tutorialOverlay; 
    [SerializeField] private RectTransform handPointer;    
    [SerializeField] private Button playButton;          
    [SerializeField] private Button backButton;          

    [Header("Scene Tiếp Theo")]
    [SerializeField] private string nextSceneName = "Story";

    private bool isFirstTime = true;
    private Vector2 initialHandPos;

    private void Awake()
    {
        int tutorialCompleted = PlayerPrefs.GetInt("TutorialCompleted", 0);
        isFirstTime = (tutorialCompleted == 0);

        if (handPointer != null)
        {
            initialHandPos = handPointer.anchoredPosition;
        }

        // Luôn luôn chọn mặc định Ramona khi vào scene
        SelectRamona();

        if (isFirstTime)
        {
            // === TÂN THỦ: ÉP CHỌN RAMONA & CHỈ SÁNG RAMONA + NÚT PLAY ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            if (backButton != null) backButton.interactable = false;

            // Bật hiệu ứng phát sáng cho Ramona
            if (ramonaGlowEffect != null) ramonaGlowEffect.SetActive(true);

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnConfirmPlayClicked);
            }
        }
        else
        {
            // === NGƯỜI CHƠI CŨ: MỞ KHÓA TOÀN BỘ ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            if (backButton != null) backButton.interactable = true;

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnConfirmPlayClicked);
            }

            this.enabled = false;
        }
    }

    private void Update()
    {
        if (isFirstTime)
        {
            // Hiệu ứng bàn tay nhấp nhô chỉ vào nút PLAY
            if (handPointer != null)
            {
                float bounce = Mathf.Sin(Time.time * 6f) * 12f;
                handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
            }

            // Hiệu ứng hào quang sau lưng Ramona nhấp nháy phát sáng nhẹ
            if (ramonaGlowEffect != null)
            {
                float pulse = 0.8f + Mathf.PingPong(Time.time * 2f, 0.4f);
                ramonaGlowEffect.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    public void SelectRamona()
    {
        // Ghi nhận Ramona vào dữ liệu chơi
        PlayerPrefs.SetInt("SelectedCharacterIndex", ramonaIndex);
        PlayerPrefs.Save();
        Debug.Log(">>> [CHARACTER] Mặc định đã chọn RAMONA (Index: " + ramonaIndex + ")!");
    }

    private void OnConfirmPlayClicked()
    {
        SelectRamona();
        SceneManager.LoadScene(nextSceneName);
    }
}