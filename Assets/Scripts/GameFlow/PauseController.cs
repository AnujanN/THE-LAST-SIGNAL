using System;
using LastSignal.Player;
using LastSignal.Utilities;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Pause menu: Resume, Restart Checkpoint, Settings, Main Menu.
    /// Freezes time, audio and gameplay input through the ModalStack.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene if left empty.")]
        [SerializeField] private PlayerInputRouter router;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject mainPage;
        [SerializeField] private GameObject settingsPage;

        public bool IsPaused { get; private set; }
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            if (router == null) router = FindAnyObjectByType<PlayerInputRouter>();
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (router == null || !router.PausePressed) return;
            if (GameFlowManager.Instance != null && (GameFlowManager.Instance.IsGameOver || GameFlowManager.Instance.IsLoading)) return;
            // Another panel (note, inventory) is open: let it handle the press.
            if (!IsPaused && ModalStack.AnyOpen) return;
            if (IsPaused && settingsPage != null && settingsPage.activeSelf) { ShowMain(); return; }
            SetPaused(!IsPaused);
        }

        public void Pause() => SetPaused(true);
        public void Resume() => SetPaused(false);

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            if (paused) ModalStack.Open(this, pauseAudio: true);
            else ModalStack.Close(this);
            if (pausePanel != null) pausePanel.SetActive(paused);
            ShowMain();
            PauseChanged?.Invoke(paused);
        }

        public void RestartCheckpoint()
        {
            SetPaused(false);
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.RetryCheckpoint();
        }

        public void OpenSettings()
        {
            if (mainPage != null) mainPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(true);
        }

        public void ShowMain()
        {
            if (mainPage != null) mainPage.SetActive(true);
            if (settingsPage != null) settingsPage.SetActive(false);
        }

        public void QuitToMainMenu()
        {
            SetPaused(false);
            if (GameFlowManager.Instance != null) GameFlowManager.Instance.GoToMainMenu();
        }

        private void OnDestroy()
        {
            if (IsPaused) ModalStack.Close(this);
        }
    }
}
