using System;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Player;
using LastSignal.Utilities;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Authored hiding place (locker, under a desk). Entering locks movement and places the
    /// player at the hide point; The Echo cannot see a hidden player. Use again to leave.
    /// A torch left on inside shines out through the vents and gaps: a faint glow outside the front
    /// that The Echo can notice (see EchoAIController).
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string spotName = "locker";
        [Tooltip("Player feet position while hidden; its forward is the view direction.")]
        [SerializeField] private Transform hidePoint;
        [Tooltip("Where the player stands after leaving.")]
        [SerializeField] private Transform exitPoint;
        [SerializeField] private bool crouchWhileHidden;

        /// <summary>The spot the player is in, or null.</summary>
        public static HidingSpot Current { get; private set; }
        public static bool PlayerHidden => Current != null;
        public static event Action<bool> HiddenChanged;

        private string enterPrompt, exitPrompt;

        /// <summary>Where The Echo stands to inspect this spot (in front of it).</summary>
        public Vector3 InspectPosition => (exitPoint != null ? exitPoint : transform).position;
        private PlayerController hiddenPlayer;
        private FlashlightController hiddenTorch;
        private Light leakGlow;

        /// <summary>The side the light leaks out of (the front, where the doors and vents are).</summary>
        public Vector3 Front
        {
            get
            {
                Vector3 f = InspectPosition - transform.position;
                f.y = 0f;
                return f.sqrMagnitude > 1e-4f ? f.normalized : transform.forward;
            }
        }

        /// <summary>Where torch light escapes: the vents at eye height (low down for crouching spots).</summary>
        public Vector3 LeakPoint
        {
            get
            {
                Vector3 p = (hidePoint != null ? hidePoint : transform).position;
                return p + Vector3.up * (crouchWhileHidden ? 0.5f : 1.55f) + Front * 0.35f;
            }
        }

        /// <summary>The hidden player's torch is on, so light is leaking out.</summary>
        public bool LightLeaking => Current == this && hiddenTorch != null && hiddenTorch.IsOn;

        private void Awake()
        {
            enterPrompt = "Hide in " + spotName;
            exitPrompt = "Leave " + spotName;
        }

        private void OnDisable()
        {
            if (Current == this) Leave();
        }

        public string GetPrompt(PlayerInteractor interactor) => Current == this ? exitPrompt : enterPrompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && (Current == null || Current == this);

        public void Interact(PlayerInteractor interactor)
        {
            if (Current == this) Leave();
            else Enter(interactor.GetComponent<PlayerController>());
        }

        public void Enter(PlayerController player)
        {
            if (player == null || Current != null) return;
            hiddenPlayer = player;
            Current = this;
            player.MovementLocked = true;
            if (crouchWhileHidden) player.SetCrouching(true);
            Transform p = hidePoint != null ? hidePoint : transform;
            player.Teleport(p.position);
            player.GetComponent<PlayerLook>()?.SetAngles(p.eulerAngles.y, 0f);
            hiddenTorch = player.GetComponentInChildren<FlashlightController>();
            if (hiddenTorch != null && hiddenTorch.IsOn) HudMessage.Show("Your torch shines out through the gaps. Turn it off.");
            NoiseSystem.Emit(p.position, 1.5f);
            AudioManager.Instance?.PlayAt(PlaceholderClips.DoorCreak, p.position, 0.4f);
            HiddenChanged?.Invoke(true);
        }

        public void Leave()
        {
            if (Current != this) return;
            Current = null;
            if (hiddenPlayer != null)
            {
                Transform p = exitPoint != null ? exitPoint : transform;
                hiddenPlayer.Teleport(p.position);
                hiddenPlayer.GetComponent<PlayerLook>()?.SetAngles(p.eulerAngles.y, 0f);
                hiddenPlayer.MovementLocked = false;
                hiddenPlayer.SetCrouching(false);
                hiddenPlayer = null;
            }
            hiddenTorch = null;
            if (leakGlow != null) leakGlow.enabled = false;
            HiddenChanged?.Invoke(false);
        }

        private void Update()
        {
            bool leaking = LightLeaking;
            if (leaking && leakGlow == null)
            {
                // Faint warm light just outside the vents (created on first use; most spots never need it).
                var go = new GameObject("TorchLeak");
                go.transform.SetParent(transform, false);
                leakGlow = go.AddComponent<Light>();
                leakGlow.type = LightType.Point;
                leakGlow.color = new Color(1f, 0.93f, 0.8f);
                leakGlow.range = 1.8f;
                leakGlow.intensity = 1.2f;
                leakGlow.shadows = LightShadows.None;
            }
            if (leakGlow == null) return;
            if (leaking) leakGlow.transform.position = LeakPoint + Front * 0.25f;
            if (leakGlow.enabled != leaking) leakGlow.enabled = leaking;
        }

        /// <summary>Clear static state (scene loads, tests).</summary>
        public static void ResetStatic() => Current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            Current = null;
            HiddenChanged = null;
        }
    }
}
