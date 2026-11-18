using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Subtle, physical camera motion: head bob and sway from footsteps, a slow breathing rise that
    /// quickens when The Echo is near, and stillness while holding your breath. Amplitudes are kept
    /// small to avoid motion sickness on phones.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class CameraMotion : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerBreath breath;

        [Header("Footsteps")]
        [Tooltip("Metres travelled per full left-right step cycle.")]
        [SerializeField] private float strideMetres = 1.5f;
        [SerializeField] private float walkBob = 0.03f;
        [SerializeField] private float sprintBob = 0.05f;
        [SerializeField] private float crouchBob = 0.018f;
        [SerializeField] private float rollDegrees = 0.6f;

        [Header("Breathing")]
        [SerializeField] private float breathAmplitude = 0.006f;
        [SerializeField] private float calmBreathsPerSecond = 0.25f;
        [SerializeField] private float scaredBreathsPerSecond = 0.75f;

        private Vector3 basePosition;
        private Quaternion baseRotation;
        private float stepPhase;
        private float breathPhase;
        private float amplitude;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            if (controller == null) controller = GetComponentInParent<PlayerController>();
            if (breath == null) breath = GetComponentInParent<PlayerBreath>();
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            float speed = controller != null && controller.IsGrounded ? controller.HorizontalSpeed : 0f;

            float target = 0f;
            if (speed > 0.15f && controller != null)
                target = (controller.IsCrouching ? crouchBob : controller.IsSprinting ? sprintBob : walkBob) * Mathf.Clamp01(speed / 2.2f);
            amplitude = Mathf.Lerp(amplitude, target, 1f - Mathf.Exp(-8f * dt));
            stepPhase += speed / Mathf.Max(0.1f, strideMetres) * Mathf.PI * 2f * dt;

            float threat = AudioManager.Instance != null ? AudioManager.Instance.ThreatLevel : 0f;
            bool holding = breath != null && breath.Holding;
            breathPhase += Mathf.Lerp(calmBreathsPerSecond, scaredBreathsPerSecond, threat) * Mathf.PI * 2f * dt;
            float breathe = holding ? 0f : Mathf.Sin(breathPhase) * breathAmplitude * (1f + threat);

            // Vertical dips twice per cycle (each footfall), lateral sway once per cycle.
            float y = -Mathf.Abs(Mathf.Sin(stepPhase)) * amplitude * 1.6f + amplitude * 0.8f;
            float x = Mathf.Cos(stepPhase) * amplitude * 0.5f;
            float roll = Mathf.Cos(stepPhase) * rollDegrees * (amplitude / Mathf.Max(0.0001f, walkBob));

            transform.localPosition = basePosition + new Vector3(x, y + breathe, 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, roll);
        }
    }
}
