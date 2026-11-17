using LastSignal.Player;
using UnityEngine;

namespace LastSignal.Utilities
{
    public static class PlayerUtil
    {
        /// <summary>True if the collider belongs to the player (works for trigger callbacks).</summary>
        public static bool IsPlayer(Collider c) => c != null && c.GetComponentInParent<PlayerController>() != null;
    }
}
