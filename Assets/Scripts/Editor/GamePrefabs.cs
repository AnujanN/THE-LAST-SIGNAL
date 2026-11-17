using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>Builds the shared prefabs: Player, HUD (touch controls + story UI + menus) and The Echo.</summary>
    public static class GamePrefabs
    {
        public const string PlayerPath = "Assets/Prefabs/Player/Player.prefab";
        public const string HudPath = "Assets/Prefabs/UI/HUD.prefab";
        public const string EchoPath = "Assets/Prefabs/Enemy/Echo.prefab";

        [MenuItem("The Last Signal/Build/Rebuild Shared Prefabs")]
        public static void RebuildAll()
        {
            BuildPlayer();
            BuildHud();
            BuildEcho();
            Debug.Log("[GamePrefabs] Rebuilt Player, HUD and Echo prefabs.");
        }

        public static GameObject Player(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath) : BuildPlayer();

        public static GameObject Hud(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) : BuildHud();

        public static GameObject Echo(bool rebuild = false) =>
            !rebuild && AssetDatabase.LoadAssetAtPath<GameObject>(EchoPath) != null ? AssetDatabase.LoadAssetAtPath<GameObject>(EchoPath) : BuildEcho();

        // ================================================================== Player

        public static GameObject BuildPlayer()
        {
            EnsureFolder("Assets/Prefabs/Player");
            var root = new GameObject("Player") { tag = "Player" };

            var cc = root.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.3f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.35f;
            cc.slopeLimit = 50f;
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;

            var router = root.AddComponent<PlayerInputRouter>();
            var inventory = root.AddComponent<InventoryManager>();
            var controller = root.AddComponent<PlayerController>();
            var look = root.AddComponent<PlayerLook>();
            var interactor = root.AddComponent<PlayerInteractor>();
            var steps = root.AddComponent<AudioSource>();
            steps.playOnAwake = false;
            steps.spatialBlend = 0f;
            var noise = root.AddComponent<NoiseEmitter>();
            Set(noise, "stepSource", steps);

            Transform cameraRoot = Group(root.transform, "CameraRoot", new Vector3(0f, 1.65f, 0f));
            var camGo = new GameObject("PlayerCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(cameraRoot, false);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 70f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.015f);
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;

            var flashGo = new GameObject("Flashlight");
            flashGo.transform.SetParent(cameraRoot, false);
            flashGo.transform.localPosition = new Vector3(0.12f, -0.12f, 0.1f);
            var spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 16f;
            spot.spotAngle = 62f;
            spot.innerSpotAngle = 28f;
            spot.intensity = 3.2f;
            spot.color = new Color(1f, 0.95f, 0.85f);
            spot.shadows = LightShadows.None;
            spot.enabled = false;
            var click = flashGo.AddComponent<AudioSource>();
            click.playOnAwake = false;
            click.spatialBlend = 0f;
            var flashlight = flashGo.AddComponent<FlashlightController>();

            Set(controller, "input", router);
            Set(controller, "cameraRoot", cameraRoot);
            Set(look, "input", router);
            Set(look, "pitchPivot", cameraRoot);
            Set(interactor, "input", router);
            Set(interactor, "viewCamera", cam);
            Set(interactor, "inventory", inventory);
            Set(interactor, "flashlight", flashlight);
            Set(flashlight, "spotLight", spot);
            Set(flashlight, "input", router);
            Set(flashlight, "inventory", inventory);
            Set(flashlight, "audioSource", click);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ================================================================== Echo

        public static GameObject BuildEcho()
        {
            EnsureFolder("Assets/Prefabs/Enemy");
            var root = new GameObject("Echo");
            Material shadow = Mat("EchoShadow", new Color(0.015f, 0.015f, 0.02f), smoothness: 0f);
            Material eyes = Mat("EchoEyes", new Color(0.85f, 0.9f, 1f), emissive: true);
            Prim(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.6f, 0.95f, 0.5f), shadow, collider: false);
            Prim(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.78f, 0.02f), new Vector3(0.42f, 0.48f, 0.42f), shadow, collider: false);
            Prim(root.transform, "EyeL", PrimitiveType.Sphere, new Vector3(-0.09f, 1.82f, 0.19f), Vector3.one * 0.06f, eyes, collider: false);
            Prim(root.transform, "EyeR", PrimitiveType.Sphere, new Vector3(0.09f, 1.82f, 0.19f), Vector3.one * 0.06f, eyes, collider: false);
            Prim(root.transform, "ArmL", PrimitiveType.Capsule, new Vector3(-0.38f, 1.05f, 0.05f), new Vector3(0.12f, 0.6f, 0.12f), shadow, new Vector3(0f, 0f, 8f), collider: false);
            Prim(root.transform, "ArmR", PrimitiveType.Capsule, new Vector3(0.38f, 1.05f, 0.05f), new Vector3(0.12f, 0.6f, 0.12f), shadow, new Vector3(0f, 0f, -8f), collider: false);
            Transform eye = Group(root.transform, "Eye", new Vector3(0f, 1.75f, 0.15f));

            var agent = root.AddComponent<NavMeshAgent>();
            agent.radius = 0.35f;
            agent.height = 1.9f;
            agent.speed = 1.3f;
            agent.acceleration = 10f;
            agent.angularSpeed = 320f;
            agent.stoppingDistance = 0.2f;

            var voice = root.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.minDistance = 1.5f;
            voice.maxDistance = 22f;

            var ai = root.AddComponent<EchoAIController>();
            Set(ai, "eye", eye);
            Set(ai, "voice", voice);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, EchoPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ================================================================== HUD

        public static GameObject BuildHud()
        {
            EnsureFolder("Assets/Prefabs/UI");
            Sprite circle = EnsureSprite("Circle", SpriteShape.Disc);
            Sprite ring = EnsureSprite("Ring", SpriteShape.Ring);
            Sprite square = EnsureSprite("Square", SpriteShape.Square);
            Sprite vignette = EnsureSprite("Vignette", SpriteShape.Vignette);

            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();
            var hud = root.AddComponent<HUDController>();
            var mobile = root.AddComponent<MobileInputUI>();
            var pause = root.AddComponent<PauseController>();
            var story = root.AddComponent<StoryHUD>();

            // Threat vignette behind everything.
            RectTransform vig = UIRect(root.transform, "ThreatVignette");
            Stretch(vig);
            Image vigImage = UIImage(vig, vignette, new Color(1f, 1f, 1f, 0f), raycast: false);

            RectTransform safe = UIRect(root.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // ---------------- touch controls
            RectTransform controls = UIRect(safe, "MobileControls");
            Stretch(controls);
            RectTransform lookRect = UIRect(controls, "LookArea");
            Anchors(lookRect, new Vector2(0.4f, 0f), new Vector2(1f, 1f));
            UIImage(lookRect, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var lookArea = lookRect.gameObject.AddComponent<TouchLookArea>();

            RectTransform stickArea = UIRect(controls, "JoystickArea");
            Anchors(stickArea, new Vector2(0f, 0f), new Vector2(0.4f, 0.75f));
            UIImage(stickArea, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            RectTransform stickBase = UIRect(stickArea, "Base");
            Place(stickBase, Vector2.zero, new Vector2(230f, 230f), new Vector2(240f, 240f));
            Image baseImage = UIImage(stickBase, ring, new Color(1f, 1f, 1f, 0.35f), raycast: false);
            RectTransform knob = UIRect(stickBase, "Knob");
            Place(knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            UIImage(knob, circle, new Color(1f, 1f, 1f, 0.55f), raycast: false);
            var stick = stickArea.gameObject.AddComponent<VirtualJoystick>();
            Set(stick, "baseRect", stickBase);
            Set(stick, "knob", knob);
            Set(stick, "baseGraphic", baseImage);

            var corner = new Vector2(1f, 0f);
            var top = new Vector2(1f, 1f);
            MobileButton use = TouchButton(controls, circle, "Interact", "USE", MobileButton.Kind.Interact, corner, new Vector2(-180f, 180f), 180f, 40);
            MobileButton crouch = TouchButton(controls, circle, "Crouch", "HIDE /\nCROUCH", MobileButton.Kind.Crouch, corner, new Vector2(-385f, 100f), 125f, 20);
            MobileButton sprint = TouchButton(controls, circle, "Sprint", "RUN", MobileButton.Kind.SprintToggle, corner, new Vector2(-350f, 285f), 120f, 28);
            MobileButton light = TouchButton(controls, circle, "Flashlight", "LIGHT", MobileButton.Kind.Flashlight, corner, new Vector2(-170f, 390f), 120f, 26);
            MobileButton bag = TouchButton(controls, circle, "Inventory", "BAG", MobileButton.Kind.Inventory, top, new Vector2(-190f, -80f), 90f, 24);
            MobileButton pauseBtn = TouchButton(controls, circle, "Pause", "II", MobileButton.Kind.Pause, top, new Vector2(-80f, -80f), 90f, 32);

            // ---------------- HUD elements
            RectTransform cross = UIRect(safe, "Crosshair");
            Place(cross, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            Image crossImage = UIImage(cross, circle, new Color(1f, 1f, 1f, 0.35f), raycast: false);

            TMP_Text prompt = UIText(safe, "Prompt", "", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 60f));
            prompt.gameObject.SetActive(false);

            TMP_Text message = UIText(safe, "Message", "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1200f, 60f));
            var messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;

            RectTransform battery = UIRect(safe, "Battery");
            Place(battery, new Vector2(0f, 1f), new Vector2(230f, -60f), new Vector2(180f, 24f));
            UIImage(battery, square, new Color(0f, 0f, 0f, 0.5f), raycast: false);
            RectTransform fillRect = UIRect(battery, "Fill");
            Stretch(fillRect, 3f);
            Image fill = UIImage(fillRect, square, new Color(1f, 0.95f, 0.75f, 0.9f), raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            UIText(battery, "Label", "LIGHT", 20, new Vector2(0f, 0.5f), new Vector2(-50f, 0f), new Vector2(90f, 30f));
            battery.gameObject.SetActive(false);

            TMP_Text objective = UIText(safe, "Objective", "", 26, new Vector2(0f, 1f), new Vector2(300f, -150f), new Vector2(520f, 120f),
                TextAlignmentOptions.TopLeft, wrap: true);
            var objectiveGroup = objective.gameObject.AddComponent<CanvasGroup>();
            objectiveGroup.alpha = 0.85f;
            objectiveGroup.blocksRaycasts = false;

            TMP_Text hidden = UIText(safe, "HiddenLabel", "HIDDEN", 30, new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(400f, 50f));
            hidden.color = new Color(0.7f, 0.85f, 1f, 0.9f);

            RectTransform subtitlePanel = UIRect(safe, "Subtitles");
            Place(subtitlePanel, new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1000f, 130f));
            UIImage(subtitlePanel, square, new Color(0f, 0f, 0f, 0.65f), raycast: false);
            TMP_Text subtitleText = UIText(subtitlePanel, "Text", "", 32, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(960f, 120f),
                TextAlignmentOptions.Center, wrap: true);

            TMP_Text title = UIText(safe, "LevelTitle", "", 46, new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(1400f, 200f));
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;

            // ---------------- modal panels (on top)
            RectTransform notePanel = UIPanel(root.transform, "NotePanel", new Color(0f, 0f, 0f, 0.8f));
            RectTransform paper = UIRect(notePanel, "Paper");
            Place(paper, new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(1150f, 760f));
            UIImage(paper, square, new Color(0.86f, 0.82f, 0.7f, 0.97f), raycast: true);
            TMP_Text noteTitle = UIText(paper, "Title", "", 40, new Vector2(0.5f, 1f), new Vector2(0f, -55f), new Vector2(1050f, 70f));
            noteTitle.color = new Color(0.12f, 0.1f, 0.08f);
            TMP_Text noteBody = UIText(paper, "Body", "", 30, new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(1030f, 580f),
                TextAlignmentOptions.TopLeft, wrap: true);
            noteBody.color = new Color(0.12f, 0.1f, 0.08f);
            UIButton(notePanel, "CloseButton", "CLOSE", new Vector2(0.5f, 0f), new Vector2(0f, 75f), new Vector2(320f, 90f), 34, story.CloseNote);

            RectTransform invPanel = UIPanel(root.transform, "InventoryPanel", new Color(0f, 0f, 0f, 0.75f));
            UIText(invPanel, "Title", "INVENTORY", 48, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 80f));
            TMP_Text invText = UIText(invPanel, "Items", "", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(800f, 460f),
                TextAlignmentOptions.TopLeft, wrap: true);
            UIButton(invPanel, "CloseButton", "CLOSE", new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(320f, 90f), 34, story.CloseInventory);

            RectTransform pausePanel = UIPanel(root.transform, "PausePanel", new Color(0f, 0f, 0f, 0.82f));
            RectTransform mainPage = UIRect(pausePanel, "MainPage");
            Stretch(mainPage);
            UIText(mainPage, "Title", "PAUSED", 72, new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(800f, 100f));
            UIButton(mainPage, "Resume", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(560f, 100f), 38, pause.Resume);
            UIButton(mainPage, "Restart", "RESTART CHECKPOINT", new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(560f, 100f), 34, pause.RestartCheckpoint);
            UIButton(mainPage, "Settings", "SETTINGS", new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(560f, 100f), 38, pause.OpenSettings);
            UIButton(mainPage, "MainMenu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0f, -210f), new Vector2(560f, 100f), 38, pause.QuitToMainMenu);
            RectTransform settingsPage = SettingsPage(pausePanel, pause.ShowMain);

            RectTransform gameOver = UIPanel(root.transform, "GameOverPanel", new Color(0.08f, 0f, 0f, 0.88f));
            UIText(gameOver, "Title", "THE ECHO FOUND YOU", 64, new Vector2(0.5f, 0.5f), new Vector2(0f, 180f), new Vector2(1400f, 100f));
            UIText(gameOver, "Hint", "Listen for its footsteps. Crouch to stay quiet, and hide when it gets close.", 30,
                new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(1400f, 60f));
            UIButton(gameOver, "Retry", "RETRY CHECKPOINT", new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(560f, 100f), 36, story.RetryCheckpoint);
            UIButton(gameOver, "MainMenu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0f, -160f), new Vector2(560f, 100f), 36, story.QuitToMenu);

            // ---------------- wiring
            Set(hud, "crosshair", crossImage);
            Set(hud, "promptText", prompt);
            Set(hud, "messageText", message);
            Set(hud, "messageGroup", messageGroup);
            Set(hud, "batteryRoot", battery.gameObject);
            Set(hud, "batteryFill", fill);
            Set(mobile, "controlsRoot", controls.gameObject);
            Set(mobile, "joystick", stick);
            Set(mobile, "lookArea", lookArea);
            SetArray(mobile, "buttons", new Object[] { use, crouch, sprint, light, bag, pauseBtn });
            Set(pause, "pausePanel", pausePanel.gameObject);
            Set(pause, "mainPage", mainPage.gameObject);
            Set(pause, "settingsPage", settingsPage.gameObject);
            Set(story, "objectiveText", objective);
            Set(story, "objectiveGroup", objectiveGroup);
            Set(story, "subtitlePanel", subtitlePanel.gameObject);
            Set(story, "subtitleText", subtitleText);
            Set(story, "notePanel", notePanel.gameObject);
            Set(story, "noteTitle", noteTitle);
            Set(story, "noteBody", noteBody);
            Set(story, "inventoryPanel", invPanel.gameObject);
            Set(story, "inventoryText", invText);
            Set(story, "gameOverPanel", gameOver.gameObject);
            Set(story, "titleGroup", titleGroup);
            Set(story, "titleText", title);
            Set(story, "hiddenLabel", hidden.gameObject);
            Set(story, "threatVignette", vigImage);

            notePanel.gameObject.SetActive(false);
            invPanel.gameObject.SetActive(false);
            pausePanel.gameObject.SetActive(false);
            gameOver.gameObject.SetActive(false);
            subtitlePanel.gameObject.SetActive(false);
            hidden.gameObject.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static MobileButton TouchButton(Transform parent, Sprite sprite, string name, string label,
            MobileButton.Kind kind, Vector2 anchor, Vector2 position, float size, float fontSize)
        {
            RectTransform r = UIRect(parent, name + "Button");
            Place(r, anchor, position, new Vector2(size, size));
            Image bg = UIImage(r, sprite, new Color(1f, 1f, 1f, 0.25f), raycast: true);
            UIText(r, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var b = r.gameObject.AddComponent<MobileButton>();
            SetValue(b, "kind", kind);
            Set(b, "background", bg);
            return b;
        }

        /// <summary>Settings page used by the pause menu and the main menu.</summary>
        public static RectTransform SettingsPage(Transform parent, UnityAction back)
        {
            RectTransform page = UIRect(parent, "SettingsPage");
            Stretch(page);
            var ui = page.gameObject.AddComponent<SettingsPanelUI>();
            var c = new Vector2(0.5f, 0.5f);
            UIText(page, "Title", "SETTINGS", 60, c, new Vector2(0f, 400f), new Vector2(800f, 80f));
            Button quality = UIButton(page, "Quality", "GRAPHICS", c, new Vector2(0f, 300f), new Vector2(640f, 80f), 32, ui.CycleQuality);
            Slider sens = LabeledSlider(page, "Sensitivity", "LOOK SENSITIVITY", 200f);
            Slider master = LabeledSlider(page, "Master", "MASTER VOLUME", 90f);
            Slider ambience = LabeledSlider(page, "Ambience", "AMBIENCE VOLUME", -20f);
            Slider sfx = LabeledSlider(page, "Sfx", "EFFECTS VOLUME", -130f);
            Button subs = UIButton(page, "Subtitles", "SUBTITLES", c, new Vector2(0f, -240f), new Vector2(640f, 80f), 30, ui.ToggleSubtitles);
            UIText(page, "SubsNote", "Story dialogue and radio messages are always subtitled.", 24, c, new Vector2(0f, -300f), new Vector2(900f, 40f));
            UIButton(page, "Back", "BACK", c, new Vector2(0f, -390f), new Vector2(360f, 90f), 36, back);
            Set(ui, "qualityLabel", quality.GetComponentInChildren<TMP_Text>());
            Set(ui, "subtitlesLabel", subs.GetComponentInChildren<TMP_Text>());
            Set(ui, "sensitivity", sens);
            Set(ui, "masterVolume", master);
            Set(ui, "ambienceVolume", ambience);
            Set(ui, "sfxVolume", sfx);
            page.gameObject.SetActive(false);
            return page;
        }

        private static Slider LabeledSlider(Transform page, string name, string label, float y)
        {
            var c = new Vector2(0.5f, 0.5f);
            UIText(page, name + "Label", label, 28, c, new Vector2(0f, y + 30f), new Vector2(640f, 40f));
            return UISlider(page, name + "Slider", c, new Vector2(0f, y - 15f), new Vector2(640f, 40f));
        }
    }
}
