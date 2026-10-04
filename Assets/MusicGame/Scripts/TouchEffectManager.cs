using UnityEngine;

public class TouchEffectManager : MonoBehaviour
{
    [SerializeField] private GameObject touchEffectPrefab;
    [SerializeField] private Canvas canvas;

    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = canvas.GetComponent<RectTransform>();
    }

    public void ShowTouch(Vector2 screenPosition)
    {
        GameObject effect = Instantiate(
            touchEffectPrefab,
            canvas.transform
        );

        RectTransform effectRect =
            effect.GetComponent<RectTransform>();

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition,
            canvas.worldCamera,
            out Vector2 localPosition
        );

        effectRect.localPosition = localPosition;

        TouchEffect touchEffect =
            effect.GetComponent<TouchEffect>();

        touchEffect.Play();
    }
}