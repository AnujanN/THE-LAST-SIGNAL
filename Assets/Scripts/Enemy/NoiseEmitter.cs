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

        private readonly System.Collections.Generic.List<float> water = new System.Collections.Generic.List<float>(4);

        /// <summary>Loudest water zone Alex is standing in (1 = dry floor).</summary>
        public float SurfaceMultiplier
        {
            get { float m = 1f; foreach (float w in water) m = Mathf.Max(m, w); return m; }
        }
        public bool InWater => water.Count > 0;

        public void EnterWater(float multiplier) => water.Add(multiplier);
        public void ExitWater(float multiplier) => water.Remove(multiplier);
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
                // Wet steps are a little louder; a fresh variation every step.
                stepSource.PlayOneShot(InWater ? PlaceholderClips.Splash : PlaceholderClips.Footstep, InWater ? vol * 1.3f : vol);
            }
        }
    }
}
