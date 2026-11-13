using UnityEngine;

public class UIParallax : MonoBehaviour
{
    [Header("Các lớp Background (Từ xa đến gần: Layer 1 -> Layer 4)")]
    public RectTransform[] layers; 

    [Header("Tốc độ dịch chuyển của từng lớp")]
    [Tooltip("Lớp ở xa để số nhỏ (VD: 0.002), lớp ở gần để số lớn hơn (VD: 0.02)")]
    public float[] parallaxSpeeds; 

    private Vector2 centerPoint;
    private Vector2[] initialPositions; // Mảng lưu vị trí gốc của từng layer

    void Start()
    {
        // Lấy tâm màn hình làm mốc đối chiếu
        centerPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);

        // Lưu lại vị trí ban đầu (Pos X, Pos Y) mà bạn đã căn chỉnh sẵn trong Editor
        initialPositions = new Vector2[layers.Length];
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null)
            {
                initialPositions[i] = layers[i].anchoredPosition;
            }
        }
    }

    void Update()
    {
        // Lấy vị trí chuột trừ đi tâm màn hình
        Vector2 mousePos = (Vector2)Input.mousePosition - centerPoint;

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] != null && i < parallaxSpeeds.Length)
            {
                // Vị trí mục tiêu = Vị trí gốc + (Độ dịch chuyển chuột * Tốc độ)
                Vector2 targetPos = initialPositions[i] + (mousePos * parallaxSpeeds[i]);
                
                // Di chuyển mượt mà (Lerp) về vị trí mới
                layers[i].anchoredPosition = Vector2.Lerp(layers[i].anchoredPosition, targetPos, Time.deltaTime * 5f);
            }
        }
    }
}