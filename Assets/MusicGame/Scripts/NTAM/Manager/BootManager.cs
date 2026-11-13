using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BootManager : MonoBehaviour
{
    [Header("UI Chuyển Cảnh")]
    public Slider loadingSlider;
    public Image fadePanel;

    [Header("Cài đặt")]
    public int nextSceneIndex = 1;
    public float fakeDelay = 1f;
    public float fadeDuration = 0.5f;

    private void Start()
    {
        if (fadePanel != null)
        {
            Color c = fadePanel.color;
            c.a = 0f;
            fadePanel.color = c;
            fadePanel.gameObject.SetActive(false);
        }
        StartCoroutine(LoadSceneAsyncCoroutine());
    }

    private IEnumerator LoadSceneAsyncCoroutine()
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(nextSceneIndex);

        if (operation == null)
        {
            Debug.LogError("[BootManager] Lỗi: Không tìm thấy Scene số " + nextSceneIndex);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (loadingSlider != null)
            {
                loadingSlider.value = progress;
            }

            if (operation.progress >= 0.9f)
            {
                yield return new WaitForSeconds(fakeDelay);

                yield return StartCoroutine(FadeInRoutine());
                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private IEnumerator FadeInRoutine()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        float timer = 0f;
        Color c = fadePanel.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Clamp01(timer / fadeDuration);
            fadePanel.color = c;
            yield return null;
        }
    }
}