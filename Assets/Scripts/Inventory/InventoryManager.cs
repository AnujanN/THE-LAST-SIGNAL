using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastSignal.Inventory
{
    /// <summary>
    /// Simple item store: keys, fuses, components and recording fragments.
    /// No crafting or slots; items are counted by id.
    /// </summary>
    public class InventoryManager : MonoBehaviour
    {
        [Serializable]
        public class Entry
        {
            public string id;
            public string displayName;
            public int count;
        }

        [SerializeField] private List<Entry> items = new List<Entry>();

        public IReadOnlyList<Entry> Items => items;

        /// <summary>Raised with the entry after its count increases.</summary>
        public event Action<Entry> ItemAdded;
        /// <summary>Raised with the entry after its count decreases (count may be 0).</summary>
        public event Action<Entry> ItemRemoved;

        public void Add(string id, string displayName, int amount = 1)
        {
            if (string.IsNullOrEmpty(id) || amount <= 0) return;
            Entry e = Find(id);
            if (e == null)
            {
                e = new Entry { id = id, displayName = displayName, count = 0 };
                items.Add(e);
            }
            e.count += amount;
            ItemAdded?.Invoke(e);
        }

        public bool Remove(string id, int amount = 1)
        {
            Entry e = Find(id);
            if (e == null || e.count < amount) return false;
            e.count -= amount;
            if (e.count == 0) items.Remove(e);
            ItemRemoved?.Invoke(e);
            return true;
        }

        public bool Has(string id, int minimum = 1) => Count(id) >= minimum;

        /// <summary>Copy of the current items (for checkpoints).</summary>
        public List<Entry> Snapshot()
        {
            var copy = new List<Entry>(items.Count);
            foreach (Entry e in items) copy.Add(new Entry { id = e.id, displayName = e.displayName, count = e.count });
            return copy;
        }

        /// <summary>Replace the inventory with saved items. Raises ItemRemoved/ItemAdded so listeners refresh.</summary>
        public void RestoreFrom(List<Entry> saved)
        {
            var old = new List<Entry>(items);
            items.Clear();
            foreach (Entry e in old) { e.count = 0; ItemRemoved?.Invoke(e); }
            if (saved == null) return;
            foreach (Entry e in saved)
                if (e != null && e.count > 0) Add(e.id, e.displayName, e.count);
        }

        public int Count(string id)
        {
            Entry e = Find(id);
            return e != null ? e.count : 0;
        }

        private Entry Find(string id)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].id == id) return items[i];
            return null;
        }
    }
}
