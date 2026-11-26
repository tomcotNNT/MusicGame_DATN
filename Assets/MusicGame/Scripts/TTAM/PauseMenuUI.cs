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
    // ---------- bảng màu ----------
    private static readonly Color Ink = new Color32(14, 8, 30, 255);
    private static readonly Color Cyan = new Color32(64, 232, 255, 255);
    private static readonly Color Pink = new Color32(255, 72, 178, 255);
    private static readonly Color Yellow = new Color32(255, 214, 64, 255);
    private static readonly Color Mint = new Color32(72, 236, 160, 255);
    private static readonly Color Violet = new Color32(120, 84, 220, 255);
    private static readonly Color Slate = new Color32(70, 62, 110, 255);

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

    private static Sprite frameSprite, buttonSprite, glowSprite;

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

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        gameObject.AddComponent<GraphicRaycaster>();
        if (FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("[PauseMenu] Scene không có EventSystem - nút sẽ không bấm được.");

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
        img.gameObject.AddComponent<PressBounce>();
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
        handle.gameObject.AddComponent<PressBounce>();

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

    private static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rt, float padX = 0, float padY = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY); rt.offsetMax = new Vector2(-padX, -padY);
    }

    private static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 size)
    {
        var img = NewUI(name, parent).AddComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.25f;     // viền pixel to, đúng phong cách 8-bit
        img.color = color;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    private TMP_Text Text(string name, Transform parent, string s, float size, Color color, Vector2 pos, Vector2 box)
    {
        var t = NewUI(name, parent).AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = s;
        t.fontSize = size;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.raycastTarget = false;
        t.rectTransform.sizeDelta = box;
        t.rectTransform.anchoredPosition = pos;
        return t;
    }

    private static TMP_FontAsset FindGameFont()
    {
        // ưu tiên font pixel đang dùng trên giao diện màn chơi
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.font != null && t.font.name.IndexOf("Pixel", StringComparison.OrdinalIgnoreCase) >= 0) return t.font;
        foreach (var t in FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.font != null) return t.font;
        return TMP_Settings.defaultFontAsset;
    }

    // ---------- sprite vẽ bằng code (viền pixel) ----------
    private static Sprite FrameSprite()
    {
        if (frameSprite != null) return frameSprite;
        const int S = 32;
        var tex = NewTex(S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                bool corner = (x < 2 || x > S - 3) && (y < 2 || y > S - 3);
                Color c;
                if (corner) c = Color.clear;
                else if (d < 2) c = Ink;
                else if (d < 4) c = Cyan;
                else if (d < 5) c = Ink;
                else if (d < 6) c = Pink;
                else c = Color.Lerp(new Color32(30, 18, 64, 250), new Color32(58, 30, 104, 250), y / (float)S);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return frameSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
    }

    private static Sprite ButtonSprite()
    {
        if (buttonSprite != null) return buttonSprite;
        const int S = 16;
        var tex = NewTex(S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                bool corner = (x == 0 || x == S - 1) && (y == 0 || y == S - 1);
                Color c;
                if (corner) c = Color.clear;
                else if (d < 1) c = new Color(0.06f, 0.03f, 0.12f, 1f);     // viền đen
                else if (y >= S - 3) c = Color.white;                       // vệt sáng trên
                else if (y <= 2) c = new Color(0.55f, 0.55f, 0.6f, 1f);     // bóng dưới
                else c = new Color(0.86f, 0.86f, 0.9f, 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return buttonSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
    }

    private static Sprite GlowSprite()
    {
        if (glowSprite != null) return glowSprite;
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = Mathf.Max(0, Mathf.Abs(x - S / 2f + 0.5f) - 8) / (S / 2f - 8);
                float dy = Mathf.Max(0, Mathf.Abs(y - S / 2f + 0.5f) - 8) / (S / 2f - 8);
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
            }
        tex.Apply();
        return glowSprite = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
    }

    private static Texture2D NewTex(int s) =>
        new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

    private static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

    private static float BackOut(float from, float to, float k)
    {
        const float s = 1.9f;
        float x = k - 1f;
        return Mathf.LerpUnclamped(from, to, x * x * ((s + 1f) * x + s) + 1f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { frameSprite = buttonSprite = glowSprite = null; }

    // ---------- nút nảy khi bấm ----------
    private class PressBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
    {
        private float target = 1f, current = 1f;
        public void OnPointerDown(PointerEventData e) => target = 0.92f;
        public void OnPointerUp(PointerEventData e) => target = 1.06f;
        public void OnPointerEnter(PointerEventData e) => target = 1.06f;
        public void OnPointerExit(PointerEventData e) => target = 1f;
        private void Update()
        {
            current = Mathf.Lerp(current, target, 1f - Mathf.Exp(-20f * Time.unscaledDeltaTime));
            transform.localScale = new Vector3(current, current, 1f);
        }
    }
}
