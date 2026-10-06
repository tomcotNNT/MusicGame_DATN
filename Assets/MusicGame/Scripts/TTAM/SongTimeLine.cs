using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SongTimeline : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private AudioSource musicSource;   // cùng AudioSource với Spawner

    [Header("Timeline UI")]
    [SerializeField] private Image timelineFill;        // Image dạng Filled
    [SerializeField] private TMP_Text timeText;         // tùy chọn: "01:23 / 03:45"

    [Header("Handle chạy theo thanh (tùy chọn)")]
    [SerializeField] private RectTransform handle;      // icon nhỏ chạy dọc thanh
    [SerializeField] private RectTransform barRect;     // RectTransform của thanh (để tính chiều rộng)

    [Header("Smooth")]
    [SerializeField] private bool smooth = true;
    [SerializeField] private float smoothSpeed = 10f;

    private bool finished;

    private void Start()
    {
        if (timelineFill != null)
        {
            timelineFill.type = Image.Type.Filled;
            timelineFill.fillMethod = Image.FillMethod.Horizontal;
            timelineFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            timelineFill.fillAmount = 0f;
            timelineFill.raycastTarget = false;

            if (timelineFill.sprite == null)
                Debug.LogWarning("SongTimeline: Image chưa có Source Image, fill sẽ không hiển thị!");
        }

        UpdateHandle(0f);
    }

    private void Update()
    {
        if (finished || musicSource == null || musicSource.clip == null)
            return;

        float length = musicSource.clip.length;
        float current = musicSource.time;
        float target = Mathf.Clamp01(current / length);

        // Nhạc chưa phát (time = 0) thì giữ nguyên, tránh tụt về 0 giữa chừng
        if (!musicSource.isPlaying && current <= 0f)
            return;

        float value = target;
        if (smooth && timelineFill != null)
            value = Mathf.Lerp(timelineFill.fillAmount, target, Time.deltaTime * smoothSpeed);

        if (timelineFill != null)
            timelineFill.fillAmount = value;

        UpdateHandle(value);

        if (timeText != null)
            timeText.text = Format(current) + " / " + Format(length);
    }

    private void UpdateHandle(float value)
    {
        if (handle == null || barRect == null)
            return;

        float width = barRect.rect.width;
        Vector2 pos = handle.anchoredPosition;
        pos.x = (value - barRect.pivot.x) * width;   // đúng với mọi pivot của thanh
        handle.anchoredPosition = pos;
    }

    // Gọi khi bài kết thúc để giữ thanh đầy
    public void SetFinished()
    {
        finished = true;

        if (timelineFill != null)
            timelineFill.fillAmount = 1f;

        UpdateHandle(1f);
    }

    private string Format(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return m.ToString("00") + ":" + s.ToString("00");
    }
}