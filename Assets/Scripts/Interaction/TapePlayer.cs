using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Player;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Plays a recording as subtitled lines. Can require items (e.g. 3 tape fragments) and/or a
    /// flag (e.g. power). With lockPlayer the player cannot move until it finishes, and there is
    /// no skip, so the story recording always plays in full.
    /// </summary>
    public class TapePlayer : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "tape";
        [SerializeField] private string deviceName = "tape player";
        [SerializeField] private string requiredItemId;
        [SerializeField, Min(1)] private int requiredCount = 1;
        [SerializeField] private string missingItemMessage = "There is no tape in it.";
        [SerializeField] private string requiredFlag;
        [SerializeField] private string missingFlagMessage = "It has no power.";
        [SerializeField] private bool lockPlayer;
        [SerializeField] private bool replayable = true;
        [SerializeField] private SubtitleLine[] lines = new SubtitleLine[0];
        [SerializeField] private UnityEvent onStarted = new UnityEvent();
        [SerializeField] private UnityEvent onFinished = new UnityEvent();

        private bool playing;
        private string prompt, replayPrompt;

        public bool IsPlaying => playing;
        public bool HasPlayed => WorldFlags.Has("played:" + id);

        private void Awake()
        {
            prompt = "Play " + deviceName;
            replayPrompt = "Replay " + deviceName;
        }

        public string GetPrompt(PlayerInteractor interactor) => HasPlayed ? replayPrompt : prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !playing && (replayable || !HasPlayed);

        public void Interact(PlayerInteractor interactor)
        {
            if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag))
            {
                interactor.ShowMessage(missingFlagMessage);
                return;
            }
            if (!HasPlayed && !string.IsNullOrEmpty(requiredItemId) &&
                (interactor.Inventory == null || !interactor.Inventory.Has(requiredItemId, requiredCount)))
            {
                interactor.ShowMessage(missingItemMessage);
                return;
            }
            Play(interactor.GetComponent<PlayerController>());
        }

        public void Play(PlayerController player = null)
        {
            if (playing) return;
            playing = true;
            if (lockPlayer && player != null) player.MovementLocked = true;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Beep, transform.position, 0.6f);
            if (!HasPlayed) onStarted.Invoke();

            void Done()
            {
                playing = false;
                if (lockPlayer && player != null) player.MovementLocked = false;
                bool first = !HasPlayed;
                WorldFlags.Set("played:" + id);
                if (first) onFinished.Invoke();
            }

            if (SubtitleManager.Instance != null) SubtitleManager.Instance.Play(lines, Done);
            else Done();
        }
    }
}
