using LastSignal.Utilities;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// A two-state switch (light switch, lever, breaker). Toggles target lights,
    /// moves an optional handle, recolours an optional indicator and raises an event.
    /// </summary>
    public class SwitchInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private bool isOn = true;
        [SerializeField] private string turnOnPrompt = "Turn on light";
        [SerializeField] private string turnOffPrompt = "Turn off light";

        [Header("Targets")]
        [SerializeField] private Light[] lights = new Light[0];
        [Tooltip("Optional handle that rotates between the two angles.")]
        [SerializeField] private Transform handle;
        [SerializeField] private Vector3 handleOnEuler = new Vector3(-30f, 0f, 0f);
        [SerializeField] private Vector3 handleOffEuler = new Vector3(30f, 0f, 0f);
        [Tooltip("Optional renderer tinted to show the state (not colour-only: the handle also moves).")]
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color indicatorOn = new Color(0.3f, 1f, 0.4f);
        [SerializeField] private Color indicatorOff = new Color(0.6f, 0.1f, 0.1f);

        [Header("Feedback")]
        [SerializeField] private AudioClip clickSound;
        [SerializeField, Range(0f, 1f)] private float volume = 0.7f;
        [SerializeField] private UnityEvent<bool> onToggled = new UnityEvent<bool>();

        private MaterialPropertyBlock block;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        public bool IsOn => isOn;

        private void Start() => Apply();

        public string GetPrompt(PlayerInteractor interactor) => isOn ? turnOffPrompt : turnOnPrompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor) => SetOn(!isOn);

        public void SetOn(bool on)
        {
            isOn = on;
            Apply();
            AudioClip clip = clickSound != null ? clickSound : PlaceholderAudio.Click;
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
            onToggled.Invoke(isOn);
        }

        private void Apply()
        {
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].enabled = isOn;

            if (handle != null) handle.localRotation = Quaternion.Euler(isOn ? handleOnEuler : handleOffEuler);

            if (indicator != null)
            {
                if (block == null) block = new MaterialPropertyBlock();
                indicator.GetPropertyBlock(block);
                block.SetColor(BaseColorId, isOn ? indicatorOn : indicatorOff);
                indicator.SetPropertyBlock(block);
            }
        }
    }
}
