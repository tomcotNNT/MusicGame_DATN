using UnityEngine;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text comboLabelText;   // chữ "COMBO" (gợn sóng)
    [SerializeField] private TMP_Text comboNumberText;  // số "x100" (scale khi tăng)
    [SerializeField] private TMP_Text judgmentText;

    [Header("Judgment Random Spawn")]
    [SerializeField] private float judgmentRangeX = 300f;   // lệch ngang tối đa so với vị trí gốc
    [SerializeField] private float judgmentRangeY = 150f;   // lệch dọc tối đa so với vị trí gốc
    [SerializeField] private float judgmentMaxRotation = 12f; // nghiêng tối đa (độ), 0 = không nghiêng
    [SerializeField] private float minDistanceFromLast = 120f; // cách vị trí lần trước tối thiểu

    private Vector2 judgmentOriginPos;
    private Vector2 lastJudgmentPos;

    [Header("Number Pop")]
    [SerializeField] private float numberPopScale = 1.5f;     // phóng to tối đa khi combo tăng
    [SerializeField] private float numberPopDuration = 0.2f;  // thời gian lớn -> nhỏ về bình thường

    [Header("Label Wave (gợn sóng)")]
    [SerializeField] private float waveAmplitude = 8f;   // độ cao sóng
    [SerializeField] private float waveSpeed = 6f;       // tốc độ sóng
    [SerializeField] private float waveFrequency = 0.6f; // độ lệch pha giữa các chữ

    [Header("Timing")]
    [SerializeField] private float hitAnimationDuration = 0.12f;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float judgmentHideDelay = 0.5f;

    [Header("Combo Threshold")]
    [SerializeField] private int awesomeCombo = 6;
    [SerializeField] private int perfectCombo = 10;

    [Header("Combo Colors")]
    [SerializeField] private int colorTier1 = 5;    // combo > 5
    [SerializeField] private int colorTier2 = 15;   // combo > 15
    [SerializeField] private int colorTier3 = 25;   // combo > 25
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
        if (judgmentText != null)
        {
            originalJudgmentScale = judgmentText.rectTransform.localScale;
            judgmentOriginPos = judgmentText.rectTransform.anchoredPosition;
            lastJudgmentPos = judgmentOriginPos;

            judgmentText.text = "";
            judgmentText.rectTransform.localScale = Vector3.zero;
        }
    }

    // =====================================================
    // WAVE: gợn sóng từng ký tự của chữ COMBO
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

    private void RandomizeJudgmentPosition()
    {
        if (judgmentText == null)
            return;

        Vector2 pos = judgmentOriginPos;

        // Thử vài lần để không trùng chỗ với lần trước
        for (int i = 0; i < 10; i++)
        {
            pos = judgmentOriginPos + new Vector2(
                Random.Range(-judgmentRangeX, judgmentRangeX),
                Random.Range(-judgmentRangeY, judgmentRangeY)
            );

            if (Vector2.Distance(pos, lastJudgmentPos) >= minDistanceFromLast)
                break;
        }

        lastJudgmentPos = pos;

        RectTransform rect = judgmentText.rectTransform;
        rect.anchoredPosition = pos;
        rect.localRotation = Quaternion.Euler(0f, 0f,
            Random.Range(-judgmentMaxRotation, judgmentMaxRotation));
    }

    private void AnimateWave()
    {
        if (comboLabelText == null)
            return;

        // Reset mesh về trạng thái gốc rồi mới cộng offset (tránh cộng dồn)
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
    // HIT / MISS
    // =====================================================

    public void HitNote()
    {
        combo++;

        UpdateJudgment();
        ShowComboUI();
        ShowJudgmentText();

        // Số: phóng to rồi nhỏ lại mỗi lần tăng
        if (numberAnimRoutine != null)
            StopCoroutine(numberAnimRoutine);
        numberAnimRoutine = StartCoroutine(AnimateNumberPop());

        // Judgment
        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);
        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentText());

        // Hẹn giờ ẩn combo
        if (comboHideRoutine != null)
            StopCoroutine(comboHideRoutine);
        comboHideRoutine = StartCoroutine(HideComboAfterDelay());

        // Hẹn giờ ẩn judgment
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

        if (judgmentText != null)
            judgmentText.text = "MISS";

        ShowJudgmentText();

        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);
        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentText());

        if (judgmentHideRoutine != null)
            StopCoroutine(judgmentHideRoutine);
        judgmentHideRoutine = StartCoroutine(HideJudgmentAfterDelay());

        Debug.Log("MISS - Combo Reset");
    }

    private void UpdateJudgment()
    {
        if (judgmentText == null)
            return;

        if (combo >= perfectCombo)
            judgmentText.text = "PERFECT";
        else if (combo >= awesomeCombo)
            judgmentText.text = "AWESOME";
        else
            judgmentText.text = "GOOD";
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

    private void ShowJudgmentText()
    {
        if (judgmentText != null)
        {
            RandomizeJudgmentPosition(); 
            judgmentText.gameObject.SetActive(true);
            judgmentText.rectTransform.localScale = originalJudgmentScale;
        }
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

        if (judgmentText != null)
        {
            judgmentText.text = "";
            judgmentText.rectTransform.localScale = Vector3.zero;
        }

        judgmentHideRoutine = null;
    }

    // =====================================================
    // ANIMATION
    // =====================================================

    // Số combo: bật to (numberPopScale) rồi thu nhỏ dần về bình thường
    private IEnumerator AnimateNumberPop()
    {
        if (comboNumberText == null)
            yield break;

        RectTransform rect = comboNumberText.rectTransform;
        float elapsed = 0f;

        while (elapsed < numberPopDuration)
        {
            float t = Mathf.Clamp01(elapsed / numberPopDuration);
            float eased = 1f - (1f - t) * (1f - t); // ease-out
            float scale = Mathf.Lerp(numberPopScale, 1f, eased);

            rect.localScale = originalNumberScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = originalNumberScale;
        numberAnimRoutine = null;
    }

    private IEnumerator AnimateJudgmentText()
    {
        if (judgmentText == null)
            yield break;

        RectTransform rect = judgmentText.rectTransform;
        float elapsed = 0f;

        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;
            float scale = Mathf.Lerp(0.5f, 1f, t);

            rect.localScale = originalJudgmentScale * scale;

            elapsed += Time.deltaTime;
            yield return null;
        }

        rect.localScale = originalJudgmentScale;
        judgmentAnimRoutine = null;
    }

    public int GetCombo()
    {
        return combo;
    }
}