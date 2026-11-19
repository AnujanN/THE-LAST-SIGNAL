using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A dying fluorescent tube far down a corridor: mostly dead, then it sputters (buzzing) and holds
    /// for a few seconds before dying again. It goes dark when Alex gets close, so it is always something
    /// glimpsed in the distance. Small and local: no full-screen strobing.
    /// </summary>
    public class DistantFlicker : MonoBehaviour
    {
        [SerializeField] private Light lamp;
        [SerializeField] private Renderer tube;
        [SerializeField] private AudioSource buzz;
        [SerializeField] private Color glow = new Color(0.8f, 0.95f, 0.85f);
        [SerializeField] private Vector2 deadSeconds = new Vector2(2.5f, 8f);
        [SerializeField] private Vector2 sputterSeconds = new Vector2(0.6f, 2.2f);
        [SerializeField] private Vector2 onSeconds = new Vector2(0.8f, 3.5f);
        [Tooltip("Within this distance of the camera it stays dead.")]
        [SerializeField] private float nearDistance = 5f;

        private enum Phase { Dead, Sputter, On }

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private Phase phase = Phase.Dead;
        private float timer, toggleTimer;
        private bool lit;
        private float shown = -1f;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            if (lamp != null) baseIntensity = lamp.intensity;
            timer = Random.Range(0.5f, deadSeconds.y);
            Show(0f);
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            Camera cam = Camera.main;
            bool near = cam != null && (cam.transform.position - transform.position).sqrMagnitude < nearDistance * nearDistance;

            timer -= dt;
            if (near && phase != Phase.Dead) { phase = Phase.Dead; timer = Random.Range(deadSeconds.x, deadSeconds.y); }
            if (timer <= 0f && !near)
            {
                phase = phase == Phase.Dead ? Phase.Sputter : phase == Phase.Sputter ? Phase.On : Phase.Dead;
                timer = phase == Phase.Dead ? Random.Range(deadSeconds.x, deadSeconds.y)
                    : phase == Phase.Sputter ? Random.Range(sputterSeconds.x, sputterSeconds.y)
                    : Random.Range(onSeconds.x, onSeconds.y);
            }

            float level;
            switch (phase)
            {
                case Phase.Sputter:
                    toggleTimer -= dt;
                    if (toggleTimer <= 0f)
                    {
                        lit = !lit;
                        toggleTimer = lit ? Random.Range(0.05f, 0.18f) : Random.Range(0.08f, 0.35f);
                        if (lit && buzz != null) buzz.PlayOneShot(Audio.PlaceholderClips.TubeFlick, 0.6f);
                    }
                    level = lit ? Random.Range(0.6f, 1f) : 0.05f;
                    break;
                case Phase.On:
                    level = 0.85f + 0.15f * Mathf.PerlinNoise(Time.time * 6f, transform.position.x);
                    break;
                default:
                    level = 0f;
                    break;
            }
            Show(level);
            Hum(phase == Phase.On);
        }

        /// <summary>The tube hums while it is lit.</summary>
        private void Hum(bool on)
        {
            if (buzz == null) return;
            if (on && !buzz.isPlaying)
            {
                AudioClip hum = Audio.PlaceholderClips.NeonHum;
                if (hum == null) return;
                buzz.clip = hum;
                buzz.loop = true;
                buzz.volume = 0.5f;
                buzz.time = Random.Range(0f, hum.length * 0.9f);
                buzz.Play();
            }
            else if (!on && buzz.isPlaying && buzz.clip != null) buzz.Stop();
        }

        private void Show(float level)
        {
            if (Mathf.Abs(level - shown) < 0.01f) return;
            shown = level;
            if (lamp != null)
            {
                lamp.enabled = level > 0.02f;
                lamp.intensity = baseIntensity * level;
            }
            if (tube != null)
            {
                tube.GetPropertyBlock(block);
                block.SetColor(EmissionId, glow * (0.05f + level * 2.2f));
                tube.SetPropertyBlock(block);
            }
        }
    }
}
