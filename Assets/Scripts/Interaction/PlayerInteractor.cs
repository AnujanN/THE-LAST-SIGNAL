using System;
using LastSignal.Inventory;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Casts from the camera centre to find the IInteractable the player is looking at,
    /// exposes it for the HUD prompt, and uses it when Interact is pressed.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private PlayerInputRouter input;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private FlashlightController flashlight;

        [Header("Detection")]
        [SerializeField, Range(0.5f, 5f)] private float range = 2.2f;
        [Tooltip("A small sphere instead of a thin ray makes targeting easier on touch screens.")]
        [SerializeField, Range(0f, 0.3f)] private float castRadius = 0.12f;
        [SerializeField] private LayerMask interactMask = ~0;

        private Collider lastCollider;
        private IInteractable lastHitInteractable;
        private readonly RaycastHit[] hits = new RaycastHit[8];

        public IInteractable Current { get; private set; }
        /// <summary>World point on the current target where the view ray touched it (for hand reach).</summary>
        public Vector3 TargetPoint { get; private set; }
        public InventoryManager Inventory => inventory;
        public FlashlightController Flashlight => flashlight;
        public PlayerInputRouter InputRouter => input;

        /// <summary>Raised when the looked-at interactable changes (null = nothing).</summary>
        public event Action<IInteractable> TargetChanged;
        /// <summary>Raised after a successful interaction.</summary>
        public event Action<IInteractable> Interacted;
        /// <summary>Short feedback for the HUD, e.g. "Picked up Flashlight".</summary>
        public event Action<string> Message;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputRouter>();
            if (inventory == null) inventory = GetComponent<InventoryManager>();
            if (flashlight == null) flashlight = GetComponentInChildren<FlashlightController>();
            if (viewCamera == null) viewCamera = GetComponentInChildren<Camera>();
        }

        private void Update()
        {
            IInteractable found = FindTarget();
            if (!ReferenceEquals(found, Current))
            {
                Current = found;
                TargetChanged?.Invoke(Current);
            }

            if (input != null && input.InteractPressed) TryInteract();
        }

        /// <summary>Use the current target. Returns true if something happened.</summary>
        public bool TryInteract()
        {
            if (Current == null || !IsAlive(Current) || !Current.CanInteract(this)) return false;
            IInteractable target = Current;
            target.Interact(this);
            Interacted?.Invoke(target);
            return true;
        }

        public void ShowMessage(string text) => Message?.Invoke(text);

        private IInteractable FindTarget()
        {
            // While hidden, the only thing to do is leave the hiding spot.
            if (HidingSpot.Current != null) return HidingSpot.Current;
            if (viewCamera == null) return null;
            Transform cam = viewCamera.transform;
            Ray ray = new Ray(cam.position, cam.forward);

            // The camera sits inside the player's own capsule, so a single cast can report the player
            // first. Collect every hit and use the nearest one that is not part of the player.
            int count = castRadius > 0f
                ? Physics.SphereCastNonAlloc(ray, castRadius, hits, range, interactMask, QueryTriggerInteraction.Collide)
                : Physics.RaycastNonAlloc(ray, hits, range, interactMask, QueryTriggerInteraction.Collide);

            int nearest = -1;
            for (int i = 0; i < count; i++)
            {
                Collider c = hits[i].collider;
                if (c == null || c.transform.IsChildOf(transform)) continue;
                // Invisible trigger volumes (story zones, checkpoints, water) must not block the view.
                // Triggers only count when they belong to an interactable (e.g. a pickup's grab box).
                if (c.isTrigger && c.GetComponentInParent<IInteractable>() == null) continue;
                if (nearest < 0 || hits[i].distance < hits[nearest].distance) nearest = i;
            }

            if (nearest < 0)
            {
                lastCollider = null;
                lastHitInteractable = null;
                return null;
            }
            RaycastHit info = hits[nearest];
            // A sphere cast that starts inside a collider reports a zero point; fall back to its centre.
            TargetPoint = info.distance <= 0f && info.point == Vector3.zero ? info.collider.bounds.center : info.point;

            // Only cache positive results, so an interactable added or enabled later is still found.
            if (info.collider != lastCollider || lastHitInteractable == null)
            {
                lastCollider = info.collider;
                lastHitInteractable = info.collider.GetComponentInParent<IInteractable>();
            }

            IInteractable candidate = lastHitInteractable;
            if (candidate == null || !IsAlive(candidate) || !candidate.CanInteract(this)) return null;
            return candidate;
        }

        /// <summary>Unity objects can be destroyed while we still hold the interface.</summary>
        private static bool IsAlive(IInteractable i)
        {
            if (i is UnityEngine.Object o) return o != null && (!(o is Behaviour b) || b.isActiveAndEnabled);
            return i != null;
        }
    }
}
