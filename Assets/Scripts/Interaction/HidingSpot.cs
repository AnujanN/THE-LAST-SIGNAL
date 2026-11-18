using System;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Authored hiding place (locker, under a desk). Entering locks movement and places the
    /// player at the hide point; The Echo cannot see a hidden player. Use again to leave.
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
        private PlayerController hiddenPlayer;

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
            HiddenChanged?.Invoke(false);
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
