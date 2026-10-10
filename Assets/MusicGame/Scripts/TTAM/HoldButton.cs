using UnityEngine;
using UnityEngine.EventSystems;

public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] private Lanee lane;

    public void OnPointerDown(PointerEventData e) => HoldInput.SetUIHeld(lane, true);
    public void OnPointerUp(PointerEventData e)   => HoldInput.SetUIHeld(lane, false);
    public void OnPointerExit(PointerEventData e) => HoldInput.SetUIHeld(lane, false);
}