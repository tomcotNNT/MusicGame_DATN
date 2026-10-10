using UnityEngine;
using UnityEngine.UI;

public class KeyButtonController : MonoBehaviour
{
    [Header("Button Images")]
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;
    [SerializeField] private Color normalColor = Color.white;

    [Header("Fill Overlay")]
    [SerializeField] private Image leftFill;
    [SerializeField] private Image rightFill;
    [SerializeField] private Image.FillMethod fillMethod = Image.FillMethod.Vertical;
    [SerializeField] private int fillOrigin = 0;

    [Header("Fill Colors")]
    [SerializeField] private Color fillColor = new Color(1f, 0.9f, 0.2f);
    [SerializeField] private Color hitColor = new Color(0.2f, 1f, 0.4f);

    [Range(0f, 1f)] [SerializeField] private float fillStartAlpha = 0.3f;
    [Range(0f, 1f)] [SerializeField] private float fillEndAlpha = 0.7f;
    [Range(0f, 1f)] [SerializeField] private float hitAlpha = 1f;

    [Header("Button Text Fade")]
    [SerializeField] private Graphic leftText;
    [SerializeField] private Graphic rightText;
    [SerializeField] private float textFadeDelay = 3f;
    [SerializeField] private float textFadeDuration = 1f;
    [Range(0f, 1f)] [SerializeField] private float textEndAlpha = 0.2f;

    [Header("Distance")]
    [SerializeField] private float detectDistance = 4f;
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

    private float textTimer;
    private float leftTextStartAlpha;
    private float rightTextStartAlpha;

    private void Awake()
    {
        scoreSystem = FindObjectOfType<ScoreSystem>();
        comboSystem = FindObjectOfType<ComboSystem>();

        if (scoreSystem == null)
            Debug.LogError("Không tìm thấy ScoreSystem!");

        if (comboSystem == null)
            Debug.LogError("Không tìm thấy ComboSystem!");
    }

    private void Start()
    {
        SetupFillImage(leftFill);
        SetupFillImage(rightFill);

        if (leftText != null)
            leftTextStartAlpha = leftText.color.a;

        if (rightText != null)
            rightTextStartAlpha = rightText.color.a;

        ResetButtons();
    }

    private void SetupFillImage(Image fill)
    {
        if (fill == null)
            return;

        fill.type = Image.Type.Filled;
        fill.fillMethod = fillMethod;
        fill.fillOrigin = fillOrigin;
        fill.raycastTarget = false;
    }

    private void Update()
    {
        if (player == null)
            return;

        FindCurrentKey();
        UpdateButtonLight();
        UpdateTextFade();
        HandleKeyboardInput();
    }

    // =====================================================
    // KEYBOARD
    // =====================================================

    private void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            CheckInput(PianoKey.KeyType.Left);

        if (Input.GetKeyDown(KeyCode.W))
            CheckInput(PianoKey.KeyType.Right);
    }

    // =====================================================
    // BUTTON INPUT
    // Gắn vào EventTrigger -> PointerDown (KHÔNG dùng Button.onClick)
    // =====================================================

    public void OnLeftButton()
    {
        CheckInput(PianoKey.KeyType.Left);
    }

    public void OnRightButton()
    {
        CheckInput(PianoKey.KeyType.Right);
    }

    // =====================================================
    // FIND NORMAL KEY
    // =====================================================

    private void FindCurrentKey()
    {
        currentKey = null;

        float closestDistance = Mathf.Infinity;

        foreach (PianoKey key in PianoKey.ActiveKeys)
        {
            if (key == null || key.IsCompleted)
                continue;

            if (key.transform.position.x < player.position.x)
                continue;

            float distance = Vector3.Distance(player.position, key.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentKey = key;
            }
        }
    }

    // =====================================================
    // BUTTON FILL
    // =====================================================

    private void UpdateButtonLight()
    {
        ResetButtons();

        if (currentKey == null)
            return;

        float dx = Mathf.Abs(currentKey.transform.position.x - player.position.x);

        if (dx > detectDistance)
            return;

        bool touched = currentKey.PlayerOnKey;

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

        Color color = touched ? hitColor : fillColor;

        color.a = touched
            ? hitAlpha
            : Mathf.Lerp(fillStartAlpha, fillEndAlpha, amount);

        fill.color = color;
    }

    private void ResetButtons()
    {
        if (leftImage != null)
            leftImage.color = normalColor;

        if (rightImage != null)
            rightImage.color = normalColor;

        ClearFill(leftFill);
        ClearFill(rightFill);
    }

    private void ClearFill(Image fill)
    {
        if (fill != null)
            fill.fillAmount = 0f;
    }

    // =====================================================
    // TEXT FADE
    // =====================================================

    private void UpdateTextFade()
    {
        textTimer += Time.deltaTime;

        if (textTimer < textFadeDelay)
            return;

        float t = Mathf.Clamp01(
            (textTimer - textFadeDelay) / Mathf.Max(0.01f, textFadeDuration)
        );

        SetTextAlpha(leftText, Mathf.Lerp(leftTextStartAlpha, textEndAlpha, t));
        SetTextAlpha(rightText, Mathf.Lerp(rightTextStartAlpha, textEndAlpha, t));
    }

    private void SetTextAlpha(Graphic text, float alpha)
    {
        if (text == null)
            return;

        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    // =====================================================
    // NORMAL NOTE HIT
    // =====================================================

    private void CheckInput(PianoKey.KeyType inputType)
    {
        // Lane đang dành cho nốt dài -> không tính là tap nốt thường.
        Lanee lane = inputType == PianoKey.KeyType.Left ? Lanee.Left : Lanee.Right;

        if (PianoKeySpawner.Instance != null &&
            LongNote.LaneBusy(lane, PianoKeySpawner.Instance.SongTime))
            return;

        if (currentKey == null)
            return;

        if (!currentKey.PlayerOnKey)
        {
            Debug.Log("Player chưa chạm Key");

            if (comboSystem != null)
                comboSystem.MissNote();

            return;
        }

        if (currentKey.GetKeyType() != inputType)
        {
            Debug.Log("SAI NÚT");

            if (comboSystem != null)
                comboSystem.MissNote();

            return;
        }

        Debug.Log("HIT +100");

        currentKey.Complete();
    }
}