using UnityEngine;

public class SongCardEffect : MonoBehaviour
{
    [Header("Hiệu ứng Nhấp Nhô (Floating)")]
    [SerializeField] private float floatSpeed = 5f;      
    [SerializeField] private float floatAmplitude = 8f;  
    
    [Header("Hiệu ứng Viền Neon")]
    [SerializeField] private GameObject neonBorderObject; // Kéo NeonBorderFrame vào đây

    private RectTransform rectTransform;
    private Vector2 defaultAnchoredPos;
    private bool isCurrentActive = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        defaultAnchoredPos = rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (isCurrentActive)
        {
            // Hiệu ứng nhấp nhô nhẹ nhàng khi ở giữa
            float newY = defaultAnchoredPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
        }
        else
        {
            // Trả về vị trí Y ban đầu khi không ở giữa
            Vector2 targetPos = new Vector2(rectTransform.anchoredPosition.x, defaultAnchoredPos.y);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPos, Time.deltaTime * 10f);
        }
    }

    // Hàm nhận lệnh trực tiếp từ ListSongControl
    public void SetActiveCard(bool isActive)
    {
        isCurrentActive = isActive;

        if (neonBorderObject != null)
        {
            neonBorderObject.SetActive(isActive); // Bật/tắt chuẩn xác viền neon
        }

        if (!isActive)
        {
            defaultAnchoredPos = rectTransform.anchoredPosition;
        }
    }
}