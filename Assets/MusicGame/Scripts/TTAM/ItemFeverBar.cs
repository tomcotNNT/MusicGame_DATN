using UnityEngine;
using UnityEngine.UI;

public class ItemFeverBar : MonoBehaviour
{
    public static ItemFeverBar Instance { get; private set; }

    [Header("UI")]
    [Tooltip("Image có Image Type = Filled")]
    [SerializeField] private Image fillImage;
    [Tooltip("(Tuỳ chọn) icon của item đang tích")]
    [SerializeField] private Image iconImage;

    public GameObject Fever;

    [Header("Cấu hình")]
    [SerializeField, Min(1)] private int notesToFull = 5;
    [SerializeField] private float fillLerpSpeed = 10f;
    [Tooltip("Tự reset thanh về 0 sau khi đầy")]
    [SerializeField] private bool resetWhenFull = true;

    private string currentItemId;
    private int currentCount;
    private float displayFill;

    public bool IsFull => currentCount >= notesToFull;
    public string CurrentItemId => currentItemId;

    // Gọi khi thanh đầy (id của item đã tích đầy)
    public System.Action<string> OnFull;

    private void Awake()
    {
        Instance = this;
        ResetBar();
    }

    private void Start()
    {
        //an  fever
        if (Fever != null)
            Fever.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (fillImage == null)
            return;

        float target = (float)currentCount / notesToFull;

        displayFill = Mathf.Lerp(
            displayFill, target, Time.deltaTime * fillLerpSpeed);

        fillImage.fillAmount = displayFill;
    }

    public void AddItem(string itemId, Sprite icon = null)
    {
        if (string.IsNullOrEmpty(itemId))
            return;

        // Thanh đã đầy, chờ bắn rồi mới tích tiếp
        if (IsFull)
            return;

        if (Fever != null)
            Fever.SetActive(true);

        // Nhặt item khác id => tính lại từ đầu
        if (itemId != currentItemId)
        {
            currentItemId = itemId;
            currentCount = 0;
            displayFill = 0f;

            if (iconImage != null && icon != null)
                iconImage.sprite = icon;
        }

        currentCount++;

        if (currentCount >= notesToFull)
            OnFull?.Invoke(currentItemId);   // KHÔNG reset ở đây
    }

    public void Consume()
    {
        ResetBar();
        HideFever();
    }

    public void ResetBar()
    {
        currentItemId = null;
        currentCount = 0;
        displayFill = 0f;

        if (fillImage != null)
            fillImage.fillAmount = 0f;
    }

    public void HideFever()
    {
        if (Fever != null)
            Fever.SetActive(false);
    }
}