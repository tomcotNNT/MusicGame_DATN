using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gắn vào 1 object trong scene LoadingScene (scene đầu tiên khi mở game).
/// Hiện màn Loading khoảng 2-3 giây (ngẫu nhiên trong khoảng min-max),
/// vừa chạy thanh tiến trình vừa nạp ngầm scene MainMenu, đủ thời gian thì chuyển.
/// </summary>
public class SplashLoading : MonoBehaviour
{
    [Header("Scene tiếp theo")]
    [Tooltip("Tên scene Main Menu (phải có trong Build Settings)")]
    [SerializeField] private string nextSceneName = "MainMenu";

    [Header("Thời gian Loading (giây)")]
    [SerializeField, Min(0f)] private float minDuration = 2f;
    [SerializeField, Min(0f)] private float maxDuration = 3f;

    [Header("UI (tùy chọn)")]
    [Tooltip("Slider thanh loading")]
    [SerializeField] private Slider progressBar;

    [Tooltip("Hoặc Image có Image Type = Filled")]
    [SerializeField] private Image progressFill;

    [Tooltip("Text phần trăm, VD: 75%")]
    [SerializeField] private TMP_Text percentText;

    [Header("Fade out (tùy chọn)")]
    [Tooltip("CanvasGroup của Canvas Loading để mờ dần trước khi chuyển")]
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField, Min(0f)] private float fadeDuration = 0.3f;

    private IEnumerator Start()
    {
        // Phòng khi scene trước để timeScale = 0
        Time.timeScale = 1f;

        if (string.IsNullOrEmpty(nextSceneName) || !Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"SplashLoading: không load được scene '{nextSceneName}'. Kiểm tra tên và Build Settings.");
            yield break;
        }

        float duration = Random.Range(minDuration, Mathf.Max(minDuration, maxDuration));

        // Nạp ngầm MainMenu, chưa cho chuyển ngay
        AsyncOperation op = SceneManager.LoadSceneAsync(nextSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;
        SetProgress(0f);

        // progress của Unity dừng ở 0.9 khi allowSceneActivation = false
        while (timer < duration || op.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            float timeRatio = Mathf.Clamp01(timer / Mathf.Max(0.01f, duration));
            float loadRatio = Mathf.Clamp01(op.progress / 0.9f);

            // Thanh chạy theo thời gian nhưng không vượt tiến độ nạp thật
            SetProgress(Mathf.Min(timeRatio, loadRatio));
            yield return null;
        }

        SetProgress(1f);

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

        op.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        if (progressBar != null)
            progressBar.value = value;

        if (progressFill != null)
            progressFill.fillAmount = value;

        if (percentText != null)
            percentText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}
