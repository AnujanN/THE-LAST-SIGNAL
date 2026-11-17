using LastSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LastSignal.UI
{
    /// <summary>
    /// Invisible right-side area: dragging one finger here turns the camera.
    /// Buttons placed above it in the hierarchy take their touches first.
    /// Needs a Graphic (e.g. a transparent Image) with Raycast Target on.
    /// </summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IInitializePotentialDragHandler
    {
        private const int NoPointer = int.MinValue;
        private int pointerId = NoPointer;

        public PlayerInputRouter Router { get; set; }
        public bool IsHeld => pointerId != NoPointer;

        private void OnDisable() => pointerId = NoPointer;

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId == NoPointer) pointerId = eventData.pointerId;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId || Router == null) return;
            Router.AddTouchLook(eventData.delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) pointerId = NoPointer;
        }
    }
}
