using System;
using UnityEngine;
using UnityEngine.UI;

// Gắn vào một object luôn bật (vd. Canvas), KHÔNG gắn vào chính object thanh Fever
public class FeverSystem : MonoBehaviour
{
    public static FeverSystem Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Object gốc của thanh Fever. Chỉ hiện khi có boss")]
    [SerializeField] private GameObject feverBar;
    [Tooltip("Image kiểu Filled (Horizontal) để chạy fillAmount")]
    [SerializeField] private Image feverFill;
    [Tooltip("Tốc độ thanh chạy mượt tới giá trị mới")]
    [SerializeField] private float fillSmooth = 8f;

    [Header("Luật")]
    [Tooltip("Cần nhặt bao nhiêu note để đầy thanh")]
    [SerializeField] private int notesToFill = 10;
    [Tooltip("Đầy thì tự về 0 để nạp lại")]
    [SerializeField] private bool resetWhenFull = true;

    // Bắn ra mỗi lần thanh đầy
    public event Action Filled;

    public bool IsActive { get; private set; }
    public float Value => value;

    private float value;   // 0..1 thật
    private float shown;   // 0..1 đang hiển thị (chạy mượt)

    private void Awake()
    {
        Instance = this;
        Show(false);
    }

    private void Update()
    {
        if (!IsActive || feverFill == null)
            return;

        shown = Mathf.Lerp(shown, value, 1f - Mathf.Exp(-fillSmooth * Time.deltaTime));
        feverFill.fillAmount = shown;
    }

    // Bật/tắt thanh (BossSpawner gọi). Mỗi lần bật là bắt đầu từ 0
    public void Show(bool show)
    {
        IsActive = show;
        value = 0f;
        shown = 0f;

        if (feverFill != null)
            feverFill.fillAmount = 0f;

        if (feverBar != null)
            feverBar.SetActive(show);
    }

    // Gọi mỗi khi player nhặt 1 note. Ngoài lúc đánh boss thì bỏ qua
    public void AddNote(int count = 1)
    {
        if (!IsActive)
            return;

        value = Mathf.Clamp01(value + count / (float)Mathf.Max(1, notesToFill));

        if (value >= 1f)
        {
            Filled?.Invoke();

            if (resetWhenFull)
                value = 0f;
        }
    }
}