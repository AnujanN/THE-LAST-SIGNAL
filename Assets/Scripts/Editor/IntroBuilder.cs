using System.Collections.Generic;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.Interaction;
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
    /// <summary>
    /// The intro scene (IntroSequence runs it). Two places in one scene:
    /// - Alex's flat in town, 11:47 PM: a small living room with rain running down the window, the city's lights
    ///   beyond, a sofa, unpacked moving boxes, a ticking clock, a floor lamp, Dad's old radio on the sideboard and
    ///   the old photo of the three of them beside it. The player walks here (real player and HUD).
    /// - The drive: the same hill, station and mast as the ending, at night in the storm; a forest road up to the
    ///   Northpoint gate, seen from the driver's seat (dashboard, wipers, rain on the glass, car radio).
    /// </summary>
    public static class IntroBuilder
    {
        /// <summary>The flat is far from the hill, sealed off on its own.</summary>
        private static readonly Vector3 F = new Vector3(0f, 0f, -300f);
        private const float RoomW = 6f, RoomD = 5f, RoomH = 2.7f;

        public static void Build(string path, System.Func<string, GameObject> canvas)
        {
            Sprite photo = PhotoArt.FamilyPhoto();
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();

            var scene = NewScene("Intro");
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.03f, 0.035f, 0.05f);
            RenderSettings.fogDensity = 0.0105f;

            Transform env = Group(null, "Environment");
            Light moon = DirectionalLight(env, "Moonlight", new Vector3(28f, 200f, 0f), new Color(0.55f, 0.62f, 0.85f), 0.18f);
            moon.shadows = LightShadows.None;
            Light lightning = DirectionalLight(env, "Lightning", new Vector3(38f, 165f, 0f), new Color(0.78f, 0.82f, 1f), 0f);
            lightning.shadows = LightShadows.Hard;
            SetLightLayers(lightning, LitLayers | (1u << 3)); // also the car bonnet, which nothing else lights

            // ------------------------------------------------------------------ the hill, the road, the gate
            EndingBuilder.Ground(env);
            EndingBuilder.Grass(env, 42f, 12f); // well off the road
            EndingBuilder.Trees(env);
            EndingBuilder.Station(env);
            Vector3 stationFront = new Vector3(-30f, EndingBuilder.H(-30f, 80f), 80f);
            Vector3 carRef = new Vector3(-10f, 0f, 22f);
            Vector3 dir = new Vector3(stationFront.x - carRef.x, 0f, stationFront.z - carRef.z).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, dir);
            Vector3 gate = new Vector3(stationFront.x, 0f, stationFront.z) - dir * 20f;
            Vector3 roadStart = gate - dir * 100f;
            Road(env, roadStart - dir * 6f, new Vector3(stationFront.x, 0f, stationFront.z) - dir * 2f);
            Forest(env, roadStart, gate, dir, right);
            ClearRoad(env.Find("Trees"), roadStart - dir * 6f, gate, 5f); // the ending's treeline crosses the road
            Gate(env, gate, dir, right);
            IntroDressing.ReflectorPosts(env, roadStart, gate - dir * 4f, right);
            IntroDressing.GroundMist(env, roadStart, gate, new Color(0.55f, 0.6f, 0.7f, 0.1f));
            Vector3 mid = (roadStart + gate) / 2f;
            Renderer stormSky = IntroDressing.StormSky(env, new Vector3(mid.x, -6f, mid.z), 92f);
            LineRenderer bolt = IntroDressing.Bolt(env);

            // The car: its origin is the driver's eyes. It drives along a path of markers that follow the ground.
            Transform car = Group(null, "Car");
            Transform[] drivePath = DrivePath(roadStart + right * 0.9f, gate - dir * 7f + right * 0.9f, dir, 24);
            car.SetPositionAndRotation(drivePath[0].position, drivePath[0].rotation);
            Interior(car, out TMP_Text carDial, out Light carGlow, out Transform carRadioAt, out Transform[] wipers);
            Headlights(car);
            IntroDressing.HeadlightBeams(car);
            Transform charm = IntroDressing.Gauges(car.Find("Interior"));
            CarRain(car);
            Transform carView = Marker(car, "CarView", Vector3.zero, 0f);
            carView.localRotation = Quaternion.Euler(3f, 0f, 0f);

            // ------------------------------------------------------------------ Alex's flat
            Flat(env, photo, out FlatParts flat);

            // ------------------------------------------------------------------ camera, grading
            GameObject postFx = PostFx("Intro");
            GameObject flatLook = IntroDressing.FlatLook();
            GameObject driveLook = IntroDressing.DriveLook();
            Transform rig = Group(null, "CameraRig");
            var camGo = new GameObject("IntroCamera") { tag = "Untagged" };
            camGo.transform.SetParent(rig, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.015f, 0.018f, 0.028f);
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 300f;
            cam.depth = 5f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            rig.SetPositionAndRotation(flat.shotStart.position, flat.shotStart.rotation);

            // Sound sources (clips are chosen at runtime from the sound bank).
            AudioSource roof = Source(car, "RoofRain", 0f, 6f);
            AudioSource engine = Source(car, "Engine", 0f, 6f);
            AudioSource wiperSound = Source(car, "WiperSound", 0f, 6f);
            wiperSound.loop = false;
            // The family theme: the same music bed as the ending, first heard over the old photo.
            AudioSource theme = EndingBuilder.Loop(rig, "Theme", "Assets/SourceFiles/SoundFX/Music_Ethereal.ogg");
            if (theme != null) theme.loop = false;
            AudioSource carRadio = Source(carRadioAt, "CarRadio", 0.5f, 6f);
            AudioSource music = EndingBuilder.Loop(rig, "Music", "Assets/SourceFiles/SoundFX/Music_Suspenseful.ogg");
            if (music != null) music.loop = false;

            // ------------------------------------------------------------------ player + HUD (the flat)
            GameObject player = Instantiate(playerPrefab, scene, flat.playerStart.position, flat.playerStart.eulerAngles.y);
            GameObject hud = (GameObject)PrefabUtility.InstantiatePrefab(hudPrefab, scene);

            // ------------------------------------------------------------------ UI
            GameObject root = canvas("IntroCanvas");
            root.GetComponent<Canvas>().sortingOrder = 20; // over the HUD
            var c = new Vector2(0.5f, 0.5f);
            var story = root.AddComponent<StoryHUD>();
            RectTransform subtitlePanel = UIRect(root.transform, "Subtitles");
            Place(subtitlePanel, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1100f, 120f));
            UIImage(subtitlePanel, EnsureSprite("Square", SpriteShape.Square), new Color(0f, 0f, 0f, 0.5f), raycast: false);
            TMP_Text subtitleText = UIText(subtitlePanel, "Text", "", 34, c, Vector2.zero, new Vector2(1060f, 110f), TextAlignmentOptions.Center, wrap: true);
            Set(story, "subtitlePanel", subtitlePanel.gameObject);
            Set(story, "subtitleText", subtitleText);
            subtitlePanel.gameObject.SetActive(false);

            // The old photo, held up close.
            RectTransform photoPanel = UIPanel(root.transform, "PhotoPanel", new Color(0f, 0f, 0f, 0.82f));
            RectTransform card = UIRect(photoPanel, "Photo");
            Place(card, c, new Vector2(0f, 60f), new Vector2(960f, 735f));
            Image cardImage = UIImage(card, photo, Color.white, raycast: false);
            cardImage.preserveAspect = true;
            TMP_Text caption = UIText(photoPanel, "Caption", "Dad, Maya and me. Northpoint, the summer before the fire.", 32, c, new Vector2(0f, -360f), new Vector2(1300f, 60f));
            caption.fontStyle = FontStyles.Italic;
            caption.color = new Color(0.9f, 0.87f, 0.8f);

            RectTransform fadeRect = UIRect(root.transform, "Fade");
            Stretch(fadeRect);
            UIImage(fadeRect, null, Color.black, raycast: false);
            var fadeGroup = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;
            TMP_Text title = UIText(root.transform, "Title", "THE LAST SIGNAL", 104, c, new Vector2(0f, 30f), new Vector2(1600f, 200f));
            title.characterSpacing = 22f;
            title.color = new Color(0.92f, 0.9f, 0.86f);
            var titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            TMP_Text tagline = UIText(title.transform, "Tagline", "A PSYCHOLOGICAL SURVIVAL HORROR", 26, c, new Vector2(0f, -105f), new Vector2(1200f, 50f));
            tagline.characterSpacing = 18f;
            tagline.color = new Color(0.75f, 0.73f, 0.7f, 0.85f);
            TMP_Text time = UIText(root.transform, "TimeCaption", "11:47 PM", 40, new Vector2(0f, 0f), new Vector2(230f, 110f), new Vector2(360f, 70f), TextAlignmentOptions.Left);
            time.characterSpacing = 6f;
            time.color = new Color(0.85f, 0.85f, 0.82f);
            var timeGroup = time.gameObject.AddComponent<CanvasGroup>();
            Button skip = UIButton(root.transform, "Skip", "SKIP  ›", new Vector2(1f, 1f), new Vector2(-400f, -80f), new Vector2(200f, 70f), 28, null);

            var intro = root.AddComponent<IntroSequence>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(skip.onClick, intro.Skip);
            Button putDown = UIButton(photoPanel, "PutDown", "PUT IT DOWN", new Vector2(0.5f, 0f), new Vector2(0f, 80f), new Vector2(380f, 90f), 32, intro.ClosePhoto);
            photoPanel.gameObject.SetActive(false);

            // Hooks in the flat.
            On(flat.radioTrigger, "onEnter", intro.ReachedRadio);
            On(flat.radioButton, "onPressed", intro.TryRadio);
            On(flat.photoButton, "onPressed", intro.ShowPhoto);

            Set(intro, "cameraRig", rig);
            Set(intro, "viewCamera", cam);
            Set(intro, "flatShotStart", flat.shotStart);
            Set(intro, "flatShotEnd", flat.shotEnd);
            Set(intro, "carView", carView);
            Set(intro, "player", player);
            Set(intro, "hud", hud.GetComponent<Canvas>());
            Set(intro, "flatStart", flat.playerStart);
            Set(intro, "radioLook", flat.radioLook);
            Set(intro, "homeDial", flat.dial);
            Set(intro, "homeRadioGlow", flat.glow);
            Set(intro, "homeDialLamp", flat.dialLamp);
            Set(intro, "homeRadio", flat.radioSource);
            Set(intro, "radioButton", flat.radioButton.gameObject);
            Set(intro, "photoButton", flat.photoButton.gameObject);
            Set(intro, "photoPanel", photoPanel.gameObject);
            Set(intro, "photoCard", card);
            Set(intro, "car", car);
            SetArray(intro, "drivePath", drivePath);
            SetArray(intro, "wipers", wipers);
            Set(intro, "carDial", carDial);
            Set(intro, "carRadioGlow", carGlow);
            Set(intro, "carRadio", carRadio);
            Set(intro, "roofRain", roof);
            Set(intro, "lightning", lightning);
            SetArray(intro, "flashSky", new Object[] { flat.sky[0], stormSky });
            Set(intro, "bolt", bolt);
            Set(intro, "flatLook", flatLook);
            Set(intro, "driveLook", driveLook);
            Set(intro, "engine", engine);
            Set(intro, "wiperSound", wiperSound);
            Set(intro, "charm", charm);
            if (theme != null) Set(intro, "theme", theme);
            Set(intro, "windowRain", flat.rainSource);
            SetLines(intro, "firstCall", new[]
            {
                L("", "[The radio clicks on by itself. Static.]", 3f),
                L("MAYA (RADIO)", "Alex... can you hear me?", 3.5f),
                L("ALEX", "Maya?", 2.5f),
            });
            SetLines(intro, "secondCall", new[]
            {
                L("MAYA (RADIO)", "I'm scared, Alex. You have to come back.", 4f),
            });
            SetLines(intro, "atTheRadio", new[]
            {
                L("ALEX", "No... this can't be happening. You're...", 3.5f),
                L("MAYA (RADIO)", "Northpoint... the station... please...", 3.5f),
                L("MAYA (RADIO)", "...before it finds me.", 3f),
            });
            SetLines(intro, "afterPhoto", new[]
            {
                L("ALEX", "Ten years. I left you there, Maya.", 3.5f),
                L("ALEX", "Whatever that was... I'm going back.", 3.5f),
            });
            Set(intro, "fade", fadeGroup);
            Set(intro, "title", titleGroup);
            Set(intro, "timeCaption", timeGroup);
            Set(intro, "skipButton", skip.gameObject);
            Set(intro, "introSubtitles", story);
            if (music != null) Set(intro, "music", music);

            EventSystemObject();
            SaveScene(scene, path);
            Debug.Log("[IntroBuilder] Built " + path);
        }

        private static AudioSource Source(Transform parent, string name, float spatial, float maxDistance)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = true;
            s.spatialBlend = spatial;
            s.minDistance = 0.5f;
            s.maxDistance = maxDistance;
            s.rolloffMode = AudioRolloffMode.Linear;
            return s;
        }

        // ------------------------------------------------------------------ the flat

        private struct FlatParts
        {
            public Transform shotStart, shotEnd, playerStart, radioLook;
            public TMP_Text dial;
            public Light glow;
            public Renderer dialLamp;
            public AudioSource radioSource, rainSource;
            public TriggerZone radioTrigger;
            public ActionButton radioButton, photoButton;
            public Renderer[] sky;
        }

        private static Vector3 P(float x, float y, float z) => F + new Vector3(x, y, z);

        private static void Flat(Transform env, Sprite photo, out FlatParts parts)
        {
            parts = new FlatParts();
            Transform room = Group(env, "Flat");
            Material floor = EndingBuilder.Textured("FlatFloor", "brown_planks_05", new Color(0.62f, 0.5f, 0.4f), 0.3f);
            Material wall = EndingBuilder.Textured("FlatWall", "beige_wall_001", new Color(0.62f, 0.6f, 0.56f), 0.1f);
            Material ceiling = Mat("FlatCeiling", new Color(0.55f, 0.55f, 0.53f));
            Material wood = Mat("FlatWood", new Color(0.2f, 0.13f, 0.09f), smoothness: 0.35f);
            Material trim = Mat("FlatTrim", new Color(0.78f, 0.76f, 0.72f), smoothness: 0.3f);
            Material rug = EndingBuilder.Textured("FlatRug", "dirty_carpet", new Color(0.45f, 0.2f, 0.18f), 0.05f);
            const float t = WallThickness;

            // Shell.
            Box(room, "Floor", P(RoomW / 2f, -0.1f, RoomD / 2f), new Vector3(RoomW, 0.2f, RoomD), floor);
            Box(room, "Ceiling", P(RoomW / 2f, RoomH + 0.1f, RoomD / 2f), new Vector3(RoomW, 0.2f, RoomD), ceiling);
            Box(room, "Wall_S", P(RoomW / 2f, RoomH / 2f, -t / 2f), new Vector3(RoomW + 2f * t, RoomH, t), wall);
            Box(room, "Wall_W", P(-t / 2f, RoomH / 2f, RoomD / 2f), new Vector3(t, RoomH, RoomD), wall);
            Box(room, "Wall_E", P(RoomW + t / 2f, RoomH / 2f, RoomD / 2f), new Vector3(t, RoomH, RoomD), wall);
            // North wall with the window (x 2.2..4.0, y 0.9..2.2).
            const float wx0 = 2.2f, wx1 = 4.0f, wy0 = 0.9f, wy1 = 2.2f;
            float nz = RoomD + t / 2f;
            Box(room, "Wall_N", P(wx0 / 2f, RoomH / 2f, nz), new Vector3(wx0, RoomH, t), wall);
            Box(room, "Wall_N", P((wx1 + RoomW) / 2f, RoomH / 2f, nz), new Vector3(RoomW - wx1, RoomH, t), wall);
            Box(room, "Wall_N_Sill", P((wx0 + wx1) / 2f, wy0 / 2f, nz), new Vector3(wx1 - wx0, wy0, t), wall);
            Box(room, "Wall_N_Head", P((wx0 + wx1) / 2f, (wy1 + RoomH) / 2f, nz), new Vector3(wx1 - wx0, RoomH - wy1, t), wall);
            // Skirting.
            Box(room, "Skirting", P(RoomW / 2f, 0.05f, 0.01f), new Vector3(RoomW, 0.1f, 0.02f), trim, collider: false);
            Box(room, "Skirting", P(0.01f, 0.05f, RoomD / 2f), new Vector3(0.02f, 0.1f, RoomD), trim, collider: false);
            Box(room, "Skirting", P(RoomW - 0.01f, 0.05f, RoomD / 2f), new Vector3(0.02f, 0.1f, RoomD), trim, collider: false);
            // Window: frame, cross bar, sill, glass with rain running down it.
            Material frame = Mat("WindowFrame", new Color(0.82f, 0.8f, 0.76f), smoothness: 0.4f);
            float fz = RoomD + 0.02f;
            Box(room, "Frame", P(wx0 + 0.04f, (wy0 + wy1) / 2f, fz), new Vector3(0.08f, wy1 - wy0, 0.12f), frame);
            Box(room, "Frame", P(wx1 - 0.04f, (wy0 + wy1) / 2f, fz), new Vector3(0.08f, wy1 - wy0, 0.12f), frame);
            Box(room, "Frame", P((wx0 + wx1) / 2f, wy1 - 0.04f, fz), new Vector3(wx1 - wx0, 0.08f, 0.12f), frame);
            Box(room, "Frame", P((wx0 + wx1) / 2f, wy0 + 0.04f, fz), new Vector3(wx1 - wx0, 0.08f, 0.12f), frame);
            Box(room, "Mullion", P((wx0 + wx1) / 2f, (wy0 + wy1) / 2f, fz), new Vector3(0.05f, wy1 - wy0, 0.08f), frame, collider: false);
            Box(room, "Sill", P((wx0 + wx1) / 2f, wy0 - 0.02f, RoomD - 0.08f), new Vector3(wx1 - wx0 + 0.2f, 0.04f, 0.22f), frame);
            Material glass = Mat("FlatGlass", new Color(0.5f, 0.55f, 0.65f, 0.12f), smoothness: 0.95f);
            EndingBuilder.MakeTransparent(glass);
            GameObject pane = Box(room, "Glass", P((wx0 + wx1) / 2f, (wy0 + wy1) / 2f, RoomD + 0.03f), new Vector3(wx1 - wx0 - 0.1f, wy1 - wy0 - 0.1f, 0.006f), glass, isStatic: false);
            pane.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // drops face into the room
            Transform windowPlane = Group(room, "WindowPlane", P((wx0 + wx1) / 2f, (wy0 + wy1) / 2f, RoomD - 0.02f)); // -Z faces into the room
            Drops(windowPlane, beads: true, new Vector2(1.65f, 1.2f));
            Drops(windowPlane, beads: false, new Vector2(1.65f, 1.2f));

            // Door to the hall (closed) and a light switch.
            Box(room, "Door", P(0.03f, 1.02f, 1.1f), new Vector3(0.05f, 2.04f, 0.9f), wood);
            Box(room, "DoorFrame", P(0.02f, 1.06f, 1.1f), new Vector3(0.03f, 2.12f, 1.02f), trim, collider: false);
            Prim(room, "Handle", PrimitiveType.Sphere, P(0.08f, 1.0f, 1.45f), Vector3.one * 0.06f, Mat("Brass", new Color(0.7f, 0.55f, 0.3f), smoothness: 0.7f), collider: false);
            Box(room, "Switch", P(0.02f, 1.3f, 1.75f), new Vector3(0.02f, 0.11f, 0.08f), trim, collider: false);

            // Furniture: sofa facing the window, a low table, the TV cabinet, a bookcase, moving boxes never unpacked.
            Box(room, "Rug", P(2.7f, 0.006f, 2.05f), new Vector3(2.6f, 0.012f, 1.7f), rug, collider: false);
            Box(room, "Sofa", P(2.7f, 0.42f, 0.55f), new Vector3(2.2f, 0.84f, 0.9f), wood);
            PropLibrary.Dress(room, "Sofa", "sofa_02");
            Box(room, "CoffeeTable", P(2.7f, 0.22f, 1.95f), new Vector3(1.2f, 0.44f, 0.65f), wood);
            PropLibrary.Dress(room, "CoffeeTable", "WoodenTable_03");
            Material bottle = Mat("BottleGlass", new Color(0.12f, 0.25f, 0.12f), smoothness: 0.9f);
            Prim(room, "Bottle", PrimitiveType.Cylinder, P(2.45f, 0.56f, 1.9f), new Vector3(0.07f, 0.12f, 0.07f), bottle, collider: false);
            Prim(room, "Bottle", PrimitiveType.Capsule, P(2.95f, 0.48f, 2.05f), new Vector3(0.07f, 0.12f, 0.07f), bottle, new Vector3(90f, 30f, 0f), collider: false);
            Prim(room, "Mug", PrimitiveType.Cylinder, P(2.75f, 0.49f, 1.78f), new Vector3(0.08f, 0.05f, 0.08f), Mat("Mug", new Color(0.7f, 0.68f, 0.62f), smoothness: 0.5f), collider: false);
            Box(room, "TVCabinet", P(1.0f, 0.25f, RoomD - 0.3f), new Vector3(1.4f, 0.5f, 0.45f), wood);
            PropLibrary.Dress(room, "TVCabinet", "modern_wooden_cabinet");
            PropLibrary.Place(room, "television_02", P(1.0f, 0.5f, RoomD - 0.32f), 180f);
            Box(room, "Bookshelf", P(0.22f, 0.9f, 3.4f), new Vector3(0.38f, 1.8f, 1.1f), wood);
            PropLibrary.Dress(room, "Bookshelf", "wooden_bookshelf_worn");
            PropLibrary.Place(room, "cardboard_box_01", P(5.35f, 0f, 4.45f), 12f);
            PropLibrary.Place(room, "cardboard_box_01", P(5.4f, 0.42f, 4.4f), -8f, scale: 0.85f);
            PropLibrary.Place(room, "cardboard_box_01", P(4.65f, 0f, 4.55f), 40f, scale: 0.9f);

            // Floor lamp by the sofa: the only light on.
            Material shade = Mat("LampShade", new Color(0.95f, 0.85f, 0.65f), emissive: true);
            Prim(room, "LampBase", PrimitiveType.Cylinder, P(0.55f, 0.02f, 0.5f), new Vector3(0.3f, 0.02f, 0.3f), wood, collider: false);
            Prim(room, "LampPole", PrimitiveType.Cylinder, P(0.55f, 0.75f, 0.5f), new Vector3(0.025f, 0.75f, 0.025f), wood, collider: false);
            Prim(room, "LampShade", PrimitiveType.Cylinder, P(0.55f, 1.55f, 0.5f), new Vector3(0.38f, 0.15f, 0.38f), shade, collider: false)
                .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Light lamp = PointLight(room, "FloorLamp", P(0.55f, 1.3f, 0.6f), new Color(1f, 0.72f, 0.42f), 1.7f, 7f);
            lamp.shadows = LightShadows.Soft;
            // A faint bounce so the room keeps its shape away from the lamp.
            PointLight(room, "RoomFill", P(3.4f, 2.3f, 2.6f), new Color(0.62f, 0.64f, 0.72f), 0.7f, 7f);
            // Cold city light through the window.
            var cityGo = new GameObject("CityLight");
            cityGo.transform.SetParent(room, false);
            cityGo.transform.position = P(3.1f, 3.4f, RoomD + 2.2f);
            cityGo.transform.rotation = Quaternion.LookRotation(P(2.9f, 0f, 2.2f) - cityGo.transform.position);
            Light city = cityGo.AddComponent<Light>();
            city.type = LightType.Spot;
            city.spotAngle = 70f;
            city.range = 9f;
            city.intensity = 2.2f;
            city.color = new Color(0.55f, 0.65f, 0.9f);
            city.shadows = LightShadows.Soft; // the window frame and the rain on the glass throw shadows into the room
            SetLightLayers(city, LitLayers);

            // The sideboard on the east wall: Dad's radio and the photo.
            Box(room, "Sideboard", P(RoomW - 0.28f, 0.42f, 2.6f), new Vector3(0.48f, 0.84f, 1.6f), wood);
            PropLibrary.Dress(room, "Sideboard", "drawer_cabinet");
            GameObject radio = PropLibrary.Place(room, "vintage_radio_transceiver", P(RoomW - 0.3f, 0.84f, 2.85f), -90f, collider: true);
            // Renderer bounds (collider bounds are not valid until physics syncs).
            Bounds rb = new Bounds(P(RoomW - 0.3f, 1.0f, 2.85f), new Vector3(0.3f, 0.3f, 0.5f));
            if (radio != null)
            {
                Renderer[] rr = radio.GetComponentsInChildren<Renderer>();
                if (rr.Length > 0) { rb = rr[0].bounds; foreach (Renderer x in rr) rb.Encapsulate(x.bounds); }
            }
            // The readout sits on the upper right of the set's front panel (its main body, not the antenna or mic).
            Bounds body = rb;
            if (radio != null)
                foreach (Renderer x in radio.GetComponentsInChildren<Renderer>())
                    if (x.name == "vintage_radio_transceiver") body = x.bounds;
            Transform dialRoot = Group(room, "Dial");
            dialRoot.position = new Vector3(body.min.x - 0.003f, body.max.y - 0.032f, body.center.z - body.extents.z * 0.3f);
            dialRoot.rotation = Quaternion.Euler(0f, 90f, 0f); // facing -X, into the room
            GameObject lampQuad = Box(dialRoot, "DialLamp", Vector3.zero, new Vector3(0.15f, 0.04f, 0.003f), Mat("DialGlass", new Color(0.35f, 0.18f, 0.05f), emissive: true, smoothness: 0.8f), collider: false, isStatic: false);
            var dialGo = new GameObject("DialText", typeof(RectTransform));
            dialGo.transform.SetParent(dialRoot, false);
            dialGo.transform.localPosition = new Vector3(0f, 0f, -0.004f);
            var dial = dialGo.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) dial.font = TMP_Settings.defaultFontAsset;
            dial.text = "FM  103.7";
            dial.fontSize = 0.27f;
            dial.alignment = TextAlignmentOptions.Center;
            dial.color = new Color(1f, 0.62f, 0.18f);
            dial.fontStyle = FontStyles.Bold;
            dial.textWrappingMode = TextWrappingModes.NoWrap;
            ((RectTransform)dialGo.transform).sizeDelta = new Vector2(0.15f, 0.04f);
            Light glow = PointLight(room, "RadioGlow", dialRoot.position + new Vector3(-0.25f, 0.05f, 0f), new Color(1f, 0.58f, 0.2f), 0.8f, 2.2f);
            parts.dial = dial;
            parts.glow = glow;
            parts.dialLamp = lampQuad.GetComponent<Renderer>();
            parts.radioSource = Source(dialRoot, "RadioSpeaker", 0.85f, 9f);
            parts.radioLook = Marker(room, "RadioLook", body.center, 0f);

            // Invisible handles for "Try the radio" / "Look at the photo" (shown once the radio has died).
            ActionButton radioUse = Button3D(room, "RadioUse", rb.center, 0f, "intro_radio", "Try the radio", wood, rb.size + Vector3.one * 0.04f);
            radioUse.GetComponent<Renderer>().enabled = false;
            parts.radioButton = radioUse;

            // The photo in a small standing frame, leaning back.
            Transform frameRoot = Group(room, "PhotoFrame");
            frameRoot.position = P(RoomW - 0.3f, 0.84f, 2.15f);
            frameRoot.rotation = Quaternion.Euler(0f, 90f, 0f) * Quaternion.Euler(12f, 0f, 0f); // print faces the room, leaning back
            Box(frameRoot, "Frame", new Vector3(0f, 0.105f, 0.006f), new Vector3(0.27f, 0.21f, 0.014f), wood, collider: false, isStatic: false);
            Box(frameRoot, "Stand", new Vector3(0f, 0.07f, 0.06f), new Vector3(0.03f, 0.14f, 0.01f), wood, collider: false, isStatic: false)
                .transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            Material printMat = PhotoArt.PrintMaterial();
            GameObject print = Prim(frameRoot, "Print", PrimitiveType.Quad, new Vector3(0f, 0.105f, -0.002f), new Vector3(0.24f, 0.18f, 1f), printMat, new Vector3(0f, 0f, 0f), collider: false);
            print.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            ActionButton photoUse = Button3D(room, "PhotoUse", frameRoot.position + Vector3.up * 0.1f, 0f, "intro_photo", "Look at the photo", wood, new Vector3(0.22f, 0.26f, 0.34f));
            photoUse.GetComponent<Renderer>().enabled = false;
            parts.photoButton = photoUse;

            // The clock above the sideboard (11:47 and ticking).
            Clock(room, P(RoomW - 0.01f, 1.95f, 2.6f), wood);

            // Walk up to the sideboard to hear the rest.
            Transform zones = Group(null, "IntroZones");
            TriggerZone zone = Trigger<TriggerZone>(zones, "Zone_Radio", P(RoomW - 1.0f, 1f, 2.6f), new Vector3(1.4f, 2f, 2.4f));
            SetValue(zone, "id", "intro_radio");
            SetValue(zone, "once", false);
            parts.radioTrigger = zone;

            // Outside: rain past the glass, the street lamp below, the next block's windows, a sky that flashes.
            Transform outside = Group(env, "Town");
            Material black = UnlitMat("TownBlack", new Color(0.012f, 0.013f, 0.018f));
            Material skyMat = UnlitMat("TownSky", new Color(0.03f, 0.035f, 0.05f));
            var rng = new System.Random(1147);
            float R() => (float)rng.NextDouble();
            for (int i = 0; i < 9; i++)
            {
                float x = -14f + i * 3.6f + R(), w = 2.6f + R() * 1.8f, h = 9f + R() * 12f, d = 16f + R() * 18f;
                Box(outside, "Block", P(x, -12f + h / 2f, RoomD + d), new Vector3(w, h, 3f), black, collider: false);
                for (int k = 0; k < 10; k++)
                {
                    if (R() < 0.6f) continue;
                    Color lit = R() < 0.5f ? new Color(1f, 0.7f, 0.38f) : new Color(0.6f, 0.75f, 1f);
                    Box(outside, "LitWindow", P(x + (R() - 0.5f) * (w - 0.8f), -10f + R() * (h - 2f), RoomD + d - 1.52f), new Vector3(0.6f, 0.8f, 0.02f),
                        UnlitMat(lit.r > 0.9f ? "TownWindowWarm" : "TownWindowCold", lit * 0.55f), collider: false);
                }
            }
            GameObject skyQuad = Box(outside, "Sky", P(3f, 10f, RoomD + 60f), new Vector3(140f, 50f, 1f), skyMat, collider: false, isStatic: false);
            PointLight(outside, "StreetGlow", P(3.5f, -6f, RoomD + 6f), new Color(1f, 0.6f, 0.3f), 2.5f, 16f);
            ParticleSystem outRain = IntroRain(outside, P(3f, 4f, RoomD + 5f), new Vector3(10f, 1f, 6f), 260f, 9f);
            outRain.name = "WindowRain";
            parts.sky = new[] { skyQuad.GetComponent<Renderer>() };
            IntroDressing.CityBokeh(outside, P(3f, 0f, RoomD + 22f), 26, 47);

            // The rest of the room: curtains, radiator, books, a laptop glowing on the table, a coat by the door...
            IntroDressing.FlatDetails(room, P, RoomW, RoomD, RoomH);
            IntroDressing.Dust(room, P(1.2f, 1.3f, 1.2f), new Vector3(2.2f, 1.4f, 2f), new Color(1f, 0.9f, 0.75f, 0.55f));

            // Rain heard through the glass (muffled) and the room's own tone.
            parts.rainSource = Source(room, "WindowRainSound", 0.6f, 12f);
            parts.rainSource.transform.position = P((wx0 + wx1) / 2f, 1.5f, RoomD + 0.3f);
            parts.rainSource.gameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 2200f;

            // Where the player stands (by the window), and the opening shot (across the room to the sideboard).
            parts.playerStart = Marker(room, "PlayerStart", P(3.0f, 0.02f, 4.05f), 0f);
            parts.shotStart = Marker(room, "FlatShotStart", P(0.75f, 1.62f, 4.3f), 0f);
            parts.shotStart.rotation = Quaternion.LookRotation(P(5.7f, 1.0f, 2.3f) - parts.shotStart.position);
            parts.shotEnd = Marker(room, "FlatShotEnd", P(3.7f, 1.38f, 3.0f), 0f);
            parts.shotEnd.rotation = Quaternion.LookRotation(P(5.75f, 1.25f, 2.55f) - parts.shotEnd.position);
            foreach (Renderer r in outside.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>A plain round wall clock (face, rim, twelve marks, three hands) driven by WallClock.</summary>
        private static void Clock(Transform room, Vector3 at, Material rimMat)
        {
            Transform clock = Group(room, "WallClock");
            clock.position = at;
            clock.rotation = Quaternion.Euler(0f, 90f, 0f); // face looks along -X (into the room)
            Material face = Mat("ClockFace", new Color(0.86f, 0.84f, 0.78f), smoothness: 0.3f);
            Material ink = Mat("ClockInk", new Color(0.05f, 0.05f, 0.05f));
            Prim(clock, "Rim", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.02f), new Vector3(0.34f, 0.02f, 0.34f), rimMat, new Vector3(90f, 0f, 0f), collider: false);
            Prim(clock, "Face", PrimitiveType.Cylinder, new Vector3(0f, 0f, -0.026f), new Vector3(0.3f, 0.02f, 0.3f), face, new Vector3(90f, 0f, 0f), collider: false);
            for (int i = 0; i < 12; i++)
            {
                float a = i * 30f * Mathf.Deg2Rad;
                Box(clock, "Mark", new Vector3(Mathf.Sin(a) * 0.125f, Mathf.Cos(a) * 0.125f, -0.048f), new Vector3(0.008f, i % 3 == 0 ? 0.03f : 0.016f, 0.002f), ink, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, -i * 30f);
            }
            Transform Hand(string name, float length, float width, float z, Material m)
            {
                Transform pivot = Group(clock, name, new Vector3(0f, 0f, z));
                Box(pivot, "Blade", new Vector3(0f, length / 2f - 0.012f, 0f), new Vector3(width, length, 0.002f), m, collider: false, isStatic: false);
                return pivot;
            }
            Transform hour = Hand("HourHand", 0.075f, 0.012f, -0.05f, ink);
            Transform minute = Hand("MinuteHand", 0.11f, 0.008f, -0.052f, ink);
            Transform second = Hand("SecondHand", 0.12f, 0.003f, -0.054f, Mat("ClockRed", new Color(0.6f, 0.08f, 0.06f)));
            var wc = clock.gameObject.AddComponent<WallClock>();
            Set(wc, "hourHand", hour);
            Set(wc, "minuteHand", minute);
            Set(wc, "secondHand", second);
            AudioSource tick = Source(clock, "Tick", 1f, 7f);
            tick.loop = false;
            Set(wc, "tickSource", tick);
        }

        // ------------------------------------------------------------------ road, forest, gate, car

        /// <summary>A wet asphalt lane following the ground.</summary>
        private static void Road(Transform env, Vector3 from, Vector3 to)
        {
            Material asphalt = EndingBuilder.Textured("IntroRoad", "asphalt_02", new Color(0.17f, 0.18f, 0.2f), 0.72f);
            float len = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));
            int steps = Mathf.CeilToInt(len / 1.5f);
            const float half = 2.6f;
            Vector3 dir = (to - from).normalized, side = Vector3.Cross(Vector3.up, dir).normalized;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int i = 0; i <= steps; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)steps);
                foreach (float s in new[] { -half, half })
                {
                    Vector3 q = p + side * s;
                    q.y = EndingBuilder.H(q.x, q.z) + 0.05f;
                    v.Add(q);
                    uv.Add(new Vector2(s / 3f, i / (float)steps * len / 3f));
                }
                if (i > 0)
                {
                    int a = (i - 1) * 2;
                    tri.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
                }
            }
            var mesh = new Mesh { name = "Road" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            var go = new GameObject("Road");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = asphalt;
            go.isStatic = true;
            // Faded white edge lines.
            Material paint = Mat("RoadPaint", new Color(0.55f, 0.55f, 0.52f), smoothness: 0.6f);
            for (int i = 0; i < steps; i += 2)
                foreach (float s in new[] { -half + 0.25f, half - 0.25f })
                {
                    Vector3 a = Vector3.Lerp(from, to, i / (float)steps) + side * s, b = Vector3.Lerp(from, to, (i + 1) / (float)steps) + side * s;
                    a.y = EndingBuilder.H(a.x, a.z) + 0.06f;
                    b.y = EndingBuilder.H(b.x, b.z) + 0.06f;
                    GameObject line = Box(env, "EdgeLine", (a + b) / 2f, new Vector3(0.12f, 0.01f, Vector3.Distance(a, b)), paint, collider: false);
                    line.transform.rotation = Quaternion.LookRotation(b - a);
                    line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
        }

        /// <summary>Removes any tree standing within clearance metres of the road line (XZ).</summary>
        private static void ClearRoad(Transform trees, Vector3 from, Vector3 to, float clearance)
        {
            if (trees == null) return;
            Vector2 a = new Vector2(from.x, from.z), b = new Vector2(to.x, to.z), ab = b - a;
            var doomed = new List<GameObject>();
            foreach (Transform t in trees)
            {
                Vector2 q = new Vector2(t.position.x, t.position.z);
                float k = Mathf.Clamp01(Vector2.Dot(q - a, ab) / ab.sqrMagnitude);
                if (Vector2.Distance(q, a + ab * k) < clearance) doomed.Add(t.gameObject);
            }
            foreach (GameObject g in doomed) Object.DestroyImmediate(g);
        }

        /// <summary>Firs crowding both sides of the road up to the gate: trunks and drooping tiers in the headlights.</summary>
        private static void Forest(Transform env, Vector3 from, Vector3 to, Vector3 dir, Vector3 right)
        {
            Material bark = Mat("Bark", new Color(0.16f, 0.13f, 0.1f), smoothness: 0.05f);
            Material needles = Mat("Needles", new Color(0.07f, 0.12f, 0.08f), smoothness: 0.05f);
            Material needlesDark = Mat("NeedlesDark", new Color(0.05f, 0.08f, 0.06f), smoothness: 0.05f);
            Mesh cone = EndingBuilder.Cone();
            var rng = new System.Random(2016);
            float R() => (float)rng.NextDouble();
            Transform forest = Group(env, "Forest");
            float len = Vector3.Distance(from, to) - 14f; // open ground for the last stretch before the gate
            for (float d = 0f; d < len; d += 2.6f + R() * 2.2f)
                foreach (float side in new[] { -1f, 1f })
                    for (int row = 0; row < 3; row++)
                    {
                        if (row > 0 && R() < 0.35f) continue;
                        float off = side * (4.4f + row * 3.6f + R() * 2.5f);
                        Vector3 p = from + dir * (d + R() * 1.5f) + right * off;
                        float s = 0.85f + R() * 0.75f;
                        Transform tree = Group(forest, "Fir", new Vector3(p.x, EndingBuilder.H(p.x, p.z) - 0.1f, p.z));
                        tree.localRotation = Quaternion.Euler((R() - 0.5f) * 4f, R() * 360f, (R() - 0.5f) * 4f);
                        Prim(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f * s, 0f), new Vector3(0.28f * s, 2f * s, 0.28f * s), bark, collider: false);
                        int tiers = 5;
                        for (int k = 0; k < tiers; k++)
                        {
                            var tier = new GameObject("Tier");
                            tier.transform.SetParent(tree, false);
                            float w = (3.0f - k * 0.5f) * s;
                            tier.transform.localPosition = new Vector3(0f, (1.8f + k * 1.7f) * s, 0f);
                            tier.transform.localScale = new Vector3(w, 2.8f * s, w);
                            tier.transform.localRotation = Quaternion.Euler(0f, R() * 360f, 0f);
                            tier.AddComponent<MeshFilter>().sharedMesh = cone;
                            tier.AddComponent<MeshRenderer>().sharedMaterial = R() < 0.5f ? needles : needlesDark;
                        }
                        foreach (Transform c in tree.GetComponentsInChildren<Transform>()) c.gameObject.isStatic = true;
                    }
        }

        /// <summary>The station gate across the road: concrete posts, a chained steel gate and the sign.</summary>
        private static void Gate(Transform env, Vector3 at, Vector3 dir, Vector3 right)
        {
            Transform g = Group(env, "Gate");
            g.position = new Vector3(at.x, EndingBuilder.H(at.x, at.z), at.z);
            g.rotation = Quaternion.LookRotation(dir);
            Material concrete = EndingBuilder.Textured("GatePost", "concrete_wall_004", new Color(0.5f, 0.5f, 0.48f), 0.1f);
            Material steel = Mat("GateSteel", new Color(0.22f, 0.2f, 0.18f), smoothness: 0.35f);
            foreach (float x in new[] { -3.3f, 3.3f })
                Box(g, "Post", new Vector3(x, 1.1f, 0f), new Vector3(0.45f, 2.2f, 0.45f), concrete);
            // Two leaves of steel bars, chained in the middle.
            foreach (float sgn in new[] { -1f, 1f })
            {
                Box(g, "Rail", new Vector3(sgn * 1.55f, 1.6f, 0f), new Vector3(3.0f, 0.07f, 0.07f), steel, collider: false);
                Box(g, "Rail", new Vector3(sgn * 1.55f, 0.35f, 0f), new Vector3(3.0f, 0.07f, 0.07f), steel, collider: false);
                for (int i = 0; i < 9; i++)
                    Box(g, "Bar", new Vector3(sgn * (0.12f + i * 0.35f), 0.95f, 0f), new Vector3(0.035f, 1.35f, 0.035f), steel, collider: false);
            }
            Box(g, "Chain", new Vector3(0f, 1.1f, -0.03f), new Vector3(0.12f, 0.22f, 0.04f), steel, collider: false);
            // Chain-link fence running off into the trees.
            Material fence = Mat("FenceWire", new Color(0.25f, 0.25f, 0.25f), smoothness: 0.3f);
            foreach (float sgn in new[] { -1f, 1f })
                for (int i = 0; i < 6; i++)
                {
                    float x = sgn * (3.3f + 2.5f * (i + 1));
                    Box(g, "FencePost", new Vector3(x, 1.0f, 0f), new Vector3(0.08f, 2f, 0.08f), steel, collider: false);
                    Box(g, "FenceTop", new Vector3(x - sgn * 1.25f, 1.95f, 0f), new Vector3(2.5f, 0.04f, 0.04f), fence, collider: false);
                }
            Box(g, "SignBoard", new Vector3(-1.55f, 1.1f, -0.06f), new Vector3(1.6f, 0.8f, 0.03f), Mat("SignWhite", new Color(0.7f, 0.68f, 0.62f)), collider: false);
            var sign = new GameObject("SignText", typeof(RectTransform));
            sign.transform.SetParent(g, false);
            sign.transform.localPosition = new Vector3(-1.55f, 1.1f, -0.08f);
            var tmp = sign.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = "<b>NORTHPOINT RADIO</b>\n<size=70%>STATION CLOSED\nNO ENTRY</size>";
            tmp.fontSize = 1.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.55f, 0.08f, 0.06f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ((RectTransform)sign.transform).sizeDelta = new Vector2(1.5f, 0.7f);
        }

        /// <summary>Markers every few metres along the lane at eye height, pitched to the slope of the road.</summary>
        private static Transform[] DrivePath(Vector3 from, Vector3 to, Vector3 dir, int count)
        {
            Transform root = Group(null, "DrivePath");
            var list = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                Vector3 p = Vector3.Lerp(from, to, i / (float)(count - 1));
                p.y = Road(p) + 1.12f;
                Vector3 ahead = p + dir * 2.5f, behind = p - dir * 2.5f;
                float slope = Mathf.Atan2(Road(ahead) - Road(behind), 5f) * Mathf.Rad2Deg;
                Transform m = Marker(root, "Drive_" + i, p, 0f);
                m.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(-slope, 0f, 0f);
                list[i] = m;
            }
            return list;
        }

        private static float Road(Vector3 p) => EndingBuilder.H(p.x, p.z) + 0.05f;

        private static void Headlights(Transform car)
        {
            foreach (float x in new[] { -0.62f, 0.62f })
            {
                var go = new GameObject("Headlight");
                go.transform.SetParent(car, false);
                go.transform.localPosition = new Vector3(x + 0.35f, -0.5f, 2.1f);
                go.transform.localRotation = Quaternion.Euler(6f, x * 3f, 0f);
                var l = go.AddComponent<Light>();
                l.type = LightType.Spot;
                l.range = 40f;
                l.spotAngle = 55f;
                l.innerSpotAngle = 24f;
                l.intensity = 6f;
                l.color = new Color(1f, 0.93f, 0.8f);
                l.shadows = LightShadows.None;
            }
        }

        /// <summary>Rain round the car, falling in the world as the car moves through it (none inside the cabin).</summary>
        private static void CarRain(Transform car)
        {
            ParticleSystem ps = IntroRain(car, Vector3.zero, new Vector3(22f, 1f, 26f), 900f, 10f);
            ps.transform.localPosition = new Vector3(0.3f, 9f, 22f); // from ~9 m in front of the glass outwards
        }

        /// <summary>Calm rain: thin, short streaks falling at a believable speed (not light-speed lines).</summary>
        private static ParticleSystem IntroRain(Transform parent, Vector3 pos, Vector3 box, float rate, float speed)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // emit downwards
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 1.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.85f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.02f);
            main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.35f);
            main.maxParticles = Mathf.CeilToInt(rate * 1.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(box.x, box.z, box.y);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 1f;
            r.velocityScale = 0.035f;
            r.sharedMaterial = EndingBuilder.SoftParticles("IntroRain", Color.white);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        /// <summary>
        /// Driver's seat, seen from the eyes: dashboard, windscreen with running raindrops and two wipers,
        /// pillars, roof, mirror, steering wheel and the car radio (amber display) on the centre console.
        /// </summary>
        private static void Interior(Transform car, out TMP_Text display, out Light glow, out Transform radioAt, out Transform[] wipers)
        {
            Transform cab = Group(car, "Interior");
            Material dash = Mat("CarDash", new Color(0.035f, 0.035f, 0.04f), smoothness: 0.35f);
            Material trim = Mat("CarTrim", new Color(0.06f, 0.06f, 0.065f), smoothness: 0.2f);
            Material glass = Mat("Windscreen", new Color(0.55f, 0.6f, 0.7f, 0.07f), smoothness: 0.96f);
            EndingBuilder.MakeTransparent(glass);

            GameObject top = Box(cab, "Dashboard", new Vector3(0.1f, -0.36f, 0.78f), new Vector3(1.75f, 0.18f, 0.5f), dash, collider: false, isStatic: false);
            top.transform.localRotation = Quaternion.Euler(-7f, 0f, 0f);
            Box(cab, "DashLip", new Vector3(0.1f, -0.27f, 1.0f), new Vector3(1.75f, 0.04f, 0.12f), dash, collider: false, isStatic: false);
            Material bonnet = Mat("CarBonnet", new Color(0.05f, 0.055f, 0.06f), smoothness: 0.4f);
            bonnet.SetFloat("_EnvironmentReflections", 0f); // the glossy paint mirrored the sky into a pale slab
            bonnet.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            GameObject hood = Box(cab, "Hood", new Vector3(0.1f, -0.36f, 1.95f), new Vector3(1.7f, 0.06f, 1.6f), bonnet, collider: false, isStatic: false);
            hood.transform.localRotation = Quaternion.Euler(4f, 0f, 0f);
            // A dark wet bonnet: only the lightning lights it (the cabin lamps made it a glowing slab).
            hood.GetComponent<Renderer>().renderingLayerMask = 1u << 3;
            GameObject screen = Box(cab, "Windscreen", new Vector3(0.1f, 0.04f, 0.86f), new Vector3(1.6f, 0.68f, 0.006f), glass, collider: false, isStatic: false);
            screen.transform.localRotation = Quaternion.Euler(-34f, 0f, 0f);
            // Not lit by the cabin lamps (their highlights sat on the glass like two orange moons).
            screen.GetComponent<Renderer>().renderingLayerMask = 1u << 3;
            foreach (float x in new[] { -0.62f, 0.82f })
                Box(cab, "Pillar", new Vector3(x, 0.04f, 0.84f), new Vector3(0.11f, 0.74f, 0.1f), trim, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(-34f, 0f, 0f);
            Box(cab, "Roof", new Vector3(0.1f, 0.33f, 0.3f), new Vector3(1.8f, 0.05f, 1.0f), trim, collider: false, isStatic: false);
            Box(cab, "SunVisor", new Vector3(-0.15f, 0.29f, 0.58f), new Vector3(0.6f, 0.03f, 0.2f), trim, collider: false, isStatic: false)
                .transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
            Box(cab, "Mirror", new Vector3(0.2f, 0.22f, 0.66f), new Vector3(0.24f, 0.07f, 0.03f), trim, collider: false, isStatic: false);
            Box(cab, "DoorPanel", new Vector3(-0.78f, -0.45f, 0.1f), new Vector3(0.06f, 0.4f, 1.2f), trim, collider: false, isStatic: false);

            // Steering wheel and column.
            var wheel = new GameObject("SteeringWheel");
            wheel.transform.SetParent(cab, false);
            wheel.transform.localPosition = new Vector3(0f, -0.27f, 0.5f);
            wheel.transform.localRotation = Quaternion.Euler(-62f, 0f, 0f);
            wheel.AddComponent<MeshFilter>().sharedMesh = Torus(0.18f, 0.017f);
            wheel.AddComponent<MeshRenderer>().sharedMaterial = dash;
            Box(cab, "Spoke", new Vector3(0f, -0.285f, 0.515f), new Vector3(0.33f, 0.03f, 0.02f), dash, collider: false, isStatic: false)
                .transform.localRotation = Quaternion.Euler(-62f, 0f, 0f);
            Prim(cab, "Column", PrimitiveType.Cylinder, new Vector3(0f, -0.33f, 0.61f), new Vector3(0.06f, 0.12f, 0.06f), dash, new Vector3(-62f, 0f, 0f), collider: false);

            // Car radio on the centre console, high enough to sit in the frame.
            Transform unit = Group(cab, "Radio", new Vector3(0.36f, -0.25f, 0.7f));
            unit.localRotation = Quaternion.Euler(-24f, -10f, 0f);
            Box(unit, "Body", Vector3.zero, new Vector3(0.22f, 0.07f, 0.04f), trim, collider: false, isStatic: false);
            Box(unit, "Window", new Vector3(0f, 0.008f, -0.0205f), new Vector3(0.13f, 0.028f, 0.001f), Mat("RadioWindow", new Color(0.04f, 0.02f, 0.0f), smoothness: 0.9f), collider: false, isStatic: false);
            foreach (float x in new[] { -0.088f, 0.088f })
                Prim(unit, "Knob", PrimitiveType.Cylinder, new Vector3(x, 0f, -0.026f), new Vector3(0.022f, 0.006f, 0.022f), dash, new Vector3(90f, 0f, 0f), collider: false);
            var t = new GameObject("Display", typeof(RectTransform));
            t.transform.SetParent(unit, false);
            t.transform.localPosition = new Vector3(0f, 0.008f, -0.0215f);
            var tmp = t.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = "FM  88.0";
            tmp.fontSize = 0.16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.62f, 0.15f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ((RectTransform)t.transform).sizeDelta = new Vector2(0.13f, 0.03f);
            display = tmp;
            var glowGo = new GameObject("RadioGlow");
            glowGo.transform.SetParent(unit, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.02f, -0.12f);
            glow = glowGo.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.range = 0.9f;
            glow.intensity = 0.45f;
            glow.color = new Color(1f, 0.6f, 0.2f);
            glow.shadows = LightShadows.None;
            radioAt = unit;

            // Faint light through the glass so the cabin's shapes read against the night.
            var fillGo = new GameObject("CabinFill");
            fillGo.transform.SetParent(cab, false);
            fillGo.transform.localPosition = new Vector3(0.1f, -0.05f, 0.05f);
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 1.4f;
            fill.intensity = 0.35f;
            fill.color = new Color(0.55f, 0.65f, 0.9f);
            fill.shadows = LightShadows.None;

            // Raindrops on the windscreen: beads that gather and run down the glass.
            // An unscaled plane lying in the glass (its -Z faces the driver): the drops and the wipers sit in it.
            // (Particle systems under the glass box's squashed scale were skewed into long streaks across the view.)
            Transform plane = Group(cab, "WiperPlane", new Vector3(0.1f, 0.04f, 0.86f));
            plane.localRotation = Quaternion.Euler(-34f, 0f, 0f);
            Drops(plane, beads: true, new Vector2(1.5f, 0.74f));
            Drops(plane, beads: false, new Vector2(1.5f, 0.74f));

            // Wipers parked at the bottom of the glass, pivoting in its plane.
            wipers = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                Transform pivot = Group(plane, "Wiper", new Vector3(i == 0 ? -0.5f : 0.22f, -0.33f, -0.012f));
                // Parked lying flat along the bottom edge (pointing +X), sweeping up anticlockwise.
                Transform arm = Group(pivot, "Arm");
                arm.localRotation = Quaternion.Euler(0f, 0f, -84f);
                Box(arm, "Blade", new Vector3(0f, 0.3f, 0f), new Vector3(0.018f, 0.6f, 0.01f), dash, collider: false, isStatic: false);
                wipers[i] = pivot;
            }
            foreach (Renderer r in cab.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>Drops on a pane: beads gather and runs trickle down, in an unscaled plane facing the viewer along -Z.</summary>
        private static void Drops(Transform plane, bool beads, Vector2 area)
        {
            var go = new GameObject(beads ? "Beads" : "Runs");
            go.transform.SetParent(plane, false);
            go.transform.localPosition = new Vector3(0f, 0f, -0.012f); // just in front of the glass
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = beads ? new ParticleSystem.MinMaxCurve(3f, 7f) : new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = 0f;
            main.startSize = beads ? new ParticleSystem.MinMaxCurve(0.004f, 0.011f) : new ParticleSystem.MinMaxCurve(0.003f, 0.006f);
            main.startColor = new Color(0.75f, 0.8f, 0.92f, beads ? 0.55f : 0.4f);
            float scale = area.x * area.y / (1.5f * 0.74f);
            main.maxParticles = Mathf.CeilToInt((beads ? 420 : 80) * scale);
            var emission = ps.emission;
            emission.rateOverTime = (beads ? 75f : 30f) * scale;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale = new Vector3(area.x, area.y, 1f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = new ParticleSystem.MinMaxCurve(-0.004f, 0.004f);
            vel.y = beads ? new ParticleSystem.MinMaxCurve(-0.012f, 0f) : new ParticleSystem.MinMaxCurve(-0.18f, -0.08f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = EndingBuilder.SoftParticles("WindscreenDrops", Color.white);
            if (!beads) { r.renderMode = ParticleSystemRenderMode.Stretch; r.velocityScale = 0.25f; r.lengthScale = 2f; }
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        private static Mesh Torus(float radius, float tube)
        {
            const int seg = 32, side = 8;
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2f;
                    v.Add(c + (c.normalized * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)) * tube);
                    if (i < seg && j < side)
                    {
                        int k = i * (side + 1) + j, n = k + side + 1;
                        tri.AddRange(new[] { k, n, k + 1, k + 1, n, n + 1 });
                    }
                }
            }
            var mesh = new Mesh { name = "Wheel" };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ grading

        /// <summary>Night: cold, desaturated, a little grain.</summary>
        private static GameObject PostFx(string name)
        {
            EnsureFolder(GeneratedDir);
            string p = GeneratedDir + "/" + name + "_PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(p) != null) AssetDatabase.DeleteAsset(p);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.6f);
            bloom.scatter.Override(0.7f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.25f);
            ca.contrast.Override(14f);
            ca.saturation.Override(-22f);
            ca.colorFilter.Override(new Color(0.88f, 0.92f, 1.04f));
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.22f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.38f);
            vig.smoothness.Override(0.5f);
            AssetDatabase.CreateAsset(profile, p);
            foreach (VolumeComponent comp in profile.components) AssetDatabase.AddObjectToAsset(comp, profile);
            AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
            return go;
        }
    }
}
