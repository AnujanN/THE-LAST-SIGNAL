using System.Text;
using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Level 3: install the transmitter modules, tune to the target frequency (clue in a note and
    /// a subtitle), then transmit. The display shows the frequency and a signal meter in text,
    /// and the static gets quieter as the tuning gets closer.
    /// </summary>
    public class FrequencyPuzzle : MonoBehaviour
    {
        [SerializeField] private string id = "frequency";
        [SerializeField] private ItemSlot[] modules = new ItemSlot[0];
        [SerializeField] private float minMHz = 88f;
        [SerializeField] private float maxMHz = 108f;
        [SerializeField] private float startMHz = 96.5f;
        [SerializeField] private float targetMHz = 103.7f;
        [SerializeField] private TMP_Text display;
        [SerializeField] private AudioSource staticSource;
        [SerializeField] private string wrongMessage = "Only static. That isn't the right frequency.";
        [SerializeField] private UnityEvent onSolved = new UnityEvent();

        private float frequency;
        private readonly StringBuilder sb = new StringBuilder(32);

        public bool IsSolved { get; private set; }
        public float Frequency => frequency;
        public float TargetMHz => targetMHz;
        private string Flag => "puzzle:" + id;

        private void Start()
        {
            frequency = startMHz;
            if (WorldFlags.Has(Flag)) { IsSolved = true; frequency = targetMHz; }
            if (staticSource != null)
            {
                staticSource.clip = PlaceholderClips.Interference;
                staticSource.loop = true;
                staticSource.Play();
            }
            Refresh();
        }

        public bool ModulesReady(out string missing)
        {
            sb.Clear();
            foreach (ItemSlot m in modules)
            {
                if (m == null || m.IsInstalled) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(m.RequiredItemName);
            }
            missing = sb.ToString();
            return sb.Length == 0;
        }

        /// <summary>Called by the tuning knobs with +/-0.1 or +/-1.0.</summary>
        public void Tune(float deltaMHz)
        {
            if (IsSolved) return;
            if (!ModulesReady(out string missing))
            {
                HudMessage.Show("The transmitter is dead. Missing: " + missing + ".");
                return;
            }
            frequency = Mathf.Clamp(Mathf.Round((frequency + deltaMHz) * 10f) / 10f, minMHz, maxMHz);
            AudioManager.Instance?.PlayAt(PlaceholderAudio.Click, transform.position, 0.5f);
            Refresh();
        }

        public void Transmit()
        {
            if (IsSolved) { HudMessage.Show("The broadcast is already running."); return; }
            if (!ModulesReady(out string missing))
            {
                HudMessage.Show("The transmitter is dead. Missing: " + missing + ".");
                AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.6f);
                return;
            }
            if (Mathf.Abs(frequency - targetMHz) > 0.05f)
            {
                HudMessage.Show(wrongMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.StaticBurst, transform.position, 0.8f);
                return;
            }
            IsSolved = true;
            WorldFlags.Set(Flag);
            Refresh();
            onSolved.Invoke();
        }

        /// <summary>0 = far off, 1 = locked on.</summary>
        public float SignalStrength => Mathf.Clamp01(1f - Mathf.Abs(frequency - targetMHz) / 5f);

        private void Refresh()
        {
            bool ready = ModulesReady(out _);
            float s = ready ? SignalStrength : 0f;
            if (staticSource != null) staticSource.volume = ready ? Mathf.Lerp(0.6f, 0.05f, s * s) : 0.15f;
            if (display == null) return;

            sb.Clear();
            if (!ready) { display.text = "NO POWER TO\nMODULES"; return; }
            sb.Append(frequency.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)).Append(" MHz\nSIGNAL ");
            int bars = Mathf.RoundToInt(s * 5f);
            for (int i = 0; i < 5; i++) sb.Append(i < bars ? '|' : '.');
            if (IsSolved) sb.Append("\nTRANSMITTING");
            display.text = sb.ToString();
        }
    }
}
