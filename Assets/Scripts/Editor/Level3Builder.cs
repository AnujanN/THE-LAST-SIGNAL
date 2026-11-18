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
    /// Level 3 - The Living Quarters (medium-hard, new in the five-level version).
    /// The lift from the basement jams at the family's old staff quarters. A long dark hallway with
    /// the kids' room, bathroom and parents' room to the west, the kitchen, Dad's study and storage to
    /// the east, and the fire door to the burned wing at the far end.
    /// Puzzle: Dad's safe (fire door key). Code = "the night everything ended, month first" (Dad's
    /// logbook) + the date circled on the kitchen calendar (14 October) = 1014.
    /// Story: Maya's diary - the voice in the static appeared before the fire and learned their
    /// voices; Dad's channel "always ended on seven" (the ".7" for the tower).
    /// </summary>
    public static class Level3Builder
    {
        public const string SceneName = "Level03_Quarters";
        public const string ScenePath = "Assets/Scenes/Level03_Quarters.unity";
        private const float H = 3.0f;

        [MenuItem("The Last Signal/Build/Level 3 - The Living Quarters")]
        public static void Build()
        {
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();
            GameObject echoPrefab = GamePrefabs.Echo();
            var scene = NewScene(SceneName);
            Atmosphere(new Color(0.03f, 0.03f, 0.035f), new Color(0.012f, 0.012f, 0.016f), 0.028f);

            Material carpet = Mat("Quarters_Floor", new Color(0.3f, 0.26f, 0.24f));
            Material wall = Mat("Quarters_Wall", new Color(0.5f, 0.48f, 0.45f));
            Material tiles = Mat("Quarters_Tiles", new Color(0.6f, 0.62f, 0.6f), smoothness: 0.4f);
            Material wood = Mat("L1_Wood", new Color(0.36f, 0.25f, 0.16f));
            Material fabric = Mat("Fabric", new Color(0.25f, 0.18f, 0.2f));
            Material metal = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Material paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            Material doorMat = Mat("L1_Door", new Color(0.34f, 0.24f, 0.16f));
            Material red = Mat("Red", new Color(0.6f, 0.08f, 0.06f));
            Material black = Mat("Black", new Color(0.03f, 0.03f, 0.03f));
            Material keyMat = Mat("Key", new Color(0.75f, 0.65f, 0.3f), smoothness: 0.7f);
            Material white = Mat("Porcelain", new Color(0.8f, 0.8f, 0.78f), smoothness: 0.7f);
            Material mirror = Mat("Mirror", new Color(0.55f, 0.6f, 0.62f), smoothness: 0.95f);

            // ------------------------------------------------------------------ geometry
            var envGo = new GameObject("Environment");
            Transform env = envGo.transform;
            Slab(env, "Floor", -9f, -2f, 9f, 27f, 0f, carpet);
            Slab(env, "Ceiling", -9f, -2f, 9f, 27f, H, wall, ceiling: true);
            Box(env, "BathroomTiles", new Vector3(-5.25f, 0.005f, 11.75f), new Vector3(7.3f, 0.01f, 4.3f), tiles);

            // Lift lobby and the long hallway.
            WallZ(env, "Lift_W", -1.5f, -2f, 1f, H, wall);
            WallZ(env, "Lift_E", 1.5f, -2f, 1f, H, wall);
            WallX(env, "Lift_S", -2f, -1.5f, 1.5f, H, wall);
            Box(env, "LiftDoors", new Vector3(0f, 1.1f, -1.85f), new Vector3(1.4f, 2.2f, 0.1f), metal);
            WallZ(env, "Hall_W", -1.5f, 1f, 25f, H, wall, 5f, 11.75f, 18f);
            WallZ(env, "Hall_E", 1.5f, 1f, 25f, H, wall, 6f, 16f, 22.5f);
            WallX(env, "Hall_N", 25f, -1.5f, 1.5f, H, wall, 0f);
            WallZ(env, "Vest_W", -1.5f, 25f, 27f, H, wall);
            WallZ(env, "Vest_E", 1.5f, 25f, 27f, H, wall);
            WallX(env, "Vest_N", 27f, -1.5f, 1.5f, H, wall);

            // West: kids' room, bathroom, parents' room.
            WallX(env, "West_S", 1f, -9f, -1.5f, H, wall);
            WallZ(env, "West_W", -9f, 1f, 25f, H, wall);
            WallX(env, "Kids_N", 9.5f, -9f, -1.5f, H, wall);
            WallX(env, "Bath_N", 14f, -9f, -1.5f, H, wall);
            WallX(env, "Parents_N", 25f, -9f, -1.5f, H, wall);

            // East: kitchen, study, storage.
            WallX(env, "East_S", 1f, 1.5f, 9f, H, wall);
            WallZ(env, "East_E", 9f, 1f, 25f, H, wall);
            WallX(env, "Kitchen_N", 12f, 1.5f, 9f, H, wall);
            WallX(env, "Study_N", 20f, 1.5f, 9f, H, wall);
            WallX(env, "Storage_N", 25f, 1.5f, 9f, H, wall);

            // Furniture.
            Box(env, "Bunk_Lower", new Vector3(-7.9f, 0.3f, 3.4f), new Vector3(1f, 0.6f, 2f), fabric);
            Box(env, "Bunk_Upper", new Vector3(-7.9f, 1.5f, 3.4f), new Vector3(1f, 0.15f, 2f), wood);
            Box(env, "Bunk_Post", new Vector3(-7.45f, 1f, 2.45f), new Vector3(0.08f, 2f, 0.08f), wood);
            Box(env, "Bunk_Post2", new Vector3(-7.45f, 1f, 4.35f), new Vector3(0.08f, 2f, 0.08f), wood);
            Box(env, "KidsDesk", new Vector3(-3.2f, 0.4f, 8.7f), new Vector3(1.6f, 0.8f, 0.7f), wood);
            Box(env, "ToyChest", new Vector3(-5f, 0.3f, 1.6f), new Vector3(1f, 0.6f, 0.6f), wood);
            Box(env, "Bathtub", new Vector3(-7.6f, 0.3f, 12.3f), new Vector3(1.6f, 0.6f, 0.8f), white);
            Box(env, "Sink", new Vector3(-3f, 0.45f, 13.55f), new Vector3(0.6f, 0.9f, 0.5f), white);
            Box(env, "MirrorGlass", new Vector3(-3f, 1.6f, 13.88f), new Vector3(0.7f, 0.6f, 0.02f), mirror, collider: false);
            Box(env, "ParentsBed", new Vector3(-6f, 0.3f, 21f), new Vector3(2f, 0.6f, 2.2f), fabric);
            Box(env, "Nightstand", new Vector3(-4.45f, 0.3f, 22.4f), new Vector3(0.5f, 0.6f, 0.5f), wood);
            Box(env, "KitchenTable", new Vector3(5f, 0.4f, 6f), new Vector3(1.8f, 0.8f, 1f), wood);
            Box(env, "Counter", new Vector3(8.5f, 0.45f, 5f), new Vector3(0.8f, 0.9f, 3.6f), wood);
            Box(env, "Fridge", new Vector3(8.45f, 0.95f, 1.75f), new Vector3(0.9f, 1.9f, 0.8f), white);
            Box(env, "Sofa", new Vector3(3.6f, 0.3f, 10.9f), new Vector3(2.2f, 0.6f, 0.8f), fabric);
            Box(env, "StudyDesk", new Vector3(5.5f, 0.4f, 18.9f), new Vector3(2f, 0.8f, 0.9f), wood);
            Box(env, "Bookshelf", new Vector3(8.6f, 1f, 16f), new Vector3(0.6f, 2f, 2f), wood);
            Box(env, "Safe", new Vector3(8.4f, 0.45f, 13f), new Vector3(0.75f, 0.9f, 0.75f), metal);
            Box(env, "StorageShelf", new Vector3(5f, 0.9f, 24.5f), new Vector3(3f, 1.8f, 0.6f), wood);
            Box(env, "Boxes", new Vector3(7.8f, 0.4f, 21.2f), new Vector3(1f, 0.8f, 1f), wood);

            BakeNavMesh(envGo, SceneName);

            // ------------------------------------------------------------------ lights
            Transform lights = Group(null, "Lighting");
            Light hall1 = PointLight(lights, "Hall_1", new Vector3(0f, 2.8f, 6f), new Color(1f, 0.85f, 0.65f), 0.8f, 7f);
            Light hall2 = PointLight(lights, "Hall_2", new Vector3(0f, 2.8f, 17f), new Color(1f, 0.85f, 0.65f), 0.7f, 7f);
            hall2.gameObject.AddComponent<FlickerLight>();
            PointLight(lights, "NightLight", new Vector3(-8.5f, 0.5f, 8.8f), new Color(0.5f, 0.6f, 1f), 0.25f, 3.5f);
            PointLight(lights, "FridgeGlow", new Vector3(7.9f, 1f, 2.4f), new Color(0.75f, 0.9f, 1f), 0.35f, 3.5f);
            Light deskLamp = PointLight(lights, "DeskLamp", new Vector3(5f, 1.1f, 18.9f), new Color(1f, 0.75f, 0.45f), 0.45f, 4f);
            deskLamp.gameObject.AddComponent<FlickerLight>();
            PointLight(lights, "ExitSign", new Vector3(0f, 2.6f, 24.7f), new Color(0.3f, 1f, 0.4f), 0.3f, 3.5f);

            // ------------------------------------------------------------------ signs
            Transform signs = Group(null, "Signs");
            Sign(signs, "Sign_Floor", "STAFF QUARTERS - FLOOR 2", new Vector3(0f, 2.4f, -1.88f), 180f, 2.4f, null, new Vector2(2.6f, 0.4f));
            Sign(signs, "Sign_FireDoor", "FIRE DOOR\nBROADCAST WING - NO ENTRY", new Vector3(0f, 2.55f, 24.88f), 0f, 2.2f,
                new Color(0.75f, 0.25f, 0.2f), new Vector2(2.4f, 0.6f));
            Sign(signs, "Sign_Kids", "MAYA + ALEX\nKEEP OUT!!", new Vector3(-1.38f, 1.6f, 6.3f), -90f, 1.6f, new Color(0.45f, 0.4f, 0.32f), new Vector2(1f, 0.5f));
            Sign(signs, "Sign_Study", "STUDY", new Vector3(1.38f, 2.45f, 16f), 90f, 2f);

            // ------------------------------------------------------------------ story + interactables
            Transform story = Group(null, "Story");
            var events = story.gameObject.AddComponent<StoryEvents>();
            Transform items = Group(null, "Interactables");

            MakeDoor(items, "KidsDoor", new Vector3(-1.5f, 0f, 5f), 90f, "l3q_kids", "door", doorMat);
            MakeDoor(items, "BathDoor", new Vector3(-1.5f, 0f, 11.75f), 90f, "l3q_bath", "door", doorMat);
            MakeDoor(items, "ParentsDoor", new Vector3(-1.5f, 0f, 18f), 90f, "l3q_parents", "door", doorMat);
            MakeDoor(items, "KitchenDoor", new Vector3(1.5f, 0f, 6f), -90f, "l3q_kitchen", "door", doorMat);
            Door studyDoor = MakeDoor(items, "StudyDoor", new Vector3(1.5f, 0f, 16f), -90f, "l3q_study", "study door", doorMat);
            MakeDoor(items, "StorageDoor", new Vector3(1.5f, 0f, 22.5f), -90f, "l3q_storage", "door", doorMat);
            Door fireDoor = MakeDoor(items, "FireDoor", new Vector3(0f, 0f, 25f), 0f, "l3q_fire", "fire door", red,
                requiredItemId: "fire_key", requiredItemName: "Fire door key");

            // Hiding places: wardrobes and a pantry.
            Locker(items, "Wardrobe_Kids", new Vector3(-8.45f, 0f, 7.6f), 90f, wood, "wardrobe");
            Locker(items, "Wardrobe_Parents", new Vector3(-8.45f, 0f, 16.2f), 90f, wood, "wardrobe");
            Locker(items, "Pantry", new Vector3(8.45f, 0f, 9.6f), -90f, wood, "pantry");
            Locker(items, "Closet_Storage", new Vector3(2.05f, 0f, 24.2f), 90f, wood, "closet");

            Battery(items, new Vector3(-5f, 0.62f, 1.6f), "l3q_bat1");
            Battery(items, new Vector3(4.4f, 1.82f, 24.5f), "l3q_bat2");

            // Clues.
            NoteInteractable diary = Note(items, "Note_MayaDiary", new Vector3(-3.4f, 0.81f, 8.65f), 12f, "l3q_diary", "Maya's diary",
                "Oct 3\nThe rig talks back now. Not words at first. Breathing. Then my name.\n\n" +
                "Oct 9\nIt said Alex's name before Alex did. It's learning us. Dad switched it off and it STILL whispered.\n\n" +
                "Oct 12\nDad says every good station ends on a seven. His was one-oh-three-POINT-SEVEN. " +
                "If I talk to it on Dad's channel during the storm test, maybe I can tell it to leave us alone.", paper);
            On(diary, "onRead", events.AddClue, "Maya's diary: Dad's channel always ended on seven - point seven.");
            On(diary, "onRead", events.Say, "ALEX|Point seven. The tape said one-oh-three... Dad's channel was 103.7.|4.5");

            NoteInteractable logbook = Note(items, "Note_DadLogbook", new Vector3(-4.45f, 0.61f, 22.35f), -20f, "l3q_logbook", "Dad's logbook",
                "Oct 2 - Interference on the experimental rig again. A voice under the carrier. It copies the kids.\n\n" +
                "Oct 13 - Storm due tomorrow night. Told Maya: NO broadcast test. Rig stays off.\n\n" +
                "Nov 3 - Came back for her things. Locked the key to the broadcast wing in my safe. Nobody goes in there again.\n" +
                "The combination is the night everything ended. Month first, the way the tower clock showed it.", paper);
            On(logbook, "onRead", events.AddClue, "Dad's safe: the night everything ended, month first (MM DD).");
            On(logbook, "onRead", events.SetObjectiveIfNotFlag, "code:l3q_safe|l3q_safe|Open Dad's safe in the study. The combination is a date.");

            NoteInteractable calendar = WallNote(items, "Note_Calendar", new Vector3(6f, 1.6f, 11.88f), 180f, "l3q_calendar", "Kitchen calendar - OCTOBER",
                "OCTOBER\n\n 3  Dentist (Alex)\n 9  Dad - night shift\n12  Groceries\n14  STORM WARNING!!  (circled twice)\n" +
                "     \"Maya: broadcast test\" - crossed out in Dad's handwriting\n21  Alex's birthday", paper, new Vector2(0.5f, 0.6f));
            On(calendar, "onRead", events.AddClue, "Kitchen calendar: the storm night was 14 October.");

            WallNote(items, "Note_FamilyPhoto", new Vector3(-6f, 1.55f, 24.88f), 0f, "l3q_photo", "Family photo",
                "A faded photo on the wall: Dad, Maya and Alex in front of the tower, squinting into the sun.\n\n" +
                "On the back, in Maya's handwriting: 'Our station. Our home. Whatever happens, we find each other.'", paper, new Vector2(0.4f, 0.3f));

            // Dad's safe -> fire door key.
            GameObject safeDial = Box(items, "SafeDial", new Vector3(8.0f, 0.55f, 13f), new Vector3(0.05f, 0.18f, 0.18f), black, isStatic: false);
            var safe = safeDial.AddComponent<CodeLock>();
            SetValue(safe, "id", "l3q_safe");
            SetValue(safe, "code", "1014");
            SetValue(safe, "title", "DAD'S SAFE - COMBINATION");
            SetValue(safe, "prompt", "Turn the safe dial");
            SetValue(safe, "wrongMessage", "The handle won't budge.");
            SetValue(safe, "hint", "Dad wrote that it's the night everything ended, month first.");
            SetValue(safe, "wrongNoiseRadius", 5f);
            GameObject safeDoor = Box(items, "SafeDoor", new Vector3(8.02f, 0.45f, 13f), new Vector3(0.04f, 0.8f, 0.65f), metal, collider: false, isStatic: false);
            PickupItem fireKey = Pickup(items, "Pickup_FireKey", new Vector3(8.4f, 0.93f, 13f), "fire_key", "Fire door key", "l3q_key",
                keyMat, PrimitiveType.Cube, new Vector3(0.16f, 0.03f, 0.06f));
            fireKey.gameObject.SetActive(false);
            var safeOpen = story.gameObject.AddComponent<FlagActivator>();
            SetValue(safeOpen, "flag", "code:l3q_safe");
            SetArray(safeOpen, "show", new Object[] { fireKey.gameObject });
            SetArray(safeOpen, "hide", new Object[] { safeDoor });
            On(safe, "onUnlocked", events.Checkpoint, "l3q_safe");
            On(safe, "onUnlocked", events.Say, "ALEX|Ten fourteen. The night I lost her.|3");
            On(fireKey, "onPickedUp", events.SetObjective, "l3q_fire|Unlock the fire door at the end of the hallway");
            On(fireDoor, "onOpened", events.SetObjective, "l3q_exit|Go through into the burned wing");

            // ------------------------------------------------------------------ The Echo
            GameObject echoGo = Instantiate(echoPrefab, scene, new Vector3(0f, 0f, 22f), 180f);
            var echo = echoGo.GetComponent<EchoAIController>();
            Transform wpRoot = Group(null, "EchoWaypoints");
            Vector3[] wps =
            {
                new Vector3(0f, 0f, 22f), new Vector3(0f, 0f, 12f), new Vector3(-5f, 0f, 5f), new Vector3(0f, 0f, 3f),
                new Vector3(5f, 0f, 6f), new Vector3(5f, 0f, 16.5f), new Vector3(-5f, 0f, 19.5f), new Vector3(-5f, 0f, 11.8f),
                new Vector3(5f, 0f, 22.8f),
            };
            var wpTransforms = new Object[wps.Length];
            for (int i = 0; i < wps.Length; i++) wpTransforms[i] = Marker(wpRoot, "WP_" + i, wps[i], 0f);
            SetArray(echo, "waypoints", wpTransforms);
            SetValue(echo, "startActive", false);
            SetValue(echo, "settings.sightRange", 12f);
            SetValue(echo, "settings.sightAngle", 110f);
            SetValue(echo, "settings.hearingMultiplier", 1.15f);
            SetValue(echo, "settings.patrolSpeed", 1.35f);
            SetValue(echo, "settings.investigateSpeed", 2.1f);
            SetValue(echo, "settings.chaseSpeed", 3.35f);
            SetValue(echo, "settings.searchSeconds", 8f);
            SetValue(echo, "settings.loseSightSeconds", 2.8f);
            SetValue(echo, "settings.inspectChance", 0.5f);
            SetValue(echo, "settings.inspectSeconds", 4.5f);
            SetValue(echo, "settings.directorHintSeconds", 45f);
            SetValue(echo, "settings.lureInterval", 55f);

            // ------------------------------------------------------------------ scripted beats
            TriggerZone hallEnter = Trigger<TriggerZone>(story, "Zone_Hallway", new Vector3(0f, 1f, 3f), new Vector3(3f, 2f, 1.5f));
            SetValue(hallEnter, "id", "l3q_hall_enter");
            On(hallEnter, "onEnter", events.PlayStatic);
            On(hallEnter, "onEnter", events.Say, "THE ECHO (MAYA'S VOICE)|Alex? Is that you? Our room is just like we left it...|4");
            On(hallEnter, "onEnter", echo.Activate);

            TriggerZone kidsRoom = Trigger<TriggerZone>(story, "Zone_KidsRoom", new Vector3(-5f, 1f, 5f), new Vector3(6f, 2f, 7f));
            SetValue(kidsRoom, "id", "l3q_kids_room");
            On(kidsRoom, "onEnter", events.Say, "ALEX|The bunk beds. She always took the top one.|3");

            TriggerZone fireDoorZone = Trigger<TriggerZone>(story, "Zone_FireDoor", new Vector3(0f, 1f, 23.5f), new Vector3(3f, 2f, 1.5f));
            SetValue(fireDoorZone, "id", "l3q_fire_door_seen");
            On(fireDoorZone, "onEnter", events.SetObjectiveIfNotFlag, "taken:l3q_key|l3q_key|The fire door is locked. Dad kept his keys locked up - search his study.");

            // Bathroom: Maya standing by the tub, gone the moment you look.
            ShadowSighting bathGhost = ScareKit.Apparition(story, "l3q_bath_ghost", ScareKit.MayaGhost(), new Vector3(-6.4f, 0f, 12.9f), 90f,
                glimpse: true, maxSeconds: 8f, vanishDistance: 2f, whisper: "MAYA?|Don't look in the mirror, Alex.|3");
            TriggerZone bath = Trigger<TriggerZone>(story, "Zone_Bathroom", new Vector3(-4.5f, 1f, 11.75f), new Vector3(5f, 2f, 3.5f));
            SetValue(bath, "id", "l3q_bathroom");
            On(bath, "onEnter", bathGhost.Show);

            // Footsteps cross the floor above the parents' room.
            ScareEvent parentsSteps = ScareKit.Scare(story, "l3q_steps_above", new Vector3(-5f, 3.3f, 20f), 0.8f);
            ScareKit.Steps(parentsSteps, ScareKit.Path(story, "StepsAbove_Parents", new Vector3(-8f, 3.3f, 23f), new Vector3(-7f, 3.3f, 22f),
                new Vector3(-6f, 3.3f, 21f), new Vector3(-5f, 3.3f, 20f), new Vector3(-4f, 3.3f, 19f), new Vector3(-3f, 3.3f, 18f)), 0.55f);
            TriggerZone parents = Trigger<TriggerZone>(story, "Zone_ParentsRoom", new Vector3(-5f, 1f, 20f), new Vector3(6f, 2f, 8f));
            SetValue(parents, "id", "l3q_parents_room");
            On(parents, "onEnter", parentsSteps.Play);
            On(parents, "onEnter", events.Say, "ALEX|Someone's upstairs. There IS no upstairs...|3");

            // Opening the safe: the hallway lamps burst and the study door slams behind you. It heard that.
            ScareEvent safeScare = ScareKit.Scare(story, "l3q_safe_scare", new Vector3(1.5f, 1f, 16f), 1.2f);
            ScareKit.Lights(safeScare, hall2, hall1);
            ScareKit.Slams(safeScare, studyDoor);
            ScareKit.Voice(safeScare, "THE ECHO|I hear you, Alex.|2.5", staticBurst: true, noiseRadius: 10f);
            On(safe, "onUnlocked", safeScare.Play);

            LevelExit exit = Trigger<LevelExit>(story, "Exit_BurnedWing", new Vector3(0f, 1f, 26.2f), new Vector3(2.6f, 2f, 1f));
            SetValue(exit, "nextScene", GameFlowManager.Level4Scene);

            // ------------------------------------------------------------------ checkpoints + level
            CheckpointZone(story, "l3q_hall", new Vector3(0f, 1f, 1.6f), 0f, new Vector3(3f, 2f, 1f));
            CheckpointZone(story, "l3q_safe", new Vector3(5.5f, 0.02f, 14.5f), 90f, Vector3.one, triggerEnabled: false);

            Transform spawn = Marker(story, "PlayerStart", new Vector3(0f, 0.02f, -1f), 0f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 3);
            SetValue(level, "levelName", "The Living Quarters");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l3q_explore");
            SetValue(level, "startObjectiveText", "The lift jammed. Find a way through the living quarters to the tower");
            SetLines(level, "introLines", new[]
            {
                L("ALEX", "The lift's dead. Floor two... the staff quarters.", 3f),
                L("ALEX", "We lived here. Me, Maya and Dad. Before the fire.", 3.5f),
            });
            SetValue(level, "flashlightDrain", true);
            SetValue(level, "drainPerSecond", 0.65f);
            SetValue(level, "ambience", Ambience.IndoorHum);
            SetValue(level, "ambienceVolume", 0.45f);

            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level3Builder] Built " + ScenePath);
        }
    }
}
