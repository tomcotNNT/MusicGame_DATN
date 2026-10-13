using TMPro;
using UnityEngine;

/// <summary>
/// Hiển thị số coin. Gắn vào Text trong scene màn chơi (sessionText)
/// và/hoặc Text tổng coin ở scene chọn màn (totalText).
/// Dùng Text thường thay vì TMP thì đổi TMP_Text thành UnityEngine.UI.Text.
/// </summary>
public class CoinUI : MonoBehaviour
{
    [Tooltip("Coin trong màn đang chơi")]
    [SerializeField] private TMP_Text sessionText;

    [Tooltip("Tổng coin đã lưu (dùng ở scene chọn màn/shop)")]
    [SerializeField] private TMP_Text totalText;

    private void OnEnable()
    {
        CoinSystem.OnSessionCoinsChanged += UpdateSession;

        UpdateSession(CoinSystem.Instance != null ? CoinSystem.Instance.SessionCoins : 0);
        RefreshTotal();
    }

    private void OnDisable()
    {
        CoinSystem.OnSessionCoinsChanged -= UpdateSession;
    }

    private void UpdateSession(int value)
    {
        if (sessionText != null)
            sessionText.text = value.ToString();
    }

    public void RefreshTotal()
    {
        if (totalText != null)
            totalText.text = CoinSystem.TotalCoins.ToString();
    }
}