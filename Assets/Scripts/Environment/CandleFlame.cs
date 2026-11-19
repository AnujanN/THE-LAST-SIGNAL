using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>A candle flame that breathes: the light and the flame's height waver softly.</summary>
    public class CandleFlame : MonoBehaviour
    {
        [SerializeField] private Light glow;
        [SerializeField] private Transform flame;
        [SerializeField] private float baseIntensity = 0.6f;

        private Vector3 flameScale;
        private float seed;

        private void Awake()
        {
            if (flame != null) flameScale = flame.localScale;
            seed = Random.value * 100f;
        }

        private void Update()
        {
            float t = Time.time * 6f + seed;
            float n = Mathf.PerlinNoise(t, 0.37f) * 0.7f + Mathf.PerlinNoise(t * 2.7f, 1.3f) * 0.3f;
            if (glow != null) glow.intensity = baseIntensity * (0.75f + 0.5f * n);
            if (flame != null)
            {
                flame.localScale = new Vector3(flameScale.x * (0.9f + 0.2f * n), flameScale.y * (0.8f + 0.45f * n), flameScale.z * (0.9f + 0.2f * n));
                flame.localRotation = Quaternion.Euler(0f, 0f, (n - 0.5f) * 8f);
            }
        }
    }
}
