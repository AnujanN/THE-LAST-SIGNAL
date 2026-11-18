using System.Text;
using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Level 2: install the missing parts, then cold-start the engine: fuel valve, prime until the
    /// pressure needle sits in the green, choke, starter. The order is split across two halves of a
    /// torn maintenance card. A wrong step or over-priming stalls the engine loudly (The Echo hears it)
    /// and resets the sequence.
    /// </summary>
    public class GeneratorPuzzle : MonoBehaviour
    {
        [SerializeField] private string id = "generator";
        [SerializeField] private ItemSlot[] parts = new ItemSlot[0];
        [Tooltip("Names of the start-up controls in the correct order.")]
        [SerializeField] private string[] steps = { "Fuel valve", "Choke", "Starter" };
        [SerializeField] private string wrongOrderMessage = "The engine coughs and stalls. Follow the start-up order on the maintenance note.";

        [Header("Fuel pressure (primer)")]
        [SerializeField] private Transform gaugeNeedle;
        [SerializeField] private int greenMin = 3;
        [SerializeField] private int greenMax = 4;
        [Tooltip("This many pumps floods the engine.")]
        [SerializeField] private int floodAt = 6;
        [SerializeField] private float needleMinAngle = 70f;
        [SerializeField] private float needleMaxAngle = -70f;
        [Tooltip("Noise radius of a stall (The Echo can hear it).")]
        [SerializeField] private float stallNoiseRadius = 14f;
        [SerializeField] private UnityEvent onPartsReady = new UnityEvent();
        [SerializeField] private UnityEvent onSolved = new UnityEvent();

        private int progress;
        private int pumps;
        private float needleAngle;

        public bool IsSolved { get; private set; }
        public int Progress => progress;
        public int Pumps => pumps;
        public bool PressureInGreen => pumps >= greenMin && pumps <= greenMax;
        private string Flag => "puzzle:" + id;

        private void Start()
        {
            if (WorldFlags.Has(Flag)) { IsSolved = true; pumps = greenMin; StartEngineLoop(); }
            needleAngle = TargetNeedleAngle();
            ApplyNeedle();
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

        private void Update()
        {
            if (gaugeNeedle == null) return;
            // The needle swings to its reading and, once running, trembles with the engine.
            float target = TargetNeedleAngle() + (IsSolved ? Mathf.Sin(Time.time * 37f) * 1.5f : 0f);
            needleAngle = Mathf.Lerp(needleAngle, target, 1f - Mathf.Exp(-6f * Time.deltaTime));
            ApplyNeedle();
        }

        private float TargetNeedleAngle() =>
            Mathf.Lerp(needleMinAngle, needleMaxAngle, Mathf.Clamp01(pumps / (float)Mathf.Max(1, floodAt)));

        private void ApplyNeedle()
        {
            if (gaugeNeedle != null) gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, needleAngle);
        }

        /// <summary>Primer pump: raises fuel pressure. Only works once the fuel valve is open.</summary>
        public void PumpPrimer()
        {
            if (IsSolved) { HudMessage.Show("The generator is running."); return; }
            if (!AllPartsInstalled(out string missing))
            {
                HudMessage.Show("The generator is missing parts: " + missing + ".");
                return;
            }
            if (progress == 0)
            {
                HudMessage.Show("The primer wheezes. Nothing is flowing - the fuel is off.");
                AudioManager.Instance?.PlayAt(PlaceholderClips.Click, transform.position, 0.6f);
                return;
            }
            if (progress > 1)
            {
                HudMessage.Show("The choke is already set.");
                return;
            }
            pumps++;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.45f);
            if (pumps >= floodAt)
            {
                Stall("Fuel spills from the carburettor. Flooded. Start again from the fuel valve.");
                return;
            }
            HudMessage.Show(PressureInGreen ? "The needle settles in the green." : pumps > greenMax ? "The needle is past the green." : "The needle creeps up.");
        }

        private void Stall(string message)
        {
            progress = 0;
            pumps = 0;
            HudMessage.Show(message);
            AudioManager.Instance?.PlayAt(PlaceholderClips.Spark, transform.position, 0.9f);
            NoiseSystem.Emit(transform.position, stallNoiseRadius);
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
                Stall(wrongOrderMessage);
                return;
            }
            // The choke needs fuel pressure in the green (primer), or the engine dies.
            if (index == 1 && !PressureInGreen)
            {
                Stall(pumps > greenMax ? "Too rich - the engine chokes on fuel. Start again." : "The engine gasps and dies. No fuel pressure - watch the gauge.");
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
