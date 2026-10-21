using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterTutorialGuide : MonoBehaviour
{
    [Header("Cài đặt Nhân vật mặc định")]
    [Tooltip("Chỉ số của Ramona (Vị trí thứ 3 trong hàng tính từ 0 là Index 2)")]
    [SerializeField] private int ramonaIndex = 2; 

    [Header("Quản Lý Layer Highlight Ramona")]
    [Tooltip("Kéo GameObject Char_01 vào đây")]
    [SerializeField] private GameObject ramonaObject;
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

        // Luôn chọn mặc định Ramona khi vào Scene
        SelectRamona();

        if (isFirstTime)
        {
            // === TÂN THỦ: BẬT NỔI BẬT RAMONA & BÀN TAY HƯỚNG DẪN ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(true);
            if (handPointer != null) handPointer.gameObject.SetActive(true);
            if (backButton != null) backButton.interactable = false;

            EnableRamonaHighlight();

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(OnConfirmPlayClicked);
            }
        }
        else
        {
            // === NGƯỜI CHƠI CŨ: RAMONA VỀ BÌNH THƯỜNG & MỞ KHÓA TƯƠNG TÁC ===
            if (tutorialOverlay != null) tutorialOverlay.SetActive(false);
            if (handPointer != null) handPointer.gameObject.SetActive(false);
            if (backButton != null) backButton.interactable = true;

            // Đưa Ramona về bình thường (giữ nguyên hình ảnh, tắt hào quang)
            ResetRamonaToNormal();

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
            // Bàn tay nhấp nhô
            if (handPointer != null)
            {
                float bounce = Mathf.Sin(Time.time * 6f) * 12f;
                handPointer.anchoredPosition = initialHandPos + new Vector2(0, bounce);
            }

            // Hào quang sau lưng Ramona nhấp nháy phát sáng
            if (ramonaGlowEffect != null && ramonaGlowEffect.activeInHierarchy)
            {
                float pulse = 0.8f + Mathf.PingPong(Time.time * 2f, 0.4f);
                ramonaGlowEffect.transform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }
    }

    /// <summary>
    /// Tân thủ: Đẩy layer Ramona lên Order = 5 để nổi trên nền tối Tutorial
    /// </summary>
    public void EnableRamonaHighlight()
    {
        if (ramonaObject != null)
        {
            Canvas c = ramonaObject.GetComponent<Canvas>();
            if (c != null)
            {
                c.enabled = true;
                c.overrideSorting = true;
                c.sortingOrder = 5;
            }
        }

        if (ramonaGlowEffect != null)
        {
            ramonaGlowEffect.SetActive(true);
        }
    }

    /// <summary>
    /// Đưa Ramona về trạng thái bình thường: Giữ nguyên hình ảnh, không phá hủy Canvas
    /// </summary>
    public void ResetRamonaToNormal()
    {
        if (ramonaObject != null)
        {
            Canvas c = ramonaObject.GetComponent<Canvas>();
            if (c != null)
            {
                // Chỉ tắt Override Sorting để hòa nhập vào Canvas chung của Scene
                c.overrideSorting = false;
                c.enabled = true; // Bắt buộc bật để Image luôn hiển thị
            }
        }

        // Tắt hào quang trắng
        if (ramonaGlowEffect != null)
        {
            ramonaGlowEffect.SetActive(false);
        }

        Debug.Log("<color=green>>>> [TUTORIAL] Ramona đã trở lại trạng thái hiển thị bình thường!</color>");
    }

    public void SelectRamona()
    {
        PlayerPrefs.SetInt("SelectedCharacterIndex", ramonaIndex);
        PlayerPrefs.Save();
        Debug.Log(">>> [CHARACTER] Mặc định đã chọn RAMONA (Index: " + ramonaIndex + ")!");
    }

    private void OnConfirmPlayClicked()
    {
        SelectRamona();

        // Đánh dấu đã hoàn thành Tutorial
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();

        // Đưa Ramona về bình thường trước khi chuyển Scene
        ResetRamonaToNormal();

        SceneManager.LoadScene(nextSceneName);
    }
}