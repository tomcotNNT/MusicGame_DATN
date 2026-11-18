using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EndGameManager : MonoBehaviour
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
        public string text = "Điểm ≥ {0} ({1})";
    }

    [Header("References")]
    [FormerlySerializedAs("pianoKeySpawner")]
    [SerializeField] private EnemySpawnerMan2 spawner;
    [SerializeField] private ScoreSystem scoreSystem;
    [SerializeField] private ComboSystem comboSystem;

    [Header("Cuộn số điểm")]
    [SerializeField] private float rollDuration = 1.5f;
    [Tooltip("Thời gian giữa 2 lần đổi chữ số ngẫu nhiên (giây)")]
    [SerializeField] private float rollInterval = 0.05f;
    [Tooltip("Giữ các chữ số cùng độ rộng để số không bị rung ngang khi cuộn")]
    [SerializeField] private bool useMonospace = true;
    [SerializeField] private float monospaceEm = 0.6f;

    [Header("Panel")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private TMP_Text scoreText;

    [Header("3 hàng điều kiện: 0 = điểm, 1 = combo, 2 = boss")]
    [SerializeField] private ConditionRow[] rows = new ConditionRow[3];

    [Header("Sprite")]
    [SerializeField] private Sprite starOn;
    [SerializeField] private Sprite starOff;

    [Header("Mốc đạt sao")]
    [SerializeField] private int scoreForStar = 5000;
    [SerializeField] private int comboForStar = 20;

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

        if (spawner != null)
            spawner.SongEnded += OnSongEnded;
    }

    private void OnDestroy()
    {
        BossController2.AnyBossDefeated -= OnBossDefeated;

        if (spawner != null)
            spawner.SongEnded -= OnSongEnded;
    }

    private void OnBossDefeated() => bossDefeated = true;

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

        bool[] earned =
        {
            score >= scoreForStar,
            maxCombo >= comboForStar,
            bossDefeated
        };

        string[] labels =
        {
            string.Format(rows[0].text, scoreForStar, score),
            string.Format(rows[1].text, comboForStar, maxCombo),
            string.Format(rows[2].text, "", "")
        };

        Stars = 0;
        foreach (bool e in earned)
            if (e) Stars++;

        if (endPanel != null)
            endPanel.SetActive(true);

        if (scoreText != null)
            yield return StartCoroutine(RollNumber(scoreText, score));
        // Trạng thái ban đầu: mọi hàng là "-", chưa có chữ, sao tắt
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

            // Gõ từng chữ
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
                if (row.status != null) row.status.text = "";   // nhường chỗ cho sao nhỏ

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

    // Sao nhỏ ở hàng bay lên ô sao chính, rồi ô sao chính sáng + pop
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

                // To lên giữa chặng bay cho có cảm giác "bay"
                float bump = Mathf.Sin(k * Mathf.PI);
                t.localScale = startScale * Mathf.Lerp(1f, flyScale, bump);

                yield return null;
            }

            // Trả sao nhỏ về chỗ cũ rồi ẩn
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

    private IEnumerator RollNumber(TMP_Text label, int target)
    {
        string final = target.ToString("#,##0", CultureInfo.InvariantCulture);
        int len = final.Length;

        // Đếm số chữ số để chia thời điểm chốt
        int digitCount = 0;
        for (int i = 0; i < len; i++)
            if (char.IsDigit(final[i])) digitCount++;

        // Chữ số bên trái chốt sớm (40% thời gian), bên phải chốt cuối cùng (100%)
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
                        buffer[i] = final[i];   // dấu phẩy hoặc chữ số đã chốt
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
}