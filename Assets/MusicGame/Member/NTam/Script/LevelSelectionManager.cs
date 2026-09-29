using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectionManager : MonoBehaviour
{
    [Header("=== THIẾT LẬP SCROLL VIEW ===")]
    public RectTransform[] levelCards; 

    [Header("=== NÚT PLAY ===")]
    public Button masterPlayButton; 

    [Header("=== DỮ LIỆU LEVEL ===")]
    public bool[] unlockedLevels = new bool[] { true, false, false, false, false };

    // BƯỚC CẢI TIẾN: Chỉ cần khai báo Tiền tố chung (Ví dụ: "Level_")
    [Tooltip("Nhập phần chữ chung của các Scene. Ví dụ scene tên Level_1 thì nhập Level_")]
    public string scenePrefix = "Level_"; 

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

        if (unlockedLevels[currentCardIndex])
        {
            masterPlayButton.interactable = true;
        }
        else
        {
            masterPlayButton.interactable = false;
        }
    }

    public void OnMasterPlayClicked()
    {
        if (unlockedLevels[currentCardIndex])
        {
            // TỰ ĐỘNG NỐI CHUỖI: scenePrefix + (Vị trí thẻ + 1)
            // Ví dụ: Thẻ đầu tiên là index 0 -> "Level_" + (0 + 1) = "Level_1"
            string sceneToLoad = scenePrefix + (currentCardIndex + 1);
            
            Debug.Log("Đang tải Màn chơi: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    public void OnMainPlayButtonClicked()
    {
        int highestLevel = PlayerPrefs.GetInt("LevelReached", 1);
        PlayerPrefs.SetInt("CurrentPlayingLevel", highestLevel);

        // TỰ ĐỘNG NỐI CHUỖI ĐỒNG BỘ: Dùng chung scenePrefix
        string sceneToLoad = scenePrefix + highestLevel;
        SceneManager.LoadScene(sceneToLoad);
    }
}