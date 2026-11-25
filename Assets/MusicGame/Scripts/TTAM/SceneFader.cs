using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Chuyển scene mượt: màn hình tối dần -> load scene -> sáng dần.
///   SceneFader.Load("SelectSong");
/// Tự tạo khi cần, sống qua mọi scene, chạy theo thời gian thật (không bị ảnh hưởng khi pause).
/// Scene nào load bằng SceneManager trực tiếp vẫn được làm sáng dần khi vào.
///
/// An toàn với "Enter Play Mode Options" (tắt Domain Reload): biến static được reset mỗi lần Play,
/// sự kiện sceneLoaded dùng hàm static nên không bao giờ gọi vào object đã bị huỷ,
/// và có cơ chế tự gỡ lớp đen nếu bị kẹt.
/// </summary>
public class SceneFader : MonoBehaviour
{
    private const float FadeOutTime = 0.35f;
    private const float FadeInTime = 0.35f;
    private const float StuckTimeout = 2f;   // lớp đen đứng yên quá lâu khi không chuyển scene -> tự gỡ

    private static SceneFader instance;
    private static bool hooked;

    private CanvasGroup group;
    private bool loading;
    private float opaqueSince = -1f;

    // Chạy đầu mỗi lần Play (kể cả khi tắt Domain Reload): xoá trạng thái static của lần Play trước
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        if (hooked)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            hooked = false;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        EnsureInstance();
    }

    private static SceneFader EnsureInstance()
    {
        if (!hooked)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            hooked = true;
        }

        if (instance != null) return instance;

        GameObject root = new GameObject("SceneFader (auto)");
        DontDestroyOnLoad(root);

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;   // trên mọi UI

        CanvasGroup g = root.AddComponent<CanvasGroup>();
        g.alpha = 0f;
        g.blocksRaycasts = false;      // chỉ chặn bấm khi đang chuyển
        g.interactable = false;

        GameObject black = new GameObject("Black", typeof(RectTransform));
        black.transform.SetParent(root.transform, false);
        RectTransform rt = (RectTransform)black.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image img = black.AddComponent<Image>();
        img.color = Color.black;
        img.raycastTarget = true;

        instance = root.AddComponent<SceneFader>();
        instance.group = g;
        return instance;
    }

    /// <summary>Đang tối dần để chuyển scene (dùng để chặn bấm đúp trừ lượt 2 lần).</summary>
    public static bool IsLoading => instance != null && instance.loading;

    /// <summary>Tối dần rồi chuyển sang scene. Đang chuyển thì bỏ qua lệnh mới (tránh bấm đúp).</summary>
    public static void Load(string sceneName)
    {
        SceneFader f = EnsureInstance();
        if (f.loading) return;

        f.StartCoroutine(f.LoadRoutine(sceneName));
    }

    // Hàm static: không giữ tham chiếu tới object cũ -> không bao giờ gọi vào SceneFader đã bị huỷ
    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return;

        SceneFader f = EnsureInstance();
        f.StopAllCoroutines();
        f.StartCoroutine(f.FadeInRoutine());
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        loading = true;
        group.blocksRaycasts = true;

        yield return Fade(group.alpha, 1f, FadeOutTime);

        SceneManager.LoadScene(sceneName);
        // Sáng dần trong HandleSceneLoaded
    }

    private IEnumerator FadeInRoutine()
    {
        loading = false;

        // Scene trước có thể đang pause / Game Over -> scene mới luôn chạy bình thường
        Time.timeScale = 1f;
        AudioListener.pause = false;
        group.blocksRaycasts = true;

        // Scene load thẳng (không qua Load) cũng có hiệu ứng sáng dần
        if (group.alpha < 1f) group.alpha = 1f;

        yield return null;   // chờ scene vẽ khung hình đầu
        yield return Fade(1f, 0f, FadeInTime);

        group.blocksRaycasts = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(t / duration));
            yield return null;
        }
        group.alpha = to;
    }

    // Lưới an toàn: còn lớp đen mà không có hiệu ứng nào đang chạy quá StuckTimeout giây -> gỡ ngay
    private void Update()
    {
        if (group == null) return;

        // Sáng dần chỉ mất ~0,4 s -> còn đen quá StuckTimeout khi không chuyển scene là bị kẹt
        bool stuck = !loading && group.alpha > 0.01f;
        if (!stuck)
        {
            opaqueSince = -1f;
            return;
        }

        if (opaqueSince < 0f) opaqueSince = Time.unscaledTime;

        if (Time.unscaledTime - opaqueSince > StuckTimeout)
        {
            Debug.LogWarning("[SceneFader] Lớp chuyển cảnh bị kẹt, tự gỡ.");
            group.alpha = 0f;
            group.blocksRaycasts = false;
            opaqueSince = -1f;
        }
    }

}
