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
    
    [Tooltip("Nhập tên Scene cho từng Level. Màn nào chưa làm thì CỨ ĐỂ TRỐNG")]
    public string[] levelSceneNames = new string[] { "GamePlay1", "", "", "", "" }; // Tạo sẵn 5 chỗ trống

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

        // Bật nút Play NẾU: Thẻ đã mở khóa VÀ có tên Scene (không bị để trống)
        if (unlockedLevels[currentCardIndex] && !string.IsNullOrEmpty(levelSceneNames[currentCardIndex]))
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
        string sceneToLoad = levelSceneNames[currentCardIndex];
        
        // Chỉ load nếu tên Scene không bị trống
        if (unlockedLevels[currentCardIndex] && !string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.Log("Đang tải Màn chơi: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogWarning("Màn chơi này chưa được thiết lập Scene!");
        }
    }

    public void OnMainPlayButtonClicked()
    {
        int highestLevel = PlayerPrefs.GetInt("LevelReached", 1);
        PlayerPrefs.SetInt("CurrentPlayingLevel", highestLevel);

        // Mảng bắt đầu từ 0, nên level 1 tương ứng với index 0
        string sceneToLoad = levelSceneNames[highestLevel - 1]; 

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}