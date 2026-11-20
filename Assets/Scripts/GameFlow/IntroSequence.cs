using System.Collections;
using LastSignal.Audio;
using LastSignal.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// The opening, ten years after the fire, before Level 1 (skippable at any time).
    ///
    /// 1. Alex's flat, 11:47 PM. Rain on the window, a clock ticking. Dad's old radio on the sideboard switches
    ///    itself on; through the static, Maya's voice: "Alex... can you hear me?"
    /// 2. Playable: the player walks to the radio. The voice begs him to come back to Northpoint "before it finds
    ///    me", then the radio dies, the dial stuck on 103.7. He picks up the old photo of Dad, Maya and himself
    ///    in front of the station. Black. THE LAST SIGNAL.
    /// 3. The drive: a forest road in the storm, wipers going, the radio hissing between stations, lightning
    ///    showing the mast. The car stops at the Northpoint gate. Level 1 starts beside the car.
    ///
    /// The Echo is never shown or explained here, and nothing says whether Maya is alive.
    /// </summary>
    public class IntroSequence : MonoBehaviour
    {
        public enum Stage { Opening, Flat, Radio, Photo, Title, Drive, Leaving }

        [Header("Cameras")]
        [SerializeField] private Transform cameraRig;
        [SerializeField] private Camera viewCamera;
        [SerializeField] private Transform flatShotStart;
        [SerializeField] private Transform flatShotEnd;
        [SerializeField] private Transform carView;
        [SerializeField] private float handheld = 0.35f;

        [Header("Player (the flat)")]
        [SerializeField] private GameObject player;
        [SerializeField] private Canvas hud;
        [SerializeField] private Transform flatStart;
        [SerializeField] private Transform radioLook;

        [Header("Dad's radio")]
        [SerializeField] private TMP_Text homeDial;
        [SerializeField] private Light homeRadioGlow;
        [SerializeField] private Renderer homeDialLamp;
        [SerializeField] private AudioSource homeRadio;
        [SerializeField] private GameObject radioButton;
        [SerializeField] private GameObject photoButton;

        [Header("Photo")]
        [SerializeField] private GameObject photoPanel;
        [SerializeField] private RectTransform photoCard;

        [Header("Car")]
        [SerializeField] private Transform car;
        [Tooltip("Markers along the road at eye height, start to stop (they follow the ground).")]
        [SerializeField] private Transform[] drivePath = new Transform[0];
        [SerializeField] private float driveSeconds = 17f;
        [SerializeField] private Transform[] wipers = new Transform[0];
        [SerializeField] private TMP_Text carDial;
        [SerializeField] private Light carRadioGlow;
        [SerializeField] private AudioSource carRadio;
        [SerializeField] private AudioSource roofRain;

        [Header("Storm")]
        [SerializeField] private Light lightning;
        [SerializeField] private Renderer[] flashSky = new Renderer[0];
        [SerializeField] private Color nightSky = new Color(0.015f, 0.018f, 0.028f);
        [SerializeField] private Vector2 lightningEvery = new Vector2(7f, 14f);
        [SerializeField] private AudioSource windowRain;

        [Header("Story")]
        [SerializeField] private SubtitleLine[] firstCall = new SubtitleLine[0];
        [SerializeField] private SubtitleLine[] secondCall = new SubtitleLine[0];
        [SerializeField] private SubtitleLine[] atTheRadio = new SubtitleLine[0];
        [SerializeField] private SubtitleLine[] afterPhoto = new SubtitleLine[0];

        [Header("UI")]
        [SerializeField] private CanvasGroup fade;
        [SerializeField] private CanvasGroup title;
        [SerializeField] private CanvasGroup timeCaption;
        [SerializeField] private GameObject skipButton;
        [SerializeField] private Behaviour introSubtitles;

        [Header("Sound")]
        [SerializeField] private AudioSource music;

        public Stage Current { get; private set; } = Stage.Opening;
        public bool PhotoOpen => photoPanel != null && photoPanel.activeSelf;

        private Transform view;
        private float lightningTimer = 9f, flash;
        private Coroutine run;
        private bool atRadio, photoSeen, photoClosed, wipe;
        private float photoOpenedAt;
        private PlayerController controller;
        private PlayerLook look;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;
        private float homeGlow = 1f, carGlow = 0.45f;
        private readonly System.Collections.Generic.List<Renderer> hiddenRenderers = new System.Collections.Generic.List<Renderer>();

        private void Start()
        {
            Time.timeScale = 1f;
            block = new MaterialPropertyBlock();
            if (homeRadioGlow != null) homeGlow = homeRadioGlow.intensity;
            if (carRadioGlow != null) carGlow = carRadioGlow.intensity;
            if (fade != null) fade.alpha = 1f;
            if (title != null) title.alpha = 0f;
            if (timeCaption != null) timeCaption.alpha = 0f;
            if (skipButton != null) skipButton.SetActive(false);
            if (photoPanel != null) photoPanel.SetActive(false);
            if (photoButton != null) photoButton.SetActive(false);
            if (radioButton != null) radioButton.SetActive(false);
            if (player != null)
            {
                controller = player.GetComponent<PlayerController>();
                look = player.GetComponent<PlayerLook>();
            }
            SetHomeRadio(false, 0f);
            AudioManager.Instance?.SetAmbience(Ambience.None);
            if (windowRain != null) { windowRain.clip = PlaceholderClips.Rain; windowRain.loop = true; windowRain.volume = 0f; windowRain.Play(); }
            Cinematic(true);
            view = flatShotStart;
            run = StartCoroutine(Run());
        }

        // ------------------------------------------------------------------ per frame

        private void Update()
        {
            float dt = Time.deltaTime;
            if (windowRain != null && windowRain.isPlaying && Current < Stage.Title)
                windowRain.volume = Mathf.MoveTowards(windowRain.volume, 0.55f * Save.GameSettings.AmbienceVolume, dt * 0.3f);

            // Hand-held breathing on the cinematic view.
            if (cameraRig != null && view != null)
            {
                float t = Time.time;
                Quaternion sway = Quaternion.Euler((Mathf.PerlinNoise(t * 0.2f, 1f) - 0.5f) * 1.4f * handheld, (Mathf.PerlinNoise(t * 0.15f, 4f) - 0.5f) * 1.8f * handheld, 0f);
                cameraRig.SetPositionAndRotation(view.position, view.rotation * sway);
            }

            // Lightning now and then.
            lightningTimer -= dt;
            if (lightningTimer <= 0f && Current != Stage.Title && Current != Stage.Leaving) Strike(Random.Range(0.5f, 1f));
            flash = Mathf.MoveTowards(flash, 0f, dt * 2.2f);
            float f = flash * (0.6f + 0.4f * Mathf.PerlinNoise(Time.time * 30f, 0.5f)); // flickers as it fades
            if (lightning != null) lightning.intensity = f * 2.4f;
            if (viewCamera != null) viewCamera.backgroundColor = Color.Lerp(nightSky, new Color(0.35f, 0.38f, 0.48f), f * 0.6f);
            foreach (Renderer r in flashSky)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                Color c = Color.Lerp(new Color(0.03f, 0.035f, 0.05f), new Color(0.55f, 0.6f, 0.75f), f);
                block.SetColor(BaseColorId, c);
                r.SetPropertyBlock(block);
            }

            // Wipers: a sweep across and back, then a short rest.
            if (wipe && wipers != null)
            {
                float cycle = Mathf.Repeat(Time.time, 1.9f) / 1.5f;
                float a = cycle < 1f ? Mathf.Sin(cycle * Mathf.PI) : 0f;
                foreach (Transform w in wipers)
                    if (w != null) w.localRotation = Quaternion.Euler(0f, 0f, -a * 78f);
            }

            // Put the photo down with E / Space / Enter (or the button).
            if (PhotoOpen)
            {
                Keyboard pk = Keyboard.current;
                if (pk != null && (pk.eKey.wasPressedThisFrame || pk.spaceKey.wasPressedThisFrame || pk.enterKey.wasPressedThisFrame) && photoOpenedAt + 0.5f < Time.unscaledTime) ClosePhoto();
            }

            // Skip from the keyboard only in the cinematic parts (Esc pauses while you are playing).
            if (Current != Stage.Leaving && skipButton != null && skipButton.activeSelf && (Current == Stage.Opening || Current >= Stage.Title))
            {
                Keyboard kb = Keyboard.current;
                if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) Skip();
            }
        }

        private void Strike(float strength)
        {
            lightningTimer = Random.Range(lightningEvery.x, lightningEvery.y);
            flash = Mathf.Max(flash, strength);
            StartCoroutine(Thunder(Random.Range(0.6f, 2.2f) / strength, strength));
        }

        private IEnumerator Thunder(float delay, float strength)
        {
            yield return new WaitForSeconds(delay);
            AudioClip c = PlaceholderClips.Thunder;
            if (c != null && Current != Stage.Leaving) AudioManager.Instance?.PlayUI(c, (Current >= Stage.Drive ? 0.55f : 0.35f) * strength);
        }

        // ------------------------------------------------------------------ the story

        private IEnumerator Run()
        {
            // 1. Black; rain and a clock. The room fades up; the camera drifts towards the sideboard.
            yield return new WaitForSeconds(1.2f);
            StartCoroutine(Fade(fade, 1f, 0f, 4f));
            StartCoroutine(Dolly(flatShotStart, flatShotEnd, 13f));
            yield return new WaitForSeconds(1f);
            if (skipButton != null) skipButton.SetActive(true);
            if (timeCaption != null) { yield return Fade(timeCaption, 0f, 1f, 1.2f); }
            yield return new WaitForSeconds(2.2f);
            if (timeCaption != null) StartCoroutine(Fade(timeCaption, 1f, 0f, 1.5f));
            yield return new WaitForSeconds(2.5f);

            // The radio switches itself on. Static, the dial racing, then settling.
            AudioManager.Instance?.PlayAt(Utilities.PlaceholderAudio.Click, homeRadio != null ? homeRadio.transform.position : transform.position, 0.9f);
            SetHomeRadio(true, 1f);
            if (homeRadio != null) { homeRadio.clip = PlaceholderClips.Interference; homeRadio.loop = true; homeRadio.volume = 0.5f; homeRadio.Play(); }
            PlayOn(homeRadio, PlaceholderClips.RadioTune, 0.6f);
            yield return Dial(homeDial, homeRadioGlow, 3.2f, homeGlow);
            SetText(homeDial, "103.7");
            if (homeRadio != null) homeRadio.volume = 0.25f;
            PlayOn(homeRadio, PlaceholderClips.EchoVoice, 0.7f);
            yield return Say(firstCall);

            // 2. Into Alex's eyes: the player walks to the radio.
            yield return Fade(fade, 0f, 1f, 0.5f);
            Cinematic(false);
            Current = Stage.Flat;
            ObjectiveManager.Instance?.Set("intro_radio", "Go to the radio");
            yield return Fade(fade, 1f, 0f, 0.8f);
            yield return new WaitForSeconds(2.5f);
            if (!atRadio)
            {
                PlayOn(homeRadio, PlaceholderClips.RadioCrackle, 0.5f);
                PlayOn(homeRadio, PlaceholderClips.EchoVoice, 0.6f);
                yield return Say(secondCall);
            }
            float nudge = 0f;
            while (!atRadio)
            {
                // The static swells now and then until he goes to it.
                nudge += Time.deltaTime;
                if (nudge > 9f) { nudge = 0f; PlayOn(homeRadio, PlaceholderClips.RadioCrackle, 0.45f); }
                yield return null;
            }

            // At the radio: the last words, and it dies.
            Current = Stage.Radio;
            if (controller != null) controller.MovementLocked = true;
            if (homeRadio != null) homeRadio.volume = 0.3f;
            PlayOn(homeRadio, PlaceholderClips.EchoVoice, 0.8f);
            yield return Say(atTheRadio);
            PlayOn(homeRadio, PlaceholderClips.RadioCrackle, 0.9f);
            yield return Dial(homeDial, homeRadioGlow, 0.6f, homeGlow * 1.3f);
            SetText(homeDial, "103.7");
            if (homeRadio != null) homeRadio.Stop();
            AudioManager.Instance?.PlayAt(Utilities.PlaceholderAudio.Click, homeRadio != null ? homeRadio.transform.position : transform.position, 0.7f);
            SetHomeRadio(false, 0.15f); // dead; the dial still faintly shows where it stopped
            yield return new WaitForSeconds(1.5f);
            if (controller != null) controller.MovementLocked = false;

            // The photo by the radio.
            Current = Stage.Photo;
            if (radioButton != null) radioButton.SetActive(true);
            if (photoButton != null) photoButton.SetActive(true);
            ObjectiveManager.Instance?.Set("intro_photo", "Look at the old photo by the radio");
            while (!photoClosed) yield return null;
            ObjectiveManager.Instance?.Restore("", "");
            yield return Say(afterPhoto);
            yield return new WaitForSeconds(0.6f);

            // Black. The title.
            Current = Stage.Title;
            if (controller != null) controller.MovementLocked = true;
            yield return Fade(fade, 0f, 1f, 2f);
            if (windowRain != null) StartCoroutine(FadeAudio(windowRain, 0f, 1f));
            Cinematic(true);
            if (player != null) player.SetActive(false);
            if (music != null) { music.volume = 0f; music.Play(); StartCoroutine(FadeAudio(music, 0.35f * Save.GameSettings.AmbienceVolume, 2f)); }
            yield return Fade(title, 0f, 1f, 2.5f);
            yield return new WaitForSeconds(2.5f);
            yield return Fade(title, 1f, 0f, 1.5f);

            // 3. The drive.
            yield return Drive();
            yield return Leave(false);
        }

        private IEnumerator Drive()
        {
            Current = Stage.Drive;
            view = carView;
            wipe = true;
            float[] along = PathLengths(out float total);
            if (car != null && drivePath.Length > 0) car.SetPositionAndRotation(drivePath[0].position, drivePath[0].rotation);
            if (roofRain != null) { roofRain.clip = PlaceholderClips.CarRain; roofRain.loop = true; roofRain.volume = 0.7f * Save.GameSettings.AmbienceVolume; roofRain.Play(); }
            if (carRadio != null) { carRadio.clip = PlaceholderClips.Interference; carRadio.loop = true; carRadio.volume = 0.18f; carRadio.Play(); }
            if (music != null) StartCoroutine(FadeAudio(music, 0.18f * Save.GameSettings.AmbienceVolume, 3f));
            StartCoroutine(Fade(fade, 1f, 0f, 2.5f));
            StartCoroutine(HuntCarRadio());
            lightningTimer = 5.5f;
            bool struck = false;
            for (float t = 0f; t < driveSeconds; t += Time.deltaTime)
            {
                // Steady along the road, easing to a stop at the gate.
                float k = t / driveSeconds;
                float s = k < 0.7f ? k / 0.7f * 0.82f : 0.82f + (1f - Mathf.Pow(1f - (k - 0.7f) / 0.3f, 2f)) * 0.18f;
                if (car != null && drivePath.Length > 1)
                {
                    PointOnPath(along, s * total, out Vector3 p, out Quaternion r);
                    // A little body roll and bump on the wet road while moving.
                    float moving = 1f - Mathf.Clamp01((k - 0.75f) / 0.25f);
                    p.y += Mathf.Sin(t * 9f) * 0.008f * moving + (Mathf.PerlinNoise(t * 2.5f, 3f) - 0.5f) * 0.03f * moving;
                    r *= Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(t * 0.7f, 9f) - 0.5f) * 1.5f * moving);
                    car.SetPositionAndRotation(p, r);
                }
                if (!struck && k > 0.55f) { struck = true; Strike(1f); } // the station shows in the lightning
                yield return null;
            }
            // Engine off: the radio crackles and gives up; just the rain on the roof.
            wipe = false;
            PlayOn(carRadio, PlaceholderClips.RadioCrackle, 0.6f);
            yield return new WaitForSeconds(0.5f);
            if (carRadio != null) carRadio.Stop();
            SetText(carDial, "");
            if (carRadioGlow != null) carRadioGlow.intensity = 0f;
            yield return new WaitForSeconds(2.5f);
        }

        private float[] PathLengths(out float total)
        {
            var along = new float[drivePath.Length];
            total = 0f;
            for (int i = 1; i < drivePath.Length; i++)
            {
                total += Vector3.Distance(drivePath[i - 1].position, drivePath[i].position);
                along[i] = total;
            }
            return along;
        }

        private void PointOnPath(float[] along, float d, out Vector3 p, out Quaternion r)
        {
            int i = 1;
            while (i < along.Length - 1 && along[i] < d) i++;
            float seg = Mathf.Max(0.0001f, along[i] - along[i - 1]);
            float k = Mathf.Clamp01((d - along[i - 1]) / seg);
            p = Vector3.Lerp(drivePath[i - 1].position, drivePath[i].position, k);
            r = Quaternion.Slerp(drivePath[i - 1].rotation, drivePath[i].rotation, k);
        }

        private IEnumerator HuntCarRadio()
        {
            while (Current == Stage.Drive && carRadio != null && carRadio.isPlaying)
            {
                yield return Dial(carDial, carRadioGlow, Random.Range(1.5f, 2.5f), carGlow);
                yield return new WaitForSeconds(Random.Range(1.5f, 3f));
            }
        }

        // ------------------------------------------------------------------ hooks (scene objects call these)

        /// <summary>The trigger in front of the sideboard.</summary>
        public void ReachedRadio()
        {
            if (Current == Stage.Flat) atRadio = true;
        }

        /// <summary>"Try the radio" after it has died.</summary>
        public void TryRadio()
        {
            if (Current < Stage.Photo) return;
            AudioManager.Instance?.PlayAt(Utilities.PlaceholderAudio.Click, homeRadio != null ? homeRadio.transform.position : transform.position, 0.7f);
            Utilities.HudMessage.Show("Dead. The dial is stuck on 103.7.");
        }

        /// <summary>"Look at the photo": the old print up close.</summary>
        public void ShowPhoto()
        {
            if (Current != Stage.Photo || PhotoOpen) return;
            AudioManager.Instance?.PlayUI(PlaceholderClips.PaperUp, 0.6f);
            if (photoPanel != null) photoPanel.SetActive(true);
            if (controller != null) controller.MovementLocked = true;
            if (look != null) look.enabled = false;
            if (photoCard != null) StartCoroutine(RaiseCard());
            photoSeen = true;
            photoOpenedAt = Time.unscaledTime;
        }

        public void ClosePhoto()
        {
            if (!PhotoOpen) return;
            AudioManager.Instance?.PlayUI(PlaceholderClips.PaperDown, 0.5f);
            photoPanel.SetActive(false);
            if (look != null) look.enabled = true;
            if (controller != null) controller.MovementLocked = false;
            if (photoSeen) photoClosed = true;
        }

        private IEnumerator RaiseCard()
        {
            Vector2 end = photoCard.anchoredPosition;
            Vector2 start = end + new Vector2(0f, -500f);
            for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                photoCard.anchoredPosition = Vector2.Lerp(start, end, k);
                photoCard.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-9f, -2.5f, k));
                yield return null;
            }
            photoCard.anchoredPosition = end;
            photoCard.localRotation = Quaternion.Euler(0f, 0f, -2.5f);
        }

        /// <summary>Skip button (and Esc / Space / Enter in the cinematic parts).</summary>
        public void Skip()
        {
            if (Current == Stage.Leaving) return;
            if (run != null) StopCoroutine(run);
            SubtitleManager.Instance?.StopAll();
            if (photoPanel != null) photoPanel.SetActive(false);
            StartCoroutine(Leave(true));
        }

        private IEnumerator Leave(bool skipped)
        {
            if (Current == Stage.Leaving) yield break;
            Current = Stage.Leaving;
            if (skipButton != null) skipButton.SetActive(false);
            ObjectiveManager.Instance?.Restore("", "");
            if (!skipped)
            {
                AudioManager.Instance?.PlayUI(PlaceholderClips.CarDoor, 0.9f);
                StartCoroutine(FadeAudio(roofRain, 0f, 1.5f));
                if (music != null) StartCoroutine(FadeAudio(music, 0f, 1.5f));
                yield return Fade(fade, fade != null ? fade.alpha : 0f, 1f, 1.5f);
                yield return new WaitForSeconds(0.5f);
            }
            else
            {
                yield return Fade(fade, fade != null ? fade.alpha : 0f, 1f, 0.5f);
            }
            GameFlowManager.Instance?.BeginFirstLevel();
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Cinematic camera and intro subtitles on, the player's view and HUD off (or the other way round).</summary>
        private void Cinematic(bool on)
        {
            if (viewCamera != null)
            {
                viewCamera.enabled = on;
                var listener = viewCamera.GetComponent<AudioListener>();
                if (listener != null) listener.enabled = on;
            }
            if (hud != null) hud.enabled = !on;
            if (introSubtitles != null) introSubtitles.enabled = on;
            if (player != null && player.activeSelf)
            {
                foreach (Camera c in player.GetComponentsInChildren<Camera>(true)) c.enabled = !on;
                foreach (AudioListener l in player.GetComponentsInChildren<AudioListener>(true)) l.enabled = !on;
                // His body is drawn for his own eyes (no head): keep it out of the cinematic shots.
                if (on)
                {
                    hiddenRenderers.Clear();
                    foreach (Renderer r in player.GetComponentsInChildren<Renderer>())
                        if (r.enabled) { r.enabled = false; hiddenRenderers.Add(r); }
                }
                else
                {
                    foreach (Renderer r in hiddenRenderers) if (r != null) r.enabled = true;
                    hiddenRenderers.Clear();
                }
                if (controller != null) controller.enabled = !on;
                if (look != null) look.enabled = !on;
                if (!on && look != null && radioLook != null && flatStart != null)
                {
                    // Pick up the view where the camera left it: looking at the radio.
                    Vector3 d = radioLook.position - (flatStart.position + Vector3.up * 1.6f);
                    float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                    float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
                    look.SetAngles(yaw, pitch);
                }
            }
            if (!on) view = null;
        }

        private void SetHomeRadio(bool on, float level)
        {
            if (homeRadioGlow != null) homeRadioGlow.intensity = on ? homeGlow : homeGlow * level;
            if (homeDial != null) homeDial.alpha = on ? 1f : Mathf.Max(0.25f, level * 2f);
            if (homeDialLamp != null)
            {
                homeDialLamp.GetPropertyBlock(block);
                block.SetColor(EmissionId, new Color(1f, 0.55f, 0.15f) * (on ? 1.6f : level));
                homeDialLamp.SetPropertyBlock(block);
            }
        }

        private static void SetText(TMP_Text t, string freq)
        {
            if (t != null) t.text = string.IsNullOrEmpty(freq) ? "" : "FM  " + freq;
        }

        /// <summary>The frequency readout racing up and down the band.</summary>
        private static IEnumerator Dial(TMP_Text display, Light glow, float seconds, float glowLevel)
        {
            float f = Random.Range(88f, 98f);
            for (float t = 0f; t < seconds; t += 0.07f)
            {
                f = Mathf.Repeat(f - 88f + Random.Range(-0.6f, 1.4f), 20f) + 88f;
                SetText(display, f.ToString("0.0"));
                if (glow != null) glow.intensity = glowLevel * Random.Range(0.8f, 1.1f);
                yield return new WaitForSeconds(0.07f);
            }
        }

        private void PlayOn(AudioSource source, AudioClip clip, float volume)
        {
            if (clip == null) return;
            if (source != null) source.PlayOneShot(clip, volume * Save.GameSettings.SfxVolume);
            else AudioManager.Instance?.PlayUI(clip, volume);
        }

        private IEnumerator Say(SubtitleLine[] lines)
        {
            foreach (SubtitleLine line in lines)
            {
                bool done = false;
                if (SubtitleManager.Instance != null) SubtitleManager.Instance.Play(new[] { line }, () => done = true);
                else done = true;
                while (!done) yield return null;
                yield return new WaitForSeconds(0.35f);
            }
        }

        private IEnumerator Dolly(Transform from, Transform to, float seconds)
        {
            if (from == null || to == null) yield break;
            Vector3 p0 = from.position;
            Quaternion r0 = from.rotation;
            var marker = new GameObject("DollyView").transform;
            view = marker;
            for (float t = 0f; t < seconds && view == marker; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                marker.SetPositionAndRotation(Vector3.Lerp(p0, to.position, k), Quaternion.Slerp(r0, to.rotation, k));
                yield return null;
            }
            if (view == marker) marker.SetPositionAndRotation(to.position, to.rotation);
        }

        private static IEnumerator Fade(CanvasGroup g, float from, float to, float seconds)
        {
            if (g == null) yield break;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                g.alpha = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            g.alpha = to;
        }

        private static IEnumerator FadeAudio(AudioSource s, float to, float seconds)
        {
            if (s == null) yield break;
            float from = s.volume;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                s.volume = Mathf.Lerp(from, to, t / seconds);
                yield return null;
            }
            s.volume = to;
            if (to <= 0f) s.Stop();
        }
    }
}
