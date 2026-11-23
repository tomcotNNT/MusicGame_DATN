using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn vào 1 UI Image (nút) trên Canvas gameplay. Tạo 2 nút: 1 nút Left, 1 nút Right.
/// Nút nhận chạm ngay khi ngón tay chạm xuống (không đợi nhả như Button thường),
/// hỗ trợ bấm 2 nút cùng lúc (nhiều ngón) và giữ nút (nốt dài).
/// </summary>
public class LaneButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Tooltip("Bật = nút TRÁI, tắt = nút PHẢI")]
    [SerializeField] private bool isLeft = true;

    [Header("Hiệu ứng bấm (tùy chọn)")]
    [SerializeField] private Transform visual;
    [SerializeField] private float pressedScale = 0.9f;

    private int pointerCount;   // Số ngón đang đè lên nút
    private Vector3 baseScale;
    private UnityEngine.UI.Selectable selectable; // Button (nếu có) trên cùng object

    private void Awake()
    {
        if (visual == null) visual = transform;
        baseScale = visual.localScale;
        selectable = GetComponent<UnityEngine.UI.Selectable>();
    }

    private void OnDisable()
    {
        // Panel bị tắt khi đang giữ -> nhả nút, tránh kẹt trạng thái giữ
        while (pointerCount > 0) ReleaseOne();
        visual.localScale = baseScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Gắn lên Button mà Button đang tắt Interactable -> không nhận
        if (selectable != null && !selectable.IsInteractable()) return;

        pointerCount++;
        visual.localScale = baseScale * pressedScale;
        LaneButtonInput.Press(isLeft);
    }

    // Unity luôn gửi PointerUp về đúng nút đã nhận PointerDown,
    // kể cả khi ngón tay trượt ra ngoài -> giữ nốt dài không bị đứt
    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseOne();
    }

    private void ReleaseOne()
    {
        if (pointerCount <= 0) return;

        pointerCount--;
        if (pointerCount == 0) visual.localScale = baseScale;

        LaneButtonInput.Release(isLeft);
    }
}

/// <summary>
/// Trạng thái 2 nút Left / Right, đọc được từ bất kỳ script nào.
///   LaneButtonInput.Pressed += (isLeft) => ...   // vừa bấm xuống
///   LaneButtonInput.IsHeld(true)                 // nút trái đang được giữ
/// </summary>
public static class LaneButtonInput
{
    private static readonly int[] holdCount = new int[2];

    /// <summary>Bắn ra ngay khi 1 nút vừa được bấm xuống (tham số: isLeft).</summary>
    public static event Action<bool> Pressed;

    /// <summary>Bắn ra khi 1 nút được nhả hoàn toàn (tham số: isLeft).</summary>
    public static event Action<bool> Released;

    private static int Idx(bool isLeft) => isLeft ? 0 : 1;

    public static bool IsHeld(bool isLeft) => holdCount[Idx(isLeft)] > 0;

    public static void Press(bool isLeft)
    {
        holdCount[Idx(isLeft)]++;
        Pressed?.Invoke(isLeft);
    }

    public static void Release(bool isLeft)
    {
        int i = Idx(isLeft);
        if (holdCount[i] <= 0) return;

        holdCount[i]--;
        if (holdCount[i] == 0)
            Released?.Invoke(isLeft);
    }

    // Xoá trạng thái khi vào scene mới (phòng kẹt nút giữa các scene)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        holdCount[0] = holdCount[1] = 0;
        Pressed = null;
        Released = null;
    }
}
