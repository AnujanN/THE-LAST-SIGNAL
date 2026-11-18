using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// Unsteady light: mostly on, with short random dips. Gentle by default
    /// (no rapid full-screen strobing, per the accessibility rules).
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class FlickerLight : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float minFactor = 0.15f;
        [SerializeField] private Vector2 calmSeconds = new Vector2(1.5f, 5f);
        [SerializeField] private Vector2 flickerSeconds = new Vector2(0.1f, 0.5f);

        private Light lightSource;
        private float baseIntensity;
        private float timer;
        private bool flickering;
        private float disturbed;

        /// <summary>Something wrong is close by (The Echo): stutter hard for a while.</summary>
        public void Disturb(float seconds) => disturbed = Mathf.Max(disturbed, seconds);

        private void Awake()
        {
            lightSource = GetComponent<Light>();
            baseIntensity = lightSource.intensity;
            timer = Random.Range(calmSeconds.x, calmSeconds.y);
        }

        private void Update()
        {
            if (disturbed > 0f)
            {
                disturbed -= Time.deltaTime;
                float n = Mathf.PerlinNoise(Time.time * 11f, 3.1f);
                lightSource.intensity = baseIntensity * Mathf.Lerp(minFactor * 0.5f, 1f, n * n);
                return;
            }
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                flickering = !flickering;
                timer = flickering ? Random.Range(flickerSeconds.x, flickerSeconds.y) : Random.Range(calmSeconds.x, calmSeconds.y);
            }
            float target = flickering ? baseIntensity * Mathf.Lerp(minFactor, 1f, Mathf.PerlinNoise(Time.time * 18f, 0f)) : baseIntensity;
            lightSource.intensity = target;
        }
    }
}
