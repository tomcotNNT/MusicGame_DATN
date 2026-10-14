using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gắn vào 1 object trong scene Loading.
/// Nạp SceneLoader.TargetScene ở chế độ async, hiện thanh tiến trình,
/// và đảm bảo màn Loading hiện tối thiểu minLoadingTime giây (cho khỏi nháy).
/// </summary>
public class LoadingScreen : MonoBehaviour
{
    [Header("UI (tuỳ chọn)")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private TMP_Text percentText;

    [Header("Timing")]
    [Tooltip("Thời gian hiện Loading tối thiểu (giây)")]
    [SerializeField] private float minLoadingTime = 1.5f;

    [Tooltip("Tốc độ thanh tiến trình chạy (1 = đầy trong 1 giây)")]
    [SerializeField] private float barSpeed = 1.2f;

    [Tooltip("Scene dự phòng nếu mở thẳng scene Loading trong Editor")]
    [SerializeField] private string fallbackScene = "";

    private IEnumerator Start()
    {
        string target = SceneLoader.TargetScene;

        if (string.IsNullOrEmpty(target))
            target = fallbackScene;

        if (string.IsNullOrEmpty(target))
        {
            Debug.LogWarning("LoadingScreen: chưa có scene đích! Hãy vào bằng SceneLoader.Load(...)");
            yield break;
        }

        SetProgress(0f);

        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;   // chờ đủ thời gian rồi mới chuyển

        float displayed = 0f;
        float timer = 0f;

        while (true)
        {
            // Dùng unscaled để vẫn chạy nếu scene trước để timeScale = 0
            timer += Time.unscaledDeltaTime;

            // progress dừng ở 0.9 khi allowSceneActivation = false
            float real = Mathf.Clamp01(op.progress / 0.9f);

            // Giữ thanh chưa đầy cho tới khi đủ thời gian tối thiểu
            float minRatio = Mathf.Clamp01(timer / Mathf.Max(0.01f, minLoadingTime));
            float target01 = Mathf.Min(real, minRatio);

            displayed = Mathf.MoveTowards(displayed, target01, barSpeed * Time.unscaledDeltaTime);
            SetProgress(displayed);

            if (displayed >= 1f && real >= 1f && timer >= minLoadingTime)
                break;

            yield return null;
        }

        op.allowSceneActivation = true;
    }

    private void SetProgress(float value)
    {
        if (progressBar != null)
            progressBar.value = value;

        if (percentText != null)
            percentText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}