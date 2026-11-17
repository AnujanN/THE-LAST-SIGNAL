using System;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// First-person movement on a CharacterController: walk, sprint, toggle crouch and gravity.
    /// The transform pivot sits at the player's feet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter input;
        [Tooltip("Camera pivot whose local height follows standing/crouching eye height.")]
        [SerializeField] private Transform cameraRoot;

        [Header("Speed (m/s)")]
        [SerializeField] private float walkSpeed = 2.2f;
        [SerializeField] private float sprintSpeed = 4.0f;
        [SerializeField] private float crouchSpeed = 1.2f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private bool canSprint = true;

        [Header("Height (m)")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.0f;
        [SerializeField] private float standingEyeHeight = 1.65f;
        [SerializeField] private float crouchEyeHeight = 0.85f;
        [SerializeField] private float crouchTransitionSpeed = 8f;
        [Tooltip("Layers that can block standing up.")]
        [SerializeField] private LayerMask headroomMask = ~0;

        [Header("Gravity")]
        [SerializeField] private float gravity = -15f;

        private CharacterController controller;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;
        private float currentHeight;
        private readonly Collider[] overlapBuffer = new Collider[8];

        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsGrounded => controller != null && controller.isGrounded;
        /// <summary>Horizontal speed in m/s; used later for footstep noise.</summary>
        public float HorizontalSpeed => new Vector2(horizontalVelocity.x, horizontalVelocity.z).magnitude;
        /// <summary>When true the player cannot move (hiding, cutscenes). Gravity still applies.</summary>
        public bool MovementLocked { get; set; }
        public bool CanSprint { get => canSprint; set => canSprint = value; }

        public event Action<bool> CrouchChanged;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputRouter>();
            currentHeight = standingHeight;
            ApplyHeight(standingHeight, standingEyeHeight);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (input != null && input.CrouchPressed) SetCrouching(!IsCrouching);

            Vector2 move = (input != null && !MovementLocked) ? input.Move : Vector2.zero;
            bool wantsSprint = input != null && input.SprintActive && canSprint && move.sqrMagnitude > 0.01f;

            // Sprinting from a crouch stands the player up if there is room.
            if (wantsSprint && IsCrouching) SetCrouching(false);
            IsSprinting = wantsSprint && !IsCrouching;

            float speed = IsCrouching ? crouchSpeed : (IsSprinting ? sprintSpeed : walkSpeed);
            Vector3 target = (transform.right * move.x + transform.forward * move.y) * speed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, target, acceleration * dt);

            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * dt;

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);

            UpdateHeight(dt);
        }

        /// <summary>Crouch or stand. Standing is refused if something is overhead.</summary>
        public bool SetCrouching(bool crouch)
        {
            if (crouch == IsCrouching) return true;
            if (!crouch && !HasHeadroom()) return false;

            IsCrouching = crouch;
            if (crouch && input != null) input.CancelMobileSprint();
            CrouchChanged?.Invoke(IsCrouching);
            return true;
        }

        /// <summary>Move the player instantly (spawns, checkpoints).</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
        }

        private void UpdateHeight(float dt)
        {
            float targetHeight = IsCrouching ? crouchHeight : standingHeight;
            if (Mathf.Approximately(currentHeight, targetHeight)) return;

            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight,
                crouchTransitionSpeed * (standingHeight - crouchHeight) * dt);
            float t = Mathf.InverseLerp(crouchHeight, standingHeight, currentHeight);
            ApplyHeight(currentHeight, Mathf.Lerp(crouchEyeHeight, standingEyeHeight, t));
        }

        private void ApplyHeight(float height, float eyeHeight)
        {
            controller.height = height;
            controller.center = new Vector3(0f, height * 0.5f, 0f);
            if (cameraRoot != null)
            {
                Vector3 p = cameraRoot.localPosition;
                cameraRoot.localPosition = new Vector3(p.x, eyeHeight, p.z);
            }
        }

        private bool HasHeadroom()
        {
            float radius = controller.radius * 0.95f;
            Vector3 feet = transform.position;
            Vector3 bottom = feet + Vector3.up * (radius + controller.skinWidth + 0.05f);
            Vector3 top = feet + Vector3.up * (standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlapBuffer,
                headroomMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = overlapBuffer[i];
                if (c != null && c != controller && !c.transform.IsChildOf(transform)) return false;
            }
            return true;
        }
    }
}
