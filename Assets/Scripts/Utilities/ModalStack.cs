using System;
using System.Collections.Generic;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Utilities
{
    /// <summary>
    /// Tracks open full-screen UI (pause, notes, inventory, game over). While any is open,
    /// gameplay input is blocked and the cursor is released. Time is frozen unless every open
    /// panel asked not to (e.g. a keypad: The Echo keeps moving while you type).
    /// </summary>
    public static class ModalStack
    {
        private static readonly List<object> open = new List<object>();
        private static readonly List<object> pausesAudio = new List<object>();
        private static readonly List<object> keepsTime = new List<object>();

        public static bool AnyOpen => open.Count > 0;
        public static object Top => open.Count > 0 ? open[open.Count - 1] : null;
        public static event Action<bool> Changed;

        /// <summary>Fast Enter Play Mode keeps statics between sessions; start each session clean.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            open.Clear();
            pausesAudio.Clear();
            keepsTime.Clear();
            Changed = null;
        }

        public static void Open(object owner, bool pauseAudio = false, bool freezeTime = true)
        {
            if (owner == null || open.Contains(owner)) return;
            open.Add(owner);
            if (pauseAudio) pausesAudio.Add(owner);
            if (!freezeTime) keepsTime.Add(owner);
            Apply();
        }

        public static void Close(object owner)
        {
            if (!open.Remove(owner)) return;
            pausesAudio.Remove(owner);
            keepsTime.Remove(owner);
            Apply();
        }

        public static bool IsOpen(object owner) => open.Contains(owner);

        public static void Reset()
        {
            open.Clear();
            pausesAudio.Clear();
            keepsTime.Clear();
            Apply();
        }

        private static void Apply()
        {
            bool any = open.Count > 0;
            bool freeze = open.Count > keepsTime.Count;
            Time.timeScale = freeze ? 0f : 1f;
            AudioListener.pause = pausesAudio.Count > 0;
            PlayerInputRouter router = UnityEngine.Object.FindAnyObjectByType<PlayerInputRouter>();
            if (router != null) router.GameplayBlocked = any;
            if (any) PlayerInputRouter.ReleaseCursor();
            Changed?.Invoke(any);
        }
    }
}
