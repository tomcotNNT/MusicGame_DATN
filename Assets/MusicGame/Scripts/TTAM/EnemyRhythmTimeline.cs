
using TMPro;
using UnityEngine;

public class EnemyRhythmTimelineUI : MonoBehaviour
{
    [SerializeField] private TMP_Text sequenceText;
    [SerializeField] private TMP_Text directionText;

    public void Setup(int totalNotes, bool isLeft)
    {
        if (directionText != null)
            directionText.text = isLeft ? "LEFT - A" : "RIGHT - D";

        UpdateProgress(0, totalNotes);
    }

    public void UpdateProgress(int currentIndex, int totalNotes)
    {
        if (sequenceText == null) return;

        string result = "";

        for (int i = 0; i < totalNotes; i++)
        {
            if (i < currentIndex)
                result += "<color=#55E88A>●</color> ";
            else if (i == currentIndex)
                result += "<color=#FFD34E>●</color> ";
            else
                result += "<color=#777777>●</color> ";
        }

        sequenceText.text = result;
    }
}
