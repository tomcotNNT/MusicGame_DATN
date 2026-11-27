using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menu tạm dừng trong màn chơi, dựng hoàn toàn bằng code (không cần prefab):
///   RESUME (đếm ngược 3-2-1 rồi chơi tiếp, khớp nhịp), REPLAY, HOME,
///   thanh MUSIC / SOUND (lưu lại, có hiệu lực ngay), nút EFFECTS (hiệu ứng chạm).
/// Màu neon tím - hồng - xanh theo phong cách pixel của game, có hiệu ứng bật lên, nhịp đập theo beat.
/// Scenes (Scene.cs) tạo và điều khiển menu này; mọi hiệu ứng chạy theo thời gian thật (game đang dừng).
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    private static readonly Color Ink = NeonUI.Ink, Cyan = NeonUI.Cyan, Pink = NeonUI.Pink, Yellow = NeonUI.Yellow,
                                  Mint = NeonUI.Mint, Violet = NeonUI.Violet, Slate = NeonUI.Slate;

    public Action OnResume, OnReplay, OnHome;

    public bool IsVisible => root != null && root.activeSelf;

    private GameObject root;
    private CanvasGroup rootGroup;
    private RectTransform card, resumeBtn, glow, resumeLabel;
    private TMP_Text title, countdownText;
    private TMP_FontAsset font;
    private readonly List<(CanvasGroup g, RectTransform rt, Vector2 pos)> items = new List<(CanvasGroup, RectTransform, Vector2)>();
    private Coroutine anim;
    private TMP_Text effectsLabel;
    private Image effectsImage;


    // ===============================================================
    public static PauseMenuUI Create()
    {
        var go = new GameObject("PauseMenu (auto)");
        var ui = go.AddComponent<PauseMenuUI>();
        ui.Build();
        return ui;
    }

    private void Build()
    {
        font = FindGameFont();

        NeonUI.MakeCanvas(gameObject, 500);

        // nền tối toàn màn hình (chặn bấm xuyên xuống game)
        root = NewUI("Root", transform);
        Stretch(root.GetComponent<RectTransform>());
        rootGroup = root.AddComponent<CanvasGroup>();
        var dim = root.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.02f, 0.10f, 0.82f);

        // quầng sáng sau thẻ
        glow = Img("Glow", root.transform, GlowSprite(), new Color(Pink.r, Pink.g, Pink.b, 0.35f), new Vector2(1060, 800)).rectTransform;

        // bóng đổ + thẻ chính
        Img("Shadow", root.transform, FrameSprite(), new Color(0, 0, 0, 0.55f), new Vector2(900, 660)).rectTransform.anchoredPosition = new Vector2(14, -16);
        card = Img("Card", root.transform, FrameSprite(), Color.white, new Vector2(900, 660)).rectTransform;

        // tiêu đề
        title = Text("Title", card, "PAUSED", 104, Yellow, new Vector2(0, 238), new Vector2(800, 130));
        title.outlineWidth = 0.22f; title.outlineColor = Ink;
        int lv = LevelFlow.CurrentLevelId;
        Track(Text("Stage", card, lv > 0 ? "STAGE " + lv : "BEAT RUN", 34, Cyan, new Vector2(0, 168), new Vector2(600, 50)).rectTransform);

        // nút chính
        resumeBtn = MakeButton(card, "RESUME", Mint, new Vector2(0, 80), new Vector2(560, 112), 60, () => OnResume?.Invoke());
        resumeLabel = resumeBtn.GetComponentInChildren<TMP_Text>().rectTransform;

        // âm lượng + hiệu ứng
        MakeSlider(card, "MUSIC", new Vector2(0, -28), AudioVolume.Music, v => AudioVolume.Music = v, Cyan);
        MakeSlider(card, "SOUND", new Vector2(0, -100), AudioVolume.Sfx, v => AudioVolume.Sfx = v, Pink);
        var fx = MakeButton(card, "", Slate, new Vector2(0, -172), new Vector2(560, 64), 34, ToggleEffects);
        effectsLabel = fx.GetComponentInChildren<TMP_Text>();
        effectsImage = fx.GetComponent<Image>();
        RefreshEffects();

        // hàng dưới
        MakeButton(card, "REPLAY", Pink, new Vector2(-150, -258), new Vector2(270, 84), 40, () => OnReplay?.Invoke());
        MakeButton(card, "HOME", Violet, new Vector2(150, -258), new Vector2(270, 84), 40, () => OnHome?.Invoke());

        // chữ đếm ngược (nằm ngoài Root để vẫn hiện khi menu đã đóng)
        var cd = NewUI("Countdown", transform);
        Stretch(cd.GetComponent<RectTransform>());
        countdownText = Text("Text", cd.transform, "", 260, Yellow, Vector2.zero, new Vector2(900, 400));
        countdownText.outlineWidth = 0.25f; countdownText.outlineColor = Ink;
        countdownText.raycastTarget = false;
        cd.SetActive(false);

        root.SetActive(false);
    }

    // ===============================================================
    // HIỆN / ẨN
    // ===============================================================
    public void Show()
    {
        root.SetActive(true);
        countdownText.transform.parent.gameObject.SetActive(false);
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(ShowRoutine());
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(resumeBtn.gameObject);
    }

    public void Hide(bool instant = false)
    {
        if (!root.activeSelf) return;
        AudioVolume.Save();
        if (anim != null) StopCoroutine(anim);
        if (instant || !isActiveAndEnabled) { root.SetActive(false); return; }
        anim = StartCoroutine(HideRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        rootGroup.alpha = 0f;
        foreach (var it in items) { it.g.alpha = 0f; it.rt.anchoredPosition = it.pos + new Vector2(0, -24); }
        float t = 0f;
        while (t < 0.55f)
        {
            t += Time.unscaledDeltaTime;
            rootGroup.alpha = Mathf.Clamp01(t / 0.18f);
            float k = Mathf.Clamp01(t / 0.3f);
            card.localScale = Vector3.one * BackOut(0.82f, 1f, k);
            for (int i = 0; i < items.Count; i++)
            {
                float e = Mathf.Clamp01((t - 0.08f - i * 0.035f) / 0.2f);
                items[i].g.alpha = e;
                items[i].rt.anchoredPosition = items[i].pos + new Vector2(0, -24 * (1 - EaseOut(e)));
            }
            yield return null;
        }
        rootGroup.alpha = 1f; card.localScale = Vector3.one;
        foreach (var it in items) { it.g.alpha = 1f; it.rt.anchoredPosition = it.pos; }
        anim = null;
    }

    private IEnumerator HideRoutine()
    {
        float t = 0f;
        while (t < 0.16f)
        {
            t += Time.unscaledDeltaTime;
            float k = t / 0.16f;
            rootGroup.alpha = 1f - k;
            card.localScale = Vector3.one * Mathf.Lerp(1f, 0.9f, k);
            yield return null;
        }
        root.SetActive(false);
        anim = null;
    }

    /// <summary>Đếm ngược 3-2-1 (thời gian thật). Gọi onDone khi tới GO.</summary>
    public IEnumerator Countdown(float stepSeconds, Action onDone)
    {
        var holder = countdownText.transform.parent.gameObject;
        holder.SetActive(true);
        Color[] colors = { Cyan, Pink, Yellow };
        for (int n = 3; n >= 1; n--)
            yield return Pop(n.ToString(), colors[3 - n], stepSeconds);
        onDone?.Invoke();
        yield return Pop("GO!", Mint, 0.45f);
        holder.SetActive(false);
    }

    private IEnumerator Pop(string s, Color c, float dur)
    {
        countdownText.text = s;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            countdownText.transform.localScale = Vector3.one * Mathf.Lerp(1.7f, 1f, EaseOut(Mathf.Clamp01(k * 2.5f)));
            countdownText.color = new Color(c.r, c.g, c.b, k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f);
            yield return null;
        }
    }

    public void CancelCountdown() => countdownText.transform.parent.gameObject.SetActive(false);

    // nhịp đập theo beat (120 BPM) khi menu đang mở
    private void Update()
    {
        if (!root.activeSelf) return;
        float t = Time.unscaledTime;
        float beat = Mathf.Pow(1f - Mathf.Repeat(t * 2f, 1f), 3f);      // đập mạnh đầu mỗi phách
        if (resumeLabel != null) resumeLabel.localScale = Vector3.one * (1f + 0.08f * beat);
        glow.localScale = Vector3.one * (1f + 0.025f * beat);
        var gi = glow.GetComponent<Image>();
        Color gc = Color.Lerp(Pink, Cyan, 0.5f + 0.5f * Mathf.Sin(t * 1.3f));
        gi.color = new Color(gc.r, gc.g, gc.b, 0.25f + 0.15f * beat);
        title.color = Color.Lerp(Yellow, Color.white, beat * 0.35f);
    }

    // ===============================================================
    // HIỆU ỨNG CHẠM (TouchVFX)
    // ===============================================================
    private void ToggleEffects()
    {
        AudioVolume.EffectsOn = !AudioVolume.EffectsOn;
        RefreshEffects();
    }

    private void RefreshEffects()
    {
        bool on = AudioVolume.EffectsOn;
        effectsLabel.text = on ? "TOUCH EFFECTS  ON" : "TOUCH EFFECTS  OFF";
        effectsImage.color = on ? Violet : Slate;
        effectsLabel.color = on ? Color.white : new Color(1, 1, 1, 0.55f);
    }

    // ===============================================================
    // DỰNG UI
    // ===============================================================
    private RectTransform MakeButton(RectTransform parent, string label, Color color, Vector2 pos, Vector2 size, float fontSize, Action onClick)
    {
        var img = Img(label.Length > 0 ? label : "Button", parent, ButtonSprite(), color, size);
        var rt = img.rectTransform;
        rt.anchoredPosition = pos;
        var btn = img.gameObject.AddComponent<Button>();
        var cb = btn.colors;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        cb.selectedColor = Color.white;
        cb.fadeDuration = 0.05f;
        btn.colors = cb;
        btn.onClick.AddListener(() => onClick?.Invoke());
        img.gameObject.AddComponent<NeonPressBounce>();
        var t = Text("Label", rt, label, fontSize, Color.white, new Vector2(0, 3), size);
        t.outlineWidth = 0.2f; t.outlineColor = Ink;
        Track(rt);
        return rt;
    }

    private void MakeSlider(RectTransform parent, string label, Vector2 pos, float value, Action<float> onChange, Color accent)
    {
        var row = NewUI(label + "Row", parent).GetComponent<RectTransform>();
        row.sizeDelta = new Vector2(700, 60);
        row.anchoredPosition = pos;
        Track(row);

        var l = Text("Label", row, label, 40, Color.white, new Vector2(-255, 0), new Vector2(200, 60));
        l.alignment = TextAlignmentOptions.MidlineLeft;
        l.outlineWidth = 0.2f; l.outlineColor = Ink;

        var bg = Img("Track", row, ButtonSprite(), Ink, new Vector2(380, 30));
        bg.rectTransform.anchoredPosition = new Vector2(60, 0);

        var fillArea = NewUI("Fill Area", bg.rectTransform).GetComponent<RectTransform>();
        Stretch(fillArea, 5, 5);
        var fill = Img("Fill", fillArea, ButtonSprite(), accent, Vector2.zero);
        Stretch(fill.rectTransform);

        var slideArea = NewUI("Handle Area", bg.rectTransform).GetComponent<RectTransform>();
        Stretch(slideArea, 14, 0);
        var handle = Img("Handle", slideArea, ButtonSprite(), Yellow, new Vector2(30, 52));
        handle.gameObject.AddComponent<NeonPressBounce>();

        var pct = Text("Value", row, "", 32, accent, new Vector2(300, 0), new Vector2(110, 60));
        pct.alignment = TextAlignmentOptions.MidlineRight;
        pct.outlineWidth = 0.2f; pct.outlineColor = Ink;

        var s = bg.gameObject.AddComponent<Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = Slider.Direction.LeftToRight;
        s.minValue = 0f; s.maxValue = 1f;
        s.SetValueWithoutNotify(value);
        pct.text = Mathf.RoundToInt(value * 100) + "%";
        s.onValueChanged.AddListener(v =>
        {
            onChange(v);
            pct.text = Mathf.RoundToInt(v * 100) + "%";
        });
    }

    private void Track(RectTransform rt)
    {
        if (!rt.TryGetComponent(out CanvasGroup g)) g = rt.gameObject.AddComponent<CanvasGroup>();
        items.Add((g, rt, rt.anchoredPosition));
    }

    private static GameObject NewUI(string name, Transform parent) => NeonUI.NewUI(name, parent);
    private static void Stretch(RectTransform rt, float padX = 0, float padY = 0) => NeonUI.Stretch(rt, padX, padY);
    private static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 size) => NeonUI.Img(name, parent, sprite, color, size);
    private TMP_Text Text(string name, Transform parent, string s, float size, Color color, Vector2 pos, Vector2 box) =>
        NeonUI.Text(name, parent, s, size, color, pos, box, font, 0f);
    private static TMP_FontAsset FindGameFont() => NeonUI.FindGameFont();
    private static Sprite FrameSprite() => NeonUI.FrameSprite();
    private static Sprite ButtonSprite() => NeonUI.ButtonSprite();
    private static Sprite GlowSprite() => NeonUI.GlowSprite();
    private static float EaseOut(float k) => NeonUI.EaseOut(k);
    private static float BackOut(float from, float to, float k) => NeonUI.BackOut(from, to, k);
}
