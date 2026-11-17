using System;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Current objective (one at a time, short text). Lives on the persistent [GameFlow] object
    /// and is saved with checkpoints.
    /// </summary>
    public class ObjectiveManager : MonoBehaviour
    {
        public static ObjectiveManager Instance { get; private set; }

        public string CurrentId { get; private set; }
        public string CurrentText { get; private set; }

        /// <summary>Raised with the new text. The bool is true for a new objective (HUD flashes it).</summary>
        public event Action<string, bool> Changed;

        private void Awake() => Instance = this;

        public void Set(string id, string text)
        {
            if (id == CurrentId && text == CurrentText) return;
            CurrentId = id;
            CurrentText = text;
            Changed?.Invoke(text, true);
        }

        /// <summary>Restore without the "new objective" flash (checkpoint reload).</summary>
        public void Restore(string id, string text)
        {
            CurrentId = id;
            CurrentText = text;
            Changed?.Invoke(text, false);
        }

        /// <summary>"id|text" form for UnityEvents with one string argument.</summary>
        public void SetPacked(string packed)
        {
            if (string.IsNullOrEmpty(packed)) return;
            int bar = packed.IndexOf('|');
            if (bar < 0) Set(packed, packed);
            else Set(packed.Substring(0, bar), packed.Substring(bar + 1));
        }
    }
}
