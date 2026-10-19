using UnityEngine;
using UnityEngine.InputSystem;

public class TouchVFXController : MonoBehaviour
{
    [Header("Touch VFX")]
    [SerializeField] private ParticleSystem touchVFXPrefab;

    [Header("Position")]
    [SerializeField] private Camera targetCamera;

    // Mặt phẳng Z nơi VFX sẽ xuất hiện
    [SerializeField] private float vfxWorldZ = 0f;

    [Header("Click Sound")]
    [Tooltip("Bật/tắt âm thanh khi click/chạm")]
    [SerializeField] private bool enableClickSound = true;

    [SerializeField] private AudioClip clickSound;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 1f;

    [Tooltip("Random pitch nhẹ để tiếng click đỡ nhàm chán")]
    [SerializeField] private bool randomizePitch = false;

    private AudioSource audioSource;

    private void Start()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        // Tự tạo AudioSource nếu chưa có
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }

    private void Update()
    {
        // =========================
        // PC - Chuột trái
        // =========================
        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            SpawnTouchVFX(mousePosition);
        }

        // =========================
        // Mobile - Touch
        // =========================
        if (Touchscreen.current != null)
        {
            foreach (var touch in Touchscreen.current.touches)
            {
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 touchPosition = touch.position.ReadValue();
                    SpawnTouchVFX(touchPosition);
                }
            }
        }
    }

    private void SpawnTouchVFX(Vector2 screenPosition)
    {
        // Phát âm thanh (độc lập với VFX)
        PlayClickSound();

        if (touchVFXPrefab == null)
        {
            Debug.LogWarning("Touch VFX Prefab chưa được gán!");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("Target Camera chưa được gán!");
            return;
        }

        // Chuyển vị trí màn hình -> World
        Vector3 screenPoint = new Vector3(
            screenPosition.x,
            screenPosition.y,
            Mathf.Abs(targetCamera.transform.position.z - vfxWorldZ)
        );

        Vector3 worldPosition =
            targetCamera.ScreenToWorldPoint(screenPoint);

        // Đưa VFX về đúng mặt phẳng Z
        worldPosition.z = vfxWorldZ;

        // Tạo VFX
        ParticleSystem vfx = Instantiate(
            touchVFXPrefab,
            worldPosition,
            Quaternion.identity
        );

        // Chạy effect
        vfx.Play();

        // Tự hủy khi Particle System chạy xong
        Destroy(
            vfx.gameObject,
            GetVFXDestroyTime(vfx)
        );
    }

    private void PlayClickSound()
    {
        if (!enableClickSound || clickSound == null || audioSource == null)
            return;

        audioSource.pitch = randomizePitch ? Random.Range(0.9f, 1.1f) : 1f;
        audioSource.PlayOneShot(clickSound, clickVolume);
    }

    private float GetVFXDestroyTime(ParticleSystem vfx)
    {
        ParticleSystem.MainModule main = vfx.main;

        float duration = main.duration;
        float lifetime = 0f;

        if (main.startLifetime.mode ==
            ParticleSystemCurveMode.Constant)
        {
            lifetime = main.startLifetime.constant;
        }
        else
        {
            lifetime = main.startLifetime.constantMax;
        }

        // Thêm một chút thời gian để đảm bảo
        // particle biến mất hoàn toàn
        return duration + lifetime + 0.2f;
    }
}