using System;
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
    /// A keypad, combination dial or safe. The code is never shown in the game directly: the player
    /// works it out from documents. Wrong entries beep loudly enough for The Echo to hear, and after
    /// a few failures a gentle hint points at where the answer is.
    /// Opening the keypad does not freeze time.
    /// </summary>
    public class CodeLock : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "keypad";
        [SerializeField] private string code = "0000";
        [SerializeField] private string title = "KEYPAD";
        [SerializeField] private string prompt = "Use keypad";
        [Tooltip("Optional flag needed before the keypad works (e.g. power).")]
        [SerializeField] private string requiredFlag = "";
        [SerializeField] private string missingFlagMessage = "The keypad is dark. No power.";
        [SerializeField] private string wrongMessage = "Wrong code.";
        [Tooltip("Shown after this many wrong entries.")]
        [SerializeField] private int hintAfterWrong = 3;
        [SerializeField] private string hint = "";
        [Tooltip("Noise radius of the error beep (The Echo can hear it).")]
        [SerializeField] private float wrongNoiseRadius = 7f;
        [SerializeField] private UnityEvent onUnlocked = new UnityEvent();

        private int wrongCount;

        /// <summary>Raised when the player opens this lock; the keypad UI listens.</summary>
        public static event Action<CodeLock> OpenRequested;

        public bool IsUnlocked { get; private set; }
        public string Title => title;
        public int CodeLength => code.Length;
        public string Flag => "code:" + id;

        private void Start()
        {
            // Restored silently after a checkpoint reload.
            if (WorldFlags.Has(Flag)) IsUnlocked = true;
        }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled && !IsUnlocked;

        public void Interact(PlayerInteractor interactor)
        {
            if (IsUnlocked) return;
            if (!string.IsNullOrEmpty(requiredFlag) && !WorldFlags.Has(requiredFlag))
            {
                HudMessage.Show(missingFlagMessage);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Click, transform.position, 0.5f);
                return;
            }
            OpenRequested?.Invoke(this);
        }

        /// <summary>Check an entered code. Returns true and unlocks if correct.</summary>
        public bool TrySubmit(string entered)
        {
            if (IsUnlocked) return true;
            if (entered == code)
            {
                IsUnlocked = true;
                WorldFlags.Set(Flag);
                AudioManager.Instance?.PlayAt(PlaceholderClips.Beep, transform.position, 0.8f);
                HudMessage.Show("The lock clicks open.");
                onUnlocked.Invoke();
                return true;
            }
            wrongCount++;
            AudioManager.Instance?.PlayAt(PlaceholderClips.Buzz, transform.position, 0.8f);
            NoiseSystem.Emit(transform.position, wrongNoiseRadius);
            HudMessage.Show(wrongCount >= hintAfterWrong && !string.IsNullOrEmpty(hint) ? wrongMessage + " " + hint : wrongMessage);
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => OpenRequested = null;
    }
}
