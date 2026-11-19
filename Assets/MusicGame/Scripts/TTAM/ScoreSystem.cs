using UnityEngine;
using TMPro;
using System.Collections;
using System.Globalization;

public class ScoreSystem : MonoBehaviour
{
    public enum ScoreGrade { Good, Great, Perfect }

    [Header("Score (Good 100 / Great 200 / Perfect 300)")]
    [SerializeField] private int goodScore = 100;
    [SerializeField] private int greatScore = 200;
    [SerializeField] private int perfectScore = 300;
    [Tooltip("Điểm mặc định khi gọi AddScore() không truyền mức")]
    [SerializeField] private int scorePerNote = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Score Popup (bay lên)")]
    [Tooltip("Text mẫu, đặt sẵn trong Canvas. Mỗi lần cộng điểm sẽ nhân bản từ nó")]
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector3 playerWorldOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float startXOffset = 60f;
    [SerializeField] private float startYOffset = -20f;
    [SerializeField] private float riseDistance = 90f;
    [SerializeField] private string popupPrefix = "+";

    [Header("Popup Timing")]
    [SerializeField] private float popAnimationDuration = 0.12f;
    [SerializeField] private float popupHideDelay = 0.5f;

    private int score;

    private RectTransform popupParentRect;
    private Vector2 lastPopupPos;
    private Vector3 originalPopupScale;

    private void Start()
    {
        score = 0;
        UpdateScoreUI();

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (popupText != null)
        {
            originalPopupScale = popupText.rectTransform.localScale;
            popupParentRect = popupText.rectTransform.parent as RectTransform;
            lastPopupPos = popupText.rectTransform.anchoredPosition;

            // Bản mẫu luôn ẩn
            popupText.text = "";
            popupText.gameObject.SetActive(false);
        }
    }

    // =====================================================
    // ADD SCORE
    // =====================================================

    public void AddScore()
    {
        AddScore(scorePerNote);
    }

    public void AddScore(ScoreGrade grade)
    {
        switch (grade)
        {
            case ScoreGrade.Perfect: AddScore(perfectScore); break;
            case ScoreGrade.Great:   AddScore(greatScore);   break;
            default:                 AddScore(goodScore);    break;
        }
    }

    public void AddScore(int amount)
    {
        score += amount;

        // Ghi nhan diem cao nhat cho Achievements.
        if (AchievementManager.Instance != null)
            AchievementManager.Instance.ReportScore(score);

        UpdateScoreUI();
        ShowPopup(amount);
    }

    // =====================================================
    // POPUP (mỗi lần cộng điểm = 1 popup riêng)
    // =====================================================

    private Vector2 GetPlayerCanvasPos()
    {
        if (player == null || worldCamera == null || popupParentRect == null)
            return lastPopupPos;

        Vector3 screen = worldCamera.WorldToScreenPoint(player.position + playerWorldOffset);

        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            popupParentRect, screen, uiCam, out Vector2 local);

        return local;
    }

    private void ShowPopup(int amount)
    {
        if (popupText == null)
            return;

        Vector2 start = GetPlayerCanvasPos() + new Vector2(startXOffset, startYOffset);
        lastPopupPos = start;

        TMP_Text t = Instantiate(popupText, popupText.transform.parent);
        t.gameObject.SetActive(true);
        t.enabled = true;
        t.text = popupPrefix + amount.ToString("#,##0", CultureInfo.InvariantCulture);

        StartCoroutine(AnimatePopup(t, start));
    }

    private IEnumerator AnimatePopup(TMP_Text t, Vector2 start)
    {
        RectTransform rect = t.rectTransform;
        rect.anchoredPosition = start;
        rect.localRotation = Quaternion.identity;

        Vector2 end = start + new Vector2(0f, riseDistance);
        float duration = Mathf.Max(0.05f, popupHideDelay);
        float elapsed = 0f;

        while (elapsed < duration && t != null)
        {
            float k = elapsed / duration;
            float eased = 1f - (1f - k) * (1f - k);
            rect.anchoredPosition = Vector2.Lerp(start, end, eased);

            float popT = Mathf.Clamp01(elapsed / popAnimationDuration);
            rect.localScale = originalPopupScale * Mathf.Lerp(0.5f, 1f, popT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (t != null)
            Destroy(t.gameObject);
    }

    // =====================================================
    // UI
    // =====================================================

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = score.ToString("#,##0", CultureInfo.InvariantCulture);
    }

    public int GetScore()
    {
        return score;
    }
}