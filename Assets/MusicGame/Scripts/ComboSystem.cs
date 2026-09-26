using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image comboIcon;
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
    private Vector3 originalIconScale;
    private Vector3 originalTextScale;
    private Vector3 originalJudgmentScale;
    private Coroutine comboHideRoutine;
    private Coroutine comboAnimRoutine;
    private Coroutine judgmentHideRoutine;
    private Coroutine judgmentAnimRoutine;

    private void Start()
    {
        combo = 0;

        if (comboIcon != null)
        {
            originalIconScale = comboIcon.rectTransform.localScale;
            comboIcon.rectTransform.localScale = Vector3.zero;
        }

        if (comboText != null)
        {
            originalTextScale = comboText.rectTransform.localScale;
            comboText.rectTransform.localScale = Vector3.zero;
        }

        if (comboText != null)
            comboText.text = "";

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

        if (comboAnimRoutine != null)
            StopCoroutine(comboAnimRoutine);

        comboAnimRoutine = StartCoroutine(AnimateComboIcon());

        if (judgmentAnimRoutine != null)
            StopCoroutine(judgmentAnimRoutine);

        judgmentAnimRoutine = StartCoroutine(AnimateJudgmentText());

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

        Debug.Log("BAD - Combo Reset");
    }

    private void UpdateJudgment()
    {
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

    private void ShowJudgmentText()
    {
        if (judgmentText != null)
        {
            judgmentText.gameObject.SetActive(true);
            judgmentText.rectTransform.localScale = originalJudgmentScale;
        }
    }

    private void ShowComboUI()
    {
        if (comboIcon != null)
            comboIcon.rectTransform.localScale = originalIconScale;

        if (comboText != null)
            comboText.rectTransform.localScale = originalTextScale;
    }

    private void HideComboUI()
    {
        if (comboIcon != null)
            comboIcon.rectTransform.localScale = Vector3.zero;

        if (comboText != null)
        {
            comboText.rectTransform.localScale = Vector3.zero;
            comboText.text = "";
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
            judgmentRect.localScale = new Vector3(scale, scale, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        judgmentRect.localScale = originalJudgmentScale;
        judgmentAnimRoutine = null;
    }

    private IEnumerator AnimateComboIcon()
    {
        RectTransform iconRect = comboIcon != null ? comboIcon.rectTransform : null;
        RectTransform textRect = comboText != null ? comboText.rectTransform : null;
        float elapsed = 0f;

        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;
            float scale = Mathf.Lerp(1f, hitScale, t);

            if (iconRect != null)
                iconRect.localScale = new Vector3(scale, scale, 1f);

            if (textRect != null)
                textRect.localScale = new Vector3(scale, scale, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < hitAnimationDuration)
        {
            float t = elapsed / hitAnimationDuration;
            float scale = Mathf.Lerp(hitScale, 1f, t);

            if (iconRect != null)
                iconRect.localScale = new Vector3(scale, scale, 1f);

            if (textRect != null)
                textRect.localScale = new Vector3(scale, scale, 1f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (iconRect != null)
            iconRect.localScale = originalIconScale;

        if (textRect != null)
            textRect.localScale = originalTextScale;

        comboAnimRoutine = null;
    }

    public int GetCombo()
    {
        return combo;
    }
}