using System.Collections;
using LastSignal.Audio;
using LastSignal.GameFlow;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Hinged door. Can be locked by an inventory item (key) and/or a world flag (e.g. power).
    /// Opens once and stays open; the open state survives checkpoint reloads.
    /// While closed, an optional NavMeshObstacle stops The Echo walking through it.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "door";
        [SerializeField] private Transform hinge;
        [SerializeField] private float openAngle = 100f;
        [Tooltip("How long the door takes to swing open; the creak is cut to the same length.")]
        [SerializeField] private float openSeconds = 1.5f;

        [Header("Lock")]
        [SerializeField] private string requiredItemId;
        [SerializeField] private string requiredItemName = "Key";
        [SerializeField] private bool consumeItem = true;
        [SerializeField] private string requiredFlag;
        [SerializeField] private string flagLockedMessage = "It won't open.";
        [SerializeField] private string doorName = "door";

        [Header("Events")]
        [SerializeField] private UnityEvent onOpened = new UnityEvent();

        private NavMeshObstacle obstacle;
        private bool isOpen;
        private bool moving;
        private string openPrompt, lockedPrompt, unlockPrompt, needItemMessage;

        public bool IsOpen => isOpen;
        private string OpenFlag => "door:" + id;

        private void Awake()
        {
            if (hinge == null) hinge = transform;
            obstacle = GetComponentInChildren<NavMeshObstacle>();
            openPrompt = "Open " + doorName;
            lockedPrompt = "Try " + doorName;
            unlockPrompt = "Unlock " + doorName + " (" + requiredItemName + ")";
            needItemMessage = "Locked. You need the " + requiredItemName + ".";
        }

        private void Start()
        {
            if (WorldFlags.Has(OpenFlag)) SetOpenImmediate();
        }

        public string GetPrompt(PlayerInteractor interactor)
        {
            if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag)) return lockedPrompt;
            if (!string.IsNullOrEmpty(requiredItemId))
                return interactor.Inventory != null && interactor.Inventory.Has(requiredItemId) ? unlockPrompt : lockedPrompt;
            return openPrompt;
        }

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !isOpen && !moving;

        public void Interact(PlayerInteractor interactor)
        {
            // Already unlocked once (e.g. slammed shut by a scare): just open it again.
            if (WorldFlags.Has(OpenFlag)) { Open(); return; }
            if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag))
            {
                interactor.ShowMessage(flagLockedMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.5f);
                return;
            }
            if (!string.IsNullOrEmpty(requiredItemId))
            {
                if (interactor.Inventory == null || !interactor.Inventory.Has(requiredItemId))
                {
                    interactor.ShowMessage(needItemMessage);
                    AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.5f);
                    return;
                }
                if (consumeItem) interactor.Inventory.Remove(requiredItemId);
                interactor.ShowMessage("Used the " + requiredItemName + ".");
            }
            Open();
        }

        /// <summary>Scare: an open door swings shut with a bang. It stays unlocked.</summary>
        public void Slam()
        {
            if (!isOpen || moving) return;
            StartCoroutine(SlamRoutine());
        }

        private IEnumerator SlamRoutine()
        {
            moving = true;
            Quaternion start = hinge.localRotation;
            Quaternion end = start * Quaternion.Euler(0f, -openAngle, 0f);
            for (float t = 0f; t < 0.22f; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(start, end, t / 0.22f);
                yield return null;
            }
            hinge.localRotation = end;
            isOpen = false;
            moving = false;
            if (obstacle != null) obstacle.enabled = true;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 1f);
            Enemy.NoiseSystem.Emit(transform.position, 9f);
        }

        /// <summary>An open door slowly swings shut by itself (creak, then a soft knock). It stays unlocked.</summary>
        public void SwingShut()
        {
            if (!isOpen || moving) return;
            StartCoroutine(SwingShutRoutine());
        }

        private IEnumerator SwingShutRoutine()
        {
            moving = true;
            AudioManager.Instance?.PlayAt(PlaceholderClips.DoorCreak, transform.position, 0.45f);
            Quaternion start = hinge.localRotation;
            Quaternion end = start * Quaternion.Euler(0f, -openAngle, 0f);
            const float seconds = 2.4f;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                // Slow at first, gathering speed as it swings in.
                float k = t / seconds;
                hinge.localRotation = Quaternion.Slerp(start, end, k * k);
                yield return null;
            }
            hinge.localRotation = end;
            isOpen = false;
            moving = false;
            if (obstacle != null) obstacle.enabled = true;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.45f);
        }

        /// <summary>Open from code or events (no lock checks).</summary>
        public void Open()
        {
            if (isOpen || moving) return;
            WorldFlags.Set(OpenFlag);
            StartCoroutine(OpenRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            moving = true;
            AudioManager.Instance?.PlayAt(PlaceholderClips.DoorCreak, transform.position, 0.7f);
            Quaternion start = hinge.localRotation;
            Quaternion end = start * Quaternion.Euler(0f, openAngle, 0f);
            for (float t = 0f; t < openSeconds; t += Time.deltaTime)
            {
                hinge.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / openSeconds));
                yield return null;
            }
            hinge.localRotation = end;
            Finish();
            onOpened.Invoke();
        }

        /// <summary>Restore an already-opened door silently (no events, no sound).</summary>
        private void SetOpenImmediate()
        {
            hinge.localRotation *= Quaternion.Euler(0f, openAngle, 0f);
            Finish();
        }

        private void Finish()
        {
            isOpen = true;
            moving = false;
            if (obstacle != null) obstacle.enabled = false;
        }
    }
}
