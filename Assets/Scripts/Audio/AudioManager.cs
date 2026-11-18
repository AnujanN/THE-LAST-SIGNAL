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

        /// <summary>0 = safe, 1 = The Echo is right here. Drives the threat loop and HUD vignette.</summary>
        public float ThreatLevel => threatLevel;

        private void Awake()
        {
            Instance = this;
            ambience = MakeSource("Ambience", true);
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
            float ambienceVol = ambienceTarget * GameSettings.AmbienceVolume;
            ambience.volume = Mathf.MoveTowards(ambience.volume, ambienceVol, Time.unscaledDeltaTime * 0.5f);

            float threatVol = Mathf.Clamp01(threatLevel) * 0.55f * GameSettings.SfxVolume;
            threat.volume = Mathf.MoveTowards(threat.volume, threatVol, Time.deltaTime * 0.8f);
            threat.pitch = 0.85f + threatLevel * 0.3f;
            // Decay so the threat fades if nothing reports it this frame.
            threatLevel = Mathf.MoveTowards(threatLevel, 0f, Time.deltaTime * 0.5f);
        }

        public void SetAmbience(Ambience type, float volume = 0.6f)
        {
            ambienceTarget = type == Ambience.None ? 0f : volume;
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
