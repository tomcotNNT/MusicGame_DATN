using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Bắt buộc phải có để chuyển Scene

public class LevelSelectionManager : MonoBehaviour
{
    [Header("=== THIẾT LẬP SCROLL VIEW ===")]
    public RectTransform[] levelCards; // Kéo 5 thẻ LevelCard vào đây

    [Header("=== NÚT PLAY ===")]
    public Button masterPlayButton; // Kéo Play_Button vào đây

    [Header("=== DỮ LIỆU LEVEL ===")]
    // true = đã mở khóa, false = bị khóa
    public bool[] unlockedLevels = new bool[] { true, false, false, false, false };
    
    // Khai báo tên chính xác của 5 Scene bạn vừa tạo ở Bước 1
    public string[] levelSceneNames = new string[] { "Level_1", "Level_2", "Level_3", "Level_4", "Level_5" };

    [Header("=== HIỆU ỨNG ===")]
    public float scaleUp = 1.15f; 
    public float scaleNormal = 0.9f; 
    public float lerpSpeed = 10f; 

    private int currentCardIndex = 0; 
    private Vector2 centerPoint;

    void Start()
    {
        centerPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
    }

    void Update()
    {
        FindClosestCardToCenter();
        UpdateCardsScale();
    }

    private void FindClosestCardToCenter()
    {
        float minDistance = float.MaxValue;
        int closestIndex = 0;

        for (int i = 0; i < levelCards.Length; i++)
        {
            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(null, levelCards[i].position);
            float distance = Mathf.Abs(centerPoint.x - cardScreenPos.x);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex != currentCardIndex)
        {
            currentCardIndex = closestIndex;
            UpdateMasterPlayButton();
        }
    }

    private void UpdateCardsScale()
    {
        for (int i = 0; i < levelCards.Length; i++)
        {
            float targetScale = (i == currentCardIndex) ? scaleUp : scaleNormal;
            Vector3 targetVector = new Vector3(targetScale, targetScale, 1f);
            levelCards[i].localScale = Vector3.Lerp(levelCards[i].localScale, targetVector, Time.deltaTime * lerpSpeed);
        }
    }

    private void UpdateMasterPlayButton()
    {
        if (masterPlayButton == null) return;

        // Nếu thẻ đang ở giữa đã được mở khóa -> Nút Play sáng lên và bấm được
        if (unlockedLevels[currentCardIndex])
        {
            masterPlayButton.interactable = true;
        }
        // Nếu thẻ bị khóa -> Nút Play xám đi và không cho bấm
        else
        {
            masterPlayButton.interactable = false;
        }
    }

    // HÀM NÀY SẼ GẮN VÀO SỰ KIỆN CLICK CỦA NÚT PLAY
    public void OnMasterPlayClicked()
    {
        if (unlockedLevels[currentCardIndex])
        {
            string sceneToLoad = levelSceneNames[currentCardIndex];
            Debug.Log("Đang tải Màn chơi: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}