using LastSignal.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// The light on top of the logo's tower, broadcasting: each pulse the red light flashes with a transmitter beep,
    /// then the three waves on either side light up one after another, travelling outwards. On the main menu it pulses
    /// by itself (silently); in the intro, IntroSequence fires exactly five pulses (one a second) while the title shows.
    /// Can also sit over a letter of a text title instead of a logo.
    /// </summary>
    public class TitleBeacon : MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [Tooltip("Which character the dot sits over (the I in SIGNAL).")]
        [SerializeField] private int characterIndex = 10;
        [Tooltip("Or: a logo image, and the point on it (0..1 across, 0..1 up) where the beacon light is.")]
        [SerializeField] private RectTransform logo;
        [SerializeField] private Vector2 logoPoint = new Vector2(0.5f, 0.86f);
        [SerializeField] private RectTransform dot;
        [SerializeField] private Image dotImage;
        [SerializeField] private Image glow;
        [Tooltip("The waves, innermost first (each image holds the left and right arc).")]
        [SerializeField] private Image[] waves = new Image[0];
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private Color colour = new Color(1f, 0.08f, 0.05f);
        [Tooltip("Lit waves: a hotter, brighter red than the logo's own arcs underneath.")]
        [SerializeField] private Color waveColour = new Color(1f, 0.62f, 0.5f);

        [Header("Timing")]
        [Tooltip("Pulse by itself every period seconds (main menu); off = only when Pulse() is called (intro).")]
        [SerializeField] private bool autoPulse = true;
        [SerializeField] private float period = 1.6f;
        [SerializeField] private float lightSeconds = 0.32f;
        [Tooltip("Delay before the first wave lights, and between one wave and the next.")]
        [SerializeField] private float waveDelay = 0.13f;
        [SerializeField] private float waveSeconds = 0.3f;
        [SerializeField, Range(0f, 1f)] private float pulseVolume = 0.55f;

        private bool placed;
        private float sincePulse = 99f, clock;
        private int pulses;

        /// <summary>How many pulses have fired.</summary>
        public int Pulses => pulses;

        private void OnEnable()
        {
            placed = false;
            sincePulse = 99f;
            clock = 0f;
        }

        /// <summary>One broadcast: the light flashes with a beep and the waves ripple outwards.</summary>
        public void Pulse()
        {
            sincePulse = 0f;
            pulses++;
            float shown = visibility != null ? visibility.alpha : 1f;
            if (pulseVolume <= 0f || shown < 0.2f || AudioManager.Instance == null) return;
            AudioClip beep = PlaceholderClips.Transmit ?? PlaceholderClips.Beep;
            AudioManager.Instance.PlayUI(beep, pulseVolume * shown);
            AudioManager.Instance.PlayUI(PlaceholderClips.Thud, 0.4f * shown); // the low weight under it
            if (pulses % 3 == 0) AudioManager.Instance.PlayUI(PlaceholderClips.RadioCrackle, 0.18f * shown);
        }

        private void LateUpdate()
        {
            if (!placed) Place();
            float dt = Time.unscaledDeltaTime;
            if (autoPulse)
            {
                clock += dt;
                if (clock >= period) { clock -= period; Pulse(); }
            }
            sincePulse += dt;

            // The light: a quick swell and fade; a faint ember between pulses.
            float k = Bump(sincePulse, 0f, lightSeconds);
            if (dotImage != null) dotImage.color = Color.Lerp(colour * 0.25f, colour, k);
            if (glow != null) glow.color = new Color(colour.r, colour.g, colour.b, 0.75f * k);
            if (dot != null) dot.localScale = Vector3.one * (1f + 0.15f * k);

            // The waves light in turn, innermost first.
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] == null) continue;
                float w = Bump(sincePulse, waveDelay * (i + 1), waveSeconds);
                waves[i].color = new Color(waveColour.r, waveColour.g, waveColour.b, w);
            }
        }

        /// <summary>0 → 1 → 0 over [start, start + length] (a smooth hump).</summary>
        private static float Bump(float t, float start, float length)
        {
            float x = (t - start) / length;
            return x <= 0f || x >= 1f ? 0f : Mathf.Sin(x * Mathf.PI);
        }

        /// <summary>Puts the light on the logo's tower top (or just above the chosen letter).</summary>
        private void Place()
        {
            if (dot == null) return;
            if (logo != null)
            {
                Rect r = logo.rect;
                dot.position = logo.TransformPoint(new Vector3(r.xMin + r.width * logoPoint.x, r.yMin + r.height * logoPoint.y, 0f));
                placed = true;
                return;
            }
            if (title == null) return;
            title.ForceMeshUpdate();
            TMP_TextInfo info = title.textInfo;
            if (info == null || characterIndex >= info.characterCount) return;
            TMP_CharacterInfo c = info.characterInfo[characterIndex];
            if (!c.isVisible) return;
            Vector3 top = (c.topLeft + c.topRight) / 2f;
            float letterHeight = c.topLeft.y - c.bottomLeft.y;
            dot.position = title.rectTransform.TransformPoint(top + Vector3.up * letterHeight * 0.26f);
            placed = true;
        }
    }
}
