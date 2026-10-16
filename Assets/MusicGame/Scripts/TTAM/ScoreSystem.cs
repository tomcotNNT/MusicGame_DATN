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

    public void AddScore(int amount)
    {
        score += amount;

        UpdateScoreUI();
    }

    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = score.ToString();
        }
    }

    public int GetScore()
    {
        return score;
    }
}