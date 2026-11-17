
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ComicPageController : MonoBehaviour
{
    public Image[] comicPanels;
    public TextMeshProUGUI nextText;

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

        // Ẩn NEXT
        SetAlpha(nextText, 0);
        nextText.gameObject.SetActive(false);

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
            // Nhấn thêm lần nữa mới hiện NEXT
            nextShown = true;

            nextText.gameObject.SetActive(true);

            StartCoroutine(FadeIn(nextText));
        }
    }

    void SetAlpha(Graphic target, float alpha)
    {
        Color color = target.color;
        color.a = alpha;
        target.color = color;
    }

    IEnumerator FadeIn(Graphic target)
    {
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
