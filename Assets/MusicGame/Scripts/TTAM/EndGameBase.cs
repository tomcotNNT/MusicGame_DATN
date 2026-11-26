using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng kết thúc màn dùng chung cho mọi màn chơi.
///   Sao 1: điểm >= scoreForStar
///   Sao 2: combo cao nhất >= comboForStar
///   Sao 3: do từng màn quy định (Man1: ít Miss, Man2/Man3: hạ boss)
/// Thứ tự hiển thị giống nhau ở mọi màn: cuộn điểm -> cuộn coin -> từng hàng điều kiện.
/// Lưu kết quả (coin, kỷ lục, thống kê Good/Great/Perfect) vào PlayerDataManager đúng 1 lần.
/// </summary>
public abstract class EndGameBase : MonoBehaviour
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
    [SerializeField] protected ScoreSystem scoreSystem;
    [SerializeField] protected ComboSystem comboSystem;

    [Header("Level Data")]
    [SerializeField] protected int levelId = 1;

    [Header("Cuộn số")]
    [SerializeField] private float rollDuration = 1.5f;
    [Tooltip("Thời gian giữa 2 lần đổi chữ số ngẫu nhiên (giây)")]
    [SerializeField] private float rollInterval = 0.05f;
    [Tooltip("Giữ các chữ số cùng độ rộng để số không bị rung ngang khi cuộn")]
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

    [Header("3 hàng điều kiện: 0 = điểm, 1 = combo, 2 = điều kiện riêng của màn")]
    [SerializeField] private ConditionRow[] rows = new ConditionRow[3];

    [Header("Sprite")]
    [SerializeField] private Sprite starOn;
    [SerializeField] private Sprite starOff;

    [Header("Mốc đạt sao")]
    [Tooltip("Bật = mốc sao tự tính theo số nốt thật của lượt chơi (đúng cho mọi màn / mọi bài). Tắt = dùng 2 số cố định bên dưới.")]
    [SerializeField] protected bool autoStarThresholds = true;
    [Tooltip("Sao 1: điểm >= tỉ lệ này x điểm tối đa (số nốt x điểm Perfect)")]
    [SerializeField, Range(0.1f, 1f)] protected float scoreStarRatio = 0.6f;
    [Tooltip("Sao 2: combo cao nhất >= tỉ lệ này x số nốt")]
    [SerializeField, Range(0.05f, 1f)] protected float comboStarRatio = 0.25f;
    [Tooltip("Mốc combo tối đa (bảng kế hoạch: 100)")]
    [SerializeField, Min(1)] protected int comboStarCap = 100;
    [SerializeField] protected int scoreForStar = 10000;
    [SerializeField] protected int comboForStar = 20;

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
    public int LevelId => levelId;

    // ================= HOOK CHO TỪNG MÀN =================

    /// <summary>Đăng ký / huỷ đăng ký sự kiện hết bài của spawner màn này.</summary>
    protected abstract void SubscribeSongEnded(bool subscribe);

    /// <summary>Sao thứ 3. <paramref name="arg0"/>/<paramref name="arg1"/> điền vào mẫu chữ của hàng 3.</summary>
    protected abstract bool EvaluateThirdStar(out object arg0, out object arg1);

    /// <summary>Màn chơi có cho hiện bảng kết quả không (ví dụ: đã Game Over thì không).</summary>
    protected virtual bool CanShowResult() => !IsAnyPlayerDead();

    protected virtual void Awake()
    {
        if (scoreSystem == null) scoreSystem = FindFirstObjectByType<ScoreSystem>();
        if (comboSystem == null) comboSystem = FindFirstObjectByType<ComboSystem>();
    }

    protected virtual void Start()
    {
        shown = false;

        if (endPanel != null)
            endPanel.SetActive(false);

        SubscribeSongEnded(true);
    }

    protected virtual void OnDestroy()
    {
        SubscribeSongEnded(false);
    }

    /// <summary>Gọi khi bài hát kết thúc.</summary>
    protected void OnSongEnded()
    {
        if (shown || !CanShowResult()) return;
        StartCoroutine(ShowRoutine());
    }

    protected static bool IsAnyPlayerDead()
    {
        foreach (PlayerHealth hp in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
        {
            if (hp.IsDead)
                return true;
        }
        return false;
    }

    // ================= HIỂN THỊ =================

    private IEnumerator ShowRoutine()
    {
        shown = true;
        yield return new WaitForSecondsRealtime(showDelay);

        // Thua trong lúc chờ hiện bảng
        if (!CanShowResult())
            yield break;

        int score = scoreSystem != null ? scoreSystem.GetScore() : 0;
        int maxCombo = comboSystem != null ? comboSystem.MaxCombo : 0;

        bool third = EvaluateThirdStar(out object arg0, out object arg1);

        ResolveStarThresholds(out int scoreGoal, out int comboGoal);

        bool[] earned =
        {
            score >= scoreGoal,
            maxCombo >= comboGoal,
            third
        };

        string[] labels =
        {
            FormatRow(0, scoreGoal, score),
            FormatRow(1, comboGoal, maxCombo),
            FormatRow(2, arg0, arg1)
        };

        Stars = 0;
        foreach (bool e in earned)
            if (e) Stars++;

        earnedCoin = CalculateCoinReward(Stars);
        SaveResult(score, maxCombo);

        if (endPanel != null)
        {
            endPanel.SetActive(true);
            UpdateNextButton();
        }

        if (Stars > 0) PlayVictoryPose();

        if (scoreText != null)
            yield return StartCoroutine(RollNumber(scoreText, score, ""));

        if (coinRewardText != null)
            yield return StartCoroutine(RollNumber(coinRewardText, earnedCoin, "+"));

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

    // Mốc sao 1 và 2. Tự tính: số nốt = Good + Great + Perfect + Miss của lượt này
    private void ResolveStarThresholds(out int scoreGoal, out int comboGoal)
    {
        scoreGoal = scoreForStar;
        comboGoal = comboForStar;

        if (!autoStarThresholds || scoreSystem == null)
            return;

        int notes = scoreSystem.GoodCount + scoreSystem.GreatCount + scoreSystem.PerfectCount + scoreSystem.MissCount;
        if (notes <= 0)
            return;

        // Làm tròn cho đẹp: điểm theo bội 100
        scoreGoal = Mathf.Max(100, Mathf.RoundToInt(notes * scoreSystem.PerfectScore * scoreStarRatio / 100f) * 100);
        comboGoal = Mathf.Clamp(Mathf.CeilToInt(notes * comboStarRatio), 1, comboStarCap);
    }

    private string FormatRow(int index, object arg0, object arg1)
    {
        if (rows == null || index >= rows.Length || rows[index] == null)
            return "";

        string template = rows[index].text ?? "";

        try
        {
            return string.Format(template, arg0, arg1);
        }
        catch (System.FormatException)
        {
            return template;
        }
    }

    private void SaveResult(int score, int maxCombo)
    {
        PlayerDataManager data = PlayerDataManager.Instance;

        if (data == null)
        {
            Debug.LogWarning("[EndGame] Không có PlayerDataManager (mở thẳng scene màn chơi?) - kết quả không được lưu.");
            return;
        }

        data.AddCoin(earnedCoin);

        // Kỷ lục + thống kê Good/Great/Perfect + tiến độ nhiệm vụ ngày (1 lần ghi file)
        data.RecordLevelResult(
            levelId, score, maxCombo,
            scoreSystem != null ? scoreSystem.GoodCount : 0,
            scoreSystem != null ? scoreSystem.GreatCount : 0,
            scoreSystem != null ? scoreSystem.PerfectCount : 0);

        Debug.Log($"[EndGame] Level {levelId} | Stars: {Stars} | Coin: +{earnedCoin} | Score: {score} | MaxCombo: {maxCombo}" +
                  (scoreSystem != null ? $" | Miss: {scoreSystem.MissCount}" : ""));
    }

    // Nút "Next" trong bảng chỉ hiện khi vào được màn kế (đã mở khóa + có trong build)
    /// <summary>Thắng (có ít nhất 1 sao): nhân vật đang chơi chuyển sang tư thế ăn mừng (trigger Victory).</summary>
    private static void PlayVictoryPose()
    {
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Player"))
        {
            Animator a = go.GetComponentInChildren<Animator>();
            if (a == null || !AnimatorSafe.Has(a, "Victory")) continue;
            a.updateMode = AnimatorUpdateMode.UnscaledTime;   // bảng kết quả có thể dừng thời gian
            a.SetTrigger("Victory");
        }
    }

    private void UpdateNextButton()
    {
        foreach (Button b in endPanel.GetComponentsInChildren<Button>(true))
        {
            if (b.gameObject.name == "Next")
                b.gameObject.SetActive(LevelFlow.IsNextLevelAvailable());
        }
    }

    // ================= HIỆU ỨNG =================

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

    // Cuộn số kiểu máy đánh bạc: chữ số trái chốt sớm, chữ số phải chốt cuối
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
