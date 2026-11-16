using System.Collections;
using UnityEngine;

public class NTHitStopAndShake : MonoBehaviour
{
    public static NTHitStopAndShake Instance { get; private set; }

    [Header("Camera Shake Settings")]
    [SerializeField] private Transform cameraTransform;
    private Vector3 originalCameraPos;
    private Coroutine shakeCoroutine;
    private Coroutine hitStopCoroutine;

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

    // =========================================================================
    // CÁC HÀM KHÔNG THAM SỐ ĐỂ GẮN TRỰC TIẾP VÀO UNITY EVENT TRONG INSPECTOR
    // =========================================================================

    /// <summary>
    /// Gắn vào On Double Hit Camera Shake: Rung lực vừa, không khựng thời gian để giữ mượt nhịp rơi
    /// </summary>
    public void TriggerDoubleHitShake()
    {
        TriggerHitImpact(0.12f, 0.16f, 0f);
    }

    /// <summary>
    /// Gắn vào On Bomb Hit Camera Shake: Rung mạnh + khựng hình 0.05s tạo cảm giác nổ Bom
    /// </summary>
    public void TriggerBombShake()
    {
        TriggerHitImpact(0.22f, 0.28f, 0.05f);
    }

    /// <summary>
    /// Hàm gốc tùy chỉnh thông số (gọi từ code khác nếu cần)
    /// </summary>
    public void TriggerHitImpact(float duration = 0.1f, float magnitude = 0.15f, float stopDuration = 0.04f)
    {
        if (stopDuration > 0f)
        {
            if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = StartCoroutine(HitStopRoutine(stopDuration));
        }

        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        // Không ghi đè nếu game đang tạm dừng (Pause)
        if (Time.timeScale == 0f) yield break;

        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);

        // Chỉ trả lại tốc độ bình thường nếu người chơi chưa bấm Pause trong lúc khựng
        if (Time.timeScale > 0f)
        {
            Time.timeScale = 1f;
        }
        hitStopCoroutine = null;
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
                cameraTransform.localPosition = new Vector3(
                    originalCameraPos.x + x,
                    originalCameraPos.y + y,
                    originalCameraPos.z
                );
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (cameraTransform != null)
        {
            cameraTransform.localPosition = originalCameraPos;
        }
        shakeCoroutine = null;
    }
}