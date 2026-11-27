using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("Score System (kéo object ScoreSystem vào đây)")]
    [SerializeField] private ScoreSystem scoreSystem;

    public enum JudgeMode
    {
        [Tooltip("Good/Great/Perfect theo độ dài combo (cách cũ)")]
        Combo,
        [Tooltip("Good/Great/Perfect theo độ lệch thời điểm bấm so với nốt MIDI")]
        Timing
    }

    [Header("Cách chấm Good / Great / Perfect")]
    [SerializeField] private JudgeMode judgeMode = JudgeMode.Combo;

    [Header("Combo Threshold (chế độ Combo)")]
    [SerializeField] private int greatCombo = 5;     // đạt 5 combo liên tiếp -> Great
    [SerializeField] private int perfectCombo = 10;  // đạt 10 combo liên tiếp -> Perfect

    [Header("Timing Window (chế độ Timing, giây lệch so với nốt)")]
    [SerializeField, Min(0.005f)] private float perfectWindow = 0.06f;
    [SerializeField, Min(0.01f)] private float greatWindow = 0.12f;
    [Tooltip("Lần trúng không có thông tin thời gian (item, đạn nhặt...) được tính mức này")]
    [SerializeField] private ScoreSystem.ScoreGrade untimedGrade = ScoreSystem.ScoreGrade.Great;

    public JudgeMode Mode => judgeMode;

    [Header("UI")]
    [SerializeField] private TMP_Text comboLabelText;   // chữ "COMBO" (gợn sóng)
    [SerializeField] private TMP_Text comboNumberText;  // số "x100" (scale khi tăng)

    [Header("Judgment Images")]
    [SerializeField] private Image judgmentImage;
    [SerializeField] private Sprite goodSprite;
    [FormerlySerializedAs("awesomeSprite")]
    [SerializeField] private Sprite greatSprite;
    [SerializeField] private Sprite perfectSprite;
    [SerializeField] private Sprite missSprite;
    [SerializeField] private bool setNativeSize = true;

    [Header("Judgment Popup (bay lên)")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera worldCamera;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector3 playerWorldOffset = new Vector3(0f, 1f, 0f);
    [SerializeField] private float startYOffset = -20f;
    [SerializeField] private float riseDistance = 90f;

    private RectTransform judgmentParentRect;
    private Vector2 lastJudgmentPos;
    private Vector2 judgmentStartPos;

    public int MaxCombo { get; private set; }

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
    private int maxCombo = 0;

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
    // GRADE THEO COMBO
    // =====================================================

    // Combo 1-4: Good, 5-9: Great, từ 10: Perfect
    private ScoreSystem.ScoreGrade GetGradeByCombo()
    {
        if (combo >= perfectCombo) return ScoreSystem.ScoreGrade.Perfect;
        if (combo >= greatCombo)   return ScoreSystem.ScoreGrade.Great;
        return ScoreSystem.ScoreGrade.Good;
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
    // JUDGMENT POSITION
    // =====================================================

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

    // Gọi khi trúng nốt KHÔNG có thông tin thời gian (item, đạn nhặt...).
    // Chế độ Combo: mức theo combo. Chế độ Timing: mức untimedGrade.
    public void HitNote()
    {
        RegisterHit(judgeMode == JudgeMode.Timing ? untimedGrade : GetGradeAfterIncrement());
    }

    /// <summary>
    /// Gọi khi trúng nốt có biết độ lệch thời gian (giây, giá trị tuyệt đối) so với nốt MIDI.
    /// Chế độ Combo: bỏ qua độ lệch, chấm theo combo như cũ.
    /// </summary>
    public void HitNoteTimed(float timingError)
    {
        if (judgeMode != JudgeMode.Timing)
        {
            RegisterHit(GetGradeAfterIncrement());
            return;
        }

        float e = Mathf.Abs(timingError);
        ScoreSystem.ScoreGrade grade =
            e <= perfectWindow ? ScoreSystem.ScoreGrade.Perfect :
            e <= greatWindow   ? ScoreSystem.ScoreGrade.Great :
                                 ScoreSystem.ScoreGrade.Good;

        RegisterHit(grade);
    }

    // Chế độ Combo: mức được tính theo combo SAU khi cộng lần trúng này (giống code cũ)
    private ScoreSystem.ScoreGrade GetGradeAfterIncrement()
    {
        combo++;
        ScoreSystem.ScoreGrade g = GetGradeByCombo();
        combo--;
        return g;
    }

    // Điểm (100/200/300) được cộng qua ScoreSystem theo mức.
    /// <summary>Mức chấm của nốt trúng gần nhất (AttackPicker dùng để chọn đòn đánh).</summary>
    public ScoreSystem.ScoreGrade LastGrade { get; private set; } = ScoreSystem.ScoreGrade.Good;

    private void RegisterHit(ScoreSystem.ScoreGrade grade)
    {
        LastGrade = grade;
        combo++;

        if (combo > MaxCombo)
            MaxCombo = combo;

        if (combo > maxCombo)
        {
            maxCombo = combo;
        }    

        // Cộng điểm + hiện popup "+100/+200/+300"
        if (EnsureScoreSystem())
            scoreSystem.AddScore(grade);

        SetJudgmentByGrade(grade);

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

    private bool EnsureScoreSystem()
    {
        if (scoreSystem == null)
            scoreSystem = FindFirstObjectByType<ScoreSystem>();
        return scoreSystem != null;
    }

    public int GetMaxCombo()
    {
        return maxCombo;
    }

    public void ResetCombo()
    {
        combo = 0;
    }

    // Code đã tự chấm mức (ví dụ EnemyRhythm). Chế độ Combo: bỏ qua mức, chấm theo combo như cũ.
    public void HitNote(ScoreSystem.ScoreGrade grade)
    {
        RegisterHit(judgeMode == JudgeMode.Timing ? grade : GetGradeAfterIncrement());
    }

    private void SetJudgmentByGrade(ScoreSystem.ScoreGrade grade)
    {
        switch (grade)
        {
            case ScoreSystem.ScoreGrade.Perfect: SetJudgmentSprite(perfectSprite); break;
            case ScoreSystem.ScoreGrade.Great:   SetJudgmentSprite(greatSprite);   break;
            default:                             SetJudgmentSprite(goodSprite);    break;
        }
    }

    public void MissNote()
    {
        combo = 0;

        // Nơi DUY NHẤT đếm Miss cho mọi màn (dùng cho điều kiện sao ở bảng kết thúc)
        if (EnsureScoreSystem())
            scoreSystem.AddMiss();

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

    private IEnumerator AnimateJudgmentImage()
    {
        if (judgmentImage == null)
            yield break;

        RectTransform rect = judgmentImage.rectTransform;
        Vector2 start = judgmentStartPos;
        Vector2 end = start + new Vector2(0f, riseDistance);

        float duration = Mathf.Max(0.05f, judgmentHideDelay * 0.95f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float easedRise = 1f - (1f - t) * (1f - t);
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