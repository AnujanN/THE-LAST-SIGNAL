using LastSignal.GameFlow;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the MainMenu and Ending scenes, and a one-click "build everything".</summary>
    public static class MenuScenesBuilder
    {
        public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        public const string EndingPath = "Assets/Scenes/Ending.unity";

        private const string CreditsText =
            "<size=150%>THE LAST SIGNAL</size>\n\nA university game project\n\n\n" +
            "<b>Game design & development</b>\nAnujan N\n\n" +
            "<b>Characters</b>\nAlex\nMaya\nThe Echo\n\n" +
            "<b>Built with</b>\nUnity 6 - Universal Render Pipeline\nClaude Code + Unity MCP\n\n" +
            "<b>Sound effects</b>\nKenney (kenney.nl) - CC0\nAdditional sounds: Joseph Sardin - BigSoundBank.com - CC0\n" +
            "OpenGameArt.org contributors - CC0\n" +
            "\"Heartbeat\" by HerbertBoland (Wikimedia Commons) - CC BY 3.0\n" +
            "CC BY 3.0: creativecommons.org/licenses/by/3.0\n\n" +
            "<b>Art</b>\nTextures: Poly Haven (polyhaven.com) - CC0\n" +
            "Alex and The Echo, characters and animations: Mixamo (Adobe)\n" +
            "Props: Poly Haven - CC0\n" +
            "Maya's ghost: Quaternius, Universal Animation Library - CC0\n\n" +
            "<b>Fonts</b>\nLiberation Sans (TextMeshPro)\nCaveat - The Caveat Project Authors, SIL Open Font License\n\n\n" +
            "For everyone who is still listening.\n\n\nThank you for playing.";

        [MenuItem("The Last Signal/Build/Build All Game Scenes", priority = 0)]
        public static void BuildAll()
        {
            RenderingSetup.Apply();
            GamePrefabs.RebuildAll();
            BuildMainMenu();
            Level1Builder.Build();
            Level2Builder.Build();
            Level3Builder.Build();
            Level4Builder.Build();
            Level5Builder.Build();
            BuildEnding();
            ApplyBuildScenes();
            EditorSceneManagerOpen(MainMenuPath);
            Debug.Log("[Build] All game scenes rebuilt.");
        }

        /// <summary>The scene order in the build: menu, the five levels, ending.</summary>
        public static readonly string[] BuildScenes =
        {
            MainMenuPath, Level1Builder.ScenePath, Level2Builder.ScenePath, Level3Builder.ScenePath,
            Level4Builder.ScenePath, Level5Builder.ScenePath, EndingPath,
        };

        [MenuItem("The Last Signal/Build/Apply Build Scene List")]
        public static void ApplyBuildScenes()
        {
            var list = new EditorBuildSettingsScene[BuildScenes.Length];
            for (int i = 0; i < BuildScenes.Length; i++) list[i] = new EditorBuildSettingsScene(BuildScenes[i], true);
            EditorBuildSettings.scenes = list;
            Debug.Log("[Build] Build scene list: " + string.Join(", ", BuildScenes));
        }

        private static void EditorSceneManagerOpen(string path) =>
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);

        // ================================================================== Main menu

        [MenuItem("The Last Signal/Build/Main Menu")]
        public static void BuildMainMenu()
        {
            var scene = NewScene("MainMenu");
            Atmosphere(new Color(0.05f, 0.05f, 0.06f), Color.black, 0f);
            var camGo = new GameObject("MenuCamera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.02f, 0.02f, 0.03f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();

            GameObject root = Canvas("MenuCanvas");
            var menu = root.AddComponent<MainMenuController>();
            Sprite vignette = EnsureSprite("Vignette", SpriteShape.Vignette);
            RectTransform vig = UIRect(root.transform, "Vignette");
            Stretch(vig);
            UIImage(vig, vignette, new Color(0f, 0f, 0f, 0.9f), raycast: false);

            var c = new Vector2(0.5f, 0.5f);
            RectTransform main = UIRect(root.transform, "MainPage");
            Stretch(main);
            TMP_Text title = UIText(main, "Title", "THE LAST SIGNAL", 110, c, new Vector2(0f, 300f), new Vector2(1600f, 160f));
            title.characterSpacing = 12f;
            UIText(main, "Tagline", "Something in the static knows your name.", 32, c, new Vector2(0f, 205f), new Vector2(1400f, 60f))
                .color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
            UIButton(main, "NewGame", "NEW GAME", c, new Vector2(0f, 70f), new Vector2(520f, 95f), 38, menu.NewGame);
            Button cont = UIButton(main, "Continue", "CONTINUE", c, new Vector2(0f, -40f), new Vector2(520f, 95f), 38, menu.Continue);
            UIButton(main, "Settings", "SETTINGS", c, new Vector2(0f, -150f), new Vector2(520f, 95f), 38, menu.ShowSettings);
            UIButton(main, "Credits", "CREDITS", c, new Vector2(0f, -260f), new Vector2(520f, 95f), 38, menu.ShowCredits);
            Button quit = UIButton(main, "Quit", "QUIT", c, new Vector2(0f, -370f), new Vector2(520f, 95f), 38, menu.Quit);

            RectTransform settings = GamePrefabs.SettingsPage(root.transform, menu.ShowMain);

            RectTransform credits = UIRect(root.transform, "CreditsPage");
            Stretch(credits);
            UIText(credits, "Text", CreditsText.Replace("\n\n\nThank you for playing.", ""), 30, c, new Vector2(0f, 60f), new Vector2(1200f, 820f),
                TextAlignmentOptions.Center, wrap: true);
            UIButton(credits, "Back", "BACK", c, new Vector2(0f, -430f), new Vector2(360f, 90f), 36, menu.ShowMain);
            credits.gameObject.SetActive(false);

            Set(menu, "continueButton", cont);
            Set(menu, "quitButton", quit.gameObject);
            Set(menu, "mainPage", main.gameObject);
            Set(menu, "settingsPage", settings.gameObject);
            Set(menu, "creditsPage", credits.gameObject);

            EventSystemObject();
            SaveScene(scene, MainMenuPath);
            Debug.Log("[MenuScenesBuilder] Built " + MainMenuPath);
        }

        // ================================================================== Ending

        [MenuItem("The Last Signal/Build/Ending")]
        public static void BuildEnding() => EndingBuilder.Build(EndingPath, Canvas);

        private static GameObject Canvas(string name)
        {
            var root = new GameObject(name, typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 5;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();
            return root;
        }
    }
}
