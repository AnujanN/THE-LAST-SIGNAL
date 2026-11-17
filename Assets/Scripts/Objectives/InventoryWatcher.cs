using LastSignal.GameFlow;
using LastSignal.Inventory;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Objectives
{
    /// <summary>
    /// Watches the inventory for an item count (e.g. 3 tape fragments), shows progress
    /// messages and fires once when reached. Remembered through a world flag.
    /// </summary>
    public class InventoryWatcher : MonoBehaviour
    {
        [SerializeField] private string id = "watch";
        [SerializeField] private string itemId = "tape_fragment";
        [SerializeField, Min(1)] private int requiredCount = 3;
        [SerializeField] private string progressLabel = "Tape fragments";
        [SerializeField] private UnityEvent onReached = new UnityEvent();

        private InventoryManager inventory;
        private string Flag => "watch:" + id;

        private void Start()
        {
            inventory = FindAnyObjectByType<InventoryManager>();
            if (inventory != null) inventory.ItemAdded += OnAdded;
        }

        private void OnDestroy()
        {
            if (inventory != null) inventory.ItemAdded -= OnAdded;
        }

        private void OnAdded(InventoryManager.Entry e)
        {
            if (e.id != itemId || WorldFlags.Has(Flag)) return;
            if (!string.IsNullOrEmpty(progressLabel)) HudMessage.Show(progressLabel + " " + Mathf.Min(e.count, requiredCount) + "/" + requiredCount);
            if (e.count < requiredCount) return;
            WorldFlags.Set(Flag);
            onReached.Invoke();
        }
    }
}
