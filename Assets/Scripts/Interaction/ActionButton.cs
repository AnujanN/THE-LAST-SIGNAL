using LastSignal.Audio;
using LastSignal.GameFlow;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Generic control: lever, button, knob. Raises onPressed when used. Can require a flag
    /// (e.g. power) and can be one-shot.
    /// </summary>
    public class ActionButton : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "button";
        [SerializeField] private string prompt = "Use";
        [SerializeField] private string requiredFlag;
        [SerializeField] private string lockedMessage = "Nothing happens.";
        [SerializeField] private bool oneShot;
        [Tooltip("Optional part that dips briefly when pressed.")]
        [SerializeField] private Transform pressVisual;
        [SerializeField] private UnityEvent onPressed = new UnityEvent();

        private float pressTimer;
        private Vector3 restPosition;

        private string UsedFlag => "used:" + id;
        public bool Used => oneShot && WorldFlags.Has(UsedFlag);

        private void Awake()
        {
            if (pressVisual != null) restPosition = pressVisual.localPosition;
        }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !Used;

        public void Interact(PlayerInteractor interactor) => Press();

        public void Press()
        {
            if (Used) return;
            if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag))
            {
                Utilities.HudMessage.Show(lockedMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Thud, transform.position, 0.4f);
                return;
            }
            if (oneShot) WorldFlags.Set(UsedFlag);
            AudioManager.Instance?.PlayAt(Utilities.PlaceholderAudio.Click, transform.position, 0.7f);
            if (pressVisual != null)
            {
                pressVisual.localPosition = restPosition + pressVisual.localRotation * Vector3.back * 0.02f;
                pressTimer = 0.15f;
            }
            onPressed.Invoke();
        }

        private void Update()
        {
            if (pressTimer <= 0f) return;
            pressTimer -= Time.deltaTime;
            if (pressTimer <= 0f && pressVisual != null) pressVisual.localPosition = restPosition;
        }
    }
}
