using System;
using UnityEngine;
using UnityEngine.UI;

public class ItemDurationBar : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image fill;   // Image Type = Filled, Horizontal

    private string id;
    private float duration;
    private float remaining;
    private Action<string> onEnd;
    private bool finished;

    public void Begin(string id, Sprite sprite, float duration, Action<string> onEnd)
    {
        this.id = id;
        this.onEnd = onEnd;

        if (icon != null)
        {
            icon.sprite = sprite;
            icon.enabled = sprite != null;
        }

        Restart(duration);
    }

    // Nhặt lại cùng loại item: reset thời gian, không tạo thanh mới.
    public void Restart(float newDuration)
    {
        duration = Mathf.Max(0.01f, newDuration);
        remaining = duration;
        finished = false;
        Refresh();
    }

    private void Update()
    {
        if (finished)
            return;

        // Pause game thì đứng im.
        if (PianoKeySpawner.Instance != null && PianoKeySpawner.Instance.IsPaused)
            return;

        remaining -= Time.deltaTime;
        Refresh();

        if (remaining <= 0f)
            Finish(true);
    }

    private void Refresh()
    {
        if (fill != null)
            fill.fillAmount = Mathf.Clamp01(remaining / duration);
    }

    // callEnd = true: hết giờ tự nhiên. false: bị hủy từ ngoài.
    public void Finish(bool callEnd)
    {
        if (finished)
            return;

        finished = true;

        if (callEnd)
            onEnd?.Invoke(id);

        Destroy(gameObject);
    }
}