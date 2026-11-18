using System;
using LastSignal.Audio;
using LastSignal.Interaction;
using TMPro;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// A rotary selector on a junction box. Each use turns it to the next position; the knob turns
    /// visibly and a small label shows the current setting.
    /// </summary>
    public class SelectorSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string label = "FEED";
        [SerializeField] private string[] positions = { "A", "B", "C" };
        [SerializeField] private int index;
        [SerializeField] private Transform knob;
        [SerializeField] private TMP_Text readout;
        [SerializeField] private float degreesPerStep = 60f;

        public event Action<SelectorSwitch> Changed;

        public int Index => index;
        public string Current => positions.Length > 0 ? positions[Mathf.Clamp(index, 0, positions.Length - 1)] : "";
        public string Label => label;

        private void Start() => Apply();

        public string GetPrompt(PlayerInteractor interactor) => "Turn " + label + " (now " + Current + ")";

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && positions.Length > 1;

        public void Interact(PlayerInteractor interactor) => Set((index + 1) % positions.Length);

        public void Set(int newIndex, bool notify = true)
        {
            if (positions.Length == 0) return;
            index = Mathf.Clamp(newIndex, 0, positions.Length - 1);
            Apply();
            if (!notify) return;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Click, transform.position, 0.6f);
            Changed?.Invoke(this);
        }

        private void Apply()
        {
            if (knob != null) knob.localRotation = Quaternion.Euler(0f, 0f, -index * degreesPerStep);
            if (readout != null) readout.text = label + "\n" + Current;
        }
    }
}
