using System.Text;
using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Level 2: install the missing parts, then run the start-up steps in the order given on
    /// the maintenance note. A wrong step stalls the engine and resets the sequence.
    /// </summary>
    public class GeneratorPuzzle : MonoBehaviour
    {
        [SerializeField] private string id = "generator";
        [SerializeField] private ItemSlot[] parts = new ItemSlot[0];
        [Tooltip("Names of the start-up controls in the correct order.")]
        [SerializeField] private string[] steps = { "Fuel valve", "Choke", "Starter" };
        [SerializeField] private string wrongOrderMessage = "The engine coughs and stalls. Follow the start-up order on the maintenance note.";
        [SerializeField] private UnityEvent onPartsReady = new UnityEvent();
        [SerializeField] private UnityEvent onSolved = new UnityEvent();

        private int progress;

        public bool IsSolved { get; private set; }
        public int Progress => progress;
        private string Flag => "puzzle:" + id;

        private void Start()
        {
            if (WorldFlags.Has(Flag)) { IsSolved = true; StartEngineLoop(); }
            foreach (ItemSlot p in parts) if (p != null) p.Installed += OnPartInstalled;
        }

        /// <summary>Running-engine loop at the generator (only when a recording exists).</summary>
        private void StartEngineLoop()
        {
            AudioClip loop = PlaceholderClips.Generator;
            if (loop == null || GetComponent<AudioSource>() != null) return;
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = loop;
            src.loop = true;
            src.spatialBlend = 1f;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.minDistance = 2f;
            src.maxDistance = 18f;
            src.volume = 0.55f * Save.GameSettings.SfxVolume;
            src.Play();
        }

        private void OnDestroy()
        {
            foreach (ItemSlot p in parts) if (p != null) p.Installed -= OnPartInstalled;
        }

        private void OnPartInstalled(ItemSlot slot)
        {
            if (AllPartsInstalled(out _)) onPartsReady.Invoke();
        }

        public bool AllPartsInstalled(out string missing)
        {
            var sb = new StringBuilder();
            foreach (ItemSlot p in parts)
            {
                if (p == null || p.IsInstalled) continue;
                if (sb.Length > 0) sb.Append(", ");
                sb.Append(p.RequiredItemName);
            }
            missing = sb.ToString();
            return sb.Length == 0;
        }

        /// <summary>Called by each start-up control with its step index (0-based).</summary>
        public void PressStep(int index)
        {
            if (IsSolved) { HudMessage.Show("The generator is running."); return; }
            if (!AllPartsInstalled(out string missing))
            {
                progress = 0;
                HudMessage.Show("The generator is missing parts: " + missing + ".");
                AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.6f);
                return;
            }
            if (index != progress)
            {
                progress = 0;
                HudMessage.Show(wrongOrderMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Spark, transform.position, 0.9f);
                return;
            }

            progress++;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Beep, transform.position, 0.6f);
            if (progress < steps.Length)
            {
                HudMessage.Show(steps[index] + " set (" + progress + "/" + steps.Length + ").");
                return;
            }

            IsSolved = true;
            WorldFlags.Set(Flag);
            HudMessage.Show("The generator roars to life.");
            AudioManager.Instance?.PlayAt(PlaceholderClips.Buzz, transform.position, 1f);
            StartEngineLoop();
            onSolved.Invoke();
        }
    }
}
