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
            "<b>Sound effects</b>\nKenney (kenney.nl) - CC0\nJoseph Sardin (BigSoundBank / LaSonotheque) - CC0\n" +
            "OpenGameArt.org contributors - CC0\n\"Rain and Thunder Loop\" by DoKashiteru - CC BY 3.0\n" +
            "\"Heartbeat\" by HerbertBoland (Wikimedia Commons) - CC BY 3.0\n" +
            "CC BY 3.0: creativecommons.org/licenses/by/3.0\n\n" +
            "<b>Fonts</b>\nLiberation Sans (TextMeshPro)\n\n\n" +
            "For everyone who is still listening.\n\n\nThank you for playing.";

        [MenuItem("The Last Signal/Build/Build All Game Scenes", priority = 0)]
        public static void BuildAll()
        {
            GamePrefabs.RebuildAll();
            BuildMainMenu();
            Level1Builder.Build();
            Level2Builder.Build();
            Level3Builder.Build();
            BuildEnding();
            EditorSceneManagerOpen(MainMenuPath);
            Debug.Log("[Build] All game scenes rebuilt.");
        }

        private static void EditorSceneManagerOpen(string path) =>
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path);

        // ================================================================== Main menu

        [MenuItem("The Last Signal/Build/Main Menu")]
        public static void BuildMainMenu()
        {
            var scene = NewScene();
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
        public static void BuildEnding()
        {
            var scene = NewScene();
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/SunriseSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, MaterialDir + "/SunriseSky.mat");
            }
            sky.SetFloat("_SunSize", 0.06f);
            sky.SetFloat("_AtmosphereThickness", 1.6f);
            sky.SetColor("_SkyTint", new Color(0.75f, 0.6f, 0.55f));
            sky.SetColor("_GroundColor", new Color(0.25f, 0.22f, 0.2f));
            sky.SetFloat("_Exposure", 1.1f);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.48f, 0.42f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.85f, 0.6f, 0.45f);
            RenderSettings.fogDensity = 0.012f;

            Transform env = Group(null, "Environment");
            Light sun = DirectionalLight(env, "Sunrise", new Vector3(16f, -35f, 0f), new Color(1f, 0.75f, 0.52f), 1.6f);
            RenderSettings.sun = sun;
            Box(env, "Hill", new Vector3(0f, -0.1f, 40f), new Vector3(300f, 0.2f, 300f), Mat("Grass", new Color(0.3f, 0.42f, 0.2f)));
            Material stone = Mat("Stone", new Color(0.55f, 0.55f, 0.53f));
            Box(env, "MemorialStone", new Vector3(0f, 0.55f, 0f), new Vector3(0.9f, 1.1f, 0.25f), stone);
            Box(env, "MemorialBase", new Vector3(0f, 0.06f, 0.05f), new Vector3(1.3f, 0.12f, 0.6f), stone);
            Sign(env, "Inscription", "MAYA\n\n<size=60%>She stayed so that I could leave.</size>", new Vector3(0f, 0.65f, -0.13f), 0f, 1.6f,
                new Color(0.2f, 0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            Box(env, "Cassette", new Vector3(0.25f, 0.15f, -0.18f), new Vector3(0.11f, 0.02f, 0.07f), Mat("Tape", new Color(0.12f, 0.12f, 0.14f)));
            Material[] flowerMats = { Mat("FlowerWhite", new Color(0.95f, 0.95f, 0.9f)), Mat("FlowerYellow", new Color(0.95f, 0.8f, 0.3f)) };
            for (int i = 0; i < 6; i++)
                Prim(env, "Flower", PrimitiveType.Sphere, new Vector3(-0.45f + i * 0.08f, 0.16f, -0.2f + (i % 2) * 0.05f), Vector3.one * 0.06f, flowerMats[i % 2], collider: false);
            // The radio station silhouette on the horizon, quiet at last.
            Material silhouette = Mat("Silhouette", new Color(0.04f, 0.035f, 0.035f));
            Box(env, "Station", new Vector3(-30f, 4f, 85f), new Vector3(16f, 8f, 10f), silhouette, collider: false);
            Box(env, "Tower", new Vector3(-24f, 14f, 88f), new Vector3(1.2f, 28f, 1.2f), silhouette, collider: false);

            Transform rig = Group(null, "CameraRig", new Vector3(0f, 1.6f, -6f));
            var camGo = new GameObject("EndingCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>();
            Transform start = Marker(null, "CamStart", new Vector3(-1.2f, 1.7f, -6.5f), 10f);
            start.rotation = Quaternion.Euler(6f, 10f, 0f);
            Transform end = Marker(null, "CamEnd", new Vector3(0.3f, 1.2f, -2.4f), 0f);
            end.rotation = Quaternion.Euler(10f, -3f, 0f);
            rig.SetPositionAndRotation(start.position, start.rotation);

            GameObject root = Canvas("EndingCanvas");
            var c = new Vector2(0.5f, 0.5f);
            // Subtitles for the epilogue lines (the HUD is not in this scene).
            var story = root.AddComponent<StoryHUD>();
            RectTransform subtitlePanel = UIRect(root.transform, "Subtitles");
            Place(subtitlePanel, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1100f, 130f));
            UIImage(subtitlePanel, EnsureSprite("Square", SpriteShape.Square), new Color(0f, 0f, 0f, 0.6f), raycast: false);
            TMP_Text subtitleText = UIText(subtitlePanel, "Text", "", 34, c, Vector2.zero, new Vector2(1060f, 120f), TextAlignmentOptions.Center, wrap: true);
            Set(story, "subtitlePanel", subtitlePanel.gameObject);
            Set(story, "subtitleText", subtitleText);
            subtitlePanel.gameObject.SetActive(false);

            TMP_Text theEnd = UIText(root.transform, "TheEnd", "THE END", 120, c, new Vector2(0f, 60f), new Vector2(1400f, 200f));
            theEnd.characterSpacing = 20f;
            var theEndGroup = theEnd.gameObject.AddComponent<CanvasGroup>();
            RectTransform creditsBack = UIRect(root.transform, "CreditsMask");
            Stretch(creditsBack);
            TMP_Text credits = UIText(creditsBack, "Credits", CreditsText, 38, new Vector2(0.5f, 0f), new Vector2(0f, -800f), new Vector2(1300f, 1600f),
                TextAlignmentOptions.Top, wrap: true);
            Button menuButton = UIButton(root.transform, "MainMenuButton", "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(420f, 95f), 36, null);

            var ending = root.AddComponent<EndingSequence>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(menuButton.onClick, ending.ReturnToMenu);
            Set(ending, "cameraRig", rig);
            Set(ending, "cameraStart", start);
            Set(ending, "cameraEnd", end);
            SetLines(ending, "lines", new[]
            {
                L("", "Three months later.", 3.5f),
                L("ALEX", "I listened to the whole tape again, Maya. Every word.", 4.5f),
                L("ALEX", "You got me out. You stayed. And I'm done blaming myself - I promised you I'd live.", 6f),
                L("", "[Alex sets the old cassette beside the memorial.]", 3.5f),
                L("ALEX", "I heard you this time, Maya. Goodbye.", 5f),
            });
            Set(ending, "theEnd", theEndGroup);
            Set(ending, "credits", (RectTransform)credits.transform);
            SetValue(ending, "creditsEndY", 1700f);
            SetValue(ending, "creditsSpeed", 80f);
            Set(ending, "menuButton", menuButton.gameObject);

            EventSystemObject();
            SaveScene(scene, EndingPath);
            Debug.Log("[MenuScenesBuilder] Built " + EndingPath);
        }

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
