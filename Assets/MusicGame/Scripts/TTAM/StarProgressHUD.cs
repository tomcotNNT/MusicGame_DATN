using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh 3 mốc sao hiện trong lúc chơi, dựng bằng code, cùng phong cách neon pixel với bảng kết quả.
///   Ô 1 (hồng)  = điểm so với mốc sao 1
///   Ô 2 (xanh)  = combo cao nhất so với mốc sao 2
///   Ô 3 (bạc hà) = máu boss đã mất (sao 3), màn không theo dõi được thì ẩn ô này
/// Gần đạt (>= 85%) thì thanh nhấp nháy, sao le lói. Đạt thì sao bật sáng: phóng to, chớp sáng,
/// bắn tia lửa, chữ "STAR x!" bay lên, phát âm thanh sao.
/// EndGameBase tự tạo khi vào màn (bật / tắt bằng ô "showStarHud"). Không chặn chạm vào màn hình.
/// </summary>
public class StarProgressHUD : MonoBehaviour
{
    private const float SegW = 190f, SegH = 18f, StarSize = 60f, BlockGap = 272f;
    private const float NearRatio = 0.85f;
    private const int CanvasOrder = 480;   // dưới bảng kết quả (490) và menu Pause (500)

    private static readonly Color StarOff = new Color32(96, 84, 150, 255);
    private static readonly Color CaptionOff = new Color32(200, 192, 230, 255);
    private static readonly Color Track = new Color32(30, 20, 58, 255);

    private class Seg
    {
        public RectTransform root, fill, star;
        public Image fillImg, starImg, flash;
        public TMP_Text label, number;
        public Color color;
        public float shown;           // phần đang hiển thị (chạy mượt tới giá trị thật)
        public bool lit;
        public float litAt = -10f;
    }

    private struct Spark
    {
        public RectTransform rt;
        public Image img;
        public Vector2 vel;
        public float life, maxLife;
    }

    private EndGameBase host;
    private AudioClip starClip;
    private TMP_FontAsset font;
    private RectTransform panel;
    private CanvasGroup group;
    private readonly Seg[] segs = new Seg[3];
    private readonly List<Spark> sparks = new List<Spark>();
    private readonly float[] progress = new float[3];
    private readonly string[] labels = new string[3];
    private float punch;
    private float baseScale = 1f;   // starHudScale trong Inspector

    public static StarProgressHUD Create(EndGameBase host, AudioClip starClip, Vector2 position, float scale = 1f)
    {
        var go = new GameObject("StarProgressHUD (auto)");
        var hud = go.AddComponent<StarProgressHUD>();
        hud.host = host;
        hud.starClip = starClip;
        hud.Build(position, scale);
        return hud;
    }

    // ===============================================================
    // DỰNG GIAO DIỆN
    // ===============================================================
    private void Build(Vector2 position, float scale)
    {
        font = NeonUI.FindGameFont();
        NeonUI.MakeCanvas(gameObject, CanvasOrder);

        // Thanh chỉ để xem: bỏ raycaster để không bao giờ chặn chạm vào nút chơi
        var raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster != null) Destroy(raycaster);

        panel = NeonUI.NewUI("Panel", transform).GetComponent<RectTransform>();
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = position;
        panel.sizeDelta = new Vector2(BlockGap * 2f + 300f, 104f);
        baseScale = Mathf.Max(0.1f, scale);
        panel.localScale = Vector3.one * baseScale;

        group = panel.gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        // Nền: viền tối + lớp tím mờ
        NoRaycast(NeonUI.Img("BgShadow", panel, null, new Color(0f, 0f, 0f, 0.35f), panel.sizeDelta + new Vector2(8f, 8f)))
            .rectTransform.anchoredPosition = new Vector2(5f, -6f);
        NoRaycast(NeonUI.Img("BgEdge", panel, null, NeonUI.Ink, panel.sizeDelta + new Vector2(8f, 8f)));
        NoRaycast(NeonUI.Img("Bg", panel, null, new Color(0.10f, 0.06f, 0.22f, 0.86f), panel.sizeDelta));
        NoRaycast(NeonUI.Img("BgLine", panel, null, new Color(NeonUI.Violet.r, NeonUI.Violet.g, NeonUI.Violet.b, 0.9f),
                             new Vector2(panel.sizeDelta.x, 4f))).rectTransform.anchoredPosition = new Vector2(0f, panel.sizeDelta.y / 2f - 2f);

