using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bảng kết quả cuối màn, dựng bằng code, cùng phong cách neon pixel với menu Pause (NeonUI).
/// Trình tự: thẻ bật lên -> cuộn điểm -> combo / coin -> thống kê Perfect/Great/Good/Miss
///           -> từng điều kiện gõ chữ, đạt thì sao lớn sáng lên kèm chớp + rung -> pháo giấy -> hiện nút.
/// Chạm vào nền tối để tua nhanh. Mọi hiệu ứng chạy theo thời gian thật.
/// EndGameBase tính dữ liệu và gọi Play().
/// </summary>
public class ResultPanelUI : MonoBehaviour
{
    public struct Data
    {
        public int levelId, score, maxCombo, coin, stars;
        public bool[] earned;      // 3 điều kiện
        public string[] labels;    // chữ đầy đủ của 3 điều kiện (không còn hiện, giữ cho tương thích)
        public string[] captions;  // chữ ngắn dưới mỗi ngôi sao, ví dụ "SCORE 19,800"
        public int perfect, great, good, miss;
        public bool nextAvailable;
    }

    public Action OnReplay, OnNext, OnHome;
    public AudioClip starClip, tickClip;

    private TMP_FontAsset font;
    private GameObject root;
    private CanvasGroup rootGroup;
    private RectTransform card, glow, buttons;
    private TMP_Text title, subtitle, scoreText, comboText, coinText;
    private RectTransform grades;
    private readonly TMP_Text[] gradeNum = new TMP_Text[4];
    private readonly Image[] stars = new Image[3];
    private readonly Image[] starFlash = new Image[3];
    private readonly TMP_Text[] caption = new TMP_Text[3];
    private readonly bool[] lit = new bool[3];
    private bool skip, finished, win;

    private static readonly Color StarOff = new Color32(96, 84, 150, 255);
    private static readonly Color CaptionOff = new Color32(200, 192, 230, 255);

    // Bố cục cho điện thoại: thẻ 1300 x 960 trên khung tham chiếu 1920 x 1080 (theo chiều cao)
    private const float CardW = 1300, CardH = 960, ButtonsY = -378;

    public static ResultPanelUI Create()
    {
        var go = new GameObject("ResultPanel (auto)");
        var ui = go.AddComponent<ResultPanelUI>();
        ui.Build();
        return ui;
    }

