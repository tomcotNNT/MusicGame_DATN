using UnityEngine;

public class CircleEffect : MonoBehaviour
{
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float maxScale = 2f;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;

        float progress = timer / duration;

        transform.localScale = Vector3.Lerp(
            Vector3.one,
            Vector3.one * maxScale,
            progress
        );

        if (timer >= duration)
        {
            Destroy(gameObject);
        }
    }
}