using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResultSystem : MonoBehaviour
{
    [Header("Star Thresholds (điểm > mốc)")]
    [SerializeField] private int star1Score = 10000;
    [SerializeField] private int star2Score = 15000;
    [SerializeField] private int star3Score = 20000;

    [Header("UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Image[] starImages;         // 3 ảnh sao theo thứ tự trái -> phải

    [Header("Star Sprites")]
    [SerializeField] private Sprite starOn;
    [SerializeField] private Sprite starOff;

    [Header("Animation")]
    [SerializeField] private float delayBetweenStars = 0.4f;
    [SerializeField] private float popDuration = 0.25f;
    [SerializeField] private float popOvershoot = 1.4f;

    [Header("Sound (tùy chọn)")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip starSound;

    private bool shown;

    private void Start()
    {
        if (resultPanel != null)
            resultPanel.SetActive(false);
    }

    // Tính số sao từ điểm
    public int CalculateStars(int score)
    {
        if (score > star3Score) return 3;
        if (score > star2Score) return 2;
        if (score > star1Score) return 1;
        return 0;
    }

    public bool IsWin(int score)
    {
        return CalculateStars(score) >= 1;
    }

    // Gọi khi bài nhạc kết thúc
    public void ShowResult(int score)
    {
        if (shown)
            return;

        shown = true;

        int stars = CalculateStars(score);

        if (resultPanel != null)
            resultPanel.SetActive(true);

        if (scoreText != null)
            scoreText.text = score.ToString();

        StartCoroutine(PlayStarAnimation(stars));
    }

    private IEnumerator PlayStarAnimation(int stars)
    {
        // Tắt hết sao trước
        for (int i = 0; i < starImages.Length; i++)
        {
            if (starImages[i] == null) continue;
            starImages[i].sprite = starOff;
            starImages[i].rectTransform.localScale = Vector3.one;
        }

        // Bật lần lượt từng sao
        for (int i = 0; i < stars && i < starImages.Length; i++)
        {
            yield return new WaitForSeconds(delayBetweenStars);

            if (starImages[i] == null) continue;

            starImages[i].sprite = starOn;

            if (sfxSource != null && starSound != null)
                sfxSource.PlayOneShot(starSound);

            yield return StartCoroutine(PopStar(starImages[i].rectTransform));
        }
    }

    private IEnumerator PopStar(RectTransform rect)
    {
        float elapsed = 0f;
        float up = popDuration * 0.6f;
        float down = popDuration * 0.4f;

        while (elapsed < up)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(0f, popOvershoot, elapsed / up);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < down)
        {
            elapsed += Time.deltaTime;
            rect.localScale = Vector3.one * Mathf.Lerp(popOvershoot, 1f, elapsed / down);
            yield return null;
        }

        rect.localScale = Vector3.one;
    }
}