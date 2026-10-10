using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DailyDisPanelControl : MonoBehaviour
{
    public GameObject dailyPanel;
    public TMP_Text targetText; 
    public Color color = Color.red; 
    public float thickness = 4f;

    void Start() 
    { 
        GameObject line = new GameObject("RedLine"); 

        line.transform.SetParent(targetText.transform, false); 

        RectTransform rect = line.AddComponent<RectTransform>(); 

        Image image = line.AddComponent<Image>(); 

        image.color = color; image.raycastTarget = false; 

        float w = targetText.rectTransform.rect.width; 
        float h = targetText.rectTransform.rect.height; 

        rect.sizeDelta = new Vector2( Mathf.Sqrt(w * w + h * h), thickness); 

        rect.anchoredPosition = Vector2.zero; 
        
        rect.localRotation = Quaternion.Euler( 0, 0, -Mathf.Atan2(h, w) * Mathf.Rad2Deg); 
    }

    public void OpenDailyPanel()
    {
        dailyPanel.SetActive(true);
    }

    public void CloseDailyPanel()
    {
        dailyPanel.SetActive(false);
    }
}