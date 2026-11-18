using System;
using UnityEngine;
using UnityEngine.UI;

public class SubmarineController : MonoBehaviour
{
    [Header("Patrol (chạy qua lại liên tục)")]
    [SerializeField] private float leftX = -3f;
    [SerializeField] private float rightX = 3f;
    [Tooltip("Tốc độ chạy qua lại. Càng lớn tàu càng nhanh")]
    [SerializeField] private float patrolSpeed = 1.2f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("Bật nếu sprite tàu gốc đang quay mặt sang phải")]
    [SerializeField] private bool spriteFacesRight = true;

    [Header("Bobbing")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Drop")]
    [SerializeField] private Transform dropPoint;   // vị trí thả (dưới bụng tàu)

    [Header("Shake khi thả")]
    [SerializeField] private CameraShake cameraShake;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeMagnitude = 0.12f;

    [Header("Boss HP")]
    [Tooltip("Image kiểu Filled (Image Type = Filled, Fill Method = Horizontal)")]
    [SerializeField] private Image hpFill;
    [SerializeField] private float hpSmoothSpeed = 8f;
    [Tooltip("Rung mạnh khi boss bị hạ")]
    [SerializeField] private float defeatShakeDuration = 0.6f;
    [SerializeField] private float defeatShakeMagnitude = 0.4f;

    public static event Action AnyBossDefeated;

    public int MaxHp { get; private set; }
    public int CurrentHp { get; private set; }
    public bool IsDefeated { get; private set; }

    private float displayedFill = 1f;

    private float baseY;
    private float lastX;

    private void Start()
    {
        baseY = transform.position.y;
        lastX = transform.position.x;

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (cameraShake == null && Camera.main != null)
            cameraShake = Camera.main.GetComponent<CameraShake>();
    }

    private void Update()
    {
        // 0..1 qua lại mượt (chậm ở hai đầu, nhanh ở giữa)
        float t = (Mathf.Sin(Time.time * patrolSpeed) + 1f) * 0.5f;

        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(leftX, rightX, t);
        pos.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.position = pos;

        // Quay mặt theo hướng đang chạy
        float dx = pos.x - lastX;
        if (spriteRenderer != null && Mathf.Abs(dx) > 0.0001f)
        {
            bool movingRight = dx > 0f;
            // Sprite gốc quay phải: chạy trái thì lật. Sprite gốc quay trái: chạy phải thì lật.
            bool needFlip = spriteFacesRight ? !movingRight : movingRight;
            spriteRenderer.flipX = needFlip;
        }
        lastX = pos.x;

        if (hpFill != null && MaxHp > 0)
        {
            float target = (float)CurrentHp / MaxHp;
            displayedFill = Mathf.MoveTowards(displayedFill, target, hpSmoothSpeed * Time.deltaTime);
            hpFill.fillAmount = displayedFill;
        }

    }

    // Vị trí thả = vị trí tàu ngay lúc này (tàu đang chạy)
    public Vector3 GetDropPosition()
    {
        return dropPoint != null ? dropPoint.position : transform.position;
    }

    public void Shake()
    {
        if (cameraShake != null)
            cameraShake.Shake(shakeDuration, shakeMagnitude);
    }

    public void Shake(float duration, float magnitude)
    {
        if (cameraShake != null)
            cameraShake.Shake(duration, magnitude);
    }

    // Spawner gọi khi bắt đầu bài: máu tối đa = số nốt
    public void InitHp(int maxHp)
    {
        MaxHp = Mathf.Max(1, maxHp);
        CurrentHp = MaxHp;
        IsDefeated = false;
        displayedFill = 1f;

        if (hpFill != null)
            hpFill.fillAmount = 1f;
    }

    // Spawner gọi mỗi lần người chơi đánh trúng 1 item
    public void TakeHit()
    {
        if (IsDefeated) return;

        CurrentHp = Mathf.Max(0, CurrentHp - 1);

        if (CurrentHp <= 0)
        {
            IsDefeated = true;
            Shake(defeatShakeDuration, defeatShakeMagnitude);
            AnyBossDefeated?.Invoke();
        }
    }
}