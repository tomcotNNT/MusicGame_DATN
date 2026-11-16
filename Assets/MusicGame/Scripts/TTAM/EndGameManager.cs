using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private EnemySpawnerMan2 pianoKeySpawner;
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Panel")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text scoreText;

    [Header("Sao (3 Image: sao 1 = điểm, sao 2 = combo, sao 3 = boss)")]
    [SerializeField] private Image[] starImages = new Image[3];
    [SerializeField] private Sprite starOn;
    [SerializeField] private Sprite starOff;

    [Header("Mốc đạt sao")]
    [SerializeField] private int scoreForStar = 5000;
    [SerializeField] private int comboForStar = 20;

    [Header("Hiển thị")]
    [Tooltip("Chờ bao lâu sau khi hết nhạc mới hiện panel (để player lùi về xong)")]
    [SerializeField] private float showDelay = 2f;
    [SerializeField] private float starInterval = 0.35f;
    [SerializeField] private float starPopScale = 1.4f;

    private bool bossDefeated;
    private bool shown;

    public int Stars { get; private set; }

    private void Start()
    {
        shown = false;
        bossDefeated = false;

        if (endPanel != null)
            endPanel.SetActive(false);

        BossController2.AnyBossDefeated += OnBossDefeated;

        if (pianoKeySpawner != null)
            pianoKeySpawner.SongEnded += OnSongEnded;
    }

    private void OnDestroy()
    {
        BossController2.AnyBossDefeated -= OnBossDefeated;

        if (pianoKeySpawner != null)
            pianoKeySpawner.SongEnded -= OnSongEnded;
    }

    private void OnBossDefeated()
    {
        bossDefeated = true;
    }

    private void OnSongEnded()
    {
        if (shown) return;
        StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        shown = true;
        yield return new WaitForSecondsRealtime(showDelay);

        int score = scoreSystem != null ? scoreSystem.GetScore() : 0;
        int maxCombo = comboSystem != null ? comboSystem.MaxCombo : 0;

        bool scoreOk = score >= scoreForStar;
        bool comboOk = maxCombo >= comboForStar;
        bool bossOk = bossDefeated;

        Stars = (scoreOk ? 1 : 0) + (comboOk ? 1 : 0) + (bossOk ? 1 : 0);

        if (endPanel != null)
            endPanel.SetActive(true);

        if (scoreText != null) scoreText.text = score.ToString();

        // Tắt hết sao rồi bật lần lượt
        foreach (Image s in starImages)
        {
            if (s == null) continue;
            s.sprite = starOff;
            s.transform.localScale = Vector3.one;
        }

        bool[] earned = { scoreOk, comboOk, bossOk };

        for (int i = 0; i < starImages.Length && i < earned.Length; i++)
        {
            yield return new WaitForSecondsRealtime(starInterval);

            if (!earned[i] || starImages[i] == null)
                continue;

            starImages[i].sprite = starOn;
            yield return StartCoroutine(Pop(starImages[i].transform));
        }
    }

    private static void SetCondition(TMP_Text text, string label, bool ok)
    {
        if (text == null) return;
        text.text = (ok ? "✔ " : "✘ ") + label;
        text.color = ok ? new Color(0.4f, 1f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
    }

    private IEnumerator Pop(Transform t)
    {
        const float dur = 0.2f;
        float time = 0f;

        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float k = time / dur;
            float s = Mathf.Lerp(starPopScale, 1f, k);
            t.localScale = Vector3.one * s;
            yield return null;
        }

        t.localScale = Vector3.one;
    }
}