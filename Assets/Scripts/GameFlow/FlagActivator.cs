using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Shows (or hides) objects once a world flag is set, including after checkpoint reloads.
    /// Example: the basement key appears after the tape has been played.
    /// </summary>
    public class FlagActivator : MonoBehaviour
    {
        [SerializeField] private string flag;
        [SerializeField] private GameObject[] show = new GameObject[0];
        [SerializeField] private GameObject[] hide = new GameObject[0];

        private void Start()
        {
            Apply(WorldFlags.Has(flag));
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.FlagSet += OnFlag;
        }

        private void OnDestroy()
        {
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.FlagSet -= OnFlag;
        }

        private void OnFlag(string f)
        {
            if (f == flag) Apply(true);
        }

        private void Apply(bool set)
        {
            foreach (GameObject g in show) if (g != null) g.SetActive(set);
            foreach (GameObject g in hide) if (g != null) g.SetActive(!set);
        }
    }
}
