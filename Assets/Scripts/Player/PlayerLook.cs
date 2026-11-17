using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// First-person camera look. Yaw turns the player body; pitch tilts the camera pivot.
    /// Mouse and touch have separate sensitivities because their deltas are measured differently.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerLook : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter input;
        [Tooltip("Camera pivot that tilts up/down. Usually the CameraRoot child.")]
        [SerializeField] private Transform pitchPivot;

        [Header("Sensitivity")]
        [Tooltip("Degrees per mouse pixel.")]
        [SerializeField, Range(0.01f, 0.5f)] private float mouseSensitivity = 0.08f;
        [Tooltip("Degrees turned when dragging one full screen height.")]
        [SerializeField, Range(30f, 400f)] private float touchDegreesPerScreen = 160f;
        [SerializeField] private bool invertY;

        [Header("Limits")]
        [SerializeField, Range(30f, 89f)] private float maxPitch = 80f;

        private float yaw;
        private float pitch;

        /// <summary>Multiplier from the settings menu (1 = default).</summary>
        public float SensitivityMultiplier { get; set; } = 1f;
        public float Pitch => pitch;
        public float Yaw => yaw;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputRouter>();
            yaw = transform.eulerAngles.y;
            if (pitchPivot != null)
            {
                float p = pitchPivot.localEulerAngles.x;
                pitch = p > 180f ? p - 360f : p;
            }
        }

        private void Update()
        {
            if (input == null) return;

            Vector2 delta = input.MouseLookPixels * mouseSensitivity
                          + input.TouchLookScreenFraction * touchDegreesPerScreen;
            delta *= SensitivityMultiplier;
            if (delta == Vector2.zero) return;

            yaw += delta.x;
            pitch += invertY ? delta.y : -delta.y;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);
            Apply();
        }

        /// <summary>Snap the view to an angle (used by spawns, checkpoints and cutscenes).</summary>
        public void SetAngles(float newYaw, float newPitch)
        {
            yaw = newYaw;
            pitch = Mathf.Clamp(newPitch, -maxPitch, maxPitch);
            Apply();
        }

        private void Apply()
        {
            transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            if (pitchPivot != null) pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
