using System;
using LastSignal.Interaction;
using UnityEngine;

namespace LastSignal.Puzzles
{
    /// <summary>
    /// One breaker in the fuse box. UP/DOWN is shown by the handle position and in the prompt,
    /// not by colour alone.
    /// </summary>
    public class BreakerSwitch : MonoBehaviour, IInteractable
    {
        [SerializeField] private string label = "B1";
        [SerializeField] private bool isUp;
        [SerializeField] private Transform handle;
        [SerializeField] private float upAngle = -35f;
        [SerializeField] private float downAngle = 35f;

        private string promptUp, promptDown;

        public string Label => label;
        public bool IsUp => isUp;
        public event Action<BreakerSwitch> Changed;

        private void Awake()
        {
            promptUp = "Breaker " + label + ": UP  (flip down)";
            promptDown = "Breaker " + label + ": DOWN  (flip up)";
            ApplyVisual();
        }

        public string GetPrompt(PlayerInteractor interactor) => isUp ? promptUp : promptDown;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor) => Set(!isUp);

        public void Set(bool up, bool notify = true)
        {
            isUp = up;
            ApplyVisual();
            Audio.AudioManager.Instance?.PlayAt(Utilities.PlaceholderAudio.Click, transform.position, 0.8f);
            if (notify) Changed?.Invoke(this);
        }

        private void ApplyVisual()
        {
            if (handle != null) handle.localRotation = Quaternion.Euler(isUp ? upAngle : downAngle, 0f, 0f);
        }
    }
}
