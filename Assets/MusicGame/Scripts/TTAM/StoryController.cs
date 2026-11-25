
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class StoryController : MonoBehaviour
{
    [Header("Comic Panels")]
    public Image[] comicPanels;

    [Header("Next Text")]
    public TextMeshProUGUI nextText;

    [Header("Scene Button")]
    public Button nextSceneButton;
    public string nextSceneName;

    [Header("Animation")]
    public float fadeDuration = 0.5f;

    private int currentPanel = 0;
    private bool isFading = false;
    private bool nextShown = false;

    void Start()
    {
        // Ẩn tất cả Panel
        for (int i = 0; i < comicPanels.Length; i++)
        {
            SetAlpha(comicPanels[i], 0);
            comicPanels[i].gameObject.SetActive(false);
        }

        // Ẩn NEXT text
        if (nextText != null)
        {
            SetAlpha(nextText, 0);
            nextText.gameObject.SetActive(false);
        }

        // Ẩn nút chuyển Scene
        if (nextSceneButton != null)
        {
            nextSceneButton.gameObject.SetActive(false);
            nextSceneButton.onClick.AddListener(LoadNextScene);
        }

        // Hiện Panel đầu tiên
        if (comicPanels.Length > 0)
        {
            comicPanels[0].gameObject.SetActive(true);
            currentPanel = 1;

            StartCoroutine(FadeIn(comicPanels[0]));
        }
    }

    public void ShowNextPanel()
    {
        if (isFading) return;

        // Hiện Panel tiếp theo
        if (currentPanel < comicPanels.Length)
        {
            Image panel = comicPanels[currentPanel];
            currentPanel++;

            panel.gameObject.SetActive(true);
            StartCoroutine(FadeIn(panel));
        }
        else if (!nextShown)
        {
            nextShown = true;

            // Hiện NEXT text
            if (nextText != null)
            {
                nextText.gameObject.SetActive(true);
                StartCoroutine(FadeIn(nextText));
            }

            // Hiện nút chuyển Scene
            if (nextSceneButton != null)
            {
                nextSceneButton.gameObject.SetActive(true);
                StartCoroutine(FadeIn(nextSceneButton.image));
            }
        }
    }

    public void LoadNextScene()
    {
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Chưa nhập tên Scene tiếp theo!");
            return;
        }

        SceneFader.Load(nextSceneName);   // tối dần rồi chuyển scene
    }

    void SetAlpha(Graphic target, float alpha)
    {
        if (target == null) return;

        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }

    IEnumerator FadeIn(Graphic target)
    {
        if (target == null) yield break;

        isFading = true;

        float time = 0;
        Color color = target.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            color.a = Mathf.Lerp(0, 1, time / fadeDuration);
            target.color = color;

            yield return null;
        }

        color.a = 1;
        target.color = color;

        isFading = false;
    }
}
