using UnityEngine;
using UnityEngine.UI;

public class InfiniteLevelScroll : MonoBehaviour
{
    [Header("=== THÀNH PHẦN ===")]
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    
    [Tooltip("Kéo component Horizontal Layout Group của Content vào đây")]
    public HorizontalLayoutGroup layoutGroup; 

    private float itemWidth;

    private void Start()
    {
        // Tự động tính toán khoảng cách chuẩn xác 100% (Chiều rộng thẻ + Spacing)
        if (contentPanel.childCount > 0 && layoutGroup != null)
        {
            RectTransform firstChild = contentPanel.GetChild(0).GetComponent<RectTransform>();
            itemWidth = firstChild.rect.width + layoutGroup.spacing;
        }
    }

    private void Update()
    {
        if (contentPanel.childCount < 3 || itemWidth == 0) return;

        // Xử lý vuốt sang trái (Vuốt từ Level 1 tiến tới Level 5)
        if (contentPanel.anchoredPosition.x < -itemWidth)
        {
            // Bốc thẻ đầu tiên ném ra sau cùng
            Transform firstChild = contentPanel.GetChild(0);
            firstChild.SetAsLastSibling();
            
            // QUAN TRỌNG: Ép Unity tính toán lại Layout ngay lập tức để không bị giật
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
            
            // Dịch khung bù trừ
            contentPanel.anchoredPosition += new Vector2(itemWidth, 0);
        }
        // Xử lý vuốt sang phải (Vuốt lùi từ Level 1 về lại Level 5)
        else if (contentPanel.anchoredPosition.x > 0)
        {
            // Bốc thẻ cuối cùng ném lên đầu
            Transform lastChild = contentPanel.GetChild(contentPanel.childCount - 1);
            lastChild.SetAsFirstSibling();
            
            // QUAN TRỌNG: Ép Unity tính toán lại Layout ngay lập tức
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentPanel);
            
            // Dịch khung bù trừ
            contentPanel.anchoredPosition -= new Vector2(itemWidth, 0);
        }
    }
}