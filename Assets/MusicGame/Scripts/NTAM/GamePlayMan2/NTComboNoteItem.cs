using UnityEngine;
using UnityEngine.UI;

public class NTComboNoteItem : MonoBehaviour
{
    [Header("Visual Components")]
    [SerializeField] private Image noteIcon;
    [SerializeField] private Text noteText;
    [SerializeField] private Outline outlineEffect;

    [Header("Colors & Scaling")]
    [SerializeField] private Color pendingColor = new Color(0.5f, 0.5f, 0.5f, 0.6f); // Nốt chờ: Hơi tối
    [SerializeField] private Color activeColor = Color.white;                         // Nốt đang sáng: Trắng sáng rực
    [SerializeField] private Vector3 activeScale = new Vector3(1.25f, 1.25f, 1f);     // Phóng to nhẹ khi tới lượt
    [SerializeField] private Vector3 normalScale = Vector3.one;

    private KeyCode assignedKey;

    public void Setup(KeyCode key, Sprite spriteL, Sprite spriteR)
    {
        assignedKey = key;

        if (noteText != null)
        {
            noteText.text = (key == KeyCode.A) ? "L" : "R";
        }

        if (noteIcon != null)
        {
            noteIcon.sprite = (key == KeyCode.A) ? spriteL : spriteR;
        }

        SetPending();
    }

    /// <summary>
    /// Trạng thái chờ: Nốt mờ, kích thước chuẩn
    /// </summary>
    public void SetPending()
    {
        transform.localScale = normalScale;
        if (noteIcon != null) noteIcon.color = pendingColor;
        if (noteText != null) noteText.color = pendingColor;
        if (outlineEffect != null) outlineEffect.enabled = false;
    }

    /// <summary>
    /// Trạng thái kích hoạt: Sáng bừng lên, phóng to nhẹ để người chơi biết nốt cần bấm
    /// </summary>
    public void SetActive()
    {
        transform.localScale = activeScale;
        if (noteIcon != null) noteIcon.color = activeColor;
        if (noteText != null) noteText.color = activeColor;
        if (outlineEffect != null)
        {
            outlineEffect.enabled = true;
            outlineEffect.effectColor = Color.yellow;
        }
    }

    /// <summary>
    /// Bấm trúng: Ẩn nốt đi
    /// </summary>
    public void OnSuccessHit()
    {
        gameObject.SetActive(false);
    }
}