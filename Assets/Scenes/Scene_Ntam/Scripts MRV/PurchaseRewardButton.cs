using UnityEngine;
using UnityEngine.UI;

// TEST ONLY: grants virtual currency without processing a real payment.
[RequireComponent(typeof(Button))]
public class PurchaseRewardButton : MonoBehaviour
{
    public enum CurrencyType { Coin, Diamond }

    [Header("Test Purchase Reward")]
    [SerializeField] private CurrencyType currencyType = CurrencyType.Coin;
    [Min(1)] [SerializeField] private int amount = 100;

    private Button purchaseButton;

    private void Awake()
    {
        purchaseButton = GetComponent<Button>();
    }

    private void OnEnable()
    {
        if (purchaseButton == null) purchaseButton = GetComponent<Button>();
        purchaseButton.onClick.AddListener(GrantTestReward);
    }

    private void OnDisable()
    {
        if (purchaseButton != null) purchaseButton.onClick.RemoveListener(GrantTestReward);
    }

    public void GrantTestReward()
    {
        if (amount <= 0)
        {
            Debug.LogWarning("Purchase amount must be greater than zero.", this);
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogError("Purchase failed: GameManager.Instance is missing.", this);
            return;
        }

        if (currencyType == CurrencyType.Coin)
            GameManager.Instance.AddCoins(amount);
        else
            GameManager.Instance.AddDiamonds(amount);

        Debug.Log($"[TEST PURCHASE] Added {amount} {currencyType}.", this);
    }
}
