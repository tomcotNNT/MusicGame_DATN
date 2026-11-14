
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SharkSwim : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float minSpeed = 60f;
    [SerializeField] private float maxSpeed = 100f;

    [Header("Pause")]
    [SerializeField] private float minPauseTime = 1f;
    [SerializeField] private float maxPauseTime = 3f;

    [Header("Swimming Motion")]
    [SerializeField] private float bobAmplitude = 5f;
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private float rotationAmount = 3f;

    [Header("Swim Distance")]
    [SerializeField] private float extraDistance = 200f;

    private RectTransform sharkRect;
    private RectTransform parentRect;
    private Image sharkImage;

    private float centerY;
    private float startX;
    private float endX;

    private float direction = 1f;
    private float currentSpeed;
    private float swimTime;

    private Coroutine swimCoroutine;

    private void Awake()
    {
        sharkRect = GetComponent<RectTransform>();
        parentRect = transform.parent.GetComponent<RectTransform>();
        sharkImage = GetComponent<Image>();

        centerY = sharkRect.anchoredPosition.y;

        // Giới hạn di chuyển theo chiều rộng Canvas cha
        startX = -parentRect.rect.width / 2f
                 - sharkRect.rect.width / 2f - extraDistance;

        endX = parentRect.rect.width / 2f
               + sharkRect.rect.width / 2f + extraDistance;
    }

    private void Start()
    {
        // Bắt đầu từ bên trái màn hình
        direction = 1f;

        sharkRect.anchoredPosition =
            new Vector2(startX, centerY);

        UpdateFacingDirection();

        swimCoroutine = StartCoroutine(SwimRoutine());
    }

    private IEnumerator SwimRoutine()
    {
        while (true)
        {
            currentSpeed = Random.Range(minSpeed, maxSpeed);

            float targetX = direction > 0 ? endX : startX;

            while (Mathf.Abs(sharkRect.anchoredPosition.x - targetX) > 1f)
            {
                float delta = Time.deltaTime;

                // Di chuyển ngang
                float newX = Mathf.MoveTowards(
                    sharkRect.anchoredPosition.x,
                    targetX,
                    currentSpeed * delta
                );

                // Nhấp nhô tự nhiên
                swimTime += delta * bobFrequency;

                float bobY = Mathf.Sin(swimTime) * bobAmplitude;

                sharkRect.anchoredPosition =
                    new Vector2(newX, centerY + bobY);

                // Nghiêng nhẹ khi bơi
                float tilt = Mathf.Sin(swimTime) * rotationAmount;

                sharkRect.localRotation =
                    Quaternion.Euler(0f, 0f, tilt * -direction);

                yield return null;
            }

            // Đưa cá về đúng vị trí nghỉ
            sharkRect.anchoredPosition =
                new Vector2(targetX, centerY);

            sharkRect.localRotation = Quaternion.identity;

            // Dừng nghỉ trước khi quay đầu
            float pause = Random.Range(minPauseTime, maxPauseTime);

            yield return new WaitForSeconds(pause);

            // Đổi hướng
            direction *= -1f;

            UpdateFacingDirection();
        }
    }

    private void UpdateFacingDirection()
    {
        if (sharkImage != null)
        {
            sharkImage.rectTransform.localScale =
                new Vector3(-direction, 1f, 1f);
        }
    }
}
