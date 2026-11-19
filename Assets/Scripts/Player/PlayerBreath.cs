using System;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Player
{
    /// <summary>
    /// Holding your breath while hidden (Space / BREATH button). The Echo can inspect a hiding spot; it only
    /// finds the player if they are breathing (not holding) or gasp when their breath runs out.
    /// </summary>
    public class PlayerBreath : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter input;
        [Tooltip("Seconds the player can hold their breath from full.")]
        [SerializeField, Min(1f)] private float maxHoldSeconds = 10f;
        [Tooltip("Seconds to recover from empty to full.")]
        [SerializeField, Min(1f)] private float recoverSeconds = 4f;
        [SerializeField] private AudioSource source;

        private float gaspTimer;
        private AudioSource breathingLoop;

        public bool Holding { get; private set; }
        public float Breath01 { get; private set; } = 1f;
        /// <summary>True for a moment after a loud gasp (ran out of breath).</summary>
        public bool RecentlyGasped => gaspTimer > 0f;
        public event Action Gasped;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputRouter>();
        }

        private void Start()
        {
            // Alex's breathing becomes audible, then ragged, as The Echo gets closer.
            AudioClip clip = PlaceholderClips.Breathing;
            if (clip == null) return;
            breathingLoop = gameObject.AddComponent<AudioSource>();
            breathingLoop.clip = clip;
            breathingLoop.loop = true;
            breathingLoop.spatialBlend = 0f;
            breathingLoop.volume = 0f;
            breathingLoop.playOnAwake = false;
            breathingLoop.Play();
        }

        private void LateUpdate()
        {
            if (breathingLoop == null) return;
            float threat = AudioManager.Instance != null ? AudioManager.Instance.ThreatLevel : 0f;
            float target = Holding ? 0f : Mathf.Clamp01((threat - 0.15f) * 1.4f) * 0.55f * Save.GameSettings.SfxVolume;
            breathingLoop.volume = Mathf.MoveTowards(breathingLoop.volume, target, Time.deltaTime * 0.8f);
            breathingLoop.pitch = Mathf.Lerp(0.9f, 1.35f, threat);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (gaspTimer > 0f) gaspTimer -= dt;
            bool wants = input != null && input.HoldBreathHeld && HidingSpot.PlayerHidden;

            if (wants && Breath01 > 0f && !RecentlyGasped)
            {
                Holding = true;
                Breath01 = Mathf.Max(0f, Breath01 - dt / maxHoldSeconds);
                if (Breath01 <= 0f) Gasp();
                return;
            }

            Holding = false;
            Breath01 = Mathf.Min(1f, Breath01 + dt / recoverSeconds);
        }

        public void SetHoldingForTests(bool holding) => Holding = holding;

        private void Gasp()
        {
            Holding = false;
            gaspTimer = 2f;
            NoiseSystem.Emit(transform.position, 5f);
            AudioClip clip = PlaceholderClips.Gasp;
            if (source != null && clip != null) source.PlayOneShot(clip, 0.9f);
            Gasped?.Invoke();
        }
    }
}
