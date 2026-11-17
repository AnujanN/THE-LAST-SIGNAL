using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace LastSignal.Player
{
    /// <summary>One-shot actions that on-screen buttons (or tests) can trigger.</summary>
    public enum PlayerAction
    {
        Interact,
        Crouch,
        Flashlight,
        Pause,
        Inventory
    }

    /// <summary>
    /// Merges keyboard/mouse input (Editor and PC fallback) with on-screen mobile controls
    /// into one per-frame input state that gameplay scripts read.
    /// Runs before other player scripts so they all see the same values in a frame.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputRouter : MonoBehaviour
    {
        [Header("Keyboard / mouse fallback")]
        [Tooltip("Read keyboard and mouse. Leave on: it is ignored on devices without them.")]
        [SerializeField] private bool enableKeyboardMouse = true;
        [Tooltip("Lock and hide the cursor when the player clicks the game view (Editor/PC).")]
        [SerializeField] private bool lockCursorOnClick = true;

        // Values written by mobile UI between frames.
        private Vector2 mobileMove;
        private Vector2 pendingTouchLookPixels;
        private bool mobileSprintToggled;
        private bool mobileWasMoving;
        private bool pendingInteract, pendingCrouch, pendingFlashlight, pendingPause, pendingInventory;

        /// <summary>Movement on X (strafe) and Y (forward), magnitude 0..1.</summary>
        public Vector2 Move { get; private set; }
        /// <summary>Mouse movement this frame, in screen pixels.</summary>
        public Vector2 MouseLookPixels { get; private set; }
        /// <summary>Touch-drag movement this frame, as a fraction of screen height.</summary>
        public Vector2 TouchLookScreenFraction { get; private set; }
        public bool SprintActive { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool CrouchPressed { get; private set; }
        public bool FlashlightPressed { get; private set; }
        public bool PausePressed { get; private set; }
        /// <summary>Works while gameplay is blocked, so the same button can close the inventory.</summary>
        public bool InventoryPressed { get; private set; }

        /// <summary>When true (e.g. paused), gameplay input reads as zero. Pause still works.</summary>
        public bool GameplayBlocked { get; set; }
        public bool MobileSprintToggled => mobileSprintToggled;
        /// <summary>Read physical keyboard/mouse. Tests turn this off so real devices cannot interfere.</summary>
        public bool KeyboardMouseEnabled { get => enableKeyboardMouse; set => enableKeyboardMouse = value; }

        // ----- API for mobile controls and tests -----

        public void SetMobileMove(Vector2 value) => mobileMove = Vector2.ClampMagnitude(value, 1f);
        public void AddTouchLook(Vector2 screenPixels) => pendingTouchLookPixels += screenPixels;
        public void ToggleMobileSprint() => mobileSprintToggled = !mobileSprintToggled;
        public void CancelMobileSprint() => mobileSprintToggled = false;

        public void Press(PlayerAction action)
        {
            switch (action)
            {
                case PlayerAction.Interact: pendingInteract = true; break;
                case PlayerAction.Crouch: pendingCrouch = true; break;
                case PlayerAction.Flashlight: pendingFlashlight = true; break;
                case PlayerAction.Pause: pendingPause = true; break;
                case PlayerAction.Inventory: pendingInventory = true; break;
            }
        }

        private void Update()
        {
            Vector2 move = mobileMove;
            Vector2 mouseLook = Vector2.zero;
            bool sprintHeld = false;
            bool interact = pendingInteract, crouch = pendingCrouch;
            bool flashlight = pendingFlashlight, pause = pendingPause, inventory = pendingInventory;
            pendingInteract = pendingCrouch = pendingFlashlight = pendingPause = pendingInventory = false;

            if (enableKeyboardMouse)
            {
                ReadKeyboardMouse(ref move, ref mouseLook, ref sprintHeld, ref interact, ref crouch, ref flashlight, ref pause);
                Keyboard kb = Keyboard.current;
                if (kb != null) inventory |= kb.iKey.wasPressedThisFrame || kb.tabKey.wasPressedThisFrame;
            }

            // Mobile sprint is a toggle; it switches itself off when the player stops moving.
            bool mobileMoving = mobileMove.sqrMagnitude > 0.01f;
            if (mobileSprintToggled && mobileWasMoving && !mobileMoving) mobileSprintToggled = false;
            mobileWasMoving = mobileMoving;

            float screenHeight = Mathf.Max(1, Screen.height);
            Vector2 touchLook = pendingTouchLookPixels / screenHeight;
            pendingTouchLookPixels = Vector2.zero;

            PausePressed = pause;
            InventoryPressed = inventory;
            if (GameplayBlocked)
            {
                Move = Vector2.zero;
                MouseLookPixels = Vector2.zero;
                TouchLookScreenFraction = Vector2.zero;
                SprintActive = InteractPressed = CrouchPressed = FlashlightPressed = false;
                return;
            }

            Move = Vector2.ClampMagnitude(move, 1f);
            MouseLookPixels = mouseLook;
            TouchLookScreenFraction = touchLook;
            SprintActive = sprintHeld || mobileSprintToggled;
            InteractPressed = interact;
            CrouchPressed = crouch;
            FlashlightPressed = flashlight;
        }

        private void ReadKeyboardMouse(ref Vector2 move, ref Vector2 look, ref bool sprint,
            ref bool interact, ref bool crouch, ref bool flashlight, ref bool pause)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                Vector2 k = Vector2.zero;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) k.y += 1f;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) k.y -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) k.x += 1f;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) k.x -= 1f;
                if (k != Vector2.zero) move = k.normalized;

                sprint |= kb.leftShiftKey.isPressed;
                interact |= kb.eKey.wasPressedThisFrame;
                crouch |= kb.cKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame;
                flashlight |= kb.fKey.wasPressedThisFrame;
                pause |= kb.escapeKey.wasPressedThisFrame;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null || Application.isMobilePlatform) return;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                look = mouse.delta.ReadValue();
            }
            else if (lockCursorOnClick && !GameplayBlocked && mouse.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private static bool IsPointerOverUI()
        {
            EventSystem es = EventSystem.current;
            return es != null && es.IsPointerOverGameObject();
        }

        public static void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
