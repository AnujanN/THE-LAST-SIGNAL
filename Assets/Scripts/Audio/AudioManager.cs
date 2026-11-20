using LastSignal.Save;
using UnityEngine;

namespace LastSignal.Audio
{
    public enum Ambience
    {
        None,
        RainAndWind,
        IndoorHum,
        BasementDrips,
        TowerInterference,
        Dawn
    }

    /// <summary>
    /// Persistent audio: one looping ambience bed, a threat loop whose volume follows how close
    /// The Echo is, and 2D/3D one-shots. Volumes follow GameSettings.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource ambience;
        private AudioSource ui;
        private AudioSource threat;
        private Ambience currentAmbience = Ambience.None;
        private float ambienceTarget;
        private float threatLevel;
        private float thunderTimer = 25f;
        private float outdoors = 1f;
        private AudioLowPassFilter ambienceFilter;

        /// <summary>0 = safe, 1 = The Echo is right here. Drives the threat loop and HUD vignette.</summary>
        public float ThreatLevel => threatLevel;

        private void Awake()
        {
            Instance = this;
            ambience = MakeSource("Ambience", true);
            ambienceFilter = ambience.gameObject.AddComponent<AudioLowPassFilter>();
            ambienceFilter.cutoffFrequency = 22000f;
            ui = MakeSource("UI", false);
            threat = MakeSource("Threat", true);
            threat.clip = PlaceholderClips.ThreatDrone;
            threat.volume = 0f;
            threat.Play();
        }

        private AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.ignoreListenerPause = name == "UI";
            return s;
        }

        private void Update()
        {
            // Indoors the storm is quieter and muffled (walls take the top off it).
            float ambienceVol = ambienceTarget * Mathf.Lerp(0.05f, 1f, outdoors) * GameSettings.AmbienceVolume;
            if (ambienceFilter != null)
                ambienceFilter.cutoffFrequency = Mathf.MoveTowards(ambienceFilter.cutoffFrequency, Mathf.Lerp(900f, 22000f, outdoors * outdoors), Time.unscaledDeltaTime * 20000f);
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceVol, Time.unscaledDeltaTime * 0.5f);

            float threatVol = Mathf.Clamp01(threatLevel) * 0.55f * GameSettings.SfxVolume;
            threat.volume = Mathf.MoveTowards(threat.volume, threatVol, Time.deltaTime * 0.8f);
            threat.pitch = 0.85f + threatLevel * 0.3f;
            // Decay so the threat fades if nothing reports it this frame.
            threatLevel = Mathf.MoveTowards(threatLevel, 0f, Time.deltaTime * 0.5f);

            // Out in the storm: distant thunder now and then (the rain bed itself has none).
            if (currentAmbience == Ambience.RainAndWind && Time.deltaTime > 0f)
            {
                thunderTimer -= Time.deltaTime;
                if (thunderTimer <= 0f)
                {
                    thunderTimer = Random.Range(40f, 85f);
                    AudioClip clip = PlaceholderClips.Thunder;
                    if (clip != null) ui.PlayOneShot(clip, Random.Range(0.35f, 0.65f) * Mathf.Lerp(0.35f, 1f, outdoors) * GameSettings.AmbienceVolume);
                }
            }
        }

        /// <summary>
        /// Respawning after a catch: cut off any sound still ringing from the catch (it played on this
        /// persistent source) and drop the threat loop at once.
        /// </summary>
        public void ResetForRespawn()
        {
            ui.Stop();
            threatLevel = 0f;
            threat.volume = 0f;
        }

        /// <summary>
        /// How much the listener is out in the weather: 1 outside or in an open doorway, falling towards 0 deeper
        /// inside (RainAmbience sets it each frame from the distance to the entrance). Resets to 1 on a new scene.
        /// </summary>
        public void SetOutdoors(float amount) => outdoors = Mathf.Clamp01(amount);

        public void SetAmbience(Ambience type, float volume = 0.6f)
        {
            ambienceTarget = type == Ambience.None ? 0f : volume;
            outdoors = 1f;
            if (type == currentAmbience) return;
            currentAmbience = type;
            AudioClip clip = ClipFor(type);
            if (clip == null) { ambience.Stop(); return; }
            ambience.clip = clip;
            ambience.volume = 0f;
            ambience.Play();
        }

        /// <summary>Report how close the threat is this frame (highest report wins).</summary>
        public void ReportThreat(float level) => threatLevel = Mathf.Max(threatLevel, Mathf.Clamp01(level));

        public void PlayUI(AudioClip clip, float volume = 1f)
        {
            if (clip != null) ui.PlayOneShot(clip, volume * GameSettings.SfxVolume);
        }

        public void PlayAt(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume * GameSettings.SfxVolume);
        }

        private static AudioClip ClipFor(Ambience type)
        {
            switch (type)
            {
                case Ambience.RainAndWind: return PlaceholderClips.Rain;
                case Ambience.IndoorHum: return PlaceholderClips.Hum;
                case Ambience.BasementDrips: return PlaceholderClips.Drips;
                case Ambience.TowerInterference: return PlaceholderClips.Wind ?? PlaceholderClips.Interference;
                // Prefer a dawn recording; otherwise soft recorded wind rather than generated chirps.
                case Ambience.Dawn: return SoundBank.Has("dawn") ? PlaceholderClips.Dawn : PlaceholderClips.Wind ?? PlaceholderClips.Dawn;
                default: return null;
            }
        }
    }
}
