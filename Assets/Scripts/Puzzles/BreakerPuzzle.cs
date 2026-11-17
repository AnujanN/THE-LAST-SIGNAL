using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Level 1: insert the main fuse, set the breakers to match the wiring diagram, then pull
    /// the main lever. Wrong settings trip the breaker with feedback; nothing needs resetting.
    /// </summary>
    public class BreakerPuzzle : MonoBehaviour
    {
        [SerializeField] private string id = "breaker";
        [SerializeField] private BreakerSwitch[] switches = new BreakerSwitch[0];
        [Tooltip("Required UP (true) / DOWN (false) per switch, same order as switches.")]
        [SerializeField] private bool[] solution = new bool[0];
        [SerializeField] private ItemSlot fuseSlot;
        [SerializeField] private string missingFuseMessage = "The main fuse socket is empty.";
        [SerializeField] private string wrongMessage = "The breaker trips. Check the wiring diagram.";
        [SerializeField] private string solvedMessage = "Power restored.";
        [SerializeField] private UnityEvent onSolved = new UnityEvent();

        public bool IsSolved { get; private set; }
        private string Flag => "puzzle:" + id;

        private void Start()
        {
            if (!WorldFlags.Has(Flag)) return;
            IsSolved = true;
            for (int i = 0; i < switches.Length && i < solution.Length; i++)
                if (switches[i] != null) switches[i].Set(solution[i], notify: false);
        }

        public bool PatternMatches()
        {
            if (switches.Length != solution.Length) return false;
            for (int i = 0; i < switches.Length; i++)
                if (switches[i] == null || switches[i].IsUp != solution[i]) return false;
            return true;
        }

        /// <summary>Main lever. Returns true if the power came on.</summary>
        public bool TryRestorePower()
        {
            if (IsSolved) { HudMessage.Show("The power is already on."); return false; }
            if (fuseSlot != null && !fuseSlot.IsInstalled)
            {
                Fail(missingFuseMessage);
                return false;
            }
            if (!PatternMatches())
            {
                Fail(wrongMessage);
                return false;
            }
            IsSolved = true;
            WorldFlags.Set(Flag);
            HudMessage.Show(solvedMessage);
            onSolved.Invoke();
            return true;
        }

        // UnityEvent-friendly wrapper (UnityEvents cannot bind methods that return a value).
        public void PullMainLever() => TryRestorePower();

        private void Fail(string message)
        {
            HudMessage.Show(message);
            AudioManager.Instance?.PlayAt(PlaceholderClips.Spark, transform.position, 0.9f);
        }
    }
}