        Color[] colors = { NeonUI.Pink, NeonUI.Cyan, NeonUI.Mint };
        for (int i = 0; i < 3; i++)
            segs[i] = BuildSeg(i, colors[i]);
    }

    private Seg BuildSeg(int index, Color color)
    {
        var s = new Seg { color = color };

        s.root = NeonUI.NewUI("Star" + (index + 1), panel).GetComponent<RectTransform>();
        s.root.anchoredPosition = new Vector2((index - 1) * BlockGap, 0f);

        const float barX = -StarSize * 0.5f + 4f;
        const float barY = 12f;

        // Rãnh thanh: viền đen + nền tối
        NoRaycast(NeonUI.Img("Edge", s.root, null, NeonUI.Ink, new Vector2(SegW + 8f, SegH + 8f)))
            .rectTransform.anchoredPosition = new Vector2(barX, barY);
        var track = NoRaycast(NeonUI.Img("Track", s.root, null, Track, new Vector2(SegW, SegH)));
        track.rectTransform.anchoredPosition = new Vector2(barX, barY);

        // Vạch chia 4 khúc cho đúng chất pixel
        for (int k = 1; k < 4; k++)
        {
            NoRaycast(NeonUI.Img("Tick", track.rectTransform, null, new Color(0f, 0f, 0f, 0.35f), new Vector2(3f, SegH)))
                .rectTransform.anchoredPosition = new Vector2(-SegW / 2f + SegW * k / 4f, 0f);
        }

        // Phần đã đầy: neo mép trái, kéo dài theo tiến độ
        s.fillImg = NoRaycast(NeonUI.Img("Fill", track.rectTransform, null, color, new Vector2(0f, SegH)));
        s.fill = s.fillImg.rectTransform;
        s.fill.anchorMin = new Vector2(0f, 0f);
        s.fill.anchorMax = new Vector2(0f, 1f);
        s.fill.pivot = new Vector2(0f, 0.5f);
        s.fill.anchoredPosition = Vector2.zero;
        s.fill.sizeDelta = new Vector2(0f, 0f);

        // Vệt sáng nửa trên của phần đầy
        var shine = NoRaycast(NeonUI.Img("Shine", s.fill, null, new Color(1f, 1f, 1f, 0.35f), Vector2.zero));
        shine.rectTransform.anchorMin = new Vector2(0f, 0.55f);
        shine.rectTransform.anchorMax = new Vector2(1f, 0.85f);
        shine.rectTransform.offsetMin = shine.rectTransform.offsetMax = Vector2.zero;

        // Sao ở cuối thanh (đè nhẹ lên đầu thanh)
        Vector2 starPos = new Vector2(barX + SegW / 2f + StarSize * 0.3f, barY);
        s.flash = NoRaycast(NeonUI.Img("Flash", s.root, NeonUI.GlowSprite(), new Color(1f, 0.9f, 0.4f, 0f),
                                       new Vector2(StarSize * 2.6f, StarSize * 2.6f)));
        s.flash.rectTransform.anchoredPosition = starPos;
        s.starImg = NoRaycast(NeonUI.Img("Star", s.root, NeonUI.StarSprite(), StarOff, new Vector2(StarSize, StarSize), sliced: false));
        s.star = s.starImg.rectTransform;
        s.star.anchoredPosition = starPos;
        s.number = NeonUI.Text("Num", s.star, (index + 1).ToString(), 24f, NeonUI.Ink, new Vector2(0f, -3f), new Vector2(StarSize, 30f), font, 0f);

        s.label = NeonUI.Text("Label", s.root, "", 22f, CaptionOff, new Vector2(barX, -24f), new Vector2(SegW + 40f, 30f), font, 0.25f);
        s.label.enableAutoSizing = true;
        s.label.fontSizeMin = 14f;
        s.label.fontSizeMax = 22f;

        return s;
    }

    private static Image NoRaycast(Image img)
    {
        img.raycastTarget = false;
        return img;
    }

    // ===============================================================
    // CẬP NHẬT
    // ===============================================================
    private void Update()
    {
        if (host == null)
        {
            Destroy(gameObject);
            return;
        }

        float dt = Time.unscaledDeltaTime;
        bool ready = host.GetLiveStarProgress(progress, labels);

        // Ẩn khi chưa vào bài hoặc khi bảng kết quả đã hiện
        float targetAlpha = ready && !EndGameBase.ResultShowing ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, targetAlpha, dt * 3f);

        if (ready)
        {
            for (int i = 0; i < 3; i++)
                UpdateSeg(i, segs[i], progress[i], labels[i], dt);
        }

        // Cả thanh nảy nhẹ mỗi khi có sao mới
        punch = Mathf.MoveTowards(punch, 0f, dt * 2.5f);
        float k = 1f + 0.06f * Mathf.Sin(punch * Mathf.PI) * punch;
        panel.localScale = Vector3.one * (baseScale * k);

        UpdateSparks(dt);
    }

    private void UpdateSeg(int index, Seg s, float value, string label, float dt)
    {
        // Màn không theo dõi được sao này -> ẩn ô
        bool tracked = value >= 0f;
        if (s.root.gameObject.activeSelf != tracked)
            s.root.gameObject.SetActive(tracked);
        if (!tracked) return;

        float target = Mathf.Clamp01(value);
        s.shown = Mathf.MoveTowards(s.shown, target, dt * Mathf.Max(0.6f, Mathf.Abs(target - s.shown) * 6f));
        s.fill.sizeDelta = new Vector2(SegW * s.shown, 0f);
        s.label.text = label;

        bool near = !s.lit && target >= NearRatio;
        float t = Time.unscaledTime;

        if (!s.lit && target >= 1f)
            Light(index, s);

        if (s.lit)
        {
            // Đã đạt: thanh vàng, sao vàng đập nhẹ theo nhịp, chữ vàng
            float since = t - s.litAt;
            float pop = since < 0.45f ? NeonUI.BackOut(2.1f, 1f, since / 0.45f) : 1f + 0.06f * NeonUI.Beat(110f);
            s.star.localScale = Vector3.one * pop;
            s.star.localRotation = Quaternion.Euler(0f, 0f, since < 0.45f ? Mathf.Lerp(-25f, 0f, since / 0.45f) : 0f);
            s.starImg.color = NeonUI.Yellow;
            s.fillImg.color = Color.Lerp(NeonUI.Yellow, Color.white, 0.25f * NeonUI.Beat(110f));
            s.label.color = NeonUI.Yellow;

            float flashA = since < 0.6f ? 0.95f * (1f - since / 0.6f) : 0.18f + 0.12f * NeonUI.Beat(110f);
            s.flash.color = new Color(1f, 0.9f, 0.4f, flashA);
            s.flash.rectTransform.localScale = Vector3.one * (since < 0.6f ? Mathf.Lerp(0.6f, 1.4f, since / 0.6f) : 1f);
        }
        else if (near)
        {
            // Sắp đạt: thanh nhấp nháy trắng, sao le lói + rung nhẹ để "nhử" người chơi
            float blink = 0.5f + 0.5f * Mathf.Sin(t * 14f);
            s.fillImg.color = Color.Lerp(s.color, Color.white, 0.45f * blink);
            s.starImg.color = Color.Lerp(StarOff, NeonUI.Yellow, 0.25f + 0.25f * blink);
            s.star.localScale = Vector3.one * (1f + 0.05f * blink);
            s.star.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 5f);
            s.flash.color = new Color(1f, 0.9f, 0.4f, 0.12f * blink);
            s.label.color = Color.Lerp(CaptionOff, Color.white, blink);
        }
        else
        {
            s.fillImg.color = s.color;
            s.starImg.color = StarOff;
            s.star.localScale = Vector3.one;
            s.star.localRotation = Quaternion.identity;
            s.flash.color = new Color(1f, 0.9f, 0.4f, 0f);
            s.label.color = CaptionOff;
        }
    }

    // ===============================================================
    // HIỆU ỨNG ĐẠT SAO
    // ===============================================================
    private void Light(int index, Seg s)
    {
        s.lit = true;
        s.litAt = Time.unscaledTime;
        s.shown = 1f;
        punch = 1f;

        Vector2 starPos = s.root.anchoredPosition + s.star.anchoredPosition;
        SpawnSparks(starPos, s.color);
        StartCoroutine(FloatText("STAR " + (index + 1) + "!", starPos + new Vector2(0f, 36f)));

        if (starClip != null)
            GameplaySfx.Play(starClip, 0.9f);
    }

    private void SpawnSparks(Vector2 pos, Color color)
    {
        const int count = 14;
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.2f, 0.2f);
            float speed = Random.Range(260f, 520f);
            Color c = i % 3 == 0 ? Color.white : (i % 3 == 1 ? NeonUI.Yellow : color);
            float size = Random.Range(7f, 12f);

            var img = NoRaycast(NeonUI.Img("Spark", panel, null, c, new Vector2(size, size)));
            img.rectTransform.anchoredPosition = pos;
            float life = Random.Range(0.45f, 0.75f);
            sparks.Add(new Spark
            {
                rt = img.rectTransform,
                img = img,
                vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * speed + new Vector2(0f, 120f),
                life = life,
                maxLife = life
            });
        }
    }

    private void UpdateSparks(float dt)
    {
        for (int i = sparks.Count - 1; i >= 0; i--)
        {
            Spark p = sparks[i];
            p.life -= dt;

            if (p.life <= 0f || p.rt == null)
            {
                if (p.rt != null) Destroy(p.rt.gameObject);
                sparks.RemoveAt(i);
                continue;
            }

            p.vel.y -= 900f * dt;   // rơi xuống như pháo hoa
            p.rt.anchoredPosition += p.vel * dt;
            float k = p.life / p.maxLife;
            Color c = p.img.color;
            c.a = k;
            p.img.color = c;
            p.rt.localScale = Vector3.one * Mathf.Lerp(0.4f, 1f, k);
            sparks[i] = p;
        }
    }

    private IEnumerator FloatText(string text, Vector2 pos)
    {
        TMP_Text t = NeonUI.Text("StarText", panel, text, 34f, NeonUI.Yellow, pos, new Vector2(240f, 46f), font, 0.3f);
        const float dur = 0.9f;
        float time = 0f;

        while (time < dur && t != null)
        {
            time += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(time / dur);
            t.rectTransform.anchoredPosition = pos + new Vector2(0f, 56f * NeonUI.EaseOut(k));
            t.rectTransform.localScale = Vector3.one * (k < 0.2f ? NeonUI.BackOut(0.4f, 1f, k / 0.2f) : 1f);
            Color c = t.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            t.color = c;
            yield return null;
        }

        if (t != null)
            Destroy(t.gameObject);
    }
}
