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
        public static readonly Vector2 LogoBeacon = new Vector2(814f / 1648f, 1f - 110f / 796f);
        // The logo's three waves on either side of the light (measured in its pixels): radius, how far round they reach.
        private static readonly float[] WaveRadius = { 68f, 108f, 151f };
        private static readonly float[] WaveReach = { 46f, 42f, 38f };

        /// <summary>
        /// The light on the logo's tower and its three waves as separate glows over the logo, driven by TitleBeacon:
        /// each pulse the light flashes, then the waves light one after another outwards.
        /// </summary>
        public static TitleBeacon LogoBeaconLights(RectTransform logo, float uiWidth, bool autoPulse, float volume, CanvasGroup visibility)
        {
            var c = new Vector2(0.5f, 0.5f);
            float s = uiWidth / 1648f;
            Vector2 at = new Vector2((814f - 824f) * s, (398f - 110f) * s);
            var waves = new Image[WaveRadius.Length];
            for (int i = 0; i < waves.Length; i++)
            {
                Sprite sprite = WaveSprite(i, WaveRadius[i], 18f, WaveReach[i]);
                RectTransform wr = UIRect(logo, "Wave" + (i + 1));
                float size = sprite.texture.width * s;
                Place(wr, c, at, new Vector2(size, size));
                waves[i] = UIImage(wr, sprite, new Color(1f, 0.32f, 0.22f, 0f), raycast: false);
            }
            RectTransform dotRect = UIRect(logo, "BeaconDot");
            Place(dotRect, c, at, new Vector2(30f * s / 0.7f, 30f * s / 0.7f));
            Image dot = UIImage(dotRect, EnsureSprite("Circle", SpriteShape.Disc), new Color(1f, 0.08f, 0.05f), raycast: false);
            RectTransform glowRect = UIRect(dotRect, "Glow");
            Place(glowRect, c, Vector2.zero, new Vector2(160f * s / 0.7f, 160f * s / 0.7f));
            Image glow = UIImage(glowRect, EnsureSprite("SoftDot", SpriteShape.Soft), new Color(1f, 0.08f, 0.05f, 0f), raycast: false);
            var beacon = logo.gameObject.AddComponent<TitleBeacon>();
            Set(beacon, "logo", logo);
            SetValue(beacon, "logoPoint", LogoBeacon);
            Set(beacon, "dot", dotRect);
            Set(beacon, "dotImage", dot);
            Set(beacon, "glow", glow);
            SetArray(beacon, "waves", waves);
            SetValue(beacon, "autoPulse", autoPulse);
            SetValue(beacon, "pulseVolume", volume);
            if (visibility != null) Set(beacon, "visibility", visibility);
            return beacon;
        }

        /// <summary>One wave (left and right arcs) as a soft white sprite: a bright core with a faint halo, fading at the ends.</summary>
        private static Sprite WaveSprite(int index, float radius, float thickness, float reachDegrees)
        {
            string path = "Assets/Art/UI/Generated/LogoWave" + (index + 1) + ".png";
            int n = Mathf.CeilToInt((radius + thickness * 2f + 4f) * 2f);
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color[n * n];
            float cx = n / 2f, cy = n / 2f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float off = Mathf.Abs(d - radius);
                    float core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(thickness * 0.35f, thickness * 0.55f, off));
                    float halo = Mathf.Exp(-off * off / (thickness * thickness * 1.2f));
                    float ang = Mathf.Atan2(Mathf.Abs(dy), Mathf.Abs(dx)) * Mathf.Rad2Deg;
                    float ends = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(reachDegrees - 12f, reachDegrees, ang));
                    float a = Mathf.Clamp01(core * 0.95f + halo * 0.4f) * ends;
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            tex.SetPixels(px);
            tex.Apply();
            EnsureFolder("Assets/Art/UI/Generated");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

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
            LogoBeaconLights(logo, 820f, autoPulse: true, volume: 0f, visibility: null);
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
