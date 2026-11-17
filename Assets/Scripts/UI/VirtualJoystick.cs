using LastSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// Floating left-thumb joystick. Touching anywhere in this area moves the joystick base
    /// under the thumb; dragging moves the knob and sends movement to the PlayerInputRouter.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler,
        IInitializePotentialDragHandler
    {
        [SerializeField] private RectTransform baseRect;
        [SerializeField] private RectTransform knob;
        [Tooltip("Knob travel in canvas units.")]
        [SerializeField, Min(10f)] private float radius = 110f;
        [SerializeField, Range(0f, 0.5f)] private float deadZone = 0.12f;
        [SerializeField] private bool floating = true;

        [Header("Visuals")]
        [SerializeField] private Graphic baseGraphic;
        [SerializeField, Range(0f, 1f)] private float idleAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 0.75f;

        private const int NoPointer = int.MinValue;
        private RectTransform area;
        private Vector3 restLocalPosition;
        private int pointerId = NoPointer;

        public PlayerInputRouter Router { get; set; }
        public Vector2 Value { get; private set; }
        public bool IsHeld => pointerId != NoPointer;

        private void Awake()
        {
            area = (RectTransform)transform;
            if (baseRect != null) restLocalPosition = baseRect.localPosition;
            SetAlpha(idleAlpha);
        }

        private void OnDisable() => Release();

        public void OnInitializePotentialDrag(PointerEventData eventData) => eventData.useDragThreshold = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pointerId != NoPointer) return;
            pointerId = eventData.pointerId;

            if (floating && baseRect != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                baseRect.localPosition = local;
            }
            SetAlpha(activeAlpha);
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != pointerId || baseRect == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(baseRect, eventData.position, eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 offset = Vector2.ClampMagnitude(local, radius);
            if (knob != null) knob.anchoredPosition = offset;

            Vector2 v = offset / radius;
            float m = v.magnitude;
            v = m < deadZone ? Vector2.zero : v / m * ((m - deadZone) / (1f - deadZone));
            SetValue(v);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == pointerId) Release();
        }

        private void Release()
        {
            pointerId = NoPointer;
            if (knob != null) knob.anchoredPosition = Vector2.zero;
            if (baseRect != null) baseRect.localPosition = restLocalPosition;
            SetAlpha(idleAlpha);
            SetValue(Vector2.zero);
        }

        private void SetValue(Vector2 v)
        {
            Value = v;
            if (Router != null) Router.SetMobileMove(v);
        }

        private void SetAlpha(float a)
        {
            if (baseGraphic == null) return;
            Color c = baseGraphic.color;
            c.a = a;
            baseGraphic.color = c;
        }
    }
}
