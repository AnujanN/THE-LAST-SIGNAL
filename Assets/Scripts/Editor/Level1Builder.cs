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
            WallX(env, "Fence_S", -18f, -12f, 12f, 2.5f, facade);
            Box(env, "Path", new Vector3(0f, 0.01f, -12f), new Vector3(2.2f, 0.02f, 12f), floor);
            Box(env, "LampPost", new Vector3(2.2f, 1.75f, -10f), new Vector3(0.12f, 3.5f, 0.12f), metal);
            Box(env, "Car", new Vector3(-6f, 0.7f, -13f), new Vector3(1.9f, 1.4f, 4.2f), Mat("Car", new Color(0.18f, 0.2f, 0.24f)));

            // Interior shell.
            // Floor with the stairwell opening (x 2.6..5.9, z 5.1..6.9) cut out.
            Slab(env, "Floor", -12f, -6f, StairX0, 24f, 0f, floor);
            Slab(env, "Floor", StairX0, -6f, StairX1, StairZ0, 0f, floor);
            Slab(env, "Floor", StairX0, StairZ1, StairX1, 24f, 0f, floor);
            Slab(env, "Floor", StairX1, -6f, 12f, 24f, 0f, floor);
            Slab(env, "Ceiling", -12f, -6f, 12f, 24f, H, wall, ceiling: true);
            WallX(env, "Facade", -6f, -12f, 12f, 6f, facade, 0f);
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
            PointLight(lights, "StreetLamp", new Vector3(1.6f, 3.4f, -10f), new Color(0.6f, 0.7f, 1f), 1.4f, 10f);
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
            Sign(signs, "StationName", "KVLR 103.7\nRADIO STATION", new Vector3(0f, 4.3f, -6.12f), 0f, 5f, new Color(0.75f, 0.75f, 0.7f), new Vector2(6f, 1.6f));
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

            MakeDoor(items, "FrontDoor", new Vector3(0f, 0f, -6f), 0f, "l1_front", "front door", doorMat);
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
                "STORM NIGHT - 14 OCTOBER\n\n23:10  IN   Maya R. (staff family)\n23:10  IN   Alex R. (staff family)\n02:47  OUT  Alex R.\n" +
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

            Transform spawn = Marker(story, "PlayerStart", new Vector3(0f, 0.02f, -15f), 0f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 1);
            SetValue(level, "levelName", "The Abandoned Station");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l1_enter");
            SetValue(level, "startObjectiveText", "Get inside the radio station");
            SetLines(level, "introLines", new[]
            {
                L("ALEX", "Ten years, and it still looks the same.", 3f),
                L("ALEX", "Maya's voice. On the radio. Tonight. It can't be her...", 3.5f),
            });
            SetValue(level, "flashlightDrain", false);
            SetValue(level, "ambience", Ambience.RainAndWind);
            SetValue(level, "ambienceVolume", 0.6f);

            Transform weather = Group(null, "Weather");
            Rain(weather, new Vector3(0f, 10f, -12f), new Vector3(24f, 12f, 1f));

            LevelDressing.Finish(1, items);
            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level1Builder] Built " + ScenePath);
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
