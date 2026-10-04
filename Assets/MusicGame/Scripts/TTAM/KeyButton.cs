using UnityEngine;
using UnityEngine.UI;

public class KeyButtonController : MonoBehaviour
{

    [Header("Button Images (nền của nút)")]
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Fill Overlay (Image con nằm trên mỗi nút)")]
    [SerializeField] private Image leftFill;
    [SerializeField] private Image rightFill;
    [Tooltip("Hướng fill. Vertical = đầy từ dưới lên, Horizontal = từ trái sang phải, Radial360 = xoay tròn")]
    [SerializeField] private Image.FillMethod fillMethod = Image.FillMethod.Vertical;
    [Tooltip("Điểm bắt đầu fill (0 = Bottom/Left, 1 = Top/Right...)")]
    [SerializeField] private int fillOrigin = 0;

    [Header("Fill Colors")]
    [Tooltip("Màu trong lúc đang fill")]
    [SerializeField] private Color fillColor = new Color(1f, 0.9f, 0.2f);
    [Tooltip("Màu khi key chạm nốt")]
    [SerializeField] private Color hitColor = new Color(0.2f, 1f, 0.4f);
    [Range(0f, 1f)] [SerializeField] private float fillStartAlpha = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float fillEndAlpha = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float hitAlpha = 1f;

    [Header("Button Text Fade")]
    [SerializeField] private Graphic leftText;    // kéo Text/TMP của nút Left vào
    [SerializeField] private Graphic rightText;   // kéo Text/TMP của nút Right vào
    [SerializeField] private float textFadeDelay = 3f;     // sau bao lâu thì bắt đầu mờ
    [SerializeField] private float textFadeDuration = 1f;  // mờ trong bao lâu
    [Range(0f, 1f)] [SerializeField] private float textEndAlpha = 0.2f; // alpha cuối

    private float textTimer;
    private float leftTextStartAlpha;
    private float rightTextStartAlpha;

    [Header("Distance")]
    [Tooltip("Key cách player bao xa (theo trục X) thì bắt đầu fill")]
    [SerializeField] private float detectDistance = 4f;
    [Tooltip("Khoảng cách X lúc key bắt đầu chạm player. Chỉnh để fill vừa đầy khi key chạm")]
    [SerializeField] private float touchOffset = 0.5f;

    [Header("Timing")]
    [SerializeField] private float perfectWindow = 0.12f;
    [SerializeField] private float goodWindow = 0.25f;

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private AudioSource audioSource;

    private PianoKey currentKey;
    private ScoreSystem scoreSystem;
    private ComboSystem comboSystem;

    private void Awake()
    {
        scoreSystem = FindObjectOfType<ScoreSystem>();
        if (scoreSystem == null)
        {
            Debug.LogError("Không tìm thấy ScoreSystem trong scene!");
        }

        comboSystem = FindObjectOfType<ComboSystem>();
        if (comboSystem == null)
        {
            Debug.LogError("Không tìm thấy ComboSystem trong scene!");
        }
    }

    private void Start()
    {
        SetupFillImage(leftFill);
        SetupFillImage(rightFill);

        if (leftText != null) leftTextStartAlpha = leftText.color.a;
        if (rightText != null) rightTextStartAlpha = rightText.color.a;

        ResetButtons();
    }

    private void SetupFillImage(Image fill)
    {
        if (fill == null)
            return;

        if (fill.sprite == null)
            Debug.LogWarning("KeyButtonController: Fill '" + fill.name + "' chưa có Source Image, fill sẽ không hiển thị đúng!");

        fill.type = Image.Type.Filled;
        fill.fillMethod = fillMethod;
        fill.fillOrigin = fillOrigin;
        fill.raycastTarget = false;   // không chặn bấm nút
    }

    private void Update()
    {
        FindCurrentKey();
        UpdateButtonLight();
        UpdateTextFade();

        HandleKeyboardInput();
    }

