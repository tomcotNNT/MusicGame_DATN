using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TouchEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startScale = 0.3f;
    [SerializeField] private float endScale = 1.5f;

    private Image image;
    private RectTransform rectTransform;

    private void Awake()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Play()
    {
        StopAllCoroutines();
        StartCoroutine(PlayEffect());
    }

    private IEnumerator PlayEffect()
    {
        float elapsed = 0f;

        Color startColor = image.color;
        startColor.a = 1f;

        Color endColor = image.color;
        endColor.a = 0f;

        rectTransform.localScale =
            Vector3.one * startScale;

        image.color = startColor;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            // Phóng to
            float scale = Mathf.Lerp(
                startScale,
                endScale,
                t
            );

            rectTransform.localScale =
                Vector3.one * scale;

            // Fade
            image.color = Color.Lerp(
                startColor,
                endColor,
                t
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        Destroy(gameObject);
    }
}