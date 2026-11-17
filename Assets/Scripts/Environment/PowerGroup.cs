using LastSignal.Audio;
using LastSignal.GameFlow;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Environment
{
    /// <summary>
    /// Lights and objects that need mains power. State is stored in a world flag, so it survives
    /// checkpoint reloads. Puzzles call PowerOn(); the finale calls PowerOff().
    /// </summary>
    public class PowerGroup : MonoBehaviour
    {
        [SerializeField] private string powerFlag = "power:station";
        [SerializeField] private string offFlag = "";
        [SerializeField] private Light[] lights = new Light[0];
        [SerializeField] private GameObject[] showWhenPowered = new GameObject[0];
        [SerializeField] private GameObject[] hideWhenPowered = new GameObject[0];
        [SerializeField] private Renderer[] emissive = new Renderer[0];
        [SerializeField] private Color emissionOn = new Color(1f, 0.9f, 0.7f);
        [SerializeField] private UnityEvent onPowered = new UnityEvent();

        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
        private MaterialPropertyBlock block;

        public bool IsPowered { get; private set; }

        private void Start()
        {
            // Restore silently; onPowered already ran before the checkpoint.
            bool on = WorldFlags.Has(powerFlag) && (string.IsNullOrEmpty(offFlag) || !WorldFlags.Has(offFlag));
            Apply(on);
        }

        public void PowerOn()
        {
            if (IsPowered) return;
            WorldFlags.Set(powerFlag);
            AudioManager.Instance?.PlayAt(PlaceholderClips.Buzz, transform.position, 0.8f);
            Apply(true);
            onPowered.Invoke();
        }

        public void PowerOff()
        {
            if (!string.IsNullOrEmpty(offFlag)) WorldFlags.Set(offFlag);
            Apply(false);
        }

        private void Apply(bool on)
        {
            IsPowered = on;
            foreach (Light l in lights) if (l != null) l.enabled = on;
            foreach (GameObject g in showWhenPowered) if (g != null) g.SetActive(on);
            foreach (GameObject g in hideWhenPowered) if (g != null) g.SetActive(!on);
            if (block == null) block = new MaterialPropertyBlock();
            foreach (Renderer r in emissive)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(EmissionId, on ? emissionOn : Color.black);
                r.SetPropertyBlock(block);
            }
        }
    }
}
