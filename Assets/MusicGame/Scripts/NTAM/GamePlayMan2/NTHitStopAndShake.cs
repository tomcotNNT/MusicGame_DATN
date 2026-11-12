using System.Collections;
using UnityEngine;

public class NTHitStopAndShake : MonoBehaviour
{
    public static NTHitStopAndShake Instance { get; private set; }

    [Header("Camera Shake Settings")]
    [SerializeField] private Transform cameraTransform;
    private Vector3 originalCameraPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            originalCameraPos = cameraTransform.localPosition;
        }
    }

    /// <summary>
    /// Kích hoạt rung màn hình và khựng khung hình khi đánh trúng
    /// </summary>
    public void TriggerHitImpact(float duration = 0.1f, float magnitude = 0.15f, float stopDuration = 0.04f)
    {
        StartCoroutine(HitStopRoutine(stopDuration));
        
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // Khựng hình nhẹ nhưng không làm lệch dspTime của Audio
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            if (cameraTransform != null)
            {
                cameraTransform.localPosition = new Vector3(originalCameraPos.x + x, originalCameraPos.y + y, originalCameraPos.z);
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalCameraPos;
        }
    }
}