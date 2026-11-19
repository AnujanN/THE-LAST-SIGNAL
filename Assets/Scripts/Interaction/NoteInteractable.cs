using System;
using LastSignal.GameFlow;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace LastSignal.Interaction
{
    /// <summary>
    /// Readable note, log or label. A note lying on a table is picked up and held in Alex's hand to read
    /// (its words appear on the sheet itself); a note pinned to a wall opens the note reader. The game pauses
    /// while reading either way.
    /// </summary>
    public class NoteInteractable : MonoBehaviour, IInteractable
    {
        [SerializeField] private string id = "note";
        [SerializeField] private string title = "Note";
        [SerializeField, TextArea(3, 12)] private string body = "";
        [SerializeField] private UnityEvent onRead = new UnityEvent();

        [Header("Paper")]
        [Tooltip("Paper look (see PaperArt): 0 aged, 1 notebook, 2 graph, 3 memo, 4 crumpled scrap, 5 water-damaged.")]
        [SerializeField] private int paperStyle;
        [SerializeField] private Color ink = new Color(0.11f, 0.09f, 0.13f);
        [Tooltip("Lying loose (table, floor): Alex picks it up and holds it to read.")]
        [SerializeField] private bool canHold;
        [Tooltip("Plain sheet shown while held (the words are the text below).")]
        [SerializeField] private Material heldMaterial;
        [Tooltip("The words, written on the sheet; only shown while held.")]
        [SerializeField] private TMP_Text writing;

        /// <summary>The HUD's note reader listens for this (note, held in the hand?).</summary>
        public static event Action<NoteInteractable, bool> ReadRequested;
        /// <summary>A holder (Alex's hand) for loose notes; null = always use the note reader.</summary>
        public static Func<NoteInteractable, bool> HoldRequested;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            ReadRequested = null;
            HoldRequested = null;
        }

        private string prompt;

        public string Id => id;
        public string Title => title;
        public string Body => body;
        public int PaperStyle => paperStyle;
        public Color Ink => ink;
        public bool CanHold => canHold;
        public Material HeldMaterial => heldMaterial;
        public TMP_Text Writing => writing;
        /// <summary>The words are showing on the sheet (it is being held).</summary>
        public bool WritingShown => writing != null && writing.gameObject.activeInHierarchy;
        public bool HasBeenRead => WorldFlags.Has("read:" + id);

        private void Awake()
        {
            prompt = (canHold ? "Pick up " : "Read ") + title;
            if (writing != null) writing.gameObject.SetActive(false);
        }

        public string GetPrompt(PlayerInteractor interactor) => prompt;

        public bool CanInteract(PlayerInteractor interactor) => isActiveAndEnabled;

        public void Interact(PlayerInteractor interactor)
        {
            bool held = canHold && HoldRequested != null && HoldRequested(this);
            ReadRequested?.Invoke(this, held);
            bool first = !HasBeenRead;
            WorldFlags.Set("read:" + id);
            if (first) onRead.Invoke();
        }
    }
}
