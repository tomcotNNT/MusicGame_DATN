using System.Collections;
using UnityEngine;

public class SongBackgroundSwitcher : MonoBehaviour
{
    public static SongBackgroundSwitcher Instance { get; private set; }

    [Header("Kéo các cụm Man1, Man2, Man3 (Yêu cầu mỗi cụm có sẵn CanvasGroup)")]
    public CanvasGroup[] backgroundCanvasGroups;

    [Header("Tốc độ chuyển cảnh mượt (Fade Duration)")]
    public float fadeDuration = 0.25f;

    private int currentIndex = -1;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void SwitchBackground(int songIndex)
    {
        if (songIndex < 0 || songIndex >= backgroundCanvasGroups.Length) return;
        if (currentIndex == songIndex) return; // Nếu đang ở đúng màn đó thì không chạy lại

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeAndSwitchRoutine(songIndex));
    }

    private IEnumerator SystemInit(int initialIndex)
    {
        yield return null;
    }

    private IEnumerator FadeAndSwitchRoutine(int targetIndex)
    {
        int previousIndex = currentIndex;
        currentIndex = targetIndex;

        // 1. Bật background mới lên với alpha = 0
        CanvasGroup targetGroup = backgroundCanvasGroups[targetIndex];
        targetGroup.gameObject.SetActive(true);
        targetGroup.alpha = 0f;

        CanvasGroup previousGroup = (previousIndex >= 0 && previousIndex < backgroundCanvasGroups.Length) 
                                    ? backgroundCanvasGroups[previousIndex] 
                                    : null;

        float timer = 0f;

        // 2. Chạy hiệu ứng Fade (Mờ dần cái cũ, hiện dần cái mới)
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / fadeDuration;

            targetGroup.alpha = Mathf.Clamp01(progress);
            if (previousGroup != null)
            {
                previousGroup.alpha = Mathf.Clamp01(1f - progress);
            }

            yield return null;
        }

        // 3. Đảm bảo alpha tuyệt đối và tắt hẳn cái cũ để tiết kiệm pin/GPU cho Android
        targetGroup.alpha = 1f;
        if (previousGroup != null)
        {
            previousGroup.alpha = 0f;
            previousGroup.gameObject.SetActive(false);
        }
    }
} 