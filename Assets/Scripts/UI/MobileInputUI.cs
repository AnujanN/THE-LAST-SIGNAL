using LastSignal.Interaction;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.UI
{
    /// <summary>
    /// Owns the on-screen controls: shows them on phones (and in the Editor when asked),
    /// connects them to the player's input router and updates button states.
    /// </summary>
    public class MobileInputUI : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene if left empty.")]
        [SerializeField] private PlayerInputRouter router;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private FlashlightController flashlight;

        [SerializeField] private GameObject controlsRoot;
        [SerializeField] private VirtualJoystick joystick;
        [SerializeField] private TouchLookArea lookArea;
        [SerializeField] private MobileButton[] buttons = new MobileButton[0];

        [Tooltip("Show touch controls in the Editor (also shown automatically in the Device Simulator).")]
        [SerializeField] private bool showInEditor;

        private MobileButton interactButton, sprintButton, flashlightButton;

        public bool ControlsVisible => controlsRoot != null && controlsRoot.activeSelf;

        private void Awake()
        {
            if (router == null) router = FindAnyObjectByType<PlayerInputRouter>();
            if (interactor == null && router != null) interactor = router.GetComponent<PlayerInteractor>();
            if (flashlight == null && router != null) flashlight = router.GetComponentInChildren<FlashlightController>();

            if (joystick != null) joystick.Router = router;
            if (lookArea != null) lookArea.Router = router;
            foreach (MobileButton b in buttons)
            {
                if (b == null) continue;
                b.Router = router;
                if (b.ButtonKind == MobileButton.Kind.Interact) interactButton = b;
                else if (b.ButtonKind == MobileButton.Kind.SprintToggle) sprintButton = b;
                else if (b.ButtonKind == MobileButton.Kind.Flashlight) flashlightButton = b;
            }

            SetVisible(Application.isMobilePlatform || (Application.isEditor && showInEditor));
        }

        public void SetVisible(bool visible)
        {
            if (controlsRoot != null) controlsRoot.SetActive(visible);
        }

        private void Update()
        {
            if (!ControlsVisible || router == null) return;
            if (interactButton != null) interactButton.SetHighlighted(interactor != null && interactor.Current != null);
            if (sprintButton != null) sprintButton.SetHighlighted(router.MobileSprintToggled);
            if (flashlightButton != null && flashlight != null)
            {
                flashlightButton.SetUsable(flashlight.IsOwned);
                flashlightButton.SetHighlighted(flashlight.IsOn);
            }
        }
    }
}
