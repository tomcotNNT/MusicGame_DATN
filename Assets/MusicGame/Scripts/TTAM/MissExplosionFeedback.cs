using UnityEngine;

/// <summary>
/// Màn 4 - The Mysterious Cave: quái vật một đầu lao tới, KHÔNG bị đánh trúng thì phát nổ.
/// Đặt 1 object có script này trong scene màn chơi. Mỗi lần bỏ lỡ nốt (RhythmEvents.NoteMissed):
///   - sinh hiệu ứng nổ tại vị trí quái,
///   - rung camera,
///   - phát tiếng nổ,
///   - trừ máu người chơi (nếu nhân vật có PlayerHealth).
/// Không cần sửa code spawner: chỉ cần gán prefab nổ / âm thanh trong Inspector.
/// </summary>
public class MissExplosionFeedback : MonoBehaviour
{
    [Header("Hiệu ứng nổ")]
    [Tooltip("Prefab VFX nổ (tự huỷ sau Vfx Lifetime). Để trống = không sinh VFX")]
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField, Min(0.1f)] private float vfxLifetime = 1.5f;
    [Tooltip("Dời vị trí nổ (ví dụ nổ ngay trước mặt nhân vật thay vì tại vạch đánh)")]
    [SerializeField] private Vector3 offset = Vector3.zero;

    [Header("Rung camera")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField, Min(0f)] private float shakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float shakeMagnitude = 0.25f;

    [Header("Âm thanh")]
    [SerializeField] private AudioClip explosionClip;
    [SerializeField, Range(0f, 1f)] private float explosionVolume = 1f;

    [Header("Sát thương")]
    [Tooltip("Trừ máu nhân vật đang chơi (PlayerHealth). 0 = chỉ hiệu ứng, không trừ máu")]
    [SerializeField, Min(0)] private int damage = 10;

    private void Awake()
    {
        if (cameraShake == null)
            cameraShake = FindFirstObjectByType<CameraShake>();
    }

    private void OnEnable() => RhythmEvents.NoteMissed += OnNoteMissed;
    private void OnDisable() => RhythmEvents.NoteMissed -= OnNoteMissed;

    private void OnNoteMissed(Vector3 position, bool isLeft)
    {
        if (Time.timeScale == 0f) return;   // đang pause / game over

        if (explosionPrefab != null)
        {
            GameObject vfx = Instantiate(explosionPrefab, position + offset, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }

        if (cameraShake != null && shakeDuration > 0f)
            cameraShake.Shake(shakeDuration, shakeMagnitude);

        GameplaySfx.Play(explosionClip, explosionVolume);

        if (damage > 0)
        {
            foreach (PlayerHealth hp in FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None))
            {
                if (hp.isActiveAndEnabled && !hp.IsDead)
                {
                    hp.TakeDamage(damage);
                    break;
                }
            }
        }
    }
}
