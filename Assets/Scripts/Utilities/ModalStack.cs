using System;
using System.Collections.Generic;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Utilities
{
    /// <summary>
    /// Tracks open full-screen UI (pause, notes, inventory, game over). While any is open,
    /// time is frozen, gameplay input is blocked and the cursor is released.
    /// </summary>
    public static class ModalStack
    {
        private static readonly List<object> open = new List<object>();
        private static readonly List<object> pausesAudio = new List<object>();

        public static bool AnyOpen => open.Count > 0;
        public static object Top => open.Count > 0 ? open[open.Count - 1] : null;
        public static event Action<bool> Changed;

        public static void Open(object owner, bool pauseAudio = false)
        {
            if (owner == null || open.Contains(owner)) return;
            open.Add(owner);
            if (pauseAudio) pausesAudio.Add(owner);
            Apply();
        }

        public static void Close(object owner)
        {
            if (!open.Remove(owner)) return;
            pausesAudio.Remove(owner);
            Apply();
        }

        public static bool IsOpen(object owner) => open.Contains(owner);

        public static void Reset()
        {
            open.Clear();
            pausesAudio.Clear();
            Apply();
        }

        private static void Apply()
        {
            bool any = open.Count > 0;
            Time.timeScale = any ? 0f : 1f;
            AudioListener.pause = pausesAudio.Count > 0;
            PlayerInputRouter router = UnityEngine.Object.FindAnyObjectByType<PlayerInputRouter>();
            if (router != null) router.GameplayBlocked = any;
            if (any) PlayerInputRouter.ReleaseCursor();
            Changed?.Invoke(any);
        }
    }
}
