using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndGameManagerMiss : MonoBehaviour
{
    [System.Serializable]
    public class ConditionRow
    {
        [Tooltip("Chữ mô tả điều kiện (sẽ được gõ từng chữ)")]
        public TMP_Text label;
        [Tooltip("Dấu '-' / 'X' đỏ ở đầu hàng")]
        public TMP_Text status;
        [Tooltip("Sao nhỏ nằm ở hàng, sẽ bay lên ô sao chính (để tắt sẵn)")]
        public Image flyStar;
        [Tooltip("Ô sao chính trên hàng sao")]
        public Image mainStar;
        [Tooltip("Mẫu chữ. {0} = mốc cần đạt, {1} = số thực tế")]
        public string text = "";
    }

    [Header("References")]
    [SerializeField] private PianoKeySpawner spawner;
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Level Data")]
    [SerializeField] private int levelId = 1;

    [Header("Cuộn số")]
    [SerializeField] private float rollDuration = 1.5f;
    [SerializeField] private float rollInterval = 0.05f;
    [SerializeField] private bool useMonospace = true;
    [SerializeField] private float monospaceEm = 0.6f;

    [Header("Coin Reward")]
    [SerializeField] private int coinFor1Star = 200;
    [SerializeField] private int coinFor2Stars = 500;
    [SerializeField] private int coinFor3Stars = 1000;
    [SerializeField] private TMP_Text coinRewardText;

    [Header("Panel")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text scoreText;

    [Header("3 hàng: 0 = điểm, 1 = combo, 2 = miss")]
    [SerializeField] private ConditionRow[] rows = new ConditionRow[3];

    [Header("Sprite")]
    [SerializeField] private Sprite starOn;
    [SerializeField] private Sprite starOff;

    [Header("Mốc đạt sao")]
    [SerializeField] private int scoreForStar = 5000;
    [SerializeField] private int comboForStar = 20;
    [Tooltip("Miss không quá số này thì được sao thứ 3")]
    [SerializeField] private int maxMissForStar = 5;

    [Header("Hiển thị")]
    [SerializeField] private float showDelay = 2f;
    [SerializeField] private float letterDelay = 0.04f;
    [SerializeField] private float rowPause = 0.25f;
    [SerializeField] private float flyDuration = 0.6f;
    [SerializeField] private float flyScale = 1.5f;
    [SerializeField] private float starPopScale = 1.4f;

    [Header("Màu")]
    [SerializeField] private Color pendingColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] private Color successColor = new Color(0.4f, 1f, 0.4f);
    [SerializeField] private Color failColor = new Color(1f, 0.25f, 0.25f);

    private bool shown;
    private int earnedCoin;

    public int Stars { get; private set; }

    private void Start()
    {
        shown = false;

        if (endPanel != null)
            endPanel.SetActive(false);

        if (spawner != null)
            spawner.SongEnded += OnSongEnded;
    }

    private void OnDestroy()
    {
        if (spawner != null)
            spawner.SongEnded -= OnSongEnded;
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
        int missCount = scoreSystem != null ? scoreSystem.MissCount : 0;

        bool[] earned =
        {
            score >= scoreForStar,
            maxCombo >= comboForStar,
            missCount <= maxMissForStar
        };

        string[] labels =
        {
            string.Format(rows[0].text, scoreForStar, score),
            string.Format(rows[1].text, comboForStar, maxCombo),
            string.Format(rows[2].text, maxMissForStar, missCount)
        };

        Stars = 0;
        foreach (bool e in earned)
            if (e) Stars++;

        earnedCoin = CalculateCoinReward(Stars);

        if (PlayerDataManager.Instance != null)
        {
            PlayerDataManager.Instance.AddCoin(earnedCoin);
            PlayerDataManager.Instance.UpdateLevelRecord(levelId, score, maxCombo);

            if (scoreSystem != null)
            {
                PlayerDataManager.Instance.AddComboCounts(
                    scoreSystem.GoodCount,
                    scoreSystem.GreatCount,
                    scoreSystem.PerfectCount);
            }

            Debug.Log($"Level Completed | Stars: {Stars} | Coin: +{earnedCoin} | Miss: {missCount}");
        }
        else
        {
            Debug.LogError("PlayerDataManager not found!");
        }

        if (endPanel != null)
            endPanel.SetActive(true);

        if (coinRewardText != null)
            yield return StartCoroutine(RollNumber(coinRewardText, earnedCoin, "+"));

        if (scoreText != null)
            yield return StartCoroutine(RollNumber(scoreText, score, ""));

        // Trạng thái ban đầu
        foreach (ConditionRow r in rows)
        {
            if (r == null) continue;

            if (r.label != null) r.label.text = "";

            if (r.status != null)
            {
                r.status.text = "-";
                r.status.color = pendingColor;
            }

            if (r.flyStar != null) r.flyStar.gameObject.SetActive(false);

            if (r.mainStar != null)
            {
                r.mainStar.sprite = starOff;
                r.mainStar.transform.localScale = Vector3.one;
            }
        }

        // Lần lượt từng hàng
        for (int i = 0; i < rows.Length && i < earned.Length; i++)
        {
            ConditionRow row = rows[i];
            if (row == null) continue;

            yield return new WaitForSecondsRealtime(rowPause);

            if (row.label != null)
            {
                row.label.color = pendingColor;

                for (int c = 1; c <= labels[i].Length; c++)
                {
                    row.label.text = labels[i].Substring(0, c);
                    yield return new WaitForSecondsRealtime(letterDelay);
                }
            }

            yield return new WaitForSecondsRealtime(0.15f);

            if (earned[i])
            {
                if (row.label != null) row.label.color = successColor;
                if (row.status != null) row.status.text = "";

                yield return StartCoroutine(FlyStar(row));
            }
            else
            {
                if (row.label != null) row.label.color = failColor;

                if (row.status != null)
                {
                    row.status.text = "X";
                    row.status.color = failColor;
                }
            }
        }
    }

    private IEnumerator FlyStar(ConditionRow row)
    {
        if (row.mainStar == null)
            yield break;

        if (row.flyStar != null)
        {
            Transform t = row.flyStar.transform;
            Vector3 startPos = t.position;
            Vector3 startScale = t.localScale;
            Vector3 endPos = row.mainStar.transform.position;

            row.flyStar.sprite = starOn;
            row.flyStar.gameObject.SetActive(true);

            float time = 0f;
            while (time < flyDuration)
            {
                time += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, time / flyDuration);

                t.position = Vector3.Lerp(startPos, endPos, k);

                float bump = Mathf.Sin(k * Mathf.PI);
                t.localScale = startScale * Mathf.Lerp(1f, flyScale, bump);

                yield return null;
            }

            t.position = startPos;
            t.localScale = startScale;
            row.flyStar.gameObject.SetActive(false);
        }

        row.mainStar.sprite = starOn;
        yield return StartCoroutine(Pop(row.mainStar.transform));
    }

    private IEnumerator Pop(Transform t)
    {
        const float dur = 0.2f;
        float time = 0f;

        while (time < dur)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / dur);
            t.localScale = Vector3.one * Mathf.Lerp(starPopScale, 1f, k);
            yield return null;
        }

        t.localScale = Vector3.one;
    }

    // Dùng chung cho điểm và coin (prefix = "+" cho coin)
    private IEnumerator RollNumber(TMP_Text label, int target, string prefix)
    {
        string final = prefix + target.ToString("#,##0", CultureInfo.InvariantCulture);
        int len = final.Length;

        int digitCount = 0;
        for (int i = 0; i < len; i++)
            if (char.IsDigit(final[i])) digitCount++;

        float[] lockTime = new float[len];
        int d = 0;
        for (int i = 0; i < len; i++)
        {
            if (!char.IsDigit(final[i])) continue;

            float p = digitCount > 1 ? (float)d / (digitCount - 1) : 1f;
            lockTime[i] = rollDuration * Mathf.Lerp(0.4f, 1f, p);
            d++;
        }

        char[] buffer = new char[len];
        float time = 0f;
        float nextChange = 0f;

        while (time < rollDuration)
        {
            time += Time.unscaledDeltaTime;

            if (time >= nextChange)
            {
                nextChange = time + rollInterval;

                for (int i = 0; i < len; i++)
                {
                    if (!char.IsDigit(final[i]) || time >= lockTime[i])
                        buffer[i] = final[i];
                    else
                        buffer[i] = (char)('0' + Random.Range(0, 10));
                }

                label.text = FormatRoll(new string(buffer));
            }

            yield return null;
        }

        label.text = FormatRoll(final);
        yield return StartCoroutine(Pop(label.transform));
    }

    private string FormatRoll(string s)
    {
        return useMonospace ? "<mspace=" + monospaceEm + "em>" + s + "</mspace>" : s;
    }

    private int CalculateCoinReward(int stars)
    {
        switch (stars)
        {
            case 1: return coinFor1Star;
            case 2: return coinFor2Stars;
            case 3: return coinFor3Stars;
            default: return 0;
        }
    }
}