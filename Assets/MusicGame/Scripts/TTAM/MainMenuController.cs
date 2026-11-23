using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gắn vào 1 object trong scene MainMenu.
/// Chạm / click bất kỳ đâu trên màn hình (Tap to start) -> sang scene Select Char.
/// Chạm vào nút UI khác (Setting, Thoát...) thì KHÔNG chuyển scene.
/// Cũng có thể gán hàm StartGame() vào OnClick của một Button "Play".
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Scene tiếp theo")]
    [Tooltip("Tên scene chọn nhân vật (phải có trong Build Settings)")]
    [SerializeField] private string nextSceneName = "SelectChar";

    [Header("Tap to start")]
    [Tooltip("Bật: chạm bất kỳ đâu để bắt đầu. Tắt: chỉ bắt đầu khi bấm nút gọi StartGame()")]
    [SerializeField] private bool tapAnywhere = true;

    [Tooltip("Chờ bao lâu sau khi vào MainMenu mới nhận chạm (tránh chạm nhầm)")]
    [SerializeField, Min(0f)] private float inputDelay = 0.5f;

    [Header("Hiệu ứng (tùy chọn)")]
    [Tooltip("Âm thanh khi bấm bắt đầu")]
    [SerializeField] private AudioSource clickSound;

    [Tooltip("CanvasGroup để mờ dần trước khi chuyển")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

    private float enterTime;
    private bool isLoading;

    private void Start()
    {
        Time.timeScale = 1f;
        enterTime = Time.unscaledTime;
    }

    private void Update()
    {
        if (!tapAnywhere || isLoading) return;
        if (Time.unscaledTime - enterTime < inputDelay) return;

        if (!TryGetTap(out int pointerId)) return;

        // Chạm trúng nút UI (Setting, Exit...) thì để nút đó tự xử lý
        if (IsPointerOverButton(pointerId)) return;

        StartGame();
    }

    /// <summary>Gán vào OnClick của nút Play (nếu có).</summary>
    public void StartGame()
    {
        if (isLoading) return;
        isLoading = true;

        if (clickSound != null) clickSound.Play();

        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName) || !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"MainMenuController: không load được scene '{nextSceneName}'. Kiểm tra tên và Build Settings.");
            isLoading = false;
            yield break;
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        // Fade out trong lúc scene mới nạp ngầm
        if (fadeGroup != null && fadeDuration > 0f)
        {
            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                fadeGroup.alpha = 1f - Mathf.Clamp01(t / fadeDuration);
                yield return null;
            }
        }

        while (op.progress < 0.9f)
            yield return null;

        op.allowSceneActivation = true;
    }

    // ---------- Input ----------

    private static bool TryGetTap(out int pointerId)
    {
        pointerId = -1; // -1 = chuột trái

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                pointerId = touch.fingerId;
                return true;
            }
            return false;
        }

        return Input.GetMouseButtonDown(0);
    }

    private static bool IsPointerOverButton(int pointerId)
    {
        EventSystem es = EventSystem.current;
        if (es == null) return false;

        Vector2 pos = pointerId >= 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

        var data = new PointerEventData(es) { position = pos };
        var results = new System.Collections.Generic.List<RaycastResult>();
        es.RaycastAll(data, results);

        foreach (RaycastResult r in results)
        {
            if (r.gameObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }
}
