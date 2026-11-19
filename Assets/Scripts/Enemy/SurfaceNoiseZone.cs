using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.Enemy
{
    /// <summary>Trigger area of water (flooded floor, a puddle): footsteps splash and carry further.</summary>
    [RequireComponent(typeof(Collider))]
    public class SurfaceNoiseZone : MonoBehaviour
    {
        [SerializeField] private float noiseMultiplier = 1.8f;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            NoiseEmitter e = other.GetComponentInParent<NoiseEmitter>();
            if (e != null) e.EnterWater(noiseMultiplier);
        }

        private void OnTriggerExit(Collider other)
        {
            NoiseEmitter e = other.GetComponentInParent<NoiseEmitter>();
            if (e != null) e.ExitWater(noiseMultiplier);
        }
    }
}