    // ===============================================================
    private void Build()
    {
        font = NeonUI.FindGameFont();
        NeonUI.MakeCanvas(gameObject, 490);

        root = NeonUI.NewUI("Root", transform);
        NeonUI.Stretch(root.GetComponent<RectTransform>());
        rootGroup = root.AddComponent<CanvasGroup>();
        var dim = root.AddComponent<Image>();
        dim.color = NeonUI.Dim;
        var skipBtn = root.AddComponent<Button>();          // chạm nền để tua nhanh
        skipBtn.transition = Selectable.Transition.None;
        skipBtn.onClick.AddListener(() => skip = true);

        glow = NeonUI.Img("Glow", root.transform, NeonUI.GlowSprite(), new Color(1, 1, 1, 0.3f), new Vector2(CardW + 160, CardH + 130)).rectTransform;
        NeonUI.Img("Shadow", root.transform, NeonUI.FrameSprite(), new Color(0, 0, 0, 0.55f), new Vector2(CardW, CardH)).rectTransform.anchoredPosition = new Vector2(14, -16);
        var cardImg = NeonUI.Img("Card", root.transform, NeonUI.FrameSprite(), Color.white, new Vector2(CardW, CardH));
        card = cardImg.rectTransform;
        cardImg.raycastTarget = true;   // bấm vào thẻ cũng tua nhanh
        var cardBtn = cardImg.gameObject.AddComponent<Button>();
        cardBtn.transition = Selectable.Transition.None;
        cardBtn.onClick.AddListener(() => skip = true);

        title = NeonUI.Text("Title", card, "", 112, NeonUI.Yellow, new Vector2(0, 386), new Vector2(1200, 130), font, 0.28f);
        subtitle = NeonUI.Text("Stage", card, "", 42, NeonUI.Cyan, new Vector2(0, 314), new Vector2(700, 56), font, 0.25f);

        // 3 ngôi sao + chữ điều kiện ngắn bên dưới mỗi sao
        for (int i = 0; i < 3; i++)
        {
            float size = i == 1 ? 196 : 156;
            var pos = new Vector2((i - 1) * 360, i == 1 ? 208 : 192);
            starFlash[i] = NeonUI.Img("StarFlash" + i, card, NeonUI.GlowSprite(), new Color(1, 0.9f, 0.4f, 0), new Vector2(size * 2.2f, size * 2.2f));
            starFlash[i].rectTransform.anchoredPosition = pos;
            stars[i] = NeonUI.Img("Star" + i, card, NeonUI.StarSprite(), StarOff, new Vector2(size, size), sliced: false);
            stars[i].rectTransform.anchoredPosition = pos;
            caption[i] = NeonUI.Text("Caption" + i, card, "", 34, CaptionOff, new Vector2((i - 1) * 360, 82), new Vector2(340, 50), font, 0.28f);
            caption[i].enableAutoSizing = true;
            caption[i].fontSizeMin = 22; caption[i].fontSizeMax = 34;
        }

        NeonUI.Text("ScoreLabel", card, "SCORE", 40, NeonUI.Pink, new Vector2(0, 14), new Vector2(500, 50), font, 0.28f);
        scoreText = NeonUI.Text("Score", card, "0", 124, Color.white, new Vector2(0, -62), new Vector2(1200, 140), font, 0.3f);
        comboText = NeonUI.Text("Combo", card, "", 52, NeonUI.Cyan, new Vector2(-290, -170), new Vector2(560, 64), font, 0.28f);
        coinText = NeonUI.Text("Coin", card, "", 52, NeonUI.Yellow, new Vector2(290, -170), new Vector2(560, 64), font, 0.28f);

        // 4 ô Perfect / Great / Good / Miss
        grades = NeonUI.NewUI("Grades", card).GetComponent<RectTransform>();
        grades.anchoredPosition = new Vector2(0, -262);
        string[] gName = { "PERFECT", "GREAT", "GOOD", "MISS" };
        Color[] gCol = { NeonUI.Cyan, NeonUI.Mint, NeonUI.Yellow, NeonUI.Red };
        for (int i = 0; i < 4; i++)
        {
            var box = NeonUI.Img(gName[i], grades, NeonUI.ButtonSprite(), Color.Lerp(NeonUI.Ink, gCol[i], 0.28f), new Vector2(280, 92));
            box.rectTransform.anchoredPosition = new Vector2((i - 1.5f) * 298, 0);
            NeonUI.Text("Name", box.rectTransform, gName[i], 26, Color.white, new Vector2(0, 22), new Vector2(270, 36), font, 0.25f);
            gradeNum[i] = NeonUI.Text("Value", box.rectTransform, "0", 44, gCol[i], new Vector2(0, -16), new Vector2(270, 52), font, 0.3f);
        }
        grades.gameObject.AddComponent<CanvasGroup>().alpha = 0f;

        buttons = NeonUI.NewUI("Buttons", card).GetComponent<RectTransform>();
        buttons.anchoredPosition = new Vector2(0, ButtonsY);
        buttons.gameObject.AddComponent<CanvasGroup>();
        root.SetActive(false);
    }

    private void BuildButtons(bool next)
    {
        // nút to, cách xa nhau -> dễ bấm bằng ngón cái
        if (next)
        {
            NeonUI.MakeButton(buttons, "REPLAY", NeonUI.Pink, new Vector2(-400, 0), new Vector2(340, 112), 54, () => OnReplay?.Invoke(), font);
            NeonUI.MakeButton(buttons, "NEXT", NeonUI.Mint, new Vector2(0, 0), new Vector2(380, 124), 64, () => OnNext?.Invoke(), font);
            NeonUI.MakeButton(buttons, "HOME", NeonUI.Violet, new Vector2(400, 0), new Vector2(340, 112), 54, () => OnHome?.Invoke(), font);
        }
        else
        {
            NeonUI.MakeButton(buttons, "REPLAY", NeonUI.Pink, new Vector2(-220, 0), new Vector2(380, 118), 58, () => OnReplay?.Invoke(), font);
            NeonUI.MakeButton(buttons, "HOME", NeonUI.Violet, new Vector2(220, 0), new Vector2(380, 118), 58, () => OnHome?.Invoke(), font);
        }
    }

