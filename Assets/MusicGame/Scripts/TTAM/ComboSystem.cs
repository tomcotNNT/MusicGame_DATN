using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text comboLabelText;   // chữ "COMBO" (gợn sóng)
    [SerializeField] private TMP_Text comboNumberText;  // số "x100" (scale khi tăng)

    [Header("Judgment Images")]
    [SerializeField] private Image judgmentImage;
    [SerializeField] private Sprite goodSprite;
    [SerializeField] private Sprite awesomeSprite;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private bool setNativeSize = true; // tự đặt kích thước theo sprite

    [Header("Judgment Popup (bay lên)")]
    [SerializeField] private Transform player;            // kéo Player vào đây
    [SerializeField] private Camera worldCamera;          // camera nhìn thấy player (để trống = Camera.main)
    [SerializeField] private Canvas canvas;               // canvas chứa judgmentImage
    [SerializeField] private Vector3 playerWorldOffset = new Vector3(0f, 1f, 0f); // lệch so với chân player
    [SerializeField] private float startYOffset = -20f;   // điểm bắt đầu, so với vị trí player trên canvas
    [SerializeField] private float riseDistance = 90f;    // bay lên bao nhiêu pixel canvas

    private RectTransform judgmentParentRect;
    private Vector2 lastJudgmentPos;
    private Vector2 judgmentStartPos;

    [Header("Number Pop")]
    [SerializeField] private float numberPopScale = 1.5f;
    [SerializeField] private float numberPopDuration = 0.2f;

    [Header("Label Wave (gợn sóng)")]
    [SerializeField] private float waveAmplitude = 8f;
    [SerializeField] private float waveSpeed = 6f;
    [SerializeField] private float waveFrequency = 0.6f;

    [Header("Timing")]
    [SerializeField] private float hitAnimationDuration = 0.12f;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float judgmentHideDelay = 0.5f;

    [Header("Combo Threshold")]
    [SerializeField] private int awesomeCombo = 6;
    [SerializeField] private int perfectCombo = 10;

    [Header("Combo Colors")]
    [SerializeField] private int colorTier1 = 5;
    [SerializeField] private int colorTier2 = 15;
    [SerializeField] private int colorTier3 = 25;
    [SerializeField] private Color tier1Color = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color tier2Color = new Color(1f, 0.5f, 0.1f);
    [SerializeField] private Color tier3Color = new Color(1f, 0.2f, 0.3f);

    private Color originalLabelColor;
    private Color originalNumberColor;

    private int combo;
    private bool comboVisible;

    private Vector3 originalLabelScale;
    private Vector3 originalNumberScale;
    private Vector3 originalJudgmentScale;

    private Coroutine comboHideRoutine;
    private Coroutine numberAnimRoutine;
    private Coroutine judgmentHideRoutine;
    private Coroutine judgmentAnimRoutine;

    private void Start()
    {
        combo = 0;
        comboVisible = false;

        if (worldCamera == null)
            worldCamera = Camera.main;

        if (comboLabelText != null)
        {
            originalLabelScale = comboLabelText.rectTransform.localScale;
            originalLabelColor = comboLabelText.color;
            comboLabelText.text = "COMBO";
            comboLabelText.rectTransform.localScale = Vector3.zero;
        }

        if (comboNumberText != null)
        {
            originalNumberScale = comboNumberText.rectTransform.localScale;
            originalNumberColor = comboNumberText.color;
            comboNumberText.text = "";
            comboNumberText.rectTransform.localScale = Vector3.zero;
        }

        if (judgmentImage != null)
        {
            originalJudgmentScale = judgmentImage.rectTransform.localScale;
            judgmentParentRect = judgmentImage.rectTransform.parent as RectTransform;
            lastJudgmentPos = judgmentImage.rectTransform.anchoredPosition;

            judgmentImage.sprite = null;
            judgmentImage.enabled = false;
            judgmentImage.rectTransform.localScale = Vector3.zero;
        }
    }

    // =====================================================
    // WAVE
    // =====================================================

    private void LateUpdate()
    {
        if (comboVisible)
            AnimateWave();
    }

    private void ApplyComboColor()
    {
        bool useTier = combo > colorTier1;
        Color tier = combo > colorTier3 ? tier3Color
                : combo > colorTier2 ? tier2Color
                : tier1Color;

        if (comboLabelText != null)
            comboLabelText.color = useTier ? tier : originalLabelColor;

        if (comboNumberText != null)
            comboNumberText.color = useTier ? tier : originalNumberColor;
    }

    private void AnimateWave()
    {
        if (comboLabelText == null)
            return;

        comboLabelText.ForceMeshUpdate();
        TMP_TextInfo info = comboLabelText.textInfo;

        for (int i = 0; i < info.characterCount; i++)
        {
            TMP_CharacterInfo c = info.characterInfo[i];
            if (!c.isVisible)
                continue;

            int mat = c.materialReferenceIndex;
            int v = c.vertexIndex;
            Vector3[] verts = info.meshInfo[mat].vertices;

            float offsetY = Mathf.Sin(Time.time * waveSpeed + i * waveFrequency) * waveAmplitude;
            Vector3 offset = new Vector3(0f, offsetY, 0f);

            verts[v + 0] += offset;
            verts[v + 1] += offset;
            verts[v + 2] += offset;
            verts[v + 3] += offset;
        }

        for (int i = 0; i < info.meshInfo.Length; i++)
        {
            info.meshInfo[i].mesh.vertices = info.meshInfo[i].vertices;
            comboLabelText.UpdateGeometry(info.meshInfo[i].mesh, i);
        }
    }

    // =====================================================
    // JUDGMENT POSITION (cố định trên player, bay thẳng lên)
    // =====================================================

    // Đổi vị trí world của player -> toạ độ local trong canvas
    private Vector2 GetPlayerCanvasPos()
    {
        if (player == null || worldCamera == null || judgmentParentRect == null)
            return lastJudgmentPos;

        Vector3 screen = worldCamera.WorldToScreenPoint(player.position + playerWorldOffset);

        Camera uiCam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCam = canvas.worldCamera;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            judgmentParentRect, screen, uiCam, out Vector2 local);

        return local;
    }

    private void PlaceJudgment()
    {
        if (judgmentImage == null)
            return;

        judgmentStartPos = GetPlayerCanvasPos() + new Vector2(0f, startYOffset);
        lastJudgmentPos = judgmentStartPos;

        RectTransform rect = judgmentImage.rectTransform;
        rect.anchoredPosition = judgmentStartPos;
        rect.localRotation = Quaternion.identity;
    }

    // =====================================================
    // HIT / MISS
    // =====================================================

    public void HitNote()
    {
        combo++;

        UpdateJudgment();
        ShowComboUI();
        ShowJudgmentImage();

        if (numberAnimRoutine != null)
            StopCoroutine(numberAnimRoutine);
        numberAnimRoutine = StartCoroutine(AnimateNumberPop());

        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);
        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentImage());

        if (comboHideRoutine != null)
            StopCoroutine(comboHideRoutine);
        comboHideRoutine = StartCoroutine(HideComboAfterDelay());

        if (judgmentHideRoutine != null)
            StopCoroutine(judgmentHideRoutine);
        judgmentHideRoutine = StartCoroutine(HideJudgmentAfterDelay());
    }

    public void MissNote()
    {
        combo = 0;

        if (comboHideRoutine != null)
        {
            StopCoroutine(comboHideRoutine);
            comboHideRoutine = null;
        }

        if (numberAnimRoutine != null)
        {
            StopCoroutine(numberAnimRoutine);
            numberAnimRoutine = null;
        }

        HideComboUI();

        SetJudgmentSprite(missSprite);
        ShowJudgmentImage();

        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);
        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentImage());

        if (judgmentHideRoutine != null)
            StopCoroutine(judgmentHideRoutine);
        judgmentHideRoutine = StartCoroutine(HideJudgmentAfterDelay());

        Debug.Log("MISS - Combo Reset");
    }

    private void UpdateJudgment()
    {
        if (combo >= perfectCombo)
            SetJudgmentSprite(perfectSprite);
        else if (combo >= awesomeCombo)
            SetJudgmentSprite(awesomeSprite);
        else
            SetJudgmentSprite(goodSprite);
    }

    private void SetJudgmentSprite(Sprite sprite)
    {
        if (judgmentImage == null)
            return;

        judgmentImage.sprite = sprite;
        if (setNativeSize && sprite != null)
            judgmentImage.SetNativeSize();
    }

    // =====================================================
    // SHOW / HIDE
    // =====================================================

    private void ShowComboUI()
    {
        comboVisible = true;

        if (comboLabelText != null)
            comboLabelText.rectTransform.localScale = originalLabelScale;

        if (comboNumberText != null)
        {
            comboNumberText.text = "x" + combo;
            comboNumberText.rectTransform.localScale = originalNumberScale;
        }

        ApplyComboColor();
    }

    private void HideComboUI()
    {
        comboVisible = false;

        if (comboLabelText != null)
            comboLabelText.rectTransform.localScale = Vector3.zero;

        if (comboNumberText != null)
        {
            comboNumberText.rectTransform.localScale = Vector3.zero;
            comboNumberText.text = "";
        }
    }

    private void ShowJudgmentImage()
    {
        if (judgmentImage == null)
            return;

        PlaceJudgment();
        judgmentImage.enabled = true;
        judgmentImage.gameObject.SetActive(true);
        judgmentImage.rectTransform.localScale = originalJudgmentScale;
    }

    private IEnumerator HideComboAfterDelay()
    {
        yield return new WaitForSeconds(hideDelay);

        HideComboUI();
        comboHideRoutine = null;
    }

    private IEnumerator HideJudgmentAfterDelay()
    {
        yield return new WaitForSeconds(judgmentHideDelay);

        if (judgmentImage != null)
        {
            judgmentImage.sprite = null;
            judgmentImage.enabled = false;
            judgmentImage.rectTransform.localScale = Vector3.zero;
        }

        judgmentHideRoutine = null;
    }

    // =====================================================
    // ANIMATION
    // =====================================================

    private IEnumerator AnimateNumberPop()
    {
        if (comboNumberText == null)
            yield break;

        RectTransform rect = comboNumberText.rectTransform;
        float elapsed = 0f;

        while (elapsed < numberPopDuration)
        {
            float t = Mathf.Clamp01(elapsed / numberPopDuration);
            float eased = 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(numberPopScale, 1f, eased);

            rect.localScale = originalNumberScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = originalNumberScale;
        numberAnimRoutine = null;
    }

    // Phóng to (pop) ở đầu, đồng thời trượt thẳng lên suốt thời gian popup hiển thị
    private IEnumerator AnimateJudgmentImage()
    {
        if (judgmentImage == null)
            yield break;

        RectTransform rect = judgmentImage.rectTransform;
        Vector2 start = judgmentStartPos;
        Vector2 end = start + new Vector2(0f, riseDistance);

        // Kết thúc ngay trước khi popup bị ẩn
        float duration = Mathf.Max(0.05f, judgmentHideDelay * 0.95f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedRise = 1f - (1f - t) * (1f - t);   // ease-out: lên nhanh rồi chậm dần
            rect.anchoredPosition = Vector2.Lerp(start, end, easedRise);

            float popT = Mathf.Clamp01(elapsed / hitAnimationDuration);
            rect.localScale = originalJudgmentScale * Mathf.Lerp(0.5f, 1f, popT);

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = end;
        rect.localScale = originalJudgmentScale;
        judgmentAnimRoutine = null;
    }

    public int GetCombo()
    {
        return combo;
    }
}