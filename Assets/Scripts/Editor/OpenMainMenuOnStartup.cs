using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// When the Editor starts on the template's GetStarted_Scene, open the game's MainMenu instead,
    /// so pressing Play starts the game. Runs once per Editor session and never discards unsaved work.
    /// </summary>
    [InitializeOnLoad]
    public static class OpenMainMenuOnStartup
    {
        private const string SessionKey = "LastSignal.OpenedMainMenuOnStartup";
        private const string TemplateScene = "Assets/Scenes/GetStarted_Scene.unity";

        static OpenMainMenuOnStartup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            EditorApplication.delayCall += OpenIfOnTemplate;
        }

        private static void OpenIfOnTemplate()
        {
            SessionState.SetBool(SessionKey, true);
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            Scene active = SceneManager.GetActiveScene();
            if (active.path != TemplateScene || active.isDirty) return;
            EditorSceneManager.OpenScene(MenuScenesBuilder.MainMenuPath);
        }
    }
}
