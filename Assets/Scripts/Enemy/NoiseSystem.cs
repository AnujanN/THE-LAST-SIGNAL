using System;
using UnityEngine;

namespace LastSignal.Enemy
{
    /// <summary>Global noise events: position and audible radius in metres.</summary>
    public static class NoiseSystem
    {
        public static event Action<Vector3, float> NoiseMade;

        public static void Emit(Vector3 position, float radius)
        {
            if (radius > 0f) NoiseMade?.Invoke(position, radius);
        }
    }
}
