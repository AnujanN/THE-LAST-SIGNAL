using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Save;
using UnityEngine;
using UnityEngine.UI;

namespace LastSignal.UI
{
    /// <summary>Main menu: New Game, Continue (only with a save), Settings, Quit.</summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button continueButton;
        [SerializeField] private GameObject quitButton;
        [SerializeField] private GameObject mainPage;
        [SerializeField] private GameObject settingsPage;

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (continueButton != null) continueButton.interactable = SaveManager.HasSave;
            AudioManager.Instance?.SetAmbience(Ambience.RainAndWind, 0.45f);
            ShowMain();
        }

        public void NewGame() => GameFlowManager.Instance?.NewGame();
        public void Continue() => GameFlowManager.Instance?.Continue();
        public void Quit() => GameFlowManager.Instance?.QuitGame();

        public void ShowMain() => Show(mainPage);
        public void ShowSettings() => Show(settingsPage);

        private void Show(GameObject page)
        {
            if (mainPage != null) mainPage.SetActive(page == mainPage);
            if (settingsPage != null) settingsPage.SetActive(page == settingsPage);
        }
    }
}
