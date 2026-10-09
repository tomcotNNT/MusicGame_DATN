using UnityEngine;

/// <summary>
/// Quản lý NỐT DÀI (phía UI): báo trạng thái giữ nút cho HoldInput.
/// Gắn EventTrigger: PointerDown -> OnXxxHoldDown, PointerUp -> OnXxxHoldUp.
/// Nên thêm cả PointerExit -> OnXxxHoldUp để trượt tay ra ngoài không bị kẹt.
/// </summary>
public class LongNoteButtonInput : MonoBehaviour
{
    private bool leftPressed;
    private bool rightPressed;

    private void OnDisable()
    {
        leftPressed = false;
        rightPressed = false;

        HoldInput.SetUIHeld(Lanee.Left, false);
        HoldInput.SetUIHeld(Lanee.Right, false);
    }

    public void OnLeftHoldDown()
    {
        if (leftPressed) return;
        leftPressed = true;
        HoldInput.SetUIHeld(Lanee.Left, true);
    }

    public void OnLeftHoldUp()
    {
        if (!leftPressed) return;
        leftPressed = false;
        HoldInput.SetUIHeld(Lanee.Left, false);
    }

    public void OnRightHoldDown()
    {
        if (rightPressed) return;
        rightPressed = true;
        HoldInput.SetUIHeld(Lanee.Right, true);
    }

    public void OnRightHoldUp()
    {
        if (!rightPressed) return;
        rightPressed = false;
        HoldInput.SetUIHeld(Lanee.Right, false);
    }
}