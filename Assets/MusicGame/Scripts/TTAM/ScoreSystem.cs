using UnityEngine;
using TMPro;
using System.Collections;
using System.Globalization;

public class ScoreSystem : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scorePerNote = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    [Header("Score Popup (bay lên)")]
    [Tooltip("Text popup +điểm, đặt sẵn trong Canvas (cùng Canvas với judgment của ComboSystem)")]
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Transform player;            // kéo Player vào đây
    [SerializeField] private Camera worldCamera;          // để trống = Camera.main
    [SerializeField] private Canvas canvas;               // canvas chứa popupText
    [SerializeField] private Vector3 playerWorldOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float startXOffset = 60f;    // lệch ngang để không đè lên judgment
    [SerializeField] private float startYOffset = -20f;
    [SerializeField] private float riseDistance = 90f;
    [SerializeField] private string popupPrefix = "+";

    [Header("Popup Timing")]
    [SerializeField] private float popAnimationDuration = 0.12f;
    [SerializeField] private float popupHideDelay = 0.5f;

    private int score;

    private RectTransform popupParentRect;
    private Vector2 lastPopupPos;
    private Vector2 popupStartPos;
    private Vector3 originalPopupScale;

    private Coroutine popupAnimRoutine;
    private Coroutine popupHideRoutine;

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

            popupText.text = "";
            popupText.enabled = false;
            popupText.rectTransform.localScale = Vector3.zero;
        }
    }

    // =====================================================
    // ADD SCORE
    // =====================================================

    public void AddScore()
    {
        AddScore(scorePerNote);
    }

    public void AddScore(int amount)
    {
        score += amount;

        UpdateScoreUI();
        ShowPopup(amount);
    }

    // =====================================================
    // POPUP
    // =====================================================

    // Đổi vị trí world của player -> toạ độ local trong canvas
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

        popupStartPos = GetPlayerCanvasPos() + new Vector2(startXOffset, startYOffset);
        lastPopupPos = popupStartPos;

        RectTransform rect = popupText.rectTransform;
        rect.anchoredPosition = popupStartPos;
        rect.localRotation = Quaternion.identity;
        rect.localScale = originalPopupScale;

        popupText.text = popupPrefix + amount.ToString("#,##0", CultureInfo.InvariantCulture);
        popupText.enabled = true;
        popupText.gameObject.SetActive(true);

        if (popupAnimRoutine != null)
            StopCoroutine(popupAnimRoutine);
        popupAnimRoutine = StartCoroutine(AnimatePopup());

        if (popupHideRoutine != null)
            StopCoroutine(popupHideRoutine);
        popupHideRoutine = StartCoroutine(HidePopupAfterDelay());
    }

    // Phóng to (pop) ở đầu, đồng thời trượt thẳng lên suốt thời gian popup hiển thị
    private IEnumerator AnimatePopup()
    {
        if (popupText == null)
            yield break;

        RectTransform rect = popupText.rectTransform;
        Vector2 start = popupStartPos;
        Vector2 end = start + new Vector2(0f, riseDistance);

        float duration = Mathf.Max(0.05f, popupHideDelay * 0.95f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedRise = 1f - (1f - t) * (1f - t);   // ease-out
            rect.anchoredPosition = Vector2.Lerp(start, end, easedRise);

            float popT = Mathf.Clamp01(elapsed / popAnimationDuration);
            rect.localScale = originalPopupScale * Mathf.Lerp(0.5f, 1f, popT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = end;
        rect.localScale = originalPopupScale;
        popupAnimRoutine = null;
    }

    private IEnumerator HidePopupAfterDelay()
    {
        yield return new WaitForSeconds(popupHideDelay);

        if (popupText != null)
        {
            popupText.text = "";
            popupText.enabled = false;
            popupText.rectTransform.localScale = Vector3.zero;
        }

        popupHideRoutine = null;
    }

    // =====================================================
    // UI
    // =====================================================

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString("#,##0", CultureInfo.InvariantCulture);
        }
    }

    public int GetScore()
    {
        return score;
    }
}