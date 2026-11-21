using LastSignal.Audio;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Objectives;
using LastSignal.Puzzles;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Level 1 - The Abandoned Station (easy, no enemy).
    /// Exterior in rain -> foyer (flashlight) -> reception + office (3 tape fragments, fuse, clues)
    /// -> service corridor (breaker puzzle, shadow sighting) -> control room (tape, key)
    /// -> basement door (radio warning) -> Level 2.
    /// </summary>
    public static class Level1Builder
    {
        public const string ScenePath = "Assets/Scenes/Level01_Station.unity";
        private const float H = 3.2f;

        [MenuItem("The Last Signal/Build/Level 1 - The Abandoned Station")]
        public static void Build()
        {
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();
            var scene = NewScene("Level01_Station");
            Atmosphere(new Color(0.035f, 0.04f, 0.055f), new Color(0.02f, 0.025f, 0.035f), 0.012f);

            Material ground = Mat("L1_Ground", new Color(0.12f, 0.13f, 0.12f));
            Material floor = Mat("L1_Floor", new Color(0.28f, 0.26f, 0.24f));
            Material wall = Mat("L1_Wall", new Color(0.46f, 0.47f, 0.45f));
            Material facade = Mat("L1_Facade", new Color(0.3f, 0.3f, 0.32f));
            Material wood = Mat("L1_Wood", new Color(0.36f, 0.25f, 0.16f));
            Material metal = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Material doorMat = Mat("L1_Door", new Color(0.32f, 0.22f, 0.14f));
            Material paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            Material tapeMat = Mat("Tape", new Color(0.12f, 0.12f, 0.14f));
            Material accent = Mat("Greybox_Accent", new Color(0.9f, 0.75f, 0.2f));
            Material keyMat = Mat("Key", new Color(0.85f, 0.7f, 0.3f), emissive: true, smoothness: 0.7f);
            Material lampMat = Mat("Greybox_Lamp", new Color(1f, 0.93f, 0.75f), emissive: true);
            Material red = Mat("Red", new Color(0.6f, 0.08f, 0.06f));

            // ------------------------------------------------------------------ geometry
            Transform env = Group(null, "Environment");
            // Exterior yard.
            Slab(env, "Yard", -12f, -18f, 12f, -6f, 0f, ground);
            WallZ(env, "Fence_W", -12f, -18f, -6f, 2.5f, facade);
            WallZ(env, "Fence_E", 12f, -18f, -6f, 2.5f, facade);
            // The way in: Northpoint's gate (the same one the car stopped at in the intro), standing open where
            // Alex pushed it, chain-link either side, and the forest road behind running off into the dark.
            GateKit.Gate(env, new Vector3(YardGateX, 0f, -18f), Quaternion.identity, 74f);
            GateKit.Fence(env, new Vector3(-12f, 0f, -18f), new Vector3(YardGateX - GateKit.HalfOpening - 0.25f, 0f, -18f), 2.4f, q => 0f, solid: true);
            GateKit.Fence(env, new Vector3(YardGateX + GateKit.HalfOpening + 0.25f, 0f, -18f), new Vector3(12f, 0f, -18f), 2.4f, q => 0f, solid: true);
            OutsideTheGate(env);
            Box(env, "Path", new Vector3(0f, 0.01f, -12f), new Vector3(2.2f, 0.02f, 12f), floor);
            StreetLamp(env, new Vector3(2.2f, 0f, -10f), metal);
            // Alex's car (right-hand drive), parked where he stopped in the rain, lamps off.
            CarModel.Build(env, "ParkedCar", new Vector3(-3.6f, 0f, -14.2f), 8f, new Color(0.07f, 0.085f, 0.1f), "LS16 ALX");
            // Northpoint's mast behind the building, its red beacon blinking over the roof.
            EndingBuilder.Mast(env, new Vector3(7f, 0f, 30f), 34f);

            // Interior shell.
            // Floor with the stairwell opening (x 2.6..5.9, z 5.1..6.9) cut out.
            Slab(env, "Floor", -12f, -6f, StairX0, 24f, 0f, floor);
            Slab(env, "Floor", StairX0, -6f, StairX1, StairZ0, 0f, floor);
            Slab(env, "Floor", StairX0, StairZ1, StairX1, 24f, 0f, floor);
            Slab(env, "Floor", StairX1, -6f, 12f, 24f, 0f, floor);
            Slab(env, "Ceiling", -12f, -6f, 12f, 24f, H, wall, ceiling: true);
            WallX(env, "Facade", -6f, -12f, 12f, 3.4f, facade, 0f);
            UpperFacade(env, facade);
            WallZ(env, "Reception_W", -12f, -6f, 2f, H, wall);
            WallZ(env, "Foyer_Reception", -4f, -6f, 2f, H, wall, -2f);
            WallZ(env, "Foyer_Office", 4f, -6f, 2f, H, wall, -2f);
            WallZ(env, "Office_E", 12f, -6f, 2f, H, wall);
            WallX(env, "FrontRooms_N", 2f, -12f, 12f, H, wall, 0f);
            WallZ(env, "Corridor_W", -1.5f, 2f, 14f, H, wall);
            WallZ(env, "Corridor_E", 1.5f, 2f, 14f, H, wall, 6f);
            WallX(env, "Stair_S", 4f, 1.5f, 6f, H, wall);
            WallX(env, "Stair_N", 8f, 1.5f, 6f, H, wall);
            WallZ(env, "Stair_E", 6f, 4f, 8f, H, wall);
            WallX(env, "Control_S", 14f, -8f, 8f, H, wall, 0f);
            WallZ(env, "Control_W", -8f, 14f, 24f, H, wall);
            WallZ(env, "Control_E", 8f, 14f, 24f, H, wall);
            WallX(env, "Control_N", 24f, -8f, 8f, H, wall);

            // Furniture.
            Box(env, "SecurityDesk", new Vector3(2.5f, 0.4f, -4f), new Vector3(1.6f, 0.8f, 0.7f), wood);
            Box(env, "Bench", new Vector3(-2.8f, 0.25f, -1f), new Vector3(0.6f, 0.5f, 2f), wood);
            Box(env, "ReceptionDesk", new Vector3(-8f, 0.45f, -4f), new Vector3(2.5f, 0.9f, 0.8f), wood);
            Box(env, "FilingCabinet", new Vector3(-11.2f, 0.6f, 1.2f), new Vector3(0.8f, 1.2f, 0.6f), metal);
            Box(env, "Sofa", new Vector3(-6f, 0.3f, 1.2f), new Vector3(2.4f, 0.6f, 0.8f), Mat("Fabric", new Color(0.25f, 0.18f, 0.2f)));
            Box(env, "OfficeDesk", new Vector3(8f, 0.4f, -1f), new Vector3(2f, 0.8f, 0.9f), wood);
            Box(env, "Bookshelf", new Vector3(11.6f, 0.7f, -4f), new Vector3(0.6f, 1.4f, 1.6f), wood);
            Box(env, "FuseBoxPanel", new Vector3(-1.36f, 1.35f, 9.8f), new Vector3(0.06f, 1.2f, 1.9f), Mat("FusePanel", new Color(0.3f, 0.34f, 0.3f)));
            Box(env, "ControlDesk", new Vector3(0f, 0.5f, 21f), new Vector3(6f, 1f, 1.2f), metal);
            Box(env, "RadioRack_L", new Vector3(-5f, 1.2f, 23.6f), new Vector3(2.5f, 2.4f, 0.6f), metal);
            Box(env, "RadioRack_R", new Vector3(5f, 1.2f, 23.6f), new Vector3(2.5f, 2.4f, 0.6f), metal);
            BasementStairs(env, floor, wall, metal);

            LevelDressing.Level1(env);

            // ------------------------------------------------------------------ lights
            Transform lights = Group(null, "Lighting");
            // The street lamp's own lights are part of the lamp (StreetLamp: they stutter with it).
            PointLight(lights, "EmergencyLight", new Vector3(0f, 2.9f, -2f), new Color(1f, 0.15f, 0.1f), 0.7f, 7f);
            Light foyerLight = PointLight(lights, "Foyer", new Vector3(0f, 2.9f, -2f), new Color(1f, 0.9f, 0.75f), 1.5f, 9f);
            Light receptionLight = PointLight(lights, "Reception", new Vector3(-8f, 2.9f, -2f), new Color(1f, 0.9f, 0.75f), 1.3f, 9f);
            Light officeLight = PointLight(lights, "Office", new Vector3(8f, 2.9f, -2f), new Color(1f, 0.9f, 0.75f), 1.3f, 9f);
            Light corridorLight = PointLight(lights, "Corridor", new Vector3(0f, 2.9f, 8f), new Color(0.9f, 0.95f, 1f), 1.2f, 9f);
            corridorLight.gameObject.AddComponent<FlickerLight>();
            Light controlLight = PointLight(lights, "ControlRoom", new Vector3(0f, 2.9f, 19f), new Color(0.85f, 0.95f, 1f), 1.6f, 12f);
            GameObject fixtures = new GameObject("Fixtures");
            var fixtureRenderers = new System.Collections.Generic.List<Renderer>();
            foreach (Light l in new[] { foyerLight, receptionLight, officeLight, corridorLight, controlLight })
                fixtureRenderers.Add(Box(fixtures.transform, "Fixture", l.transform.position + Vector3.up * 0.25f, new Vector3(0.8f, 0.06f, 0.3f), lampMat, collider: false).GetComponent<Renderer>());
            var power = new GameObject("StationPower").AddComponent<PowerGroup>();
            SetValue(power, "powerFlag", "power:station");
            SetArray(power, "lights", new Object[] { foyerLight, receptionLight, officeLight, corridorLight, controlLight });
            SetArray(power, "emissive", fixtureRenderers.ToArray());

            // ------------------------------------------------------------------ signs
            Transform signs = Group(null, "Signs");
            Sign(signs, "StationName", "NORTHPOINT\nRADIO STATION", new Vector3(0f, 4.3f, -6.12f), 0f, 5f, new Color(0.75f, 0.75f, 0.7f), new Vector2(6f, 1.6f));
            Sign(signs, "ReceptionSign", "RECEPTION", new Vector3(-3.88f, 2.55f, -2f), -90f);
            Sign(signs, "OfficeSign", "OFFICE", new Vector3(3.88f, 2.55f, -2f), 90f);
            Sign(signs, "CorridorSign", "SERVICE CORRIDOR", new Vector3(0f, 2.55f, 1.88f), 0f);
            Sign(signs, "BasementSign", "BASEMENT\nAUTHORISED PERSONNEL ONLY", new Vector3(1.38f, 2.55f, 6f), 90f, 2.4f, new Color(0.9f, 0.5f, 0.4f));
            Sign(signs, "ControlSign", "MAIN CONTROL ROOM", new Vector3(0f, 2.55f, 13.88f), 0f);
            Sign(signs, "FuseSign", "FUSE BOX - MAIN POWER", new Vector3(-1.31f, 2.15f, 9.8f), -90f, 2.4f);

            // ------------------------------------------------------------------ story + interactables
            Transform story = Group(null, "Story");
            var events = story.gameObject.AddComponent<StoryEvents>();
            Transform items = Group(null, "Interactables");

            Door frontDoor = MakeDoor(items, "FrontDoor", new Vector3(0f, 0f, -6f), 0f, "l1_front", "front door", doorMat);
            MakeDoor(items, "ReceptionDoor", new Vector3(-4f, 0f, -2f), 90f, "l1_reception", "door", doorMat);
            MakeDoor(items, "OfficeDoor", new Vector3(4f, 0f, -2f), -90f, "l1_office", "door", doorMat);
            Door controlDoor = MakeDoor(items, "ControlDoor", new Vector3(0f, 0f, 14f), 0f, "l1_control", "control room door", metal,
                requiredFlag: "code:l1_control", flagMessage: "Locked. There's a keypad on the wall beside it.");
            Door basementDoor = MakeDoor(items, "BasementDoor", new Vector3(1.5f, 0f, 6f), -90f, "l1_basement", "basement door", red,
                requiredItemId: "basement_key", requiredItemName: "Basement Key");

            // Flashlight.
            PickupItem flashlight = Pickup(items, "Pickup_Flashlight", new Vector3(2.5f, 0.86f, -4f), "flashlight", "Flashlight", "l1_flashlight",
                accent, PrimitiveType.Cylinder, new Vector3(0.09f, 0.13f, 0.09f));
            flashlight.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            On(flashlight, "onPickedUp", events.SetObjective, "l1_search|Search reception and the office for clues about Maya");
            On(flashlight, "onPickedUp", events.Say, "ALEX|Still works. Barely.|2.5");

            // Tape fragments.
            Vector3 tapeScale = new Vector3(0.2f, 0.04f, 0.13f);
            Pickup(items, "Tape_1", new Vector3(-8.6f, 0.93f, -4f), "tape_fragment", "Tape fragment", "l1_tape1", tapeMat, PrimitiveType.Cube, tapeScale);
            Pickup(items, "Tape_2", new Vector3(11.55f, 1.42f, -3.6f), "tape_fragment", "Tape fragment", "l1_tape2", tapeMat, PrimitiveType.Cube, tapeScale);
            PickupItem tape3 = Pickup(items, "Tape_3", new Vector3(-11.2f, 1.22f, 1.2f), "tape_fragment", "Tape fragment", "l1_tape3", tapeMat, PrimitiveType.Cube, tapeScale);
            var watcher = story.gameObject.AddComponent<InventoryWatcher>();
            SetValue(watcher, "id", "l1_tapes");
            SetValue(watcher, "itemId", "tape_fragment");
            SetValue(watcher, "requiredCount", 3);
            SetValue(watcher, "progressLabel", "Tape fragments");
            On(watcher, "onReached", events.SetObjectiveIfNotFlag, "power:station|l1_power|Restore the power. The fuse box is in the service corridor.");
            On(watcher, "onReached", events.SetObjectiveIfFlag, "power:station|l1_tape|Play the recovered tape in the control room");

            // Main fuse.
            Pickup(items, "Pickup_MainFuse", new Vector3(8.5f, 0.86f, -1f), "main_fuse", "Main fuse", "l1_fuse",
                Mat("Fuse", new Color(0.8f, 0.8f, 0.85f), smoothness: 0.6f), PrimitiveType.Cylinder, new Vector3(0.06f, 0.08f, 0.06f));

            // Clue notes.
            NoteInteractable visitorLog = Note(items, "Note_VisitorLog", new Vector3(-7.4f, 0.91f, -4.1f), 10f, "l1_visitor_log", "Visitor log",
                "NORTHPOINT RADIO - VISITOR LOG\nSTORM NIGHT - 14 OCTOBER\n\n23:10  IN   Maya R. (staff family)\n23:10  IN   Alex R. (staff family)\n02:47  OUT  Alex R.\n" +
                "--:--  OUT  Maya R.  ________\n\nThe last line was never signed.", paper);
            On(visitorLog, "onRead", events.AddClue, "Storm night (14 Oct): Alex signed out at 02:47. Maya never signed out.");
            Note(items, "Note_ManagerMemo", new Vector3(7.4f, 0.81f, -0.9f), -15f, "l1_memo", "Station manager's memo",
                "To all staff:\n\nThe experimental broadcast rig in the tower stays OFF during storms. " +
                "Lightning took out the old mast last year and the rig nearly caught fire.\n\n" +
                "And please stop letting the kids play with the transmitter.\n\n- R.", paper);
            WallNote(items, "Note_WiringMemo", new Vector3(-1.37f, 1.55f, 8.25f), 90f, "l1_wiring", "Sticker on the fuse box",
                "STORM RULE\n\nAnything that feeds the TOWER stays DOWN.\nEverything else UP.\n\n" +
                "(The breaker labels burned off in the '09 fire. The circuit diagram is pinned up in the manager's office.)\n\n" +
                "Insert the main fuse first, then pull the main lever.", paper, new Vector2(0.5f, 0.45f));
            NoteInteractable circuits = WallNote(items, "Note_CircuitDiagram", new Vector3(9.5f, 1.6f, 1.88f), 180f, "l1_circuits", "Circuit diagram (manager's office)",
                "MAIN PANEL - SERVICE CORRIDOR\n\nB1  Corridor and room lights\nB2  Control room door lock\n" +
                "B3  Roof antenna (tower mast)\nB4  Heating\nB5  Broadcast rig (tower)\nB6  Tower lift motor\n\n" +
                "Maya - stop switching the rig back on. I mean it.\n- Dad", paper, new Vector2(0.6f, 0.5f));
            On(circuits, "onRead", events.AddClue, "Circuits: B1 lights, B2 door lock, B3 antenna (tower), B4 heating, B5 rig (tower), B6 lift (tower).");
            Note(items, "Note_Maya", new Vector3(2.6f, 1.01f, 20.8f), -8f, "l1_maya_note", "Note in Maya's handwriting",
                "Alex,\n\nThe transmitter keeps talking back. It knows our names.\n\n" +
                "I put the spare basement key in the control desk. If anything goes wrong tonight, " +
                "DON'T come looking for me downstairs. Just go home.\n\n- M", paper);

            // Breaker puzzle.
            Transform breaker = Group(items, "BreakerPuzzle", new Vector3(-1.32f, 0f, 0f));
            var puzzle = breaker.gameObject.AddComponent<BreakerPuzzle>();
            // Labels are only numbers: the storm rule (sticker) + circuit diagram (office) give the answer.
            string[] labels = { "B1", "B2", "B3", "B4", "B5", "B6" };
            float[] zs = { 8.95f, 9.25f, 9.55f, 9.85f, 10.15f, 10.45f };
            bool[] startUp = { false, true, true, false, true, false };
            var switches = new BreakerSwitch[labels.Length];
            for (int i = 0; i < labels.Length; i++)
            {
                GameObject body = Box(items, "Breaker_" + labels[i], new Vector3(-1.3f, 1.45f, zs[i]), new Vector3(0.08f, 0.22f, 0.13f), metal, isStatic: false);
                GameObject handleGo = Box(body.transform, "Handle", new Vector3(0.6f, 0f, 0f), new Vector3(0.9f, 0.5f, 0.35f), red, collider: false, isStatic: false);
                switches[i] = body.AddComponent<BreakerSwitch>();
                SetValue(switches[i], "label", labels[i]);
                SetValue(switches[i], "isUp", startUp[i]);
                Set(switches[i], "handle", handleGo.transform);
                Sign(signs, "Label_" + labels[i], labels[i], new Vector3(-1.3f, 1.7f, zs[i]), -90f, 1.6f, null, new Vector2(0.3f, 0.15f));
            }
            ItemSlot fuseSlot = Slot(items, "FuseSocket", new Vector3(-1.3f, 0.95f, 9.6f), "l1_fuse_socket", "main_fuse", "Main fuse",
                "main fuse socket", Mat("SlotEmpty", new Color(0.45f, 0.3f, 0.08f)), Mat("Fuse", new Color(0.8f, 0.8f, 0.85f)), new Vector3(0.1f, 0.18f, 0.14f));
            SetArray(puzzle, "switches", switches);
            SetBools(puzzle, "solution", new[] { true, true, false, true, false, false });
            SetValue(puzzle, "wrongMessage", "The breaker trips. Something on the tower is still switched on.");
            Set(puzzle, "fuseSlot", fuseSlot);
            ActionButton lever = Button3D(items, "MainLever", new Vector3(-1.29f, 1.45f, 10.7f), 0f, "l1_main_lever", "Pull main lever", red, new Vector3(0.1f, 0.32f, 0.08f));
            On(lever, "onPressed", puzzle.PullMainLever);
            On(puzzle, "onSolved", power.PowerOn);
            On(puzzle, "onSolved", events.SetObjectiveIfFlag, "watch:l1_tapes|l1_tape|Play the recovered tape in the control room");
            On(puzzle, "onSolved", events.SetObjectiveIfNotFlag, "watch:l1_tapes|l1_search2|Power is back. Find the rest of the tape fragments.");
            On(puzzle, "onSolved", events.Checkpoint, "l1_power");
            On(puzzle, "onSolved", events.Say, "ALEX|Lights. Okay. Okay.|2.5");
            On(puzzle, "onSolved", events.Say, "ALEX|The control room has a keypad. Dad always changed that code.|3.5");

            // Control room keypad: the code is in the visitor log (time Alex signed out), per Dad's note.
            GameObject keypadGo = Box(items, "ControlKeypad", new Vector3(1.0f, 1.3f, 13.86f), new Vector3(0.18f, 0.26f, 0.06f),
                Mat("Keypad", new Color(0.1f, 0.11f, 0.1f), smoothness: 0.5f), isStatic: false);
            Box(keypadGo.transform, "Screen", new Vector3(0f, 0.3f, -0.6f), new Vector3(0.7f, 0.2f, 0.2f),
                Mat("KeypadScreen", new Color(0.15f, 0.35f, 0.18f), emissive: true), collider: false, isStatic: false);
            var keypad = keypadGo.AddComponent<CodeLock>();
            SetValue(keypad, "id", "l1_control");
            SetValue(keypad, "code", "0247");
            SetValue(keypad, "title", "CONTROL ROOM - ENTER CODE");
            SetValue(keypad, "prompt", "Use keypad");
            SetValue(keypad, "requiredFlag", "power:station");
            SetValue(keypad, "missingFlagMessage", "The keypad is dark. No power.");
            SetValue(keypad, "hint", "Dad's note says the code is a time from the storm night.");
            On(keypad, "onUnlocked", controlDoor.Open);
            WallNote(items, "Note_KeypadSticky", new Vector3(-1.0f, 1.4f, 13.88f), 0f, "l1_sticky", "Sticky note by the keypad",
                "Changed the code again.\nSame rule as always: the time the last person signed OUT on the storm night.\n\n" +
                "I never want to forget it.\n- Dad", paper, new Vector2(0.18f, 0.18f));

            // Control room: tape player and key.
            TapePlayer tape = Tape(items, "TapePlayer", new Vector3(-1.5f, 1.09f, 21f), 180f, "l1_tape", "tape player", tapeMat, new[]
            {
                L("RECORDING", "[Static. Rain on a microphone.]", 2.5f),
                L("MAYA (TAPE)", "Test, test... It's the night of the storm. Alex is here with me at the station.", 5f),
                L("MAYA (TAPE)", "Dad's old transmitter is acting strange. The needles keep jumping, even with the power low.", 5f),
                L("MAYA (TAPE)", "There's a voice under the static. It sounds like... me. That isn't possible.", 5f),
                L("MAYA (TAPE)", "The spare key to the basement is in the control desk. Alex, whatever happens, don't-", 5f),
                L("RECORDING", "[The tape cuts to static.]", 2.5f),
            }, requiredItemId: "tape_fragment", requiredCount: 3, missingItemMessage: "The tape is incomplete. Find all three fragments.",
                requiredFlag: "power:station", missingFlagMessage: "It has no power.");
            On(tape, "onFinished", events.SetObjective, "l1_key|Find the basement key in the control room");
            On(tape, "onFinished", events.Say, "ALEX|Don't what, Maya?|2.5");
            PickupItem key = Pickup(items, "Pickup_BasementKey", new Vector3(1.5f, 1.03f, 21f), "basement_key", "Basement Key", "l1_key",
                keyMat, PrimitiveType.Cube, new Vector3(0.16f, 0.03f, 0.06f));
            On(key, "onPickedUp", events.SetObjective, "l1_basement|Unlock the basement door in the service corridor");
            key.gameObject.SetActive(false);
            var keyActivator = story.gameObject.AddComponent<FlagActivator>();
            SetValue(keyActivator, "flag", "played:l1_tape");
            SetArray(keyActivator, "show", new Object[] { key.gameObject });

            // ------------------------------------------------------------------ scripted beats
            TriggerZone entrance = Trigger<TriggerZone>(story, "Zone_Entrance", new Vector3(0f, 1f, -4.5f), new Vector3(5f, 2f, 2f));
            SetValue(entrance, "id", "l1_entrance");
            On(entrance, "onEnter", events.SetObjectiveIfNotFlag, "taken:l1_flashlight|l1_light|Find a working light");
            On(entrance, "onEnter", events.Say, "ALEX|Power's out. I need some light.|3");
            On(entrance, "onEnter", frontDoor.SwingShut); // the door slowly swings shut behind him

            // Arriving: Alex's eyes are drawn up to the lit window, where someone stands watching him, then walks
            // away past the second window. No control until his view comes back (ArrivalCutscene).
            WindowWatcher upstairs = WindowFigure(story);
            Transform firstWindow = Marker(story, "FirstWindowView", new Vector3(UpperWindowX[0], 4.65f, -6.05f), 0f);
            var arrival = story.gameObject.AddComponent<ArrivalCutscene>();
            Set(arrival, "watcher", upstairs);
            Set(arrival, "firstWindow", firstWindow);
            // Walking back to the open gate: he won't leave.
            TriggerZone leave = Trigger<TriggerZone>(story, "Zone_GateBack", new Vector3(YardGateX, 1f, -17.2f), new Vector3(GateKit.HalfOpening * 2f, 2f, 1.2f));
            SetValue(leave, "id", "l1_gate_back");
            SetValue(leave, "once", false);
            On(leave, "onEnter", events.Say, "ALEX|Not yet. I have to know who that was.|3");

            TriggerZone receptionRadio = Trigger<TriggerZone>(story, "Zone_ReceptionRadio", new Vector3(-8f, 1f, -1f), new Vector3(7f, 2f, 5f));
            SetValue(receptionRadio, "id", "l1_reception_radio");
            On(receptionRadio, "onEnter", events.PlayStatic);
            On(receptionRadio, "onEnter", events.Say, "RADIO|...kssh... Alex... please come back... I'm still here...|4");
            Box(items, "Radio", new Vector3(-11.3f, 1.4f, 1.1f), new Vector3(0.5f, 0.3f, 0.3f), Mat("RadioBody", new Color(0.35f, 0.3f, 0.25f)), isStatic: false);

            TriggerZone officeSteps = Trigger<TriggerZone>(story, "Zone_OfficeFootsteps", new Vector3(8f, 1f, -2f), new Vector3(7f, 2f, 7f));
            SetValue(officeSteps, "id", "l1_footsteps");
            On(officeSteps, "onEnter", events.PlayThud);
            On(officeSteps, "onEnter", events.Say, "|[Footsteps somewhere above.]|2.5");
            ScareEvent stepsAbove = ScareKit.Scare(story, "l1_steps_above", new Vector3(8f, 3.4f, -2f), 0.6f);
            ScareKit.Steps(stepsAbove, ScareKit.Path(story, "StepsAbove_Office", new Vector3(5f, 3.4f, -5f), new Vector3(6f, 3.4f, -4f),
                new Vector3(7f, 3.4f, -3f), new Vector3(8f, 3.4f, -2f), new Vector3(9f, 3.4f, -1f), new Vector3(10f, 3.4f, 0f), new Vector3(11f, 3.4f, 1f)), 0.5f);
            On(officeSteps, "onEnter", stepsAbove.Play);

            // Taking the last tape fragment: a binder slides off the cabinet behind you.
            Rigidbody binder = ScareKit.DropProp(story, "Binder", new Vector3(-10.95f, 1.33f, 1.35f), new Vector3(0.08f, 0.24f, 0.3f), Mat("Binder", new Color(0.2f, 0.25f, 0.35f)));
            ScareEvent binderFall = ScareKit.Scare(story, "l1_binder", new Vector3(-10.95f, 1.3f, 1.35f), 1.2f);
            ScareKit.Drops(binderFall, new Vector3(1.6f, 0f, 0f), binder);
            ScareKit.Voice(binderFall, "|[Something falls behind you.]|2");
            On(tape3, "onPickedUp", binderFall.Play);

            // A few seconds after the power comes back, the corridor bulb bursts.
            ScareEvent bulb = ScareKit.Scare(story, "l1_bulb", new Vector3(0f, 2.9f, 8f), 7f);
            ScareKit.Lights(bulb, corridorLight);
            ScareKit.Voice(bulb, "RADIO|...kssh... Alex... the dark is safer...|3", staticBurst: true);
            On(puzzle, "onSolved", bulb.Play);

            GameObject figure = (GameObject)PrefabUtility.InstantiatePrefab(ScareKit.EchoShadow());
            figure.transform.SetParent(story, false);
            figure.transform.position = new Vector3(0f, 0f, 13f);
            figure.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            var shadow = story.gameObject.AddComponent<ShadowSighting>();
            SetValue(shadow, "id", "l1_shadow");
            Set(shadow, "figure", figure);
            SetValue(shadow, "vanishDistance", 7f);
            TriggerZone corridorZone = Trigger<TriggerZone>(story, "Zone_Corridor", new Vector3(0f, 1f, 3.5f), new Vector3(3f, 2f, 2f));
            SetValue(corridorZone, "id", "l1_corridor_shadow");
            On(corridorZone, "onEnter", shadow.Show);

            TriggerZone basementRadio = Trigger<TriggerZone>(story, "Zone_BasementRadio", new Vector3(0f, 1f, 6f), new Vector3(3f, 2f, 4f));
            SetValue(basementRadio, "id", "l1_dont_go");
            SetValue(basementRadio, "requiredFlag", "taken:l1_key");
            On(basementRadio, "onEnter", events.PlayStatic);
            On(basementRadio, "onEnter", events.Say, "RADIO|Alex... don't go downstairs.|3.5");

            On(basementDoor, "onOpened", events.SetObjective, "l1_down|Go down to the basement");
            // At the foot of the stairs, where they disappear into the dark under the building.
            LevelExit exit = Trigger<LevelExit>(story, "Exit_Basement", new Vector3(5.7f, StairBottom + 0.8f, 6f), new Vector3(0.9f, 1.6f, 1.8f));
            SetValue(exit, "nextScene", GameFlowManager.Level2Scene);

            // ------------------------------------------------------------------ checkpoints + level
            CheckpointZone(story, "l1_power", new Vector3(0f, 0.02f, 9f), 0f, Vector3.one, triggerEnabled: false);
            CheckpointZone(story, "l1_control", new Vector3(0f, 1f, 15.5f), 0f, new Vector3(4f, 2f, 1.5f));

            // Beside the driver's door (right-hand side) of the car he came in, looking up at the station.
            Transform spawn = Marker(story, "PlayerStart", new Vector3(-1.95f, 0.02f, -14.0f), -8f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 1);
            SetValue(level, "levelName", "The Abandoned Station");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l1_enter");
            SetValue(level, "startObjectiveText", "Find the source of the transmission. Get inside the station.");
            SetLines(level, "introLines", new SubtitleLine[0]); // ArrivalCutscene speaks the arrival
            SetValue(level, "flashlightDrain", false);
            SetValue(level, "ambience", Ambience.RainAndWind);
            SetValue(level, "ambienceVolume", 0.95f);
            // The storm is loud in the yard and the doorway and fades the further in Alex goes.
            Transform entranceMark = Marker(story, "EntranceSound", new Vector3(0f, 1f, -6f), 0f);
            var rainMix = story.gameObject.AddComponent<RainAmbience>();
            Set(rainMix, "entrance", entranceMark);
            Set(rainMix, "frontDoor", frontDoor);

            Transform weather = Group(null, "Weather");
            Rain(weather, new Vector3(0f, 10f, -12f), new Vector3(24f, 12f, 1f));

            LevelDressing.Finish(1, items);
            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level1Builder] Built " + ScenePath);
        }
        /// <summary>The middle of the yard's gateway (in line with where Alex parked).</summary>
        private const float YardGateX = -3.6f;

        /// <summary>
        /// Beyond the gate: the wet forest road Alex drove in on, the verge and the firs, fading into the fog. You can
        /// see out but not leave: an unseen barrier fills the gateway, and Alex won't go back.
        /// </summary>
        private static void OutsideTheGate(Transform env)
        {
            Transform outside = Group(env, "OutsideTheGate");
            Material asphalt = EndingBuilder.Textured("IntroRoad", "asphalt_02", new Color(0.17f, 0.18f, 0.2f), 0.72f);
            Material verge = EndingBuilder.Textured("Verge", "aerial_grass_rock", new Color(0.25f, 0.27f, 0.22f), 0.15f);
            Slab(outside, "Road", YardGateX - 2.6f, -60f, YardGateX + 2.6f, -18f, 0.005f, asphalt);
            Slab(outside, "Verge", -40f, -60f, 36f, -18f, -0.01f, verge);
            Material bark = Mat("Bark", new Color(0.16f, 0.13f, 0.1f), smoothness: 0.05f);
            Material needles = Mat("Needles", new Color(0.07f, 0.12f, 0.08f), smoothness: 0.05f);
            Mesh cone = EndingBuilder.Cone();
            var rng = new System.Random(47);
            float R() => (float)rng.NextDouble();
            for (int k = 0; k < 46; k++)
            {
                float side = k % 2 == 0 ? -1f : 1f;
                float x = YardGateX + side * (4.2f + R() * 16f), z = -21f - R() * 36f;
                float s = 0.8f + R() * 0.7f;
                Transform tree = Group(outside, "Fir", new Vector3(x, -0.1f, z));
                tree.localRotation = Quaternion.Euler(0f, R() * 360f, 0f);
                Prim(tree, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 2f * s, 0f), new Vector3(0.28f * s, 2f * s, 0.28f * s), bark, collider: false);
                for (int t = 0; t < 5; t++)
                {
                    var tier = new GameObject("Tier");
                    tier.transform.SetParent(tree, false);
                    float w = (3.0f - t * 0.5f) * s;
                    tier.transform.localPosition = new Vector3(0f, (1.8f + t * 1.7f) * s, 0f);
                    tier.transform.localScale = new Vector3(w, 2.8f * s, w);
                    tier.AddComponent<MeshFilter>().sharedMesh = cone;
                    tier.AddComponent<MeshRenderer>().sharedMaterial = needles;
                }
                foreach (Transform c in tree.GetComponentsInChildren<Transform>()) c.gameObject.isStatic = true;
            }
            // Can't walk back out through the open gate.
            GameObject block = Box(outside, "GateBarrier", new Vector3(YardGateX, 1.5f, -18.3f), new Vector3(GateKit.HalfOpening * 2f, 3f, 0.3f), asphalt);
            block.GetComponent<Renderer>().enabled = false;
        }

        // ------------------------------------------------------------------ arrival: the car, the upstairs window

        /// <summary>Centres (x) of the two upstairs windows, left of the station's name board.</summary>
        private static readonly float[] UpperWindowX = { -5f, -3.3f };
        private static readonly float[] UpperWindowWidth = { 1.4f, 1.1f };

        /// <summary>
        /// The facade above the doorway (3.4..6 m) with two upstairs windows and the room behind them: a dim cold
        /// light at the back so whoever stands there shows as a black shape against it.
        /// </summary>
        private static void UpperFacade(Transform env, Material facade)
        {
            const float z = -6f, y0 = 3.4f, y1 = 6f, wy0 = 4.0f, wy1 = 5.3f, t = WallThickness;
            float a0 = UpperWindowX[0] - UpperWindowWidth[0] / 2f, a1 = UpperWindowX[0] + UpperWindowWidth[0] / 2f;
            float b0 = UpperWindowX[1] - UpperWindowWidth[1] / 2f, b1 = UpperWindowX[1] + UpperWindowWidth[1] / 2f;
            // Solid wall either side of and between the windows, and above and below them.
            Box(env, "Facade_Upper", new Vector3((-12f + a0) / 2f, (y0 + y1) / 2f, z), new Vector3(a0 + 12f, y1 - y0, t), facade);
            Box(env, "Facade_Upper", new Vector3((a1 + b0) / 2f, (y0 + y1) / 2f, z), new Vector3(b0 - a1, y1 - y0, t), facade);
            Box(env, "Facade_Upper", new Vector3((b1 + 12f) / 2f, (y0 + y1) / 2f, z), new Vector3(12f - b1, y1 - y0, t), facade);
            Material frame = Mat("UpperWindowFrame", new Color(0.25f, 0.25f, 0.24f), smoothness: 0.2f);
            Material pane = Mat("UpperWindowGlass", new Color(0.3f, 0.34f, 0.4f, 0.12f), smoothness: 0.95f);
            EndingBuilder.MakeTransparent(pane);
            Material netCurtain = UnlitMat("NetCurtain", new Color(0.6f, 0.64f, 0.72f, 0.2f), transparent: true);
            for (int i = 0; i < 2; i++)
            {
                float x0 = UpperWindowX[i] - UpperWindowWidth[i] / 2f, x1 = UpperWindowX[i] + UpperWindowWidth[i] / 2f, cx = UpperWindowX[i];
                Box(env, "Facade_UnderWindow", new Vector3(cx, (y0 + wy0) / 2f, z), new Vector3(x1 - x0, wy0 - y0, t), facade);
                Box(env, "Facade_OverWindow", new Vector3(cx, (wy1 + y1) / 2f, z), new Vector3(x1 - x0, y1 - wy1, t), facade);
                Box(env, "WindowFrame", new Vector3(x0 + 0.04f, (wy0 + wy1) / 2f, z - 0.08f), new Vector3(0.08f, wy1 - wy0, 0.06f), frame, collider: false);
                Box(env, "WindowFrame", new Vector3(x1 - 0.04f, (wy0 + wy1) / 2f, z - 0.08f), new Vector3(0.08f, wy1 - wy0, 0.06f), frame, collider: false);
                Box(env, "WindowFrame", new Vector3(cx, wy0 + 0.04f, z - 0.08f), new Vector3(x1 - x0, 0.08f, 0.06f), frame, collider: false);
                Box(env, "WindowFrame", new Vector3(cx, wy1 - 0.04f, z - 0.08f), new Vector3(x1 - x0, 0.08f, 0.06f), frame, collider: false);
                Box(env, "WindowFrame", new Vector3(cx, (wy0 + wy1) / 2f, z - 0.08f), new Vector3(x1 - x0, 0.04f, 0.04f), frame, collider: false);
                Box(env, "WindowGlass", new Vector3(cx, (wy0 + wy1) / 2f, z - 0.06f), new Vector3(x1 - x0 - 0.08f, wy1 - wy0 - 0.08f, 0.01f), pane, collider: false);
                // A thin net curtain just inside: whoever stands behind it is a shape, never a face.
                int folds = Mathf.CeilToInt((x1 - x0) / 0.2f);
                float fw = (x1 - x0 - 0.06f) / folds;
                for (int k = 0; k < folds; k++)
                {
                    GameObject fold = Box(env, "NetCurtain", new Vector3(x0 + 0.03f + fw * (k + 0.5f), (wy0 + wy1) / 2f + 0.02f, z + 0.22f + (k % 2) * 0.03f),
                        new Vector3(fw * 1.12f, wy1 - wy0 + 0.04f, 0.004f), netCurtain, collider: false);
                    fold.transform.localRotation = Quaternion.Euler(0f, k % 2 == 0 ? 12f : -12f, 0f);
                    fold.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            // The room behind: a long, bare upstairs room running behind both windows and on behind the name board.
            Material wall = Mat("UpperRoom", new Color(0.32f, 0.33f, 0.35f));
            Material dark = Mat("UpperRoomDark", new Color(0.1f, 0.1f, 0.11f));
            float rx0 = -6.2f, rx1 = -0.6f, rz1 = -4.1f;
            Box(env, "UpperRoom_Floor", new Vector3((rx0 + rx1) / 2f, y0 + 0.01f, (z + rz1) / 2f), new Vector3(rx1 - rx0, 0.02f, rz1 - z), dark, collider: false);
            Box(env, "UpperRoom_Back", new Vector3((rx0 + rx1) / 2f, (y0 + y1) / 2f, rz1 + 0.05f), new Vector3(rx1 - rx0, y1 - y0, 0.1f), wall, collider: false);
            Box(env, "UpperRoom_End", new Vector3(rx0 - 0.05f, (y0 + y1) / 2f, (z + rz1) / 2f), new Vector3(0.1f, y1 - y0, rz1 - z), dark, collider: false);
            Box(env, "UpperRoom_End", new Vector3(rx1 + 0.05f, (y0 + y1) / 2f, (z + rz1) / 2f), new Vector3(0.1f, y1 - y0, rz1 - z), dark, collider: false);
            Box(env, "UpperRoom_Top", new Vector3((rx0 + rx1) / 2f, y1 - 0.05f, (z + rz1) / 2f), new Vector3(rx1 - rx0, 0.1f, rz1 - z), dark, collider: false);
            // A cold light washing the back wall behind each window: whoever stands there is a black shape against it.
            for (int i = 0; i < 2; i++)
                PointLight(env, "UpperRoomLight", new Vector3(UpperWindowX[i], 4.9f, -4.4f), new Color(0.62f, 0.7f, 0.92f), 0.75f, 2.6f);
        }

        /// <summary>
        /// Whoever is upstairs: a slight, dark, faceless figure (the size of a young woman, or not) that stands at the
        /// first window, then walks the length of the room and out of sight past the second.
        /// </summary>
        private static WindowWatcher WindowFigure(Transform story)
        {
            Transform holder = Group(story, "WindowWatcher");
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabs.EchoModelPath);
            var figure = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(figure, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            figure.name = "Figure";
            figure.transform.SetParent(holder, false);
            figure.transform.localScale = Vector3.one * 0.9f;
            Material black = Mat("WatcherSilhouette", new Color(0.012f, 0.012f, 0.015f), smoothness: 0.1f);
            foreach (SkinnedMeshRenderer smr in figure.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                var mats = new Material[smr.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = black;
                smr.sharedMaterials = mats;
                smr.updateWhenOffscreen = true;
            }
            DressAsGirl(figure.transform, black, out Transform[] upperArms, out Transform[] forearms);
            Animator anim = figure.GetComponent<Animator>();
            if (anim == null) anim = figure.AddComponent<Animator>();
            anim.runtimeAnimatorController = WatcherController();
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // Close to the glass (and a step up) so the sill hides little of it from the yard below.
            Transform watch = Marker(holder, "WatchPoint", new Vector3(UpperWindowX[0], 3.62f, -5.55f), 0f);
            Transform exit = Marker(holder, "ExitPoint", new Vector3(-1.1f, 3.62f, -5.55f), 0f);
            var watcher = holder.gameObject.AddComponent<WindowWatcher>();
            Set(watcher, "figure", figure.transform);
            Set(watcher, "animator", anim);
            Set(watcher, "watchPoint", watch);
            Set(watcher, "exitPoint", exit);
            SetArray(watcher, "upperArms", upperArms);
            SetArray(watcher, "forearms", forearms);
            return watcher;
        }

        /// <summary>
        /// Turns the mannequin into the outline of a young woman (so nobody can say it is not Maya): long hair over the
        /// shoulders and down the back, a long loose dress to the ankles hiding the legs, and wide sleeves past the hands.
        /// All pitch black and fixed to the bones, built in the model's rest pose (it stands at the origin here).
        /// </summary>
        private static void DressAsGirl(Transform figure, Material black, out Transform[] upperArms, out Transform[] forearms)
        {
            Transform Bone(string n)
            {
                foreach (Transform t in figure.GetComponentsInChildren<Transform>(true)) if (t.name == n) return t;
                return null;
            }
            Transform head = Bone("DEF-head"), hips = Bone("DEF-hips"), chest = Bone("DEF-spine.002");
            upperArms = new[] { Bone("DEF-upper_arm.L"), Bone("DEF-upper_arm.R") };
            forearms = new[] { Bone("DEF-forearm.L"), Bone("DEF-forearm.R") };
            Transform[] hands = { Bone("DEF-hand.L"), Bone("DEF-hand.R") };
            Vector3 back = figure.forward;   // the model faces its -Z
            void Piece(PrimitiveType type, Transform bone, Vector3 worldPos, Vector3 scale, Quaternion worldRot, string name)
            {
                if (bone == null) return;
                GameObject g = GameObject.CreatePrimitive(type);
                g.name = name;
                Object.DestroyImmediate(g.GetComponent<Collider>());
                g.GetComponent<Renderer>().sharedMaterial = black;
                g.transform.SetPositionAndRotation(worldPos, worldRot);
                g.transform.localScale = scale;
                g.transform.SetParent(bone, true);
            }
            if (head != null)
            {
                Vector3 h = head.position;
                // Hair: a full crown, a long fall down the back, and two locks over the shoulders.
                Piece(PrimitiveType.Sphere, head, h + Vector3.up * 0.11f + back * 0.015f, new Vector3(0.21f, 0.23f, 0.23f), Quaternion.identity, "HairCrown");
                Piece(PrimitiveType.Capsule, head, h + Vector3.down * 0.12f + back * 0.07f, new Vector3(0.24f, 0.27f, 0.09f), Quaternion.identity, "HairBack");
                foreach (float x in new[] { -0.085f, 0.085f })
                    Piece(PrimitiveType.Capsule, head, h + new Vector3(x, -0.09f, 0f) - back * 0.02f, new Vector3(0.075f, 0.18f, 0.07f), Quaternion.Euler(0f, 0f, x > 0f ? 4f : -4f), "HairLock");
            }
            if (chest != null)
                Piece(PrimitiveType.Capsule, chest, chest.position + Vector3.down * 0.02f, new Vector3(0.29f, 0.2f, 0.19f), Quaternion.identity, "Bodice");
            if (hips != null)
            {
                // A long, slightly flared dress from the waist to the ankles.
                var skirt = new GameObject("Skirt");
                skirt.AddComponent<MeshFilter>().sharedMesh = Frustum(0.15f, 0.34f, 0.86f, 18);
                skirt.AddComponent<MeshRenderer>().sharedMaterial = black;
                skirt.transform.position = new Vector3(hips.position.x, hips.position.y + 0.12f, hips.position.z);
                skirt.transform.SetParent(hips, true);
            }
            // Wide sleeves from the shoulder to past the hand (it never shows a hand).
            for (int i = 0; i < 2; i++)
            {
                if (upperArms[i] == null || forearms[i] == null || hands[i] == null) continue;
                Vector3 a = upperArms[i].position, b = forearms[i].position, c = hands[i].position + (hands[i].position - b).normalized * 0.09f;
                Piece(PrimitiveType.Capsule, upperArms[i], (a + b) / 2f, new Vector3(0.11f, Vector3.Distance(a, b) / 2f + 0.03f, 0.11f), Quaternion.FromToRotation(Vector3.up, b - a), "Sleeve");
                Piece(PrimitiveType.Capsule, forearms[i], (b + c) / 2f, new Vector3(0.13f, Vector3.Distance(b, c) / 2f + 0.03f, 0.13f), Quaternion.FromToRotation(Vector3.up, c - b), "Cuff");
            }
            foreach (Renderer r in figure.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>An open cone from radius top (at y 0) down to radius bottom (at y -height), with a base.</summary>
        private static Mesh Frustum(float top, float bottom, float height, int seg)
        {
            var v = new System.Collections.Generic.List<Vector3>();
            var tri = new System.Collections.Generic.List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(d * top);
                v.Add(d * bottom + Vector3.down * height);
                if (i < seg)
                {
                    int k = i * 2;
                    tri.AddRange(new[] { k, k + 2, k + 1, k + 1, k + 2, k + 3 });
                }
            }
            int centre = v.Count;
            v.Add(Vector3.down * height);
            for (int i = 0; i < seg; i++) tri.AddRange(new[] { centre, i * 2 + 1, i * 2 + 3 });
            var m = new Mesh { name = "Skirt" };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Standing still (idle) or walking, by a Walking bool; the mannequin's own clips.</summary>
        private static UnityEditor.Animations.AnimatorController WatcherController()
        {
            const string path = "Assets/Art/Models/Echo/WindowWatcher.controller";
            var existing = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(path);
            if (existing != null) return existing;
            AnimationClip idle = null, walk = null;
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(GamePrefabs.EchoModelPath))
            {
                if (!(o is AnimationClip c)) continue;
                if (c.name == "Idle_Loop" || c.name == "Rig|Idle_Loop") idle = c;
                if (c.name == "Walk_Loop" || c.name == "Rig|Walk_Loop") walk = c;
                if (walk == null && (c.name == "Walk_Formal_Loop" || c.name == "Rig|Walk_Formal_Loop")) walk = c;
            }
            var ctrl = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            ctrl.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            var sm = ctrl.layers[0].stateMachine;
            var still = sm.AddState("Idle");
            still.motion = idle;
            var go = sm.AddState("Walk");
            go.motion = walk;
            sm.defaultState = still;
            var t1 = still.AddTransition(go);
            t1.AddCondition(UnityEditor.Animations.AnimatorConditionMode.If, 0f, "Walking");
            t1.hasExitTime = false;
            t1.duration = 0.3f;
            var t2 = go.AddTransition(still);
            t2.AddCondition(UnityEditor.Animations.AnimatorConditionMode.IfNot, 0f, "Walking");
            t2.hasExitTime = false;
            t2.duration = 0.3f;
            AssetDatabase.SaveAssets();
            return ctrl;
        }

        // ------------------------------------------------------------------ street lamp

        /// <summary>
        /// An old street light: a tapered steel pole on a base, an arm reaching out over the path, and a lamp
        /// head with a glowing glass bowl underneath (the light itself sits just below the bowl).
        /// </summary>
        private static void StreetLamp(Transform env, Vector3 foot, Material metal)
        {
            Transform lamp = Group(env, "StreetLamp", foot);
            Material paint = Mat("LampPostPaint", new Color(0.12f, 0.14f, 0.13f), smoothness: 0.35f);
            Material glass = Mat("StreetLampGlass", new Color(0.75f, 0.82f, 1f), emissive: true, smoothness: 0.8f);
            Box(lamp, "Base", new Vector3(0f, 0.25f, 0f), new Vector3(0.32f, 0.5f, 0.32f), paint);
            Prim(lamp, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 2.2f, 0f), new Vector3(0.15f, 1.95f, 0.15f), paint);
            Prim(lamp, "PoleTop", PrimitiveType.Cylinder, new Vector3(0f, 4.2f, 0f), new Vector3(0.11f, 0.08f, 0.11f), paint, collider: false);
            // Arm out over the path (towards -X), rising slightly.
            GameObject arm = Prim(lamp, "Arm", PrimitiveType.Cylinder, new Vector3(-0.5f, 4.15f, 0f), new Vector3(0.07f, 0.52f, 0.07f), paint,
                new Vector3(0f, 0f, 84f), collider: false);
            Box(lamp, "Brace", new Vector3(-0.22f, 3.95f, 0f), new Vector3(0.04f, 0.45f, 0.04f), paint, collider: false)
                .transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            // Lamp head: a dark hood with the glowing bowl under it.
            Box(lamp, "Head", new Vector3(-1.0f, 4.12f, 0f), new Vector3(0.62f, 0.12f, 0.32f), paint, collider: false);
            Box(lamp, "HeadRim", new Vector3(-1.0f, 4.05f, 0f), new Vector3(0.58f, 0.04f, 0.3f), metal, collider: false);
            Prim(lamp, "Bowl", PrimitiveType.Sphere, new Vector3(-1.0f, 4.02f, 0f), new Vector3(0.48f, 0.11f, 0.24f), glass, collider: false);
            foreach (Renderer r in lamp.GetComponentsInChildren<Renderer>())
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            Renderer bowl = lamp.Find("Bowl").GetComponent<Renderer>();
            bowl.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            // Its light: a wide cone down onto the path (with shadows) and a soft glow round the head.
            var downGo = new GameObject("LampDown");
            downGo.transform.SetParent(lamp, false);
            downGo.transform.localPosition = new Vector3(-1.0f, 3.9f, 0f);
            downGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Light down = downGo.AddComponent<Light>();
            down.type = LightType.Spot;
            down.spotAngle = 125f;
            down.innerSpotAngle = 55f;
            down.range = 14f;
            down.intensity = 26f;
            down.color = new Color(0.82f, 0.87f, 1f);
            down.shadows = LightShadows.Soft;
            SetLightLayers(down, LitLayers);
            Light glow = PointLight(lamp, "LampGlow", new Vector3(-1.0f, 3.75f, 0f), new Color(0.7f, 0.78f, 1f), 1.6f, 9f);
            // Failing: it stutters every few seconds, buzzing.
            var buzzGo = new GameObject("Buzz");
            buzzGo.transform.SetParent(lamp, false);
            buzzGo.transform.localPosition = new Vector3(-1.0f, 4.0f, 0f);
            var buzz = buzzGo.AddComponent<AudioSource>();
            buzz.playOnAwake = false;
            buzz.spatialBlend = 1f;
            buzz.rolloffMode = AudioRolloffMode.Linear;
            buzz.minDistance = 1f;
            buzz.maxDistance = 10f;
            var failing = lamp.gameObject.AddComponent<FailingLamp>();
            SetArray(failing, "lamps", new Object[] { down, glow });
            Set(failing, "bowl", bowl);
            Set(failing, "buzz", buzz);
        }

        // ------------------------------------------------------------------ stairs to the basement

        private const float StairX0 = 2.6f, StairX1 = 5.9f, StairZ0 = 5.1f, StairZ1 = 6.9f;
        private const int StairSteps = 9;
        private const float StairRise = 0.17f, StairRun = 0.3f;
        public const float StairBottom = -StairSteps * StairRise;

        /// <summary>
        /// A real flight of concrete steps down from the stairwell: nine 17 cm steps (about 1.5 m) into the
        /// dark under the building, with pipe handrails round the opening. Level 2 starts at the bottom.
        /// </summary>
        private static void BasementStairs(Transform env, Material floor, Material wall, Material metal)
        {
            Transform stairs = Group(env, "BasementStairs");
            float depth = -StairBottom + 0.2f, under = StairBottom - 0.2f;
            for (int i = 0; i < StairSteps; i++)
            {
                float top = -StairRise * (i + 1);
                float x0 = StairX0 + StairRun * i;
                Box(stairs, "Step", new Vector3(x0 + StairRun / 2f, (top + under) / 2f, (StairZ0 + StairZ1) / 2f),
                    new Vector3(StairRun, top - under, StairZ1 - StairZ0), floor);
                // Worn dark nosing on each step edge.
                Box(stairs, "Nosing", new Vector3(x0 + 0.02f, top + 0.004f, (StairZ0 + StairZ1) / 2f),
                    new Vector3(0.04f, 0.01f, StairZ1 - StairZ0), Mat("Black", new Color(0.03f, 0.03f, 0.03f)), collider: false);
            }
            // The landing at the bottom runs on under the floor into darkness.
            float landX0 = StairX0 + StairRun * StairSteps, endX = 7.6f;
            Box(stairs, "Landing", new Vector3((landX0 + endX) / 2f, StairBottom - 0.1f, (StairZ0 + StairZ1) / 2f),
                new Vector3(endX - landX0, 0.2f, StairZ1 - StairZ0), floor);
            // Shaft walls down the sides and the end (blackened where the light never reaches).
            Material black = UnlitMat("Void", new Color(0.004f, 0.004f, 0.006f));
            float wallY = (StairBottom + 0f) / 2f - 0.1f, wallH = depth;
            Box(stairs, "ShaftWall_S", new Vector3((StairX0 + endX) / 2f, wallY, StairZ0 - 0.1f), new Vector3(endX - StairX0, wallH, 0.2f), wall);
            Box(stairs, "ShaftWall_N", new Vector3((StairX0 + endX) / 2f, wallY, StairZ1 + 0.1f), new Vector3(endX - StairX0, wallH, 0.2f), wall);
            Box(stairs, "ShaftWall_W", new Vector3(StairX0 - 0.1f, wallY, (StairZ0 + StairZ1) / 2f), new Vector3(0.2f, wallH, StairZ1 - StairZ0), wall);
            Box(stairs, "ShaftEnd", new Vector3(endX + 0.1f, wallY, (StairZ0 + StairZ1) / 2f), new Vector3(0.2f, wallH, StairZ1 - StairZ0 + 0.4f), black);
            // Low ceiling over the part that runs under the floor.
            Box(stairs, "ShaftCeiling", new Vector3((StairX1 + endX) / 2f, -0.25f, (StairZ0 + StairZ1) / 2f),
                new Vector3(endX - StairX1, 0.1f, StairZ1 - StairZ0), black, collider: false);

            // Pipe handrails round the opening (open on the west side, where the steps start).
            Transform rails = Group(stairs, "Handrails");
            void Rail(Vector3 a, Vector3 b)
            {
                Vector3 mid = (a + b) / 2f, d = b - a;
                GameObject bar = Box(rails, "Rail", mid + Vector3.up * 0.95f, new Vector3(Mathf.Abs(d.x) + 0.05f, 0.05f, Mathf.Abs(d.z) + 0.05f), metal, collider: false);
                bar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                int posts = Mathf.Max(2, Mathf.CeilToInt(d.magnitude / 1.1f) + 1);
                for (int k = 0; k < posts; k++)
                    Box(rails, "Post", Vector3.Lerp(a, b, k / (float)(posts - 1)) + Vector3.up * 0.475f, new Vector3(0.045f, 0.95f, 0.045f), metal, collider: false);
                // Invisible barrier so nobody steps over the edge.
                GameObject block = Box(rails, "RailBlocker", mid + Vector3.up * 0.6f, new Vector3(Mathf.Abs(d.x) + 0.06f, 1.2f, Mathf.Abs(d.z) + 0.06f), metal);
                block.GetComponent<Renderer>().enabled = false;
            }
            Rail(new Vector3(StairX0 + 0.3f, 0f, StairZ0 - 0.03f), new Vector3(StairX1 + 0.03f, 0f, StairZ0 - 0.03f));
            Rail(new Vector3(StairX0 + 0.3f, 0f, StairZ1 + 0.03f), new Vector3(StairX1 + 0.03f, 0f, StairZ1 + 0.03f));
            Rail(new Vector3(StairX1 + 0.03f, 0f, StairZ0 - 0.03f), new Vector3(StairX1 + 0.03f, 0f, StairZ1 + 0.03f));
        }
    }
}