    // ===============================================================
    // TRÌNH TỰ
    // ===============================================================
    public IEnumerator Play(Data d)
    {
        win = d.stars > 0;
        root.SetActive(true);
        title.text = win ? (d.stars == 3 ? "PERFECT CLEAR!" : "STAGE CLEAR!") : "TRY AGAIN";
        title.color = win ? NeonUI.Yellow : NeonUI.Pink;
        subtitle.text = d.levelId > 0 ? "STAGE " + d.levelId : "BEAT RUN";
        BuildButtons(d.nextAvailable && win);
        var btnGroup = buttons.GetComponent<CanvasGroup>();
        btnGroup.alpha = 0f; btnGroup.interactable = false;

        // 1) thẻ bật lên
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Clamp01(t / 0.2f);
            card.localScale = Vector3.one * NeonUI.BackOut(0.8f, 1f, Mathf.Clamp01(t / 0.35f));
            yield return null;
        }
        card.localScale = Vector3.one;

        // 2) cuộn điểm
        yield return Roll(scoreText, d.score, "", 1.2f);
        yield return Pop(scoreText.transform, 1.25f);

        // 3) combo + coin đếm lên
        yield return CountUp(0.45f, k =>
        {
            comboText.text = "MAX COMBO " + Mathf.RoundToInt(d.maxCombo * k);
            coinText.text = "+" + Mathf.RoundToInt(d.coin * k).ToString("#,##0", CultureInfo.InvariantCulture) + " COIN";
        });

        // 4) ô Perfect / Great / Good / Miss
        var gg = grades.GetComponent<CanvasGroup>();
        int[] gv = { d.perfect, d.great, d.good, d.miss };
        yield return CountUp(0.4f, k =>
        {
            gg.alpha = Mathf.Clamp01(k * 2f);
            for (int g = 0; g < 4; g++) gradeNum[g].text = Mathf.RoundToInt(gv[g] * k).ToString();
        });

        // 5) chữ điều kiện dưới mỗi sao -> đạt thì sao sáng
        for (int i = 0; i < 3; i++)
            caption[i].text = d.captions != null && i < d.captions.Length ? d.captions[i] : "";
        for (int i = 0; i < 3; i++)
        {
            if (!skip) yield return new WaitForSecondsRealtime(0.22f);
            bool ok = d.earned != null && i < d.earned.Length && d.earned[i];
            caption[i].color = ok ? NeonUI.Mint : NeonUI.Red;
            StartCoroutine(Pop(caption[i].transform, 1.35f));
            if (ok) yield return LightStar(i);
        }

        // 6) pháo giấy khi thắng
        if (win) StartCoroutine(Confetti(d.stars == 3 ? 70 : 40));

