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
        public const string IntroPath = "Assets/Scenes/Intro.unity";

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
            BuildIntro();
            ApplyBuildScenes();
            EditorSceneManagerOpen(MainMenuPath);
            Debug.Log("[Build] All game scenes rebuilt.");
        }

        /// <summary>The scene order in the build: menu, the five levels, ending.</summary>
        public static readonly string[] BuildScenes =
        {
            MainMenuPath, IntroPath, Level1Builder.ScenePath, Level2Builder.ScenePath, Level3Builder.ScenePath,
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

        // ================================================================== Logo

        public const string LogoPath = "Assets/Art/UI/Logo.png";
        /// <summary>Where the light on top of the logo's tower is (0..1 across, 0..1 up the image).</summary>
        public static readonly Vector2 LogoBeacon = new Vector2(811f / 1648f, 1f - 110f / 796f);

        /// <summary>The game's logo as a UI sprite (transparent background, imported crisp, no mipmaps).</summary>
        public static Sprite Logo()
        {
            var imp = AssetImporter.GetAtPath(LogoPath) as TextureImporter;
            if (imp != null && (imp.textureType != TextureImporterType.Sprite || imp.mipmapEnabled || imp.maxTextureSize != 2048))
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = 2048;
                imp.textureCompression = TextureImporterCompression.CompressedHQ;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(LogoPath);
        }

        /// <summary>The logo's size at a given width, keeping its shape.</summary>
        public static Vector2 LogoSize(float width) => new Vector2(width, width * 796f / 1648f);

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
            // The logo, its tower light blinking red (silently here; the intro's title has the transmitter pulse).
            RectTransform logo = UIRect(main, "Logo");
            Place(logo, c, new Vector2(0f, 300f), LogoSize(820f));
            UIImage(logo, Logo(), Color.white, raycast: false).preserveAspect = true;
            RectTransform dotRect = UIRect(logo, "BeaconDot");
            Place(dotRect, c, Vector2.zero, new Vector2(20f, 20f));
            Image dot = UIImage(dotRect, EnsureSprite("Circle", SpriteShape.Disc), new Color(1f, 0.08f, 0.05f), raycast: false);
            RectTransform glowRect = UIRect(dotRect, "Glow");
            Place(glowRect, c, Vector2.zero, new Vector2(120f, 120f));
            Image glow = UIImage(glowRect, EnsureSprite("SoftDot", SpriteShape.Soft), new Color(1f, 0.08f, 0.05f, 0f), raycast: false);
            var beacon = logo.gameObject.AddComponent<TitleBeacon>();
            Set(beacon, "logo", logo);
            SetValue(beacon, "logoPoint", LogoBeacon);
            Set(beacon, "dot", dotRect);
            Set(beacon, "dotImage", dot);
            Set(beacon, "glow", glow);
            SetValue(beacon, "pulseVolume", 0f);
            UIText(main, "Tagline", "Something in the static knows your name.", 30, c, new Vector2(0f, 82f), new Vector2(1400f, 50f))
                .color = new Color(0.8f, 0.8f, 0.8f, 0.7f);
            UIButton(main, "NewGame", "NEW GAME", c, new Vector2(0f, -15f), new Vector2(520f, 95f), 38, menu.NewGame);
            Button cont = UIButton(main, "Continue", "CONTINUE", c, new Vector2(0f, -125f), new Vector2(520f, 95f), 38, menu.Continue);
            UIButton(main, "Settings", "SETTINGS", c, new Vector2(0f, -235f), new Vector2(520f, 95f), 38, menu.ShowSettings);
            Button quit = UIButton(main, "Quit", "QUIT", c, new Vector2(0f, -345f), new Vector2(520f, 95f), 38, menu.Quit);

            RectTransform settings = GamePrefabs.SettingsPage(root.transform, menu.ShowMain);


            Set(menu, "continueButton", cont);
            Set(menu, "quitButton", quit.gameObject);
            Set(menu, "mainPage", main.gameObject);
            Set(menu, "settingsPage", settings.gameObject);

            EventSystemObject();
            SaveScene(scene, MainMenuPath);
            Debug.Log("[MenuScenesBuilder] Built " + MainMenuPath);
        }

        // ================================================================== Ending

        [MenuItem("The Last Signal/Build/Ending")]
        public static void BuildEnding() => EndingBuilder.Build(EndingPath, Canvas);

        [MenuItem("The Last Signal/Build/Intro")]
        public static void BuildIntro() => IntroBuilder.Build(IntroPath, Canvas);

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
