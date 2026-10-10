using UnityEngine;
using UnityEngine.EventSystems;

public enum Lanee { Left, Right }

public class HoldInput : MonoBehaviour
{
    private static bool uiLeft, uiRight;

    public static bool IsHeld(Lanee lane)
    {
        if (lane == Lanee.Left)
            return uiLeft || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        return uiRight || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
    }

    public static void SetUIHeld(Lanee lane, bool held)
    {
        if (lane == Lanee.Left) uiLeft = held;
        else uiRight = held;
    }
}