using TMPro;
using UnityEngine;

public class LobbyCoinDisplay : MonoBehaviour
{
    [Header("Coin UI")]
    public TMP_Text coinText;

    private void OnEnable()
    {
        GameManager.OnCurrencyChanged -= UpdateCoinUI;
        GameManager.OnCurrencyChanged += UpdateCoinUI;
        UpdateCoinUI();
    }

    private void Start()
    {
        UpdateCoinUI();
    }

    private void OnDisable()
    {
        GameManager.OnCurrencyChanged -= UpdateCoinUI;
    }

    public void UpdateCoinUI()
    {
        if (coinText == null) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LobbyCoinDisplay] GameManager chưa tồn tại.", this);
            return;
        }
        coinText.text = GameManager.Instance.playerData.totalCoins.ToString("N0");
    }
}
