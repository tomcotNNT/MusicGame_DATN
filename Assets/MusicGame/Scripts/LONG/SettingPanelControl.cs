using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SettingPanelControl : MonoBehaviour
{
    [Header("Buttons")]
    public Button settingButton;
    public Button backButton;

    [Header("Setting Panel")]
    public GameObject settingPanel;
    public RectTransform panelRect;

    [Header("Animation")]
    public float animationDuration = 0.4f;

    private Vector2 centerPosition;
    private Vector2 topPosition;

    private bool isAnimating = false;

    private void Start()
    {
        // Gán sự kiện cho Setting Button
        settingButton.onClick.AddListener(OpenSetting);

        // Gán sự kiện cho Back Button
        backButton.onClick.AddListener(CloseSetting);

        // Lấy vị trí hiện tại của Panel
        centerPosition = panelRect.anchoredPosition;

        // Vị trí Panel nằm phía trên màn hình
        topPosition = new Vector2(
            centerPosition.x,
            centerPosition.y + panelRect.rect.height
        );

        // Đưa Panel lên trên
        panelRect.anchoredPosition = topPosition;

        // Ban đầu Panel tắt
        settingPanel.SetActive(false);
    }

    public void OpenSetting()
    {
        if (isAnimating)
            return;

        // Active Panel trước
        settingPanel.SetActive(true);

        // Đặt Panel ở phía trên
        panelRect.anchoredPosition = topPosition;

        // Chạy Panel xuống
        StartCoroutine(MovePanel(topPosition, centerPosition));
    }

    public void CloseSetting()
    {
        if (isAnimating)
            return;

        // Chạy Panel lên
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

            // Ease Out
            t = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                endPosition,
                t
            );

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

            // Ease Out
            t = 1f - Mathf.Pow(1f - t, 3f);

            panelRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                topPosition,
                t
            );

            yield return null;
        }

        panelRect.anchoredPosition = topPosition;

        // Chạy lên xong mới tắt
        settingPanel.SetActive(false);

        isAnimating = false;
    }
}