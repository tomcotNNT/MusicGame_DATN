using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image goodIcon;
    [SerializeField] private Image awesomeIcon;
    [SerializeField] private Image perfectIcon;

    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text judgmentText;

    [Header("Combo Animation")]
    [SerializeField] private float hitScale = 0.8f;
    [SerializeField] private float hitAnimationDuration = 0.12f;
    [SerializeField] private float hideDelay = 3f;
    [SerializeField] private float judgmentHideDelay = 0.5f;

    [Header("Combo Threshold")]
    [SerializeField] private int awesomeCombo = 6;
    [SerializeField] private int perfectCombo = 10;

    private int combo;

    private Vector3 originalGoodScale;
    private Vector3 originalAwesomeScale;
    private Vector3 originalPerfectScale;
    private Vector3 originalTextScale;
    private Vector3 originalJudgmentScale;

    private Coroutine comboHideRoutine;
    private Coroutine comboAnimRoutine;
    private Coroutine judgmentHideRoutine;
    private Coroutine judgmentAnimRoutine;

    private void Start()
    {
        combo = 0;

        // Lưu scale ban đầu
        if (goodIcon != null)
        {
            originalGoodScale = goodIcon.rectTransform.localScale;
            goodIcon.rectTransform.localScale = Vector3.zero;
        }

        if (awesomeIcon != null)
        {
            originalAwesomeScale = awesomeIcon.rectTransform.localScale;
            awesomeIcon.rectTransform.localScale = Vector3.zero;
        }

        if (perfectIcon != null)
        {
            originalPerfectScale = perfectIcon.rectTransform.localScale;
            perfectIcon.rectTransform.localScale = Vector3.zero;
        }

        if (comboText != null)
        {
            originalTextScale = comboText.rectTransform.localScale;
            comboText.rectTransform.localScale = Vector3.zero;
            comboText.text = "";
        }

        if (judgmentText != null)
        {
            originalJudgmentScale = judgmentText.rectTransform.localScale;
            judgmentText.rectTransform.localScale = Vector3.zero;
            judgmentText.text = "";
        }
    }

    public void HitNote()
    {
        combo++;

        UpdateJudgment();

        if (comboText != null)
            comboText.text = "COMBO x " + combo;

        ShowComboUI();
        ShowJudgmentText();

        // Animation Combo
        if (comboAnimRoutine != null)
            StopCoroutine(comboAnimRoutine);

        comboAnimRoutine = StartCoroutine(AnimateComboIcon());

        // Animation Judgment
        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);

        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentText());

        // Hide Combo
        if (comboHideRoutine != null)
            StopCoroutine(comboHideRoutine);

        comboHideRoutine = StartCoroutine(HideComboAfterDelay());

        // Hide Judgment
        if (judgmentHideRoutine != null)
            StopCoroutine(judgmentHideRoutine);

        judgmentHideRoutine = StartCoroutine(HideJudgmentAfterDelay());
    }

    public void MissNote()
    {
        combo = 0;

        if (comboText != null)
            comboText.text = "";

        if (judgmentText != null)
            judgmentText.text = "MISS";

        HideComboUI();
        ShowJudgmentText();

        if (comboHideRoutine != null)
        {
            StopCoroutine(comboHideRoutine);
            comboHideRoutine = null;
        }

        if (comboAnimRoutine != null)
        {
            StopCoroutine(comboAnimRoutine);
            comboAnimRoutine = null;
        }

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
        {
            judgmentText.text = "PERFECT";
        }
        else if (combo >= awesomeCombo)
        {
            judgmentText.text = "AWESOME";
        }
        else
        {
            judgmentText.text = "GOOD";
        }
    }

    private void ShowComboUI()
    {
        // Tắt cả 3 icon trước
        HideAllIcons();

        // Bật icon tương ứng
        if (combo >= perfectCombo)
        {
            if (perfectIcon != null)
                perfectIcon.rectTransform.localScale = originalPerfectScale;
        }
        else if (combo >= awesomeCombo)
        {
            if (awesomeIcon != null)
                awesomeIcon.rectTransform.localScale = originalAwesomeScale;
        }
        else
        {
            if (goodIcon != null)
                goodIcon.rectTransform.localScale = originalGoodScale;
        }

        if (comboText != null)
            comboText.rectTransform.localScale = originalTextScale;
    }

    private void HideAllIcons()
    {
        if (goodIcon != null)
            goodIcon.rectTransform.localScale = Vector3.zero;

        if (awesomeIcon != null)
            awesomeIcon.rectTransform.localScale = Vector3.zero;

        if (perfectIcon != null)
            perfectIcon.rectTransform.localScale = Vector3.zero;
    }

    private void HideComboUI()
    {
        HideAllIcons();

        if (comboText != null)
        {
            comboText.rectTransform.localScale = Vector3.zero;
            comboText.text = "";
        }
    }

    private void ShowJudgmentText()
    {
        if (judgmentText != null)
        {
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

    private IEnumerator AnimateJudgmentText()
    {
        if (judgmentText == null)
            yield break;

        RectTransform judgmentRect = judgmentText.rectTransform;

        float elapsed = 0f;

        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;

            float scale = Mathf.Lerp(0.5f, 1f, t);

            judgmentRect.localScale =
                new Vector3(scale, scale, 1f);

            elapsed += Time.deltaTime;

            yield return null;
        }

        judgmentRect.localScale = originalJudgmentScale;

        judgmentAnimRoutine = null;
    }

    private IEnumerator AnimateComboIcon()
    {
        RectTransform activeIcon = GetActiveIcon();

        if (activeIcon == null)
            yield break;

        float elapsed = 0f;

        // Scale down
        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;

            float scale = Mathf.Lerp(1f, hitScale, t);

            activeIcon.localScale =
                new Vector3(scale, scale, 1f);

            if (comboText != null)
            {
                comboText.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Scale up
        elapsed = 0f;

        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;

            float scale = Mathf.Lerp(hitScale, 1f, t);

            activeIcon.localScale =
                new Vector3(scale, scale, 1f);

            if (comboText != null)
            {
                comboText.rectTransform.localScale =
                    new Vector3(scale, scale, 1f);
            }

            elapsed += Time.deltaTime;

            yield return null;
        }

        // Trả về scale ban đầu
        if (combo >= perfectCombo)
        {
            activeIcon.localScale = originalPerfectScale;
        }
        else if (combo >= awesomeCombo)
        {
            activeIcon.localScale = originalAwesomeScale;
        }
        else
        {
            activeIcon.localScale = originalGoodScale;
        }

        if (comboText != null)
            comboText.rectTransform.localScale = originalTextScale;

        comboAnimRoutine = null;
    }

    private RectTransform GetActiveIcon()
    {
        if (combo >= perfectCombo)
        {
            return perfectIcon != null
                ? perfectIcon.rectTransform
                : null;
        }

        if (combo >= awesomeCombo)
        {
            return awesomeIcon != null
                ? awesomeIcon.rectTransform
                : null;
        }

        return goodIcon != null
            ? goodIcon.rectTransform
            : null;
    }

    public int GetCombo()
    {
        return combo;
    }
}