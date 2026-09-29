using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bắt buộc phải có để chuyển Scene

public class LevelCardManager : MonoBehaviour
{
    [Header("=== CẤU HÌNH MÀN CHƠI ===")]
    public int levelID;           // Nhập 1 cho Level 1, 2 cho Level 2...
    public string sceneName;      // Nhập đúng tên Scene (VD: "Level_1")

    [Header("=== KÉO THẢ UI ===")]
    public GameObject lockIcon;   // Kéo object Lock_Icon vào đây
    public Button cardButton;     // Kéo component Button của chính thẻ này vào

    private bool isUnlocked;

    private void Start()
    {
        // Kiểm tra Level cao nhất người chơi đã đạt được (Mặc định người mới chơi là 1)
        int levelReached = PlayerPrefs.GetInt("LevelReached", 1);

        // LOGIC MỞ KHÓA
        if (levelID <= levelReached) 
        {
            isUnlocked = true;
            lockIcon.SetActive(false);     // Ẩn ổ khóa
            cardButton.interactable = true;// Cho phép bấm vào thẻ
            
            // Xóa màu tối của thẻ (Nếu bạn có hiệu ứng làm tối thẻ khi khóa)
            GetComponent<Image>().color = Color.white; 
        }
        else // BỊ KHÓA
        {
            isUnlocked = false;
            lockIcon.SetActive(true);       // Hiện ổ khóa
            cardButton.interactable = false;// Chặn không cho bấm
            
            // Làm thẻ tối đi một chút để người chơi dễ nhận biết
            GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1f); 
        }

        // Tự động gắn hàm LoadLevel vào sự kiện bấm nút của thẻ
        cardButton.onClick.AddListener(OnCardClicked);
    }

    // Hàm này chạy khi bấm trực tiếp vào LevelCard
    public void OnCardClicked()
    {
        if (isUnlocked)
        {
            // Lưu lại thông tin đang chơi màn nào (để code trong game biết đường tính điểm/tăng level)
            PlayerPrefs.SetInt("CurrentPlayingLevel", levelID);
            
            // Chuyển Scene
            SceneManager.LoadScene(sceneName);
        }
    }
}