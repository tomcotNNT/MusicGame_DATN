using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Bộ "phong cách neon pixel" dùng chung cho các panel dựng bằng code (Pause, Kết quả...):
/// bảng màu, sprite viền pixel vẽ bằng code, và hàm tạo Text / Button / Image cho nhanh.
/// Đổi màu ở đây thì mọi panel đổi theo.
/// </summary>
public static class NeonUI
{
    // ---------- bảng màu ----------
    public static readonly Color Ink = new Color32(14, 8, 30, 255);
    public static readonly Color Cyan = new Color32(64, 232, 255, 255);
    public static readonly Color Pink = new Color32(255, 72, 178, 255);
    public static readonly Color Yellow = new Color32(255, 214, 64, 255);
    public static readonly Color Mint = new Color32(72, 236, 160, 255);
    public static readonly Color Violet = new Color32(120, 84, 220, 255);
    public static readonly Color Slate = new Color32(70, 62, 110, 255);
    public static readonly Color Red = new Color32(255, 84, 96, 255);
    public static readonly Color Dim = new Color(0.04f, 0.02f, 0.10f, 0.82f);

    private static Sprite frame, button, glow, star;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { frame = button = glow = star = null; }

    // ===============================================================
    // TẠO PHẦN TỬ
    // ===============================================================
    /// <summary>Canvas phủ màn hình, 1920x1080, đứng trên mọi UI khác.</summary>
    public static void MakeCanvas(GameObject go, int order)
    {
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;
        go.AddComponent<GraphicRaycaster>();
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            Debug.LogWarning("[NeonUI] Scene không có EventSystem - nút sẽ không bấm được.");
    }

    public static GameObject NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    public static void Stretch(RectTransform rt, float padX = 0, float padY = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(padX, padY); rt.offsetMax = new Vector2(-padX, -padY);
    }

    public static Image Img(string name, Transform parent, Sprite sprite, Color color, Vector2 size, bool sliced = true)
    {
        var img = NewUI(name, parent).AddComponent<Image>();
        img.sprite = sprite;
        img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
        img.pixelsPerUnitMultiplier = 0.25f;     // viền pixel to, đúng phong cách 8-bit
        img.color = color;
        img.rectTransform.sizeDelta = size;
        return img;
    }

    public static TMP_Text Text(string name, Transform parent, string s, float size, Color color, Vector2 pos, Vector2 box, TMP_FontAsset font, float outline = 0.2f)
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
        if (outline > 0f) { t.outlineWidth = outline; t.outlineColor = Ink; }
        return t;
    }

    public static RectTransform MakeButton(Transform parent, string label, Color color, Vector2 pos, Vector2 size, float fontSize, Action onClick, TMP_FontAsset font)
    {
        var img = Img(string.IsNullOrEmpty(label) ? "Button" : label, parent, ButtonSprite(), color, size);
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
        Text("Label", rt, label, fontSize, Color.white, new Vector2(0, 3), size, font);
        return rt;
    }

    public static TMP_FontAsset FindGameFont()
    {
        // Font chữ béo của HUD (chữ SCORE) dễ đọc nhất trên điện thoại -> ưu tiên theo thứ tự tên
        string[] prefer = { "HollyBerry", "Impact", "Pixel" };
        var fonts = new System.Collections.Generic.List<TMP_FontAsset>();
        foreach (var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.font != null && !fonts.Contains(t.font)) fonts.Add(t.font);
        foreach (var f in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            if (f != null && !fonts.Contains(f)) fonts.Add(f);

        foreach (string key in prefer)
            foreach (var f in fonts)
                if (f.name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return f;
        foreach (var f in fonts)
            if (f.name.IndexOf("Liberation", StringComparison.OrdinalIgnoreCase) < 0) return f;
        return TMP_Settings.defaultFontAsset;
    }

    // ===============================================================
    // SPRITE VẼ BẰNG CODE
    // ===============================================================
    /// <summary>Thẻ: viền đen - xanh neon - đen - hồng, nền tím đậm chuyển sắc.</summary>
    public static Sprite FrameSprite()
    {
        if (frame != null) return frame;
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
        return frame = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(8, 8, 8, 8));
    }

    /// <summary>Nút xám có vát sáng trên / bóng dưới, tô màu bằng Image.color.</summary>
    public static Sprite ButtonSprite()
    {
        if (button != null) return button;
        const int S = 16;
        var tex = NewTex(S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(S - 1 - x, S - 1 - y));
                bool corner = (x == 0 || x == S - 1) && (y == 0 || y == S - 1);
                Color c;
                if (corner) c = Color.clear;
                else if (d < 1) c = new Color(0.06f, 0.03f, 0.12f, 1f);
                else if (y >= S - 3) c = Color.white;
                else if (y <= 2) c = new Color(0.55f, 0.55f, 0.6f, 1f);
                else c = new Color(0.86f, 0.86f, 0.9f, 1f);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return button = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
    }

    /// <summary>Quầng sáng mềm (tô màu bằng Image.color).</summary>
    public static Sprite GlowSprite()
    {
        if (glow != null) return glow;
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
        return glow = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
    }

    /// <summary>Ngôi sao 5 cánh pixel 24x24: viền đen, thân trắng (tô màu), vệt sáng góc trên.</summary>
    public static Sprite StarSprite()
    {
        if (star != null) return star;
        const int S = 24;
        var pts = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
            float r = i % 2 == 0 ? 11.2f : 4.9f;
            pts[i] = new Vector2(S / 2f + Mathf.Cos(a) * r, S / 2f - 0.6f + Mathf.Sin(a) * r);
        }
        bool In(float x, float y)
        {
            bool c = false;
            for (int i = 0, j = 9; i < 10; j = i++)
                if ((pts[i].y > y) != (pts[j].y > y) &&
                    x < (pts[j].x - pts[i].x) * (y - pts[i].y) / (pts[j].y - pts[i].y) + pts[i].x) c = !c;
            return c;
        }
        var tex = NewTex(S);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                Color c = Color.clear;
                if (In(x + 0.5f, y + 0.5f))
                    c = (x + y < S + 2 && y > S / 2) ? Color.white : new Color(0.85f, 0.85f, 0.85f, 1f);
                else
                    for (int k = 0; k < 4 && c.a == 0; k++)
                    {
                        int ox = k == 0 ? 1 : k == 1 ? -1 : 0, oy = k == 2 ? 1 : k == 3 ? -1 : 0;
                        if (In(x + 0.5f + ox, y + 0.5f + oy)) c = Ink;
                    }
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return star = Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100);
    }

    private static Texture2D NewTex(int s) =>
        new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };

    // ===============================================================
    // EASING
    // ===============================================================
    public static float EaseOut(float k) => 1f - (1f - k) * (1f - k);

    public static float BackOut(float from, float to, float k)
    {
        const float s = 1.9f;
        float x = k - 1f;
        return Mathf.LerpUnclamped(from, to, x * x * ((s + 1f) * x + s) + 1f);
    }

    /// <summary>0..1, đập mạnh đầu mỗi phách (mặc định 120 BPM), theo thời gian thật.</summary>
    public static float Beat(float bpm = 120f) => Mathf.Pow(1f - Mathf.Repeat(Time.unscaledTime * bpm / 60f, 1f), 3f);
}

/// <summary>Nút nảy khi rê / bấm (chạy cả khi game đang dừng).</summary>
public class NeonPressBounce : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
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
