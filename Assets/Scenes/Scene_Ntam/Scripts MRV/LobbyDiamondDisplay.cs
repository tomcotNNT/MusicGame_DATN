using TMPro;
using UnityEngine;

public class LobbyDiamondDisplay : MonoBehaviour
{
    [Header("Diamond Text")]
    public TMP_Text diamondText;

    private void OnEnable()
    {
        GameManager.OnCurrencyChanged -= UpdateDiamondUI;
        GameManager.OnCurrencyChanged += UpdateDiamondUI;
        UpdateDiamondUI();
    }

    private void Start()
    {
        UpdateDiamondUI();
    }

    private void OnDisable()
    {
        GameManager.OnCurrencyChanged -= UpdateDiamondUI;
    }

    public void UpdateDiamondUI()
    {
        if (diamondText == null) return;
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[LobbyDiamondDisplay] GameManager chưa tồn tại.", this);
            return;
        }
        diamondText.text = GameManager.Instance.playerData.totalDiamonds.ToString("N0");
    }
}
