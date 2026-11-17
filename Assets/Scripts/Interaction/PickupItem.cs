using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// An object the player picks up into the inventory (keys, fuses, recordings, the flashlight).
    /// The object is deactivated rather than destroyed so checkpoints can restore it later.
    /// </summary>
    public class PickupItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string itemId = "item";
        [SerializeField] private string displayName = "Item";
        [SerializeField, Min(1)] private int amount = 1;
        [SerializeField] private string promptVerb = "Pick up";
        [Tooltip("Unique per object (e.g. tape_1). When set, the pickup stays gone after checkpoint reloads.")]
        [SerializeField] private string saveId;

        [Header("Feedback")]
        [Tooltip("Optional. A generated placeholder sound plays when empty.")]
        [SerializeField] private AudioClip pickupSound;
        [SerializeField, Range(0f, 1f)] private float volume = 0.8f;
        [SerializeField] private UnityEvent onPickedUp = new UnityEvent();

        private string prompt;
        private string message;

        public string ItemId => itemId;
        public string DisplayName => displayName;

        private void Awake() => BuildStrings();

        private void Start()
        {
            GameFlow.GameFlowManager flow = GameFlow.GameFlowManager.Instance;
            if (!string.IsNullOrEmpty(saveId) && flow != null && flow.HasFlag(TakenFlag)) gameObject.SetActive(false);
        }

        private string TakenFlag => "taken:" + saveId;

        /// <summary>Set up the item from code (runtime spawns, tests).</summary>
        public void Configure(string id, string name, int count = 1)
        {
            itemId = id;
            displayName = name;
            amount = Mathf.Max(1, count);
            BuildStrings();
        }

        private void BuildStrings()
        {
            prompt = promptVerb + " " + displayName;
            message = "Picked up " + displayName;
        }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor.Inventory != null) interactor.Inventory.Add(itemId, displayName, amount);
            interactor.ShowMessage(message);
            if (!string.IsNullOrEmpty(saveId)) GameFlow.GameFlowManager.Instance?.SetFlag(TakenFlag);

            AudioClip clip = pickupSound != null ? pickupSound : PlaceholderAudio.Pickup;
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);

            onPickedUp.Invoke();
            gameObject.SetActive(false);
        }
    }
}
