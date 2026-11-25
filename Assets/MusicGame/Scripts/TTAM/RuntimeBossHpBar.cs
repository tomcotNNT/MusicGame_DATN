using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Thanh máu boss tạo bằng code (dùng khi prefab boss chưa có sẵn thanh máu).
/// Nằm trên đầu màn hình, giảm dần mượt khi boss mất máu.
/// </summary>
public class RuntimeBossHpBar : MonoBehaviour
{
    private RectTransform fillRect;
    private TMP_Text valueText;
    private float targetFill = 1f;
    private float shownFill = 1f;

    public static RuntimeBossHpBar Create(string label)
    {
        // Canvas riêng, vẽ trên cùng
        GameObject root = new GameObject("BossHpBar (auto)", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Khung
        RectTransform frame = NewRect("Frame", root.transform);
        frame.anchorMin = frame.anchorMax = new Vector2(0.5f, 1f);
        frame.pivot = new Vector2(0.5f, 1f);
        frame.anchoredPosition = new Vector2(0f, -24f);
        frame.sizeDelta = new Vector2(720f, 34f);
        frame.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        // Phần máu (co theo anchorMax.x, không cần sprite)
        RectTransform fill = NewRect("Fill", frame);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = new Vector2(4f, 4f);
        fill.offsetMax = new Vector2(-4f, -4f);
        fill.gameObject.AddComponent<Image>().color = new Color(0.86f, 0.16f, 0.2f, 1f);

        RuntimeBossHpBar bar = root.AddComponent<RuntimeBossHpBar>();
        bar.fillRect = fill;

        // Chữ "BOSS 120/120"
        RectTransform textRect = NewRect("Label", frame);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 22;
        text.color = Color.white;
        text.text = label;
        bar.valueText = text;
        bar.labelPrefix = label;

        return bar;
    }

    private string labelPrefix = "BOSS";

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    public void SetFill(float fill, int current, int max)
    {
        targetFill = Mathf.Clamp01(fill);

        if (valueText != null)
            valueText.text = labelPrefix + "  " + current + "/" + max;
    }

    private bool fadingOut;

    /// <summary>Boss bị hạ: chạy nốt về 0 rồi mờ dần và tự huỷ.</summary>
    public void FadeOutAndDestroy()
    {
        if (fadingOut) return;
        fadingOut = true;
        targetFill = 0f;
        StartCoroutine(FadeRoutine());
    }

    private System.Collections.IEnumerator FadeRoutine()
    {
        CanvasGroup g = gameObject.AddComponent<CanvasGroup>();
        yield return new WaitForSecondsRealtime(0.6f);

        float t = 0f;
        while (t < 0.4f)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = 1f - t / 0.4f;
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (fillRect == null) return;

        shownFill = Mathf.MoveTowards(shownFill, targetFill, Time.unscaledDeltaTime * 1.5f);

        Vector2 max = fillRect.anchorMax;
        max.x = shownFill;
        fillRect.anchorMax = max;
    }
}
