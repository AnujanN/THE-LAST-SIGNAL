using LastSignal.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>
    /// The dot over the "I" in THE LAST SIGNAL: a red aircraft-warning beacon that blinks like the one on the
    /// station's mast, each flash with a transmitter pulse (and now and then a crackle of static). Only heard while
    /// the title is actually showing (its CanvasGroup alpha).
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
        [SerializeField] private CanvasGroup visibility;
        [SerializeField] private Color colour = new Color(1f, 0.08f, 0.05f);
        [SerializeField] private float period = 1.6f;
        [SerializeField] private float onFraction = 0.32f;
        [SerializeField, Range(0f, 1f)] private float pulseVolume = 0.55f;

        private int lastBlink = -1;
        private bool placed;
        private float clock;

        private void OnEnable()
        {
            placed = false;
            clock = 0f;
            lastBlink = -1;
        }

        private void LateUpdate()
        {
            if (!placed) Place();
            float shown = visibility != null ? visibility.alpha : 1f;
            clock += Time.unscaledDeltaTime;
            float phase = Mathf.Repeat(clock, period) / period;
            // Quick swell in and out, like the mast beacon; a faint ember between flashes.
            float k = phase < onFraction ? Mathf.Sin(phase / onFraction * Mathf.PI) : 0f;
            if (dotImage != null) dotImage.color = Color.Lerp(colour * 0.25f, colour, k) * new Color(1f, 1f, 1f, 1f);
            if (glow != null) glow.color = new Color(colour.r, colour.g, colour.b, 0.75f * k);
            if (dot != null) dot.localScale = Vector3.one * (1f + 0.15f * k);

            int blink = Mathf.FloorToInt(clock / period);
            if (blink != lastBlink)
            {
                lastBlink = blink;
                if (shown > 0.4f && AudioManager.Instance != null)
                {
                    AudioClip pulse = PlaceholderClips.Transmit ?? PlaceholderClips.Beep;
                    AudioManager.Instance.PlayUI(pulse, pulseVolume * shown);
                    AudioManager.Instance.PlayUI(PlaceholderClips.Thud, 0.4f * shown); // the low weight under it
                    if (blink % 3 == 0) AudioManager.Instance.PlayUI(PlaceholderClips.RadioCrackle, 0.18f * shown);
                }
            }
        }

        /// <summary>Puts the dot just above the top of the chosen letter (once the text has its layout).</summary>
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
            Vector3 world = title.rectTransform.TransformPoint(top + Vector3.up * letterHeight * 0.26f);
            dot.position = world;
            placed = true;
        }
    }
}
