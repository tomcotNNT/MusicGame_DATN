using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class LoadingManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string nextSceneName = "MainMenu2"; 

    [Header("Sound Wave Pulse Effect")]
    [SerializeField] private RectTransform pulseImage; 
    [SerializeField] private float pulseDuration = 0.6f; 

    private void Start()
    {
        if (pulseImage != null)
        {
            pulseImage.localScale = Vector3.zero;
            SetImageAlpha(pulseImage, 1f);
        }

        // Bắt đầu quá trình tải thực tế
        StartCoroutine(RealtimeLoadingRoutine());
    }

    private IEnumerator RealtimeLoadingRoutine()
    {
        // 1. Khôi phục dữ liệu máy nhanh chóng thông qua GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameData();
        }

        // 2. Bắt đầu nạp Scene tiếp theo ngầm dựa vào cấu hình phần cứng thiết bị
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false; // Giữ lại chưa cho chuyển cảnh vội

        // Chờ thiết bị tải dữ liệu Scene thực tế đến 90%
        while (asyncLoad.progress < 0.9f)
        {
            yield return null;
        }

        // 3. Kích hoạt âm thanh bắt đầu và hiệu ứng sóng âm bùng nổ
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayStartSound();
        }

        float elapsed = 0f;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;

            // Chạy hiệu ứng sóng âm mượt mà
            if (pulseImage != null)
            {
                float t = elapsed / pulseDuration;
                float scale = Mathf.Lerp(0f, 50f, Mathf.Sin(t * Mathf.PI * 0.5f)); 
                pulseImage.localScale = new Vector3(scale, scale, 1f);

                float alpha = Mathf.Lerp(1f, 0f, t);
                SetImageAlpha(pulseImage, alpha);
            }

            yield return null;
        }

        // 4. Cho phép chuyển thẳng sang Scene tiếp theo
        asyncLoad.allowSceneActivation = true;
    }

    private void SetImageAlpha(RectTransform rt, float alpha)
    {
        Image img = rt.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c; // Cập nhật lại giá trị Alpha mới cho Image
        }
    }
}