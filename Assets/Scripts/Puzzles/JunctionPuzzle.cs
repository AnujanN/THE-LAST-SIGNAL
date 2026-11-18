using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// Level 4: re-route emergency power through a fire-damaged junction box. The schematic says which
    /// feeds can reach which lines; the fire marshal's report says what burned. Only one route works.
    /// A wrong route arcs violently: sparks, a burst of light and a bang The Echo will hear.
    /// </summary>
    public class JunctionPuzzle : MonoBehaviour
    {
        [SerializeField] private string id = "junction";
        [SerializeField] private SelectorSwitch[] selectors = new SelectorSwitch[0];
        [Tooltip("Correct position index per selector, same order.")]
        [SerializeField] private int[] solution = new int[0];
        [SerializeField] private Light arcLight;
        [SerializeField] private float wrongNoiseRadius = 16f;
        [SerializeField] private string wrongMessage = "The box arcs and bangs. That route is dead.";
        [SerializeField] private string solvedMessage = "Relays clunk. Somewhere above, a motor starts.";
        [SerializeField] private UnityEvent onSolved = new UnityEvent();

        private float arcTimer;

        public bool IsSolved { get; private set; }
        private string Flag => "puzzle:" + id;

        private void Start()
        {
            if (arcLight != null) arcLight.enabled = false;
            if (!WorldFlags.Has(Flag)) return;
            IsSolved = true;
            for (int i = 0; i < selectors.Length && i < solution.Length; i++)
                if (selectors[i] != null) selectors[i].Set(solution[i], notify: false);
        }

        private void Update()
        {
            if (arcTimer <= 0f || arcLight == null) return;
            arcTimer -= Time.deltaTime;
            arcLight.intensity = Random.Range(2f, 9f);
            if (arcTimer <= 0f) arcLight.enabled = false;
        }

        public bool RouteMatches()
        {
            if (selectors.Length != solution.Length) return false;
            for (int i = 0; i < selectors.Length; i++)
                if (selectors[i] == null || selectors[i].Index != solution[i]) return false;
            return true;
        }

        /// <summary>Throw the engage lever. Returns true when power reaches the load.</summary>
        public bool TryEngage()
        {
            if (IsSolved) { HudMessage.Show("Power is already routed."); return false; }
            if (!RouteMatches())
            {
                HudMessage.Show(wrongMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Spark, transform.position, 1f);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.9f);
                NoiseSystem.Emit(transform.position, wrongNoiseRadius);
                if (arcLight != null) { arcLight.enabled = true; arcTimer = 0.35f; }
                return false;
            }
            IsSolved = true;
            WorldFlags.Set(Flag);
            HudMessage.Show(solvedMessage);
            AudioManager.Instance?.PlayAt(PlaceholderClips.Buzz, transform.position, 0.9f);
            onSolved.Invoke();
            return true;
        }

        // UnityEvent-friendly wrapper.
        public void Engage() => TryEngage();
    }
}
