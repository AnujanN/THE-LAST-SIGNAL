using LastSignal.Audio;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Enemy
{
    /// <summary>
    /// Player footsteps: plays a step sound and reports noise. Sprinting is loud, walking is
    /// moderate, crouching is nearly silent. Water and similar surfaces multiply the radius.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class NoiseEmitter : MonoBehaviour
    {
        [Header("Noise radius per step (m)")]
        [SerializeField] private float sprintRadius = 10f;
        [SerializeField] private float walkRadius = 4f;
        [SerializeField] private float crouchRadius = 1.2f;

        [Header("Step timing (m between steps)")]
        [SerializeField] private float strideLength = 1.6f;
        [SerializeField] private AudioSource stepSource;

        private PlayerController player;
        private float distanceSinceStep;

        /// <summary>Set by surface zones such as shallow water (1 = normal).</summary>
        public float SurfaceMultiplier { get; set; } = 1f;
        public AudioClip SurfaceClip { get; set; }
        /// <summary>Radius of the most recent step, for HUD hints and tests.</summary>
        public float LastNoiseRadius { get; private set; }

        private void Awake() => player = GetComponent<PlayerController>();

        private void Update()
        {
            if (!player.IsGrounded || player.MovementLocked) return;
            float speed = player.HorizontalSpeed;
            if (speed < 0.2f) return;

            distanceSinceStep += speed * Time.deltaTime;
            float stride = player.IsSprinting ? strideLength * 1.3f : player.IsCrouching ? strideLength * 0.7f : strideLength;
            if (distanceSinceStep < stride) return;
            distanceSinceStep = 0f;

            float radius = player.IsSprinting ? sprintRadius : player.IsCrouching ? crouchRadius : walkRadius;
            radius *= SurfaceMultiplier;
            LastNoiseRadius = radius;
            NoiseSystem.Emit(transform.position, radius);

            if (stepSource != null)
            {
                stepSource.pitch = Random.Range(0.9f, 1.1f);
                float vol = player.IsSprinting ? 0.8f : player.IsCrouching ? 0.2f : 0.45f;
                stepSource.PlayOneShot(SurfaceClip != null ? SurfaceClip : PlaceholderClips.Footstep, vol);
            }
        }
    }
}