    private void UpdateTextFade()
    {
        textTimer += Time.deltaTime;

        if (textTimer < textFadeDelay)
            return;

        // 0 -> 1 trong khoảng textFadeDuration
        float t = Mathf.Clamp01((textTimer - textFadeDelay) / Mathf.Max(0.01f, textFadeDuration));

        SetTextAlpha(leftText, Mathf.Lerp(leftTextStartAlpha, textEndAlpha, t));
        SetTextAlpha(rightText, Mathf.Lerp(rightTextStartAlpha, textEndAlpha, t));
    }

private void SetTextAlpha(Graphic text, float alpha)
{
    if (text == null)
        return;

    Color c = text.color;
    c.a = alpha;
    text.color = c;
}

    private void HandleKeyboardInput()
    {
        // Q = L
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CheckInput(PianoKey.KeyType.Left);
        }

        // W = R
        if (Input.GetKeyDown(KeyCode.W))
        {
            CheckInput(PianoKey.KeyType.Right);
        }
    }

    private void FindCurrentKey()
    {
        currentKey = null;

        float closestDistance = Mathf.Infinity;

        foreach (PianoKey key in PianoKey.ActiveKeys)
        {
            if (key == null)
                continue;

            if (key.IsCompleted)
                continue;

            // Không lấy key đã nằm sau Player
            if (key.transform.position.x < player.position.x)
                continue;

            float distance = Vector3.Distance(
                player.position,
                key.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentKey = key;
            }
        }
    }

    // =====================================================
    // FILL: đầy dần khi key tới gần, chạm nốt thì đổi màu + alpha tối đa
    // =====================================================

    private void UpdateButtonLight()
    {
        ResetButtons();

        if (currentKey == null)
            return;

        // Khoảng cách theo trục X (không bị ảnh hưởng bởi chênh lệch độ cao)
        float dx = Mathf.Abs(currentKey.transform.position.x - player.position.x);

        if (dx > detectDistance)
            return;

        bool touched = currentKey.PlayerOnKey;

        // 0 khi key ở xa (detectDistance), 1 khi key tới chỗ chạm (touchOffset)
        float progress = 1f - Mathf.Clamp01(
            (dx - touchOffset) / Mathf.Max(0.01f, detectDistance - touchOffset)
        );

        Image fill = currentKey.GetKeyType() == PianoKey.KeyType.Left
            ? leftFill
            : rightFill;

        ApplyFill(fill, touched ? 1f : progress, touched);
    }

    private void ApplyFill(Image fill, float amount, bool touched)
    {
        if (fill == null)
            return;

        fill.fillAmount = amount;

        Color c = touched ? hitColor : fillColor;

        c.a = touched
            ? hitAlpha
            : Mathf.Lerp(fillStartAlpha, fillEndAlpha, amount);

        fill.color = c;
    }

    private void ResetButtons()
    {
        if (leftImage != null) leftImage.color = normalColor;
        if (rightImage != null) rightImage.color = normalColor;

        ClearFill(leftFill);
        ClearFill(rightFill);
    }

    private void ClearFill(Image fill)
    {
        if (fill == null)
            return;

        fill.fillAmount = 0f;
    }

    public void OnLeftButton()
    {
        CheckInput(PianoKey.KeyType.Left);
    }

    public void OnRightButton()
    {
        CheckInput(PianoKey.KeyType.Right);
    }


    private void CheckInput(PianoKey.KeyType inputType)
    {
        if (currentKey == null)
            return;

        if (!currentKey.PlayerOnKey)
        {
            Debug.Log("Player chưa chạm Key");

            // Gọi hàm miss
            comboSystem.MissNote();

            return;
        }

        if (currentKey.GetKeyType() != inputType)
        {
            Debug.Log("SAI NÚT");
            // Gọi hàm miss
            comboSystem.MissNote();
            return;
        }

        Debug.Log("HIT +100");

        // ScoreSystem.AddScore(100);

        currentKey.Complete();
    }

    
}