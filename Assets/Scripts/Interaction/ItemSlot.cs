using LastSignal.Audio;
using LastSignal.GameFlow;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A place that takes a specific item: fuse socket, generator part, transmitter module.
    /// Shows what is needed, consumes the item and reveals an "installed" visual.
    /// </summary>
    public class ItemSlot : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "slot";
        [SerializeField] private string requiredItemId = "fuse";
        [SerializeField] private string requiredItemName = "Fuse";
        [SerializeField] private string slotName = "socket";
        [SerializeField] private bool consumeItem = true;
        [SerializeField] private GameObject installedVisual;
        [SerializeField] private GameObject emptyVisual;
        [SerializeField] private UnityEvent onInstalled = new UnityEvent();

        private string installPrompt, emptyPrompt, needMessage;

        public bool IsInstalled { get; private set; }
        public string RequiredItemName => requiredItemName;
        private string Flag => "slot:" + id;

        private void Awake()
        {
            installPrompt = "Install " + requiredItemName;
            emptyPrompt = "Inspect " + slotName;
            needMessage = "The " + slotName + " needs a " + requiredItemName + ".";
            ApplyVisual();
        }

        private void Start()
        {
            // Restore silently: events already ran before the checkpoint (they may set objectives).
            if (WorldFlags.Has(Flag)) { IsInstalled = true; ApplyVisual(); }
        }

        public string GetPrompt(PlayerInteractor interactor) =>
            interactor.Inventory != null && interactor.Inventory.Has(requiredItemId) ? installPrompt : emptyPrompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !IsInstalled;

        public void Interact(PlayerInteractor interactor)
        {
            if (interactor.Inventory == null || !interactor.Inventory.Has(requiredItemId))
            {
                interactor.ShowMessage(needMessage);
                return;
            }
            if (consumeItem) interactor.Inventory.Remove(requiredItemId);
            interactor.ShowMessage(requiredItemName + " installed.");
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.6f);
            Install();
        }

        /// <summary>Install from code (tests, scripted events).</summary>
        public void Install()
        {
            if (IsInstalled) return;
            IsInstalled = true;
            WorldFlags.Set(Flag);
            ApplyVisual();
            onInstalled.Invoke();
        }

        private void ApplyVisual()
        {
            if (installedVisual != null) installedVisual.SetActive(IsInstalled);
            if (emptyVisual != null) emptyVisual.SetActive(!IsInstalled);
        }
    }
}
