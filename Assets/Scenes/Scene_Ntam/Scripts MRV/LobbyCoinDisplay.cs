
using UnityEngine;
using TMPro;

public class LobbyCoinDisplay : MonoBehaviour
{
    [Header("Coin UI")]
    public TMP_Text coinText;

    private void Start()
    {
        UpdateCoinUI();
    }

    public void UpdateCoinUI()
    {
        int currentCoin = PlayerPrefs.GetInt("Coin", 0);

        if (coinText != null)
        {
            coinText.text = currentCoin.ToString("N0");
        }
    }
}
