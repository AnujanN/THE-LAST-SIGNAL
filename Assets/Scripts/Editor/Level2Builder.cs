using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Puzzles;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Level 2 - The Underground Secrets (medium). Introduces The Echo, noise, crouching and hiding.
    /// Landing -> maintenance corridor (Echo patrol, lockers) -> storage / flooded section / pump room
    /// (3 generator parts) -> generator chamber (start-up order puzzle, fire recording) -> elevator.
    /// </summary>
    public static class Level2Builder
    {
        public const string ScenePath = "Assets/Scenes/Level02_Basement.unity";
        private const float H = 3f;

        [MenuItem("The Last Signal/Build/Level 2 - The Underground Secrets")]
        public static void Build()
        {
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();
            GameObject echoPrefab = GamePrefabs.Echo();
            var scene = NewScene("Level02_Basement");
            Atmosphere(new Color(0.03f, 0.035f, 0.04f), new Color(0.01f, 0.012f, 0.015f), 0.03f);

            Material floor = Mat("L2_Concrete", new Color(0.22f, 0.22f, 0.21f));
            Material wall = Mat("L2_Wall", new Color(0.33f, 0.34f, 0.32f));
            Material pipe = Mat("L2_Pipe", new Color(0.32f, 0.22f, 0.15f), smoothness: 0.4f);
            Material metal = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Material locker = Mat("L2_Locker", new Color(0.25f, 0.3f, 0.32f), smoothness: 0.35f);
            Material crate = Mat("L1_Wood", new Color(0.36f, 0.25f, 0.16f));
            Material paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            Material partMat = Mat("Greybox_Accent", new Color(0.9f, 0.75f, 0.2f));
            Material black = Mat("Black", new Color(0.03f, 0.03f, 0.03f));
            Material slotMat = Mat("SlotEmpty", new Color(0.45f, 0.3f, 0.08f));
            Material red = Mat("Red", new Color(0.6f, 0.08f, 0.06f));
            Material yellow = Mat("Yellow", new Color(0.75f, 0.65f, 0.1f));
            Material green = Mat("Green", new Color(0.15f, 0.55f, 0.2f));
            Material lampMat = Mat("Greybox_Lamp", new Color(1f, 0.93f, 0.75f), emissive: true);

            // ------------------------------------------------------------------ geometry (NavMesh source)
            var envGo = new GameObject("Environment");
            Transform env = envGo.transform;
            Slab(env, "Floor", -16f, -2f, 16f, 34f, 0f, floor);
            Slab(env, "Ceiling", -16f, -2f, 16f, 34f, H, wall, ceiling: true);
            // Landing.
            WallZ(env, "Landing_W", -2f, -1f, 4f, H, wall);
            WallZ(env, "Landing_E", 2f, -1f, 4f, H, wall);
            WallX(env, "Landing_S", -1f, -2f, 2f, H, wall);
            // Corridor A (east-west).
            WallX(env, "CorridorA_S", 4f, -15f, 15f, H, wall, 0f);
            WallX(env, "CorridorA_N", 7f, -15f, 15f, H, wall, -11f, 0f, 11f);
            WallZ(env, "CorridorA_W", -15f, 4f, 7f, H, wall);
            WallZ(env, "CorridorA_E", 15f, 4f, 7f, H, wall);
            // Storage 1 (west).
            WallZ(env, "Storage1_W", -15f, 7f, 15f, H, wall);
            WallZ(env, "Storage1_E", -7f, 7f, 15f, H, wall);
            WallX(env, "Storage1_N", 15f, -15f, -7f, H, wall);
            // Corridor B (north) with storage 2 (east) and pump room (west).
            WallZ(env, "CorridorB_W", -1.5f, 7f, 25f, H, wall, 20f);
            WallZ(env, "CorridorB_E", 1.5f, 7f, 25f, H, wall, 12f);
            WallX(env, "Storage2_S", 9f, 1.5f, 7f, H, wall);
            WallX(env, "Storage2_N", 15f, 1.5f, 7f, H, wall);
            WallZ(env, "Storage2_E", 7f, 9f, 15f, H, wall);
            WallX(env, "Pump_S", 17f, -7f, -1.5f, H, wall);
            WallX(env, "Pump_N", 23f, -7f, -1.5f, H, wall);
            WallZ(env, "Pump_W", -7f, 17f, 23f, H, wall);
            // Flooded section (east).
            WallZ(env, "Flood_W", 7f, 7f, 15f, H, wall);
            WallZ(env, "Flood_E", 15f, 7f, 15f, H, wall);
            WallX(env, "Flood_N", 15f, 7f, 15f, H, wall);
            // Generator chamber + elevator.
            WallX(env, "Gen_S", 25f, -8f, 8f, H, wall, 0f);
            WallX(env, "Gen_N", 33f, -8f, 8f, H, wall);
            WallZ(env, "Gen_W", -8f, 25f, 33f, H, wall);
            WallZ(env, "Gen_E", 8f, 25f, 33f, H, wall, 29f);
            WallX(env, "Lift_S", 27f, 8f, 11f, H, wall);
            WallX(env, "Lift_N", 31f, 8f, 11f, H, wall);
            WallZ(env, "Lift_E", 11f, 27f, 31f, H, wall);

            // Furniture (static, part of the NavMesh source).
            Box(env, "Shelf_Storage1", new Vector3(-14.5f, 0.55f, 11f), new Vector3(0.8f, 1.1f, 2.4f), crate);
            Box(env, "Crates_Storage1", new Vector3(-9f, 0.5f, 11.5f), new Vector3(1.2f, 1f, 1.2f), crate);
            Box(env, "Table_Storage2", new Vector3(4.5f, 0.4f, 13.8f), new Vector3(1.6f, 0.8f, 0.8f), crate);
            Box(env, "Crate_Flood", new Vector3(14f, 0.5f, 13f), Vector3.one, crate);
            Box(env, "Crate_Flood2", new Vector3(9f, 0.4f, 13.8f), new Vector3(1.4f, 0.8f, 1f), crate);
            Box(env, "PumpHousing", new Vector3(-5f, 0.6f, 20f), new Vector3(1.4f, 1.2f, 1.4f), metal);
            Box(env, "Generator", new Vector3(0f, 0.9f, 30.3f), new Vector3(3.2f, 1.8f, 1.6f), Mat("Generator", new Color(0.35f, 0.4f, 0.32f), smoothness: 0.4f));
            Box(env, "ControlPanel", new Vector3(3.6f, 0.6f, 29.6f), new Vector3(1.6f, 1.2f, 0.5f), metal);
            Box(env, "Desk_Generator", new Vector3(-5f, 0.45f, 31.5f), new Vector3(2f, 0.9f, 0.9f), crate);
            for (int i = 0; i < 6; i++)
                Box(env, "Pipe", new Vector3(-12f + i * 5f, H - 0.3f, 6.6f), new Vector3(4.6f, 0.2f, 0.2f), pipe);

            // ------------------------------------------------------------------ NavMesh (environment only)
            BakeNavMesh(envGo, "Level02_Basement");

            // ------------------------------------------------------------------ water (not walkable geometry)
            Transform water = Group(null, "Water");
            Box(water, "FloodWater", new Vector3(11f, 0.12f, 11f), new Vector3(7.8f, 0.02f, 7.8f),
                UnlitMat("Water", new Color(0.15f, 0.25f, 0.3f, 0.55f), transparent: true), collider: false);
            SurfaceNoiseZone splash = Trigger<SurfaceNoiseZone>(water, "Zone_Water", new Vector3(11f, 0.8f, 11f), new Vector3(7.8f, 1.6f, 7.8f));
            SetValue(splash, "noiseMultiplier", 2f);

            // ------------------------------------------------------------------ lights
            Transform lights = Group(null, "Lighting");
            PointLight(lights, "Emergency_Landing", new Vector3(0f, 2.7f, 2f), new Color(1f, 0.15f, 0.1f), 0.8f, 6f);
            PointLight(lights, "Emergency_CorridorA_W", new Vector3(-9f, 2.7f, 5.5f), new Color(1f, 0.15f, 0.1f), 0.6f, 6f);
            PointLight(lights, "Emergency_CorridorA_E", new Vector3(9f, 2.7f, 5.5f), new Color(1f, 0.15f, 0.1f), 0.6f, 6f);
            PointLight(lights, "Emergency_Gen", new Vector3(0f, 2.7f, 26f), new Color(1f, 0.15f, 0.1f), 0.6f, 6f);
            Light genLight = PointLight(lights, "Generator_Main", new Vector3(0f, 2.8f, 29f), new Color(1f, 0.92f, 0.75f), 1.8f, 12f);
            Light corrB = PointLight(lights, "CorridorB", new Vector3(0f, 2.8f, 16f), new Color(0.9f, 0.95f, 1f), 1.2f, 9f);
            Light liftLight = PointLight(lights, "Lift", new Vector3(9.5f, 2.7f, 29f), new Color(0.85f, 0.95f, 1f), 1.2f, 5f);
            corrB.gameObject.AddComponent<FlickerLight>();
            var power = new GameObject("GeneratorPower").AddComponent<PowerGroup>();
            SetValue(power, "powerFlag", "power:generator");
            SetArray(power, "lights", new Object[] { genLight, corrB, liftLight });
            Renderer liftPanel = Box(lights, "LiftPanelGlow", new Vector3(10.88f, 1.3f, 29.5f), new Vector3(0.02f, 0.3f, 0.2f), lampMat, collider: false).GetComponent<Renderer>();
            SetArray(power, "emissive", new Object[] { liftPanel });

            // ------------------------------------------------------------------ signs
            Transform signs = Group(null, "Signs");
            Sign(signs, "Sign_Storage1", "STORAGE A", new Vector3(-11f, 2.5f, 6.88f), 0f);
            Sign(signs, "Sign_Flood", "PUMP OVERFLOW", new Vector3(11f, 2.5f, 6.88f), 0f);
            Sign(signs, "Sign_CorridorB", "GENERATOR ROOM ->", new Vector3(0f, 2.5f, 6.88f), 0f);
            Sign(signs, "Sign_Storage2", "STORAGE B", new Vector3(1.38f, 2.5f, 12f), 90f);
            Sign(signs, "Sign_Pump", "PUMP ROOM", new Vector3(-1.38f, 2.5f, 20f), -90f);
            Sign(signs, "Sign_Lift", "ELEVATOR\nTO TRANSMISSION TOWER", new Vector3(7.88f, 2.6f, 29f), 90f, 2.2f);
            Sign(signs, "Sign_Gen", "BACKUP GENERATOR", new Vector3(0f, 2.3f, 29.45f), 0f, 2f, null, new Vector2(4f, 0.5f));

            // ------------------------------------------------------------------ story + interactables
            Transform story = Group(null, "Story");
            var events = story.gameObject.AddComponent<StoryEvents>();
            Transform items = Group(null, "Interactables");

            // Generator parts.
            Pickup(items, "Part_SparkPlug", new Vector3(-14.4f, 1.17f, 11.4f), "spark_plug", "Spark plug", "l2_spark", partMat,
                PrimitiveType.Cylinder, new Vector3(0.05f, 0.09f, 0.05f));
            Pickup(items, "Part_Fuse30A", new Vector3(14f, 1.06f, 13f), "fuse_30a", "30A fuse", "l2_fuse", partMat,
                PrimitiveType.Cylinder, new Vector3(0.06f, 0.08f, 0.06f));
            Pickup(items, "Part_DriveBelt", new Vector3(-5f, 1.23f, 20f), "drive_belt", "Drive belt", "l2_belt", partMat,
                PrimitiveType.Cylinder, new Vector3(0.3f, 0.02f, 0.3f));
            Battery(items, new Vector3(4.6f, 0.86f, 13.8f), "l2_bat1");
            Battery(items, new Vector3(-9f, 1.06f, 11.5f), "l2_bat2");
            Battery(items, new Vector3(9f, 0.86f, 13.8f), "l2_bat3");

            // Lockers (hiding).
            Locker(items, "Locker_A1", new Vector3(-5f, 0f, 6.45f), 180f, locker);
            Locker(items, "Locker_A2", new Vector3(6f, 0f, 6.45f), 180f, locker);
            Locker(items, "Locker_Storage2", new Vector3(6.45f, 0f, 11f), -90f, locker);
            Locker(items, "Locker_Pump", new Vector3(-6.45f, 0f, 18.5f), 90f, locker);
            Locker(items, "Locker_Gen", new Vector3(-6.5f, 0f, 32.45f), 180f, locker);
            Locker(items, "Locker_Storage1", new Vector3(-9.5f, 0f, 14.45f), 180f, locker);

            // Generator puzzle.
            var genGo = new GameObject("GeneratorPuzzle");
            genGo.transform.SetParent(items, false);
            genGo.transform.position = new Vector3(0f, 0.9f, 30.3f);
            var generator = genGo.AddComponent<GeneratorPuzzle>();
            SetValue(generator, "id", "l2_generator");
            ItemSlot plugSlot = Slot(items, "Slot_SparkPlug", new Vector3(-1f, 1.1f, 29.45f), "l2_slot_plug", "spark_plug", "Spark plug", "spark plug socket", slotMat, partMat, new Vector3(0.22f, 0.22f, 0.1f));
            ItemSlot fuseSlot = Slot(items, "Slot_Fuse", new Vector3(0f, 1.1f, 29.45f), "l2_slot_fuse", "fuse_30a", "30A fuse", "fuse holder", slotMat, partMat, new Vector3(0.22f, 0.22f, 0.1f));
            ItemSlot beltSlot = Slot(items, "Slot_Belt", new Vector3(1f, 1.1f, 29.45f), "l2_slot_belt", "drive_belt", "Drive belt", "belt pulley", slotMat, partMat, new Vector3(0.22f, 0.22f, 0.1f));
            Sign(signs, "Label_Plug", "SPARK PLUG", new Vector3(-1f, 1.4f, 29.44f), 0f, 1.4f, null, new Vector2(0.8f, 0.2f));
            Sign(signs, "Label_Fuse", "30A FUSE", new Vector3(0f, 1.4f, 29.44f), 0f, 1.4f, null, new Vector2(0.8f, 0.2f));
            Sign(signs, "Label_Belt", "DRIVE BELT", new Vector3(1f, 1.4f, 29.44f), 0f, 1.4f, null, new Vector2(0.8f, 0.2f));
            SetArray(generator, "parts", new Object[] { plugSlot, fuseSlot, beltSlot });
            SetStrings(generator, "steps", new[] { "Fuel valve", "Choke", "Starter" });
            // Controls deliberately out of order on the panel; the maintenance note gives the order.
            (string label, int step, Material mat, float x)[] controls =
            {
                ("CHOKE", 1, yellow, 3.0f), ("STARTER", 2, green, 3.6f), ("FUEL VALVE", 0, red, 4.2f),
            };
            foreach (var c in controls)
            {
                ActionButton b = Button3D(items, "Control_" + c.label.Replace(' ', '_'), new Vector3(c.x, 0.95f, 29.32f), 0f,
                    "l2_ctrl_" + c.step, "Use " + c.label.ToLowerInvariant(), c.mat, new Vector3(0.16f, 0.16f, 0.1f));
                On(b, "onPressed", generator.PressStep, c.step);
                Sign(signs, "Label_" + c.label, c.label, new Vector3(c.x, 1.3f, 29.3f), 0f, 1.2f, null, new Vector2(0.6f, 0.2f));
            }
            // Torn maintenance card: the first half is on the wall, the second half is in the pump room.
            WallNote(items, "Note_Maintenance", new Vector3(-2.6f, 1.6f, 32.88f), 180f, "l2_maintenance", "Generator maintenance card (torn)",
                "BACKUP GENERATOR - COLD START\n\nFit all parts first: spark plug, 30A fuse, drive belt.\n\n" +
                "1. Open the FUEL VALVE (red).\n2. PRIME her until the pressure needle sits in the GREEN.\n" +
                "    Not past it - she floods, and she stalls LOUD.\n3. ..........\n\n[The rest of the card has been torn away.]",
                paper, new Vector2(0.6f, 0.5f));
            Note(items, "Note_MaintenanceTorn", new Vector3(-4.6f, 1.21f, 19.7f), 30f, "l2_maintenance_b", "Torn piece of card (oil-stained)",
                "...3. CHOKE (yellow) - only with the needle in the green, or she dies on you.\n" +
                "4. STARTER (green). Never before the choke.\n\nIf she stalls, everything resets - begin again at 1.\n- D.", paper);

            // Fuel pressure gauge above the part sockets, primer pump beside it.
            Transform gauge = Group(items, "PressureGauge", new Vector3(0f, 1.63f, 29.47f));
            Prim(gauge, "Dial", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.26f, 0.01f, 0.26f), Mat("GaugeFace", new Color(0.78f, 0.76f, 0.7f)),
                new Vector3(90f, 0f, 0f), collider: false);
            for (int z = 0; z < 3; z++)
            {
                // Zones around the dial: white (low), green (3-4 pumps), red (flood).
                float a = z == 0 ? 45f : z == 1 ? -11f : -58f;
                Color c = z == 0 ? new Color(0.9f, 0.9f, 0.88f) : z == 1 ? new Color(0.15f, 0.6f, 0.2f) : new Color(0.7f, 0.12f, 0.1f);
                Transform arm = Group(gauge, "Zone" + z, new Vector3(0f, 0f, -0.012f));
                arm.localRotation = Quaternion.Euler(0f, 0f, a);
                Prim(arm, "Mark", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(z == 1 ? 0.07f : 0.05f, 0.025f, 0.004f),
                    Mat("GaugeZone" + z, c, emissive: z > 0), collider: false);
            }
            Transform needlePivot = Group(gauge, "NeedlePivot", new Vector3(0f, 0f, -0.018f));
            Prim(needlePivot, "Needle", PrimitiveType.Cube, new Vector3(0f, 0.05f, 0f), new Vector3(0.008f, 0.1f, 0.004f),
                Mat("GaugeNeedle", new Color(0.1f, 0.08f, 0.08f)), collider: false);
            Sign(signs, "Label_Gauge", "FUEL PRESSURE", new Vector3(0f, 1.84f, 29.44f), 0f, 1.1f, null, new Vector2(0.8f, 0.15f));
            Set(generator, "gaugeNeedle", needlePivot);
            ActionButton primer = Button3D(items, "Control_PRIMER", new Vector3(1.35f, 0.75f, 29.45f), 0f, "l2_primer", "Pump the primer",
                Mat("PrimerBulb", new Color(0.5f, 0.45f, 0.4f)), new Vector3(0.14f, 0.14f, 0.12f));
            On(primer, "onPressed", generator.PumpPrimer);
            Sign(signs, "Label_PRIMER", "PRIMER", new Vector3(1.35f, 0.95f, 29.44f), 0f, 1.1f, null, new Vector2(0.5f, 0.15f));
            On(generator, "onPartsReady", events.SetObjective, "l2_start|Start the generator. The maintenance card on the north wall is torn - find the rest of it.");
            On(generator, "onSolved", power.PowerOn);
            On(generator, "onSolved", events.SetObjective, "l2_tape|Play the emergency recording on the generator room desk");
            On(generator, "onSolved", events.Checkpoint, "l2_generator");

            // Fire recording (needs power) - also gives the frequency clue for Level 3.
            TapePlayer tape = Tape(items, "EmergencyRecorder", new Vector3(-5f, 0.99f, 31.5f), 180f, "l2_tape", "emergency recorder", black, new[]
            {
                L("RECORDING", "[Emergency log. Alarms. Rain against the windows.]", 2.5f),
                L("MAYA (TAPE)", "Lightning hit the mast. The broadcast rig caught fire and the signal won't stop.", 5f),
                L("MAYA (TAPE)", "The smoke's so thick. I can't see Alex anywhere.", 4f),
                L("MAYA (TAPE)", "I'm trapped near the broadcasting room. The fire's between me and the stairs.", 5f),
                L("MAYA (TAPE)", "If the signal won't stop, the emergency shutdown only works on 103.7. One-oh-three point seven.", 6f),
                L("RECORDING", "[The recording ends.]", 2f),
            }, requiredFlag: "power:generator", missingFlagMessage: "It needs power. The generator is down.");
            On(tape, "onFinished", events.SetObjective, "l2_lift|Take the elevator up to the transmission tower");
            On(tape, "onFinished", events.Say, "ALEX|103.7. Dad's old frequency. I need to get up to the tower.|4");

            // Elevator.
            Transform doorRig = Group(items, "LiftDoors", new Vector3(8f, 0f, 29f));
            doorRig.localRotation = Quaternion.Euler(0f, 90f, 0f);
            GameObject leftDoor = Box(doorRig, "LeftDoor", new Vector3(-1.2f, 1.1f, 0f), new Vector3(0.8f, 2.2f, 0.06f), metal, collider: false, isStatic: false);
            GameObject rightDoor = Box(doorRig, "RightDoor", new Vector3(1.2f, 1.1f, 0f), new Vector3(0.8f, 2.2f, 0.06f), metal, collider: false, isStatic: false);
            Transform echoAppear = Marker(story, "EchoAppearPoint", new Vector3(6.6f, 0f, 29f), 90f);
            var sequence = story.gameObject.AddComponent<ElevatorSequence>();
            Set(sequence, "leftDoor", leftDoor.transform);
            Set(sequence, "rightDoor", rightDoor.transform);
            SetValue(sequence, "slideDistance", 0.8f);
            Set(sequence, "echoAppearPoint", echoAppear);
            SetValue(sequence, "nextScene", GameFlowManager.Level3Scene);
            ActionButton liftButton = Button3D(items, "LiftButton", new Vector3(10.88f, 1.3f, 29.2f), 90f, "l2_lift_button", "Press elevator button",
                metal, new Vector3(0.12f, 0.12f, 0.06f), requiredFlag: "played:l2_tape", lockedMessage: "The elevator won't respond yet.", oneShot: true);
            On(liftButton, "onPressed", sequence.Begin);

            // The Echo.
            GameObject echoGo = Instantiate(echoPrefab, scene, new Vector3(-13f, 0f, 5.5f), 90f);
            var echo = echoGo.GetComponent<EchoAIController>();
            Transform wpRoot = Group(null, "EchoWaypoints");
            Vector3[] wps =
            {
                new Vector3(-13f, 0f, 5.5f), new Vector3(-11f, 0f, 11f), new Vector3(0f, 0f, 5.5f), new Vector3(0f, 0f, 18f),
                new Vector3(0f, 0f, 27f), new Vector3(0f, 0f, 14f), new Vector3(13f, 0f, 5.5f), new Vector3(11f, 0f, 11f),
            };
            var wpTransforms = new Object[wps.Length];
            for (int i = 0; i < wps.Length; i++) wpTransforms[i] = Marker(wpRoot, "WP_" + i, wps[i], 0f);
            SetArray(echo, "waypoints", wpTransforms);
            SetValue(echo, "startActive", false);
            SetValue(echo, "settings.sightRange", 10f);
            SetValue(echo, "settings.sightAngle", 100f);
            SetValue(echo, "settings.hearingMultiplier", 1f);
            SetValue(echo, "settings.patrolSpeed", 1.2f);
            SetValue(echo, "settings.investigateSpeed", 1.9f);
            SetValue(echo, "settings.chaseSpeed", 3.2f);
            SetValue(echo, "settings.searchSeconds", 7f);
            SetValue(echo, "settings.loseSightSeconds", 2.5f);
            SetValue(echo, "settings.inspectChance", 0.35f);
            SetValue(echo, "settings.directorHintSeconds", 55f);
            SetValue(echo, "settings.lureInterval", 75f);
            Set(sequence, "echo", echo);

            // ------------------------------------------------------------------ scripted beats + tutorial
            TriggerZone meet = Trigger<TriggerZone>(story, "Zone_FirstEncounter", new Vector3(0f, 1f, 5f), new Vector3(3f, 2f, 1.5f));
            SetValue(meet, "id", "l2_meet_echo");
            On(meet, "onEnter", echo.Activate);
            On(meet, "onEnter", events.PlayStatic);
            On(meet, "onEnter", events.Say, "THE ECHO (MAYA'S VOICE)|Alex... you came back for me...|3.5");
            On(meet, "onEnter", events.ShowMessage, "Something is down here. Running makes noise - crouch to move quietly.");

            TriggerZone lockerHint = Trigger<TriggerZone>(story, "Zone_LockerHint", new Vector3(-3f, 1f, 5.5f), new Vector3(2f, 2f, 3f));
            SetValue(lockerHint, "id", "l2_locker_hint");
            On(lockerHint, "onEnter", events.ShowMessage, "If it comes close, hide in a locker and wait for it to leave.");
            TriggerZone lockerHint2 = Trigger<TriggerZone>(story, "Zone_LockerHint2", new Vector3(3f, 1f, 5.5f), new Vector3(2f, 2f, 3f));
            SetValue(lockerHint2, "id", "l2_locker_hint");
            On(lockerHint2, "onEnter", events.ShowMessage, "If it comes close, hide in a locker and wait for it to leave.");

            TriggerZone genRoom = Trigger<TriggerZone>(story, "Zone_GeneratorRoom", new Vector3(0f, 1f, 26.5f), new Vector3(3f, 2f, 2f));
            SetValue(genRoom, "id", "l2_gen_room");
            On(genRoom, "onEnter", events.SetObjectiveIfNotFlag, "puzzle:l2_generator|l2_parts|Find the 3 missing generator parts: spark plug, 30A fuse, drive belt");
            On(genRoom, "onEnter", events.Say, "ALEX|The generator's been gutted. Parts are missing.|3");

            TriggerZone floodZone = Trigger<TriggerZone>(story, "Zone_FloodHint", new Vector3(11f, 1f, 8f), new Vector3(3f, 2f, 1.5f));
            SetValue(floodZone, "id", "l2_flood_hint");
            On(floodZone, "onEnter", events.ShowMessage, "The water is loud. Move slowly.");

            // ------------------------------------------------------------------ checkpoints + level
            CheckpointZone(story, "l2_gen_room", new Vector3(0f, 1f, 24f), 0f, new Vector3(3f, 2f, 1.5f));
            CheckpointZone(story, "l2_generator", new Vector3(0f, 0.02f, 27.5f), 0f, Vector3.one, triggerEnabled: false);

            Transform spawn = Marker(story, "PlayerStart", new Vector3(0f, 0.02f, 0.5f), 0f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 2);
            SetValue(level, "levelName", "The Underground Secrets");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l2_find_gen");
            SetValue(level, "startObjectiveText", "Find the generator room");
            SetLines(level, "introLines", new[]
            {
                L("ALEX", "Water everywhere. The power down here is dead too.", 3f),
                L("ALEX", "There has to be a generator. Maybe an elevator up to the tower.", 3.5f),
            });
            SetValue(level, "flashlightDrain", true);
            SetValue(level, "drainPerSecond", 0.55f);
            SetValue(level, "ambience", Ambience.BasementDrips);
            SetValue(level, "ambienceVolume", 0.65f);

            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level2Builder] Built " + ScenePath);
        }
    }
}