        // 7) hiện nút
        t = 0f;
        while (t < 0.25f)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / 0.25f);
            btnGroup.alpha = k;
            buttons.anchoredPosition = new Vector2(0, ButtonsY - 30 * (1 - NeonUI.EaseOut(k)));
            yield return null;
        }
        btnGroup.interactable = true;
        finished = true;
    }

    private IEnumerator LightStar(int i)
    {
        lit[i] = true;
        GameplaySfx.Play(starClip, 0.9f);
        var st = stars[i].rectTransform;
        var fl = starFlash[i];
        float t = 0f, dur = skip ? 0.12f : 0.4f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            stars[i].color = Color.Lerp(Color.white, NeonUI.Yellow, k);
            st.localScale = Vector3.one * NeonUI.BackOut(1.6f, 1f, k);
            st.localRotation = Quaternion.Euler(0, 0, (1 - k) * 25f);
            fl.color = new Color(1f, 0.9f, 0.4f, 0.9f * (1 - k));
            fl.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.4f, k);
            card.anchoredPosition = UnityEngine.Random.insideUnitCircle * 10f * (1 - k);   // rung nhẹ
            yield return null;
        }
        st.localScale = Vector3.one; st.localRotation = Quaternion.identity;
        card.anchoredPosition = Vector2.zero;
        fl.color = new Color(1, 1, 1, 0);
    }

    private IEnumerator Roll(TMP_Text label, int target, string prefix, float duration)
    {
        string final = prefix + target.ToString("#,##0", CultureInfo.InvariantCulture);
        char[] buf = new char[final.Length];
        float t = 0f, next = 0f;
        while (t < duration && !skip)
        {
            t += Time.unscaledDeltaTime;
            if (t >= next)
            {
                next = t + 0.05f;
                for (int i = 0; i < final.Length; i++)
                {
                    float lockAt = duration * Mathf.Lerp(0.35f, 1f, (float)i / Mathf.Max(1, final.Length - 1));
                    buf[i] = !char.IsDigit(final[i]) || t >= lockAt ? final[i] : (char)('0' + UnityEngine.Random.Range(0, 10));
                }
                label.text = "<mspace=0.6em>" + new string(buf) + "</mspace>";
                GameplaySfx.Play(tickClip, 0.35f);
            }
            yield return null;
        }
        label.text = "<mspace=0.6em>" + final + "</mspace>";
    }

    private IEnumerator CountUp(float duration, Action<float> apply)
    {
        float t = 0f;
        while (t < duration && !skip)
        {
            t += Time.unscaledDeltaTime;
            apply(NeonUI.EaseOut(Mathf.Clamp01(t / duration)));
            yield return null;
        }
        apply(1f);
    }

    private IEnumerator FadeIn(TMP_Text text, float duration)
    {
        float t = 0f;
        while (t < duration && !skip)
        {
            t += Time.unscaledDeltaTime;
            text.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }
        text.alpha = 1f;
    }

    private IEnumerator Pop(Transform tr, float from)
    {
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.unscaledDeltaTime;
            tr.localScale = Vector3.one * Mathf.Lerp(from, 1f, NeonUI.EaseOut(Mathf.Clamp01(t / 0.2f)));
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    private IEnumerator Confetti(int count)
    {
        Color[] cols = { NeonUI.Cyan, NeonUI.Pink, NeonUI.Yellow, NeonUI.Mint, NeonUI.Violet };
        var parts = new List<(RectTransform rt, Vector2 v, float spin)>();
        var layer = NeonUI.NewUI("Confetti", root.transform).GetComponent<RectTransform>();
        layer.SetAsLastSibling();
        for (int i = 0; i < count; i++)
        {
            var img = NeonUI.Img("c", layer, NeonUI.ButtonSprite(), cols[i % cols.Length],
                                 new Vector2(UnityEngine.Random.Range(10, 22), UnityEngine.Random.Range(14, 30)), sliced: false);
            img.raycastTarget = false;
            img.rectTransform.anchoredPosition = new Vector2(UnityEngine.Random.Range(-640f, 640f), 330f);
            parts.Add((img.rectTransform, new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(420f, 900f)),
                       UnityEngine.Random.Range(-540f, 540f)));
        }
        float t = 0f;
        while (t < 3f)
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            for (int i = 0; i < parts.Count; i++)
            {
                var p = parts[i];
                p.v.y -= 1100f * dt;
                p.v.x *= 1f - 0.8f * dt;
                p.rt.anchoredPosition += p.v * dt;
                p.rt.Rotate(0, 0, p.spin * dt);
                parts[i] = p;
            }
            yield return null;
        }
        Destroy(layer.gameObject);
    }

    // nhịp đập theo beat sau khi hiện xong
    private void Update()
    {
        if (root == null || !root.activeSelf) return;
        float beat = NeonUI.Beat();
        Color gc = win ? Color.Lerp(NeonUI.Yellow, NeonUI.Pink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f))
                       : Color.Lerp(NeonUI.Violet, NeonUI.Pink, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f));
        glow.GetComponent<Image>().color = new Color(gc.r, gc.g, gc.b, 0.22f + 0.14f * beat);
        glow.localScale = Vector3.one * (1f + 0.02f * beat);
        title.rectTransform.localScale = Vector3.one * (1f + 0.04f * beat);
        if (!finished) return;
        for (int i = 0; i < 3; i++)
            if (lit[i]) stars[i].rectTransform.localScale = Vector3.one * (1f + 0.08f * beat);
    }
}
