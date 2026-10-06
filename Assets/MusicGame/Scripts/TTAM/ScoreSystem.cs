using UnityEngine;
using TMPro;

public class ScoreSystem : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private int scorePerNote = 100;

    [Header("UI")]
    [SerializeField] private TMP_Text scoreText;

    private int score;
    private FeverSystem feverSystem;

    private void Awake()
    {
        feverSystem = FindObjectOfType<FeverSystem>();
        if (feverSystem == null)
        {
            Debug.LogError("Không tìm thấy FeverSystem trong scene!");
        }
    }

    private void Start()
    {
        score = 0;
        UpdateScoreUI();
    }

    public void AddScore()
    {
        score += scorePerNote;

        //nếu đang fever thì x2 điểm
        if (feverSystem != null && feverSystem.isFever)
        {
            score += scorePerNote; // Thêm điểm gấp đôi khi đang trong trạng thái Fever
        }

        UpdateScoreUI();

        Debug.Log("Score: " + score);
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