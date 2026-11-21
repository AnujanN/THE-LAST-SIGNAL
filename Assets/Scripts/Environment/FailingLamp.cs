using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.Environment
{
    /// <summary>
    /// A street lamp on its last legs: it lights the path, but it never holds steady: it stutters every few seconds
    /// (a burst of quick dips, sometimes going almost out), buzzing, with a tick of the starter on each burst. The
    /// glass bowl dims with it.
    /// </summary>
    public class FailingLamp : MonoBehaviour
    {
        [SerializeField] private Light[] lamps = new Light[0];
        [SerializeField] private Renderer bowl;
        [SerializeField] private Color bowlColour = new Color(0.75f, 0.82f, 1f);
        [SerializeField] private AudioSource buzz;
        [SerializeField] private Vector2 calmSeconds = new Vector2(0.6f, 2.6f);
        [SerializeField] private Vector2 burstSeconds = new Vector2(0.25f, 0.9f);

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float[] baseIntensity;
        private float timer, burstLeft, level = 1f;
        private bool bursting;

        private void Awake()
        {
            block = new MaterialPropertyBlock();
            baseIntensity = new float[lamps.Length];
            for (int i = 0; i < lamps.Length; i++) baseIntensity[i] = lamps[i] != null ? lamps[i].intensity : 0f;
            timer = Random.Range(calmSeconds.x, calmSeconds.y);
        }

        private void Start()
        {
            AudioClip hum = PlaceholderClips.NeonHum;
            if (buzz != null && hum != null) { buzz.clip = hum; buzz.loop = true; buzz.volume = 0.22f * Save.GameSettings.SfxVolume; buzz.Play(); }
        }

        private void Update()
        {
            float dt = Time.deltaTime, t = Time.time;
            timer -= dt;
            if (!bursting && timer <= 0f)
            {
                bursting = true;
                burstLeft = Random.Range(burstSeconds.x, burstSeconds.y);
                if (buzz != null && PlaceholderClips.TubeFlick != null) buzz.PlayOneShot(PlaceholderClips.TubeFlick, 0.5f * Save.GameSettings.SfxVolume);
            }
            float target;
            if (bursting)
            {
                burstLeft -= dt;
                // Rapid stutter: hard dips, sometimes nearly out.
                float n = Mathf.PerlinNoise(t * 26f, 0.7f);
                target = n < 0.45f ? Mathf.Lerp(0.05f, 0.35f, n / 0.45f) : Mathf.Lerp(0.6f, 1f, (n - 0.45f) / 0.55f);
                if (burstLeft <= 0f) { bursting = false; timer = Random.Range(calmSeconds.x, calmSeconds.y); }
            }
            else
            {
                // Never quite steady: a faint shiver even between bursts.
                target = 0.85f + 0.15f * Mathf.PerlinNoise(t * 7f, 3.3f);
            }
            level = bursting ? target : Mathf.MoveTowards(level, target, dt * 4f);
            for (int i = 0; i < lamps.Length; i++) if (lamps[i] != null) lamps[i].intensity = baseIntensity[i] * level;
            if (bowl != null)
            {
                bowl.GetPropertyBlock(block);
                block.SetColor(EmissionId, bowlColour * (0.2f + 1.6f * level));
                bowl.SetPropertyBlock(block);
            }
            if (buzz != null && buzz.isPlaying) buzz.pitch = 0.95f + 0.1f * level;
        }
    }
}
