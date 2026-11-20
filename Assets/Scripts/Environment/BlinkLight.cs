using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>Aircraft warning beacon on a mast: a slow red blink (lamp and its glow).</summary>
    public class BlinkLight : MonoBehaviour
    {
        [SerializeField] private Light lamp;
        [SerializeField] private Renderer bulb;
        [SerializeField] private Color color = new Color(1f, 0.12f, 0.08f);
        [SerializeField] private float period = 1.6f;
        [SerializeField] private float onFraction = 0.35f;

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float baseIntensity;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (lamp != null) baseIntensity = lamp.intensity;
        }

        private void Update()
        {
            float phase = Mathf.Repeat(Time.time, period) / period;
            // Quick fade in and out rather than a hard on/off.
            float k = phase < onFraction ? Mathf.Sin(phase / onFraction * Mathf.PI) : 0f;
            if (lamp != null) lamp.intensity = baseIntensity * k;
            if (bulb != null)
            {
                bulb.GetPropertyBlock(block);
                block.SetColor(EmissionId, color * (0.1f + 1.0f * k));
                bulb.SetPropertyBlock(block);
            }
        }
    }
}
