using System.IO;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Builds the M1 vertical-slice test content: Player prefab, HUD prefab and a greybox scene.
    /// Prefabs are only created if missing (use the Rebuild menu to overwrite them);
    /// the dev scene is always regenerated.
    /// </summary>
    public static class M1GreyboxBuilder
    {
        public const string ScenePath = "Assets/Scenes/Dev/M1_Greybox.unity";
        public const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        public const string HudPrefabPath = "Assets/Prefabs/UI/HUD.prefab";
        private const string MaterialDir = "Assets/Art/Materials/Greybox";
        private const string SpriteDir = "Assets/UI/HUD";

        [MenuItem("The Last Signal/Dev/Build M1 Greybox Scene")]
        public static void BuildScene() => Build(false);

        [MenuItem("The Last Signal/Dev/Rebuild Player + HUD Prefabs and M1 Scene")]
        public static void RebuildAll() => Build(true);

        private static void Build(bool overwritePrefabs)
        {
            EnsureFolder("Assets/Scenes/Dev");
            EnsureFolder(MaterialDir);
            EnsureFolder(SpriteDir);

            Sprite circle = EnsureSprite(SpriteDir + "/Circle.png", SpriteShape.Disc);
            Sprite ring = EnsureSprite(SpriteDir + "/Ring.png", SpriteShape.Ring);
            Sprite square = EnsureSprite(SpriteDir + "/Square.png", SpriteShape.Square);

            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (player == null || overwritePrefabs) player = CreatePlayerPrefab();

            GameObject hud = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
            if (hud == null || overwritePrefabs) hud = CreateHudPrefab(circle, ring, square);

            BuildGreyboxScene(player, hud);
            Debug.Log("[M1GreyboxBuilder] Built " + ScenePath);
        }

        // ------------------------------------------------------------------ Player

        private static GameObject CreatePlayerPrefab()
        {
            EnsureFolder(Path.GetDirectoryName(PlayerPrefabPath).Replace('\\', '/'));
            var root = new GameObject("Player");

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

            Transform cameraRoot = Child(root.transform, "CameraRoot", new Vector3(0f, 1.65f, 0f));

            GameObject camGo = Child(cameraRoot, "PlayerCamera", Vector3.zero).gameObject;
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 60f;
            cam.fieldOfView = 70f;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;

            GameObject flashGo = Child(cameraRoot, "Flashlight", new Vector3(0.12f, -0.12f, 0.1f)).gameObject;
            var spot = flashGo.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.range = 14f;
            spot.spotAngle = 60f;
            spot.innerSpotAngle = 28f;
            spot.intensity = 3f;
            spot.color = new Color(1f, 0.95f, 0.85f);
            spot.shadows = LightShadows.None;
            spot.enabled = false;
            var audio = flashGo.AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.spatialBlend = 0f;
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
            Set(flashlight, "audioSource", audio);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ HUD

        private static GameObject CreateHudPrefab(Sprite circle, Sprite ring, Sprite square)
        {
            EnsureFolder(Path.GetDirectoryName(HudPrefabPath).Replace('\\', '/'));
            var root = new GameObject("HUD", typeof(RectTransform));
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f; // landscape phones: size controls by screen height
            root.AddComponent<GraphicRaycaster>();
            var hud = root.AddComponent<HUDController>();
            var mobile = root.AddComponent<MobileInputUI>();
            var pause = root.AddComponent<PauseController>();

            RectTransform safe = Rect(root.transform, "SafeArea");
            Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            // --- Touch controls (look area first so buttons sit on top of it)
            RectTransform controls = Rect(safe, "MobileControls");
            Stretch(controls);

            RectTransform lookRect = Rect(controls, "LookArea");
            SetAnchors(lookRect, new Vector2(0.4f, 0f), new Vector2(1f, 1f));
            AddImage(lookRect, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            var lookArea = lookRect.gameObject.AddComponent<TouchLookArea>();

            RectTransform stickArea = Rect(controls, "JoystickArea");
            SetAnchors(stickArea, new Vector2(0f, 0f), new Vector2(0.4f, 0.75f));
            AddImage(stickArea, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            RectTransform stickBase = Rect(stickArea, "Base");
            Place(stickBase, new Vector2(0f, 0f), new Vector2(230f, 230f), new Vector2(240f, 240f));
            Image baseImage = AddImage(stickBase, ring, new Color(1f, 1f, 1f, 0.35f), raycast: false);
            RectTransform knob = Rect(stickBase, "Knob");
            Place(knob, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
            AddImage(knob, circle, new Color(1f, 1f, 1f, 0.55f), raycast: false);
            var stick = stickArea.gameObject.AddComponent<VirtualJoystick>();
            Set(stick, "baseRect", stickBase);
            Set(stick, "knob", knob);
            Set(stick, "baseGraphic", baseImage);

            var corner = new Vector2(1f, 0f);
            MobileButton use = MakeButton(controls, circle, "Interact", "USE", MobileButton.Kind.Interact, corner, new Vector2(-180f, 180f), 180f, 40);
            MobileButton crouch = MakeButton(controls, circle, "Crouch", "CROUCH", MobileButton.Kind.Crouch, corner, new Vector2(-380f, 100f), 120f, 24);
            MobileButton sprint = MakeButton(controls, circle, "Sprint", "RUN", MobileButton.Kind.SprintToggle, corner, new Vector2(-350f, 280f), 120f, 28);
            MobileButton light = MakeButton(controls, circle, "Flashlight", "LIGHT", MobileButton.Kind.Flashlight, corner, new Vector2(-170f, 390f), 120f, 26);
            MobileButton pauseBtn = MakeButton(controls, circle, "Pause", "II", MobileButton.Kind.Pause, new Vector2(1f, 1f), new Vector2(-80f, -80f), 90f, 32);

            // --- HUD elements
            RectTransform cross = Rect(safe, "Crosshair");
            Place(cross, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(8f, 8f));
            Image crossImage = AddImage(cross, circle, new Color(1f, 1f, 1f, 0.35f), raycast: false);

            TMP_Text prompt = AddText(safe, "Prompt", "", 34, new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(1000f, 60f));
            prompt.gameObject.SetActive(false);

            TMP_Text message = AddText(safe, "Message", "", 32, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(1100f, 60f));
            var messageGroup = message.gameObject.AddComponent<CanvasGroup>();
            messageGroup.alpha = 0f;
            messageGroup.blocksRaycasts = false;

            RectTransform battery = Rect(safe, "Battery");
            // Left edge at x = 140 leaves room for the "LIGHT" label to its left.
            Place(battery, new Vector2(0f, 1f), new Vector2(230f, -60f), new Vector2(180f, 24f));
            AddImage(battery, square, new Color(0f, 0f, 0f, 0.5f), raycast: false);
            RectTransform fillRect = Rect(battery, "Fill");
            Stretch(fillRect, 3f);
            Image fill = AddImage(fillRect, square, new Color(1f, 0.95f, 0.75f, 0.9f), raycast: false);
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;
            AddText(battery, "Label", "LIGHT", 20, new Vector2(0f, 0.5f), new Vector2(-50f, 0f), new Vector2(90f, 30f));
            battery.gameObject.SetActive(false);

            // --- Pause panel (full screen, on top)
            RectTransform panel = Rect(root.transform, "PausePanel");
            Stretch(panel);
            AddImage(panel, null, new Color(0f, 0f, 0f, 0.8f), raycast: true);
            AddText(panel, "Title", "PAUSED", 72, new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(800f, 100f));
            RectTransform resumeRect = Rect(panel, "ResumeButton");
            Place(resumeRect, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(380f, 110f));
            AddImage(resumeRect, square, new Color(1f, 1f, 1f, 0.2f), raycast: true);
            var resume = resumeRect.gameObject.AddComponent<Button>();
            AddText(resumeRect, "Label", "RESUME", 40, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(380f, 110f));
            UnityEventTools.AddPersistentListener(resume.onClick, pause.Resume);
            panel.gameObject.SetActive(false);

            Set(hud, "crosshair", crossImage);
            Set(hud, "promptText", prompt);
            Set(hud, "messageText", message);
            Set(hud, "messageGroup", messageGroup);
            Set(hud, "batteryRoot", battery.gameObject);
            Set(hud, "batteryFill", fill);
            Set(mobile, "controlsRoot", controls.gameObject);
            Set(mobile, "joystick", stick);
            Set(mobile, "lookArea", lookArea);
            SetArray(mobile, "buttons", new Object[] { use, crouch, sprint, light, pauseBtn });
            Set(pause, "pausePanel", panel.gameObject);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, HudPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static MobileButton MakeButton(Transform parent, Sprite sprite, string name, string label,
            MobileButton.Kind kind, Vector2 anchor, Vector2 position, float size, float fontSize)
        {
            RectTransform r = Rect(parent, name + "Button");
            Place(r, anchor, position, new Vector2(size, size));
            Image bg = AddImage(r, sprite, new Color(1f, 1f, 1f, 0.25f), raycast: true);
            AddText(r, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
            var b = r.gameObject.AddComponent<MobileButton>();
            SerializedObject so = new SerializedObject(b);
            so.FindProperty("kind").enumValueIndex = (int)kind;
            so.FindProperty("background").objectReferenceValue = bg;
            so.ApplyModifiedPropertiesWithoutUndo();
            return b;
        }

        // ------------------------------------------------------------------ Scene

        private static void BuildGreyboxScene(GameObject playerPrefab, GameObject hudPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.07f, 0.07f, 0.08f);
            RenderSettings.fog = false;

            Material floor = Mat("Greybox_Floor", new Color(0.32f, 0.32f, 0.34f));
            Material wall = Mat("Greybox_Wall", new Color(0.55f, 0.55f, 0.57f));
            Material prop = Mat("Greybox_Prop", new Color(0.45f, 0.36f, 0.26f));
            Material accent = Mat("Greybox_Accent", new Color(0.9f, 0.75f, 0.2f));
            Material lampMat = Mat("Greybox_Lamp", new Color(1f, 0.93f, 0.75f), emissive: true);

            Transform env = new GameObject("Environment").transform;
            const float h = 3f;   // room height
            const float t = 0.2f; // wall thickness

            // Room A: 12 x 12, lit by the ceiling lamp.
            Transform roomA = Group(env, "RoomA");
            Box(roomA, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(12f, 0.2f, 12f), floor);
            Box(roomA, "Ceiling", new Vector3(0f, h + 0.1f, 0f), new Vector3(12f, 0.2f, 12f), wall);
            Box(roomA, "Wall_N", new Vector3(0f, h / 2f, 6f), new Vector3(12f + t, h, t), wall);
            Box(roomA, "Wall_S", new Vector3(0f, h / 2f, -6f), new Vector3(12f + t, h, t), wall);
            Box(roomA, "Wall_W", new Vector3(-6f, h / 2f, 0f), new Vector3(t, h, 12f), wall);
            // East wall with a 1.6 m x 2.2 m doorway at z = 0 into Room B.
            Box(roomA, "Wall_E_South", new Vector3(6f, h / 2f, -3.4f), new Vector3(t, h, 5.2f), wall);
            Box(roomA, "Wall_E_North", new Vector3(6f, h / 2f, 3.4f), new Vector3(t, h, 5.2f), wall);
            Box(roomA, "Wall_E_Lintel", new Vector3(6f, 2.2f + (h - 2.2f) / 2f, 0f), new Vector3(t, h - 2.2f, 1.6f), wall);

            // Crouch test: low overhang (1.3 m clearance) in the south-west corner.
            Box(roomA, "LowOverhang", new Vector3(-4.5f, 1.3f + (h - 1.3f) / 2f, -4f), new Vector3(3f, h - 1.3f, 3.6f), wall);
            // Step and ramp tests.
            Box(roomA, "Step", new Vector3(-3f, 0.125f, 3.5f), new Vector3(1.5f, 0.25f, 1.5f), prop);
            GameObject ramp = Box(roomA, "Ramp", new Vector3(2.5f, 0.35f, 4.2f), new Vector3(1.6f, 0.2f, 3f), prop);
            ramp.transform.rotation = Quaternion.Euler(-14f, 0f, 0f);
            Box(roomA, "Crate_1", new Vector3(3.5f, 0.5f, -3.5f), Vector3.one, prop);
            Box(roomA, "Crate_2", new Vector3(4.2f, 0.4f, -2.3f), new Vector3(0.8f, 0.8f, 0.8f), prop);

            // Ceiling lamp controlled by the switch.
            Box(roomA, "LampFixture", new Vector3(0f, h - 0.05f, 0f), new Vector3(0.6f, 0.08f, 0.6f), lampMat, collider: false);
            var lampGo = new GameObject("CeilingLamp");
            lampGo.transform.SetParent(roomA, false);
            lampGo.transform.localPosition = new Vector3(0f, h - 0.3f, 0f);
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.range = 11f;
            lamp.intensity = 1.6f;
            lamp.color = new Color(1f, 0.9f, 0.75f);
            lamp.shadows = LightShadows.None;

            // Room B: 6 x 6, unlit, needs the flashlight.
            Transform roomB = Group(env, "RoomB");
            Box(roomB, "Floor", new Vector3(9f, -0.1f, 0f), new Vector3(6f, 0.2f, 6f), floor);
            Box(roomB, "Ceiling", new Vector3(9f, h + 0.1f, 0f), new Vector3(6f, 0.2f, 6f), wall);
            Box(roomB, "Wall_N", new Vector3(9f, h / 2f, 3f), new Vector3(6f, h, t), wall);
            Box(roomB, "Wall_S", new Vector3(9f, h / 2f, -3f), new Vector3(6f, h, t), wall);
            Box(roomB, "Wall_E", new Vector3(12f, h / 2f, 0f), new Vector3(t, h, 6f + t), wall);
            Box(roomB, "Table", new Vector3(10.6f, 0.375f, 0f), new Vector3(0.8f, 0.75f, 1.4f), prop);

            foreach (Transform child in env.GetComponentsInChildren<Transform>())
                if (child.GetComponent<Renderer>() != null) child.gameObject.isStatic = true;

            // Interactables (not static).
            Transform interactables = new GameObject("Interactables").transform;

            // Flashlight pickup on the table in Room B.
            GameObject torch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            torch.name = "Pickup_Flashlight";
            torch.transform.SetParent(interactables, false);
            torch.transform.position = new Vector3(10.6f, 0.8f, 0f);
            torch.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            torch.transform.localScale = new Vector3(0.09f, 0.13f, 0.09f);
            torch.GetComponent<Renderer>().sharedMaterial = accent;
            // Larger trigger so it is easy to target on a phone.
            var grab = torch.AddComponent<BoxCollider>();
            grab.isTrigger = true;
            grab.size = new Vector3(3f, 1.4f, 3f);
            var pickup = torch.AddComponent<PickupItem>();
            SerializedObject ps = new SerializedObject(pickup);
            ps.FindProperty("itemId").stringValue = "flashlight";
            ps.FindProperty("displayName").stringValue = "Flashlight";
            ps.ApplyModifiedPropertiesWithoutUndo();

            // Light switch on the east wall of Room A, beside the doorway.
            GameObject plate = Box(interactables, "LightSwitch", new Vector3(5.86f, 1.3f, -1.3f), new Vector3(0.06f, 0.26f, 0.18f), wall);
            GameObject handle = Box(plate.transform, "Handle", Vector3.zero, Vector3.one, prop, collider: false);
            handle.transform.localPosition = new Vector3(-0.8f, 0f, 0f);
            handle.transform.localScale = new Vector3(1.2f, 0.35f, 0.25f);
            GameObject indicator = Box(plate.transform, "Indicator", Vector3.zero, Vector3.one, accent, collider: false);
            indicator.transform.localPosition = new Vector3(-0.6f, 0.38f, 0f);
            indicator.transform.localScale = new Vector3(0.5f, 0.12f, 0.25f);
            var sw = plate.AddComponent<SwitchInteractable>();
            SetArray(sw, "lights", new Object[] { lamp });
            Set(sw, "handle", handle.transform);
            Set(sw, "indicator", indicator.GetComponent<Renderer>());
            SerializedObject sws = new SerializedObject(sw);
            sws.FindProperty("handleOnEuler").vector3Value = new Vector3(0f, 0f, 25f);
            sws.FindProperty("handleOffEuler").vector3Value = new Vector3(0f, 0f, -25f);
            sws.ApplyModifiedPropertiesWithoutUndo();

            // Player, HUD and EventSystem.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
            player.transform.SetPositionAndRotation(new Vector3(-2.5f, 0f, 1.5f), Quaternion.Euler(0f, 90f, 0f));
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            EditorSceneManager.SaveScene(scene, ScenePath);
        }

        // ------------------------------------------------------------------ Helpers

        private enum SpriteShape { Disc, Ring, Square }

        private static Sprite EnsureSprite(string path, SpriteShape shape)
        {
            if (!File.Exists(path))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float c = (size - 1) / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a;
                    if (shape == SpriteShape.Square) a = 1f;
                    else if (shape == SpriteShape.Disc) a = Mathf.Clamp01(c - d);
                    else a = Mathf.Clamp01(c - d) * Mathf.Clamp01(d - (c - 8f));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Material Mat(string name, Color color, bool emissive = false)
        {
            string path = MaterialDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.15f);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Material mat, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static Transform Group(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            return go.transform;
        }

        private static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
        }

        private static void SetAnchors(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        private static Image AddImage(RectTransform r, Sprite sprite, Color color, bool raycast)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        private static TMP_Text AddText(Transform parent, string name, string text, float fontSize,
            Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform r = Rect(parent, name);
            Place(r, anchor, position, size);
            var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 1f, 1f, 0.92f);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            return tmp;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'");
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new System.ArgumentException($"{target.GetType().Name} has no serialized field '{field}'");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
