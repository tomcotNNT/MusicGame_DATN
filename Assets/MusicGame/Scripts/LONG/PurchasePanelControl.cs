
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PurchasePanelControl : MonoBehaviour
{
    [Header("Buttons")]
    public Button[] purchaseButtons;
    public Button backButton;

    [Header("Purchase Panel")]
    public GameObject purchasePanel;
    public RectTransform panelRect;

    [Header("Animation")]
    public float animationDuration = 0.4f;

    [Header("Gold / Diamond")]
    public GameObject goldPurchase;
    public GameObject diamondPurchase;

    public Button chooseGold;
    public Button chooseDiamond;

    private Vector2 centerPosition;
    private Vector2 topPosition;
    private bool isAnimating = false;

    private void Start()
    {
        // Gán sự kiện cho tất cả Purchase Button
        foreach (Button button in purchaseButtons)
        {
            if (button != null)
                button.onClick.AddListener(OpenPurchase);
        }

        backButton.onClick.AddListener(ClosePurchase);

        centerPosition = panelRect.anchoredPosition;

        topPosition = new Vector2(
            centerPosition.x,
            centerPosition.y + panelRect.rect.height
        );

        panelRect.anchoredPosition = topPosition;
        purchasePanel.SetActive(false);

        // Gán sự kiện chọn Gold / Diamond
        chooseGold.onClick.AddListener(ChooseGold);
        chooseDiamond.onClick.AddListener(ChooseDiamond);

        // Mặc định chọn Diamond
        ChooseDiamond();
    }

    public void OpenPurchase()
    {
        if (isAnimating)
            return;

        purchasePanel.SetActive(true);
        panelRect.anchoredPosition = topPosition;

        StartCoroutine(MovePanel(topPosition, centerPosition));
    }

    public void ClosePurchase()
    {
        if (isAnimating)
            return;

        StartCoroutine(MovePanelBack());
    }

    private IEnumerator MovePanel(Vector2 startPosition, Vector2 endPosition)
    {
        isAnimating = true;
        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / animationDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition =
                Vector2.Lerp(startPosition, endPosition, t);

            yield return null;
        }

        panelRect.anchoredPosition = endPosition;
        isAnimating = false;
    }

    private IEnumerator MovePanelBack()
    {
        isAnimating = true;
        Vector2 startPosition = panelRect.anchoredPosition;
        float time = 0f;

        while (time < animationDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / animationDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition =
                Vector2.Lerp(startPosition, topPosition, t);

            yield return null;
        }

        panelRect.anchoredPosition = topPosition;
        purchasePanel.SetActive(false);
        isAnimating = false;
    }

    
    public void ChooseGold()
    {
        goldPurchase.SetActive(true);
        diamondPurchase.SetActive(false);
    }

    public void ChooseDiamond()
    {
        goldPurchase.SetActive(false);
        diamondPurchase.SetActive(true);
    }

}
