using UnityEngine;
using TMPro;

public class ComboSystem : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text judgmentText;

    [Header("Combo Threshold")]
    [SerializeField] private int awesomeCombo = 6;
    [SerializeField] private int perfectCombo = 10;

    private int combo;

    private void Start()
    {
        combo = 0;

        comboText.text = "";
        judgmentText.text = "";
    }

    public void HitNote()
    {
        combo++;

        UpdateJudgment();

        comboText.text = combo.ToString();
    }

    public void MissNote()
    {
        combo = 0;

        comboText.text = "0";
        judgmentText.text = "BAD";

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

    public int GetCombo()
    {
        return combo;
    }
}