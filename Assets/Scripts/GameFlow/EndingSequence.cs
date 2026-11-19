using System.Collections;
using LastSignal.Audio;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Ending: dawn on the hill above the radio station, three months later. The sun climbs out of the mist
    /// behind the old tower while the camera drifts in (a little handheld) to Maya's headstone; Alex says
    /// goodbye and lays the cassette beside it; the view lifts towards the sunrise, THE END fades in over
    /// it, and a button returns to the main menu. Birdsong, wind in the grass and a soft music bed.
    /// </summary>
    public class EndingSequence : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Transform cameraStart;
        [SerializeField] private Transform cameraEnd;
        [SerializeField] private Transform cameraFinal;
        [SerializeField] private float dollySeconds = 30f;
        [SerializeField] private float handheld = 0.6f;

        [Header("Story")]
        [SerializeField] private SubtitleLine[] lines = new SubtitleLine[0];
        [Tooltip("Index of the line where Alex lays the cassette down (it appears then, with a click).")]
        [SerializeField] private int cassetteLine = 3;
        [SerializeField] private GameObject cassette;
        [SerializeField] private CanvasGroup theEnd;
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private GameObject menuButton;

        [Header("Sunrise")]
        [SerializeField] private Light sun;
        [SerializeField] private float sunYaw = 165f;
        [SerializeField] private Vector2 sunElevation = new Vector2(0.5f, 9f);
        [SerializeField] private Vector2 sunIntensity = new Vector2(0.45f, 1.9f);
        [SerializeField] private Color sunColorStart = new Color(1f, 0.45f, 0.22f);
        [SerializeField] private Color sunColorEnd = new Color(1f, 0.8f, 0.6f);
        [SerializeField] private Color ambientStart = new Color(0.28f, 0.22f, 0.22f);
        [SerializeField] private Color ambientEnd = new Color(0.6f, 0.53f, 0.48f);
        [SerializeField] private Vector2 fogDensity = new Vector2(0.03f, 0.011f);
        [SerializeField] private Color fogStart = new Color(0.55f, 0.38f, 0.32f);
        [SerializeField] private Color fogEnd = new Color(0.92f, 0.72f, 0.55f);
        [SerializeField] private Vector2 skyExposure = new Vector2(0.55f, 1.25f);
        [SerializeField] private float sunriseSeconds = 55f;
        [SerializeField] private ParticleSystem mist;

        [Header("Sound")]
        [SerializeField] private AudioSource wind;
        [SerializeField] private AudioSource music;
        [SerializeField] private float musicVolume = 0.32f;
        [SerializeField] private float windVolume = 0.22f;

        private Material sky;
        private float elapsed;

        private void Start()
        {
            Time.timeScale = 1f;
            AudioManager.Instance?.SetAmbience(Ambience.Dawn, 0.55f);
            if (RenderSettings.skybox != null)
            {
                // Animate a copy, never the material asset itself.
                sky = new Material(RenderSettings.skybox);
                RenderSettings.skybox = sky;
            }
            if (theEnd != null) theEnd.alpha = 0f;
            if (fade != null) fade.alpha = 1f;
            if (cassette != null) cassette.SetActive(false);
            if (menuButton != null) menuButton.SetActive(false);
            if (wind != null) { wind.volume = 0f; wind.Play(); }
            if (music != null) music.volume = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ApplySunrise(0f);
            StartCoroutine(Dolly());
            StartCoroutine(Run());
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            ApplySunrise(Mathf.Clamp01(elapsed / sunriseSeconds));
            if (wind != null) wind.volume = Mathf.MoveTowards(wind.volume, windVolume * Save.GameSettings.AmbienceVolume, Time.deltaTime * 0.05f);
        }

        /// <summary>0 = first light, the sun just clearing the hills; 1 = morning.</summary>
        private void ApplySunrise(float k)
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(Mathf.Lerp(sunElevation.x, sunElevation.y, e), sunYaw, 0f);
                sun.intensity = Mathf.Lerp(sunIntensity.x, sunIntensity.y, e);
                sun.color = Color.Lerp(sunColorStart, sunColorEnd, e);
            }
            RenderSettings.ambientLight = Color.Lerp(ambientStart, ambientEnd, e);
            RenderSettings.fogDensity = Mathf.Lerp(fogDensity.x, fogDensity.y, e);
            RenderSettings.fogColor = Color.Lerp(fogStart, fogEnd, e);
            if (sky != null && sky.HasProperty("_Exposure")) sky.SetFloat("_Exposure", Mathf.Lerp(skyExposure.x, skyExposure.y, e));
            if (mist != null)
            {
                // The mist burns off as the sun rises.
                ParticleSystem.MainModule main = mist.main;
                Color c = main.startColor.color;
                c.a = Mathf.Lerp(0.3f, 0.1f, e);
                main.startColor = c;
            }
        }

        private IEnumerator Dolly()
        {
            if (cameraRig == null || cameraStart == null || cameraEnd == null) yield break;
            for (float t = 0f; t < dollySeconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / dollySeconds);
                Place(Vector3.Lerp(cameraStart.position, cameraEnd.position, k), Quaternion.Slerp(cameraStart.rotation, cameraEnd.rotation, k), t);
                yield return null;
            }
            float hold = 0f;
            while (cameraFinal == null || !lifting)
            {
                hold += Time.deltaTime;
                Place(cameraEnd.position, cameraEnd.rotation, dollySeconds + hold);
                yield return null;
            }
            // Lift away from the stone towards the sunrise.
            for (float t = 0f; t < 9f; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 9f);
                Place(Vector3.Lerp(cameraEnd.position, cameraFinal.position, k), Quaternion.Slerp(cameraEnd.rotation, cameraFinal.rotation, k), dollySeconds + hold + t);
                yield return null;
            }
            for (float t = 0f; ; t += Time.deltaTime)
            {
                Place(cameraFinal.position, cameraFinal.rotation, dollySeconds + hold + 9f + t);
                yield return null;
            }
        }

        private bool lifting;

        /// <summary>A breathing, hand-held drift on top of the path.</summary>
        private void Place(Vector3 pos, Quaternion rot, float t)
        {
            float a = handheld;
            Vector3 drift = new Vector3(Mathf.PerlinNoise(t * 0.23f, 1.1f) - 0.5f, Mathf.PerlinNoise(t * 0.19f, 7.3f) - 0.5f, 0f) * 0.05f * a;
            Quaternion sway = Quaternion.Euler((Mathf.PerlinNoise(t * 0.17f, 3.7f) - 0.5f) * 1.2f * a, (Mathf.PerlinNoise(t * 0.13f, 5.1f) - 0.5f) * 1.6f * a, 0f);
            cameraRig.SetPositionAndRotation(pos + rot * drift, rot * sway);
        }

        private IEnumerator Run()
        {
            // Open from black on the first light.
            yield return FadeGroup(fade, 1f, 0f, 4f);
            if (music != null) StartCoroutine(FadeAudio(music, 0f, musicVolume * Save.GameSettings.AmbienceVolume, 8f, play: true));
            yield return new WaitForSeconds(1.5f);

            for (int i = 0; i < lines.Length; i++)
            {
                if (i == cassetteLine && cassette != null)
                {
                    cassette.SetActive(true);
                    AudioManager.Instance?.PlayAt(PlaceholderClips.TapeStop, cassette.transform.position, 0.6f);
                }
                bool done = false;
                if (SubtitleManager.Instance != null) SubtitleManager.Instance.Play(new[] { lines[i] }, () => done = true);
                else done = true;
                while (!done) yield return null;
                yield return new WaitForSeconds(0.4f);
            }

            yield return new WaitForSeconds(1.5f);
            lifting = true;
            yield return new WaitForSeconds(4f);
            yield return FadeGroup(theEnd, 0f, 1f, 3f);
            yield return new WaitForSeconds(5f);
            if (music != null) StartCoroutine(FadeAudio(music, music.volume, 0f, 6f, play: false));
            yield return FadeGroup(fade, 0f, 1f, 4f);
            if (menuButton != null) menuButton.SetActive(true);
        }

        private static IEnumerator FadeGroup(CanvasGroup g, float from, float to, float seconds)
        {
            if (g == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            g.alpha = to;
        }

        private static IEnumerator FadeAudio(AudioSource s, float from, float to, float seconds, bool play)
        {
            if (play && !s.isPlaying) s.Play();
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                s.volume = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            s.volume = to;
        }

        public void ReturnToMenu() => GameFlowManager.Instance?.GoToMainMenu();
    }
}
