using UnityEngine;
using TMPro;

public class ScoreSystem : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scorePerNote = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private int score;

    private void Start()
    {
        score = 0;
        UpdateScoreUI();
    }

    public void AddScore()
    {
        score += scorePerNote;

        UpdateScoreUI();

        Debug.Log("Score: " + score);
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + score.ToString();
        }
    }

    public int GetScore()
    {
        return score;
    }
}