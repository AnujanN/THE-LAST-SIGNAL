using System;
using LastSignal.Player;
using UnityEngine;

namespace LastSignal.GameFlow
{
    /// <summary>
    /// Basic pause: freezes time and gameplay input and shows the pause panel.
    /// The full pause menu (restart checkpoint, settings, main menu) comes in M2.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [Tooltip("Found automatically in the scene if left empty.")]
        [SerializeField] private PlayerInputRouter router;
        [SerializeField] private GameObject pausePanel;

        public bool IsPaused { get; private set; }
        public event Action<bool> PauseChanged;

        private void Awake()
        {
            if (router == null) router = FindAnyObjectByType<PlayerInputRouter>();
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (router != null && router.PausePressed) SetPaused(!IsPaused);
        }

        public void Pause() => SetPaused(true);
        public void Resume() => SetPaused(false);

        public void SetPaused(bool paused)
        {
            if (paused == IsPaused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (router != null) router.GameplayBlocked = paused;
            if (pausePanel != null) pausePanel.SetActive(paused);
            if (paused) PlayerInputRouter.ReleaseCursor();
            PauseChanged?.Invoke(paused);
        }

        private void OnDestroy()
        {
            if (!IsPaused) return;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}
