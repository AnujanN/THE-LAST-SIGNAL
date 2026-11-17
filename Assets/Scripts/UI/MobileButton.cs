using LastSignal.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// On-screen action button. Fires on touch-down for quick response.
    /// Sprint is a toggle so the player does not need a third finger.
    /// </summary>
    public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum Kind { Interact, Crouch, Flashlight, Pause, SprintToggle, Inventory }

        [SerializeField] private Kind kind;
        [SerializeField] private Graphic background;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color highlightColor = new Color(1f, 0.85f, 0.4f, 0.55f);
        [SerializeField] private Color pressedColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color disabledColor = new Color(1f, 1f, 1f, 0.1f);

        private bool pressed;
        private bool highlighted;
        private bool usable = true;

        public Kind ButtonKind => kind;
        public PlayerInputRouter Router { get; set; }

        /// <summary>Highlight when the action is available or active (e.g. something to interact with).</summary>
        public void SetHighlighted(bool value)
        {
            if (highlighted == value) return;
            highlighted = value;
            Refresh();
        }

        /// <summary>Dim the button when its action cannot be used (e.g. no flashlight yet).</summary>
        public void SetUsable(bool value)
        {
            if (usable == value) return;
            usable = value;
            Refresh();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            pressed = true;
            Refresh();
            if (Router == null) return;
            switch (kind)
            {
                case Kind.Interact: Router.Press(PlayerAction.Interact); break;
                case Kind.Crouch: Router.Press(PlayerAction.Crouch); break;
                case Kind.Flashlight: Router.Press(PlayerAction.Flashlight); break;
                case Kind.Pause: Router.Press(PlayerAction.Pause); break;
                case Kind.SprintToggle: Router.ToggleMobileSprint(); break;
                case Kind.Inventory: Router.Press(PlayerAction.Inventory); break;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            pressed = false;
            Refresh();
        }

        private void OnDisable()
        {
            pressed = false;
            Refresh();
        }

        private void Refresh()
        {
            if (background == null) return;
            background.color = pressed ? pressedColor : !usable ? disabledColor : highlighted ? highlightColor : normalColor;
        }
    }
}
