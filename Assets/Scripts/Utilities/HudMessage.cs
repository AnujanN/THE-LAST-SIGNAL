using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Utilities
{
    /// <summary>Show a short HUD message from anywhere (puzzles, scripted events).</summary>
    public static class HudMessage
    {
        private static PlayerInteractor cached;

        public static void Show(string text)
        {
            if (cached == null) cached = Object.FindAnyObjectByType<PlayerInteractor>();
            if (cached != null) cached.ShowMessage(text);
        }
    }
}
