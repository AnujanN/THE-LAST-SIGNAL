using System.Text;
using LastSignal.GameFlow;
using LastSignal.Puzzles;
using LastSignal.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastSignal.UI
{
    /// <summary>
    /// Keypad panel for CodeLocks: big touch buttons (0-9, clear, enter) plus keyboard digits.
    /// Time keeps running while it is open, so typing near The Echo is dangerous.
    /// </summary>
    public class CodePanelUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text displayText;

        private CodeLock current;
        private readonly StringBuilder entered = new StringBuilder(8);
        private readonly StringBuilder view = new StringBuilder(32);
        private readonly object owner = new object();

        public bool IsOpen => panel != null && panel.activeSelf;

        private void Awake()
        {
            if (panel != null) panel.SetActive(false);
        }

        private void OnEnable()
        {
            CodeLock.OpenRequested += Open;
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.GameOverChanged += OnGameOver;
        }

        private void OnDisable()
        {
            CodeLock.OpenRequested -= Open;
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.GameOverChanged -= OnGameOver;
            ModalStack.Close(owner);
        }

        private void OnGameOver(bool over)
        {
            if (over) Close();
        }

        public void Open(CodeLock lockToOpen)
        {
            if (panel == null || lockToOpen == null) return;
            current = lockToOpen;
            entered.Clear();
            if (titleText != null) titleText.text = lockToOpen.Title;
            Refresh();
            panel.SetActive(true);
            ModalStack.Open(owner, freezeTime: false);
        }

        public void Close()
        {
            if (panel != null) panel.SetActive(false);
            current = null;
            ModalStack.Close(owner);
        }

        public void PressDigit(int digit)
        {
            if (current == null || entered.Length >= current.CodeLength) return;
            entered.Append((char)('0' + Mathf.Clamp(digit, 0, 9)));
            Audio.AudioManager.Instance?.PlayUI(Audio.PlaceholderClips.Click, 0.35f);
            Refresh();
        }

        public void Clear()
        {
            entered.Clear();
            Refresh();
        }

        public void Submit()
        {
            if (current == null) return;
            if (entered.Length < current.CodeLength)
            {
                HudMessage.Show("Enter all " + current.CodeLength + " digits.");
                return;
            }
            bool ok = current.TrySubmit(entered.ToString());
            if (ok) Close();
            else
            {
                entered.Clear();
                Refresh();
            }
        }

        private void Update()
        {
            if (!IsOpen) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;
            for (int d = 0; d <= 9; d++)
            {
                // Top-row keys run Digit1..Digit9 then Digit0; numpad keys run Numpad0..Numpad9.
                Key top = d == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + d - 1);
                if (kb[top].wasPressedThisFrame || kb[(Key)((int)Key.Numpad0 + d)].wasPressedThisFrame)
                    PressDigit(d);
            }
            if (kb.backspaceKey.wasPressedThisFrame) Clear();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) Submit();
            if (kb.escapeKey.wasPressedThisFrame) Close();
        }

        private void Refresh()
        {
            if (displayText == null || current == null) return;
            view.Clear();
            for (int i = 0; i < current.CodeLength; i++)
            {
                if (i > 0) view.Append(' ');
                view.Append(i < entered.Length ? entered[i] : '_');
            }
            displayText.text = view.ToString();
        }
    }
}
