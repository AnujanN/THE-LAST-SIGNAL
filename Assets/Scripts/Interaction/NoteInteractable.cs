using System;
using LastSignal.GameFlow;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>Readable note, log or label. Opens the note reader (game pauses while reading).</summary>
    public class NoteInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "note";
        [SerializeField] private string title = "Note";
        [SerializeField, TextArea(3, 12)] private string body = "";
        [SerializeField] private UnityEvent onRead = new UnityEvent();

        /// <summary>The HUD's note reader listens for this (title, body).</summary>
        public static event Action<string, string> ReadRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => ReadRequested = null;

        private string prompt;

        public string Title => title;
        public string Body => body;
        public bool HasBeenRead => WorldFlags.Has("read:" + id);

        private void Awake() => prompt = "Read " + title;

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor)
        {
            ReadRequested?.Invoke(title, body);
            bool first = !HasBeenRead;
            WorldFlags.Set("read:" + id);
            if (first) onRead.Invoke();
        }
    }
}
