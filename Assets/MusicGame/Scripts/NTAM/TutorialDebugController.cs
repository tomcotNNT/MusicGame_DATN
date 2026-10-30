using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialDebugController : MonoBehaviour
{
    [Header("Cài đặt phím tắt (PC Only)")]
    [SerializeField] private KeyCode cheatKey = KeyCode.G;
    [SerializeField] private string lobbySceneName = "Lobby";

    [Header("UI WinPanel Tham Chiếu")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private TextMeshProUGUI scoreText;        // Chữ "0000000"
    [SerializeField] private TextMeshProUGUI rankOrStatusText; // Chữ "New Text"
    [SerializeField] private GameObject[] starObjects;         // Mảng 3 GameObject ngôi sao

    [Header("Nút điều hướng trên WinPanel")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button homeButton;

    private bool isCompleted = false;

    private void Start()
    {
        // Ẩn WinPanel khi vừa bắt đầu màn chơi
        if (winPanel != null && winPanel.activeSelf)
        {
            winPanel.SetActive(false);
        }

        // Đăng ký sự kiện quay về Lobby cho nút Next và Home
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ReturnToLobby);
        }

        if (homeButton != null)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(ReturnToLobby);
        }
    }

    private void Update()
    {
        // Chỉ chạy trên Unity Editor hoặc bản xuất game máy tính (PC / Standalone)
        #if UNITY_EDITOR || UNITY_STANDALONE
        bool pressedG = false;
        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.gKey.wasPressedThisFrame)
        {
            pressedG = true;
        }
        #else
        if (Input.GetKeyDown(cheatKey))
        {
            pressedG = true;
        }
        #endif

        if (pressedG && !isCompleted)
        {
            TriggerInstantWin();
        }
        #endif
    }

    public void TriggerInstantWin()
    {
        isCompleted = true;
        Debug.Log(">>> [CHEAT G] Đã kích hoạt phím G! Mở WinPanel.");

        // 1. Lưu trạng thái ngay lập tức khi kích hoạt cheat
        SaveTutorialProgress();

        // 2. Kích hoạt WinPanel
        if (winPanel != null)
        {
            winPanel.SetActive(true);
        }

        // 3. Random điểm số phong cách nhịp điệu (từ 850,000 đến 999,999)
        int randomScore = Random.Range(850000, 999999);
        if (scoreText != null)
        {
            scoreText.text = randomScore.ToString("D7");
        }

        // 4. Random số sao đạt được (2 hoặc 3 sao)
        int starsEarned = Random.Range(2, 4); 
        if (starObjects != null && starObjects.Length > 0)
        {
            for (int i = 0; i < starObjects.Length; i++)
            {
                if (starObjects[i] != null)
                {
                    starObjects[i].SetActive(i < starsEarned);
                }
            }
        }

        // 5. Cập nhật hạng (Rank)
        if (rankOrStatusText != null)
        {
            rankOrStatusText.text = (starsEarned == 3) ? "RANK S - CLEAR!" : "RANK A - CLEAR!";
        }
    }

    public void ReturnToLobby()
    {
        // BẮT BUỘC: Lưu cờ TutorialCompleted = 1 tại đây
        // Đảm bảo dù người chơi thắng bằng cheat G hay thắng bình thường đều được mở khóa Lobby
        SaveTutorialProgress();

        Debug.Log(">>> [CLICK NEXT/HOME] Đã lưu TutorialCompleted = 1. Đang tải scene: " + lobbySceneName);

        Time.timeScale = 1f; // Khôi phục thời gian đề phòng game đang pause
        SceneManager.LoadScene(lobbySceneName);
    }

    private void SaveTutorialProgress()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
        Debug.Log(">>> [DATA SAVED] PlayerPrefs 'TutorialCompleted' = 1 thành công!");
    }
}