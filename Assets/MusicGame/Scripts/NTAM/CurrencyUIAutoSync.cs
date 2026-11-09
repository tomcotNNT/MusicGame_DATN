using UnityEngine;
using TMPro;

public class CurrencyUIAutoSync : MonoBehaviour
{
    [Header("UI Text Hiển Thị Tiền (TextMeshPro)")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI diamondText;

    private void OnEnable()
    {
        // Đăng ký nhận thông báo khi số tiền thay đổi
        GameManager.OnCurrencyChanged += RefreshUI;
    }

    private void OnDisable()
    {
        // Hủy đăng ký khi Scene bị tắt để tránh rò rỉ bộ nhớ
        GameManager.OnCurrencyChanged -= RefreshUI;
    }

    private void Start()
    {
        // Vừa vào Scene: Tự động cập nhật số tiền ngay lập tức
        RefreshUI();
    }

    /// <summary>
    /// Đọc dữ liệu mới nhất từ GameManager và nạp vào Text
    /// </summary>
    public void RefreshUI()
    {
        if (GameManager.Instance == null) return;

        if (goldText != null)
        {
            goldText.text = GameManager.Instance.playerData.totalCoins.ToString();
        }

        if (diamondText != null)
        {
            diamondText.text = GameManager.Instance.playerData.totalDiamonds.ToString();
        }
    }
}