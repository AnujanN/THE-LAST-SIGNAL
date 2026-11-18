using LastSignal.Audio;
using LastSignal.Enemy;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Puzzles;
using TMPro;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Level 3 - The Last Transmission (hard). The Echo is active and more alert, battery drains faster,
    /// fewer hiding spots. Access corridor -> equipment room + antenna control room (3 modules, clues)
    /// -> broadcast chamber (frequency puzzle 103.7, final recording, shutdown) -> escape -> Ending.
    /// </summary>
    public static class Level3Builder
    {
        public const string ScenePath = "Assets/Scenes/Level03_Tower.unity";
        private const float H = 3.4f;

        [MenuItem("The Last Signal/Build/Level 3 - The Last Transmission")]
        public static void Build()
        {
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();
            GameObject echoPrefab = GamePrefabs.Echo();
            var scene = NewScene();
            Atmosphere(new Color(0.03f, 0.03f, 0.045f), new Color(0.015f, 0.01f, 0.02f), 0.025f);

            Material floor = Mat("L3_Floor", new Color(0.2f, 0.2f, 0.23f));
            Material wall = Mat("L3_Wall", new Color(0.36f, 0.36f, 0.4f));
            Material metal = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Material dark = Mat("L3_Dark", new Color(0.12f, 0.12f, 0.14f), smoothness: 0.4f);
            Material locker = Mat("L2_Locker", new Color(0.25f, 0.3f, 0.32f), smoothness: 0.35f);
            Material wood = Mat("L1_Wood", new Color(0.36f, 0.25f, 0.16f));
            Material paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            Material partMat = Mat("Greybox_Accent", new Color(0.9f, 0.75f, 0.2f));
            Material black = Mat("Black", new Color(0.03f, 0.03f, 0.03f));
            Material slotMat = Mat("SlotEmpty", new Color(0.45f, 0.3f, 0.08f));
            Material red = Mat("Red", new Color(0.6f, 0.08f, 0.06f));
            Material glow = Mat("TransmitterGlow", new Color(0.5f, 0.7f, 1f), emissive: true);
            Material doorMat = Mat("L3_Door", new Color(0.3f, 0.32f, 0.36f), smoothness: 0.5f);

            // ------------------------------------------------------------------ geometry
            var envGo = new GameObject("Environment");
            Transform env = envGo.transform;
            Slab(env, "Floor", -17f, -3f, 13f, 29f, 0f, floor);
            Slab(env, "Ceiling", -17f, -3f, 13f, 29f, H, wall, ceiling: true);
            WallZ(env, "Lift_W", -1.5f, -2f, 1f, H, wall);
            WallZ(env, "Lift_E", 1.5f, -2f, 1f, H, wall);
            WallX(env, "Lift_S", -2f, -1.5f, 1.5f, H, wall);
            WallZ(env, "Corridor_W", -1.5f, 1f, 16f, H, wall, 9f);
            WallZ(env, "Corridor_E", 1.5f, 1f, 16f, H, wall, 9f);
            WallX(env, "Equip_S", 4f, 1.5f, 12f, H, wall);
            WallX(env, "Equip_N", 14f, 1.5f, 12f, H, wall);
            WallZ(env, "Equip_E", 12f, 4f, 14f, H, wall);
            WallX(env, "Antenna_S", 4f, -12f, -1.5f, H, wall);
            WallX(env, "Antenna_N", 14f, -12f, -1.5f, H, wall);
            WallZ(env, "Antenna_W", -12f, 4f, 14f, H, wall);
            WallX(env, "Chamber_S", 16f, -7f, 7f, H, wall, 0f);
            WallZ(env, "Chamber_E", 7f, 16f, 28f, H, wall);
            WallZ(env, "Chamber_W", -7f, 16f, 28f, H, wall, 25f);
            WallX(env, "Chamber_N", 28f, -7f, 7f, H, wall);
            WallX(env, "Escape_S", 23.5f, -16f, -7f, H, wall);
            WallX(env, "Escape_N", 26.5f, -16f, -7f, H, wall);
            WallZ(env, "Escape_W", -16f, 23.5f, 26.5f, H, wall, 25f);

            Box(env, "Workbench", new Vector3(5f, 0.45f, 5f), new Vector3(2.4f, 0.9f, 0.9f), wood);
            Box(env, "EquipRack", new Vector3(10f, 0.5f, 12.5f), new Vector3(2f, 1f, 0.8f), metal);
            Box(env, "EquipRack2", new Vector3(11.4f, 1.1f, 9f), new Vector3(0.8f, 2.2f, 2f), metal);
            Box(env, "AntennaDesk", new Vector3(-9f, 0.45f, 12.4f), new Vector3(2.4f, 0.9f, 0.9f), wood);
            Box(env, "AntennaConsole", new Vector3(-11.4f, 0.8f, 8f), new Vector3(0.8f, 1.6f, 3f), dark);
            Box(env, "Transmitter", new Vector3(0f, 1.2f, 25.6f), new Vector3(4.2f, 2.4f, 1.2f), dark);
            Box(env, "TuningConsole", new Vector3(0f, 0.5f, 23.2f), new Vector3(3.2f, 1f, 0.8f), metal);
            Box(env, "RecorderDesk", new Vector3(-4.5f, 0.45f, 26.8f), new Vector3(1.8f, 0.9f, 0.9f), wood);
            Box(env, "CableRun", new Vector3(4.5f, 0.15f, 22f), new Vector3(0.4f, 0.3f, 8f), black);

            BakeNavMesh(envGo, "Level03_Tower");

            // ------------------------------------------------------------------ lights
            Transform lights = Group(null, "Lighting");
            Light corrLight1 = PointLight(lights, "Corridor_1", new Vector3(0f, 3.1f, 4f), new Color(0.85f, 0.9f, 1f), 1f, 8f);
            Light corrLight2 = PointLight(lights, "Corridor_2", new Vector3(0f, 3.1f, 12f), new Color(0.85f, 0.9f, 1f), 1f, 8f);
            corrLight2.gameObject.AddComponent<FlickerLight>();
            Light equipLight = PointLight(lights, "Equipment", new Vector3(7f, 3.1f, 9f), new Color(1f, 0.9f, 0.75f), 1.1f, 9f);
            Light antennaLight = PointLight(lights, "Antenna", new Vector3(-7f, 3.1f, 9f), new Color(0.75f, 0.85f, 1f), 0.9f, 9f);
            antennaLight.gameObject.AddComponent<FlickerLight>();
            Light chamberLight = PointLight(lights, "Chamber", new Vector3(0f, 3.1f, 21f), new Color(0.75f, 0.85f, 1f), 1.4f, 13f);
            Light transmitterGlow = PointLight(lights, "TransmitterGlow", new Vector3(0f, 1.8f, 24.6f), new Color(0.4f, 0.6f, 1f), 1.2f, 6f);
            var towerPower = new GameObject("TowerPower").AddComponent<PowerGroup>();
            SetValue(towerPower, "startPowered", true);
            SetValue(towerPower, "offFlag", FinaleSequence.ShutdownFlag);
            SetArray(towerPower, "lights", new Object[] { corrLight1, corrLight2, equipLight, antennaLight, chamberLight, transmitterGlow });
            GameObject glowStrip = Box(lights, "TransmitterStrip", new Vector3(0f, 2.2f, 24.98f), new Vector3(3.8f, 0.08f, 0.02f), glow, collider: false);
            SetArray(towerPower, "emissive", new Object[] { glowStrip.GetComponent<Renderer>() });
            SetValue(towerPower, "emissionOn", new Color(0.5f, 0.7f, 1f));

            var emergency = new GameObject("EmergencyLights");
            emergency.transform.SetParent(lights, false);
            PointLight(emergency.transform, "Red_Chamber", new Vector3(0f, 3f, 20f), new Color(1f, 0.1f, 0.08f), 1.2f, 12f);
            PointLight(emergency.transform, "Red_Escape", new Vector3(-11f, 3f, 25f), new Color(1f, 0.1f, 0.08f), 1.2f, 9f);
            PointLight(emergency.transform, "Exit_Glow", new Vector3(-15.5f, 2.6f, 25f), new Color(1f, 0.85f, 0.6f), 1.5f, 6f);

            // ------------------------------------------------------------------ signs
            Transform signs = Group(null, "Signs");
            Sign(signs, "Sign_Equip", "EQUIPMENT ROOM", new Vector3(1.38f, 2.6f, 9f), 90f);
            Sign(signs, "Sign_Antenna", "ANTENNA CONTROL", new Vector3(-1.38f, 2.6f, 9f), -90f);
            Sign(signs, "Sign_Chamber", "BROADCAST CHAMBER", new Vector3(0f, 2.6f, 15.88f), 0f);
            Sign(signs, "Sign_Escape", "EMERGENCY EXIT", new Vector3(-6.88f, 2.6f, 25f), -90f, 3f, new Color(0.95f, 0.4f, 0.35f));
            Sign(signs, "Sign_Transmitter", "KVLR EXPERIMENTAL TRANSMITTER", new Vector3(0f, 2.9f, 24.98f), 0f, 2.6f, null, new Vector2(4f, 0.5f));

            // ------------------------------------------------------------------ story + interactables
            Transform story = Group(null, "Story");
            var events = story.gameObject.AddComponent<StoryEvents>();
            Transform items = Group(null, "Interactables");

            MakeDoor(items, "EquipDoor", new Vector3(1.5f, 0f, 9f), -90f, "l3_equip", "door", doorMat);
            MakeDoor(items, "AntennaDoor", new Vector3(-1.5f, 0f, 9f), 90f, "l3_antenna", "door", doorMat);
            Door escapeDoor = MakeDoor(items, "EscapeDoor", new Vector3(-7f, 0f, 25f), 90f, "l3_escape", "emergency exit", red,
                requiredFlag: FinaleSequence.ShutdownFlag, flagMessage: "Sealed. The transmitter's safety lock is still engaged.");

            Pickup(items, "Module_RFAmp", new Vector3(10f, 1.06f, 12.5f), "rf_amp", "RF amplifier", "l3_rf", partMat, PrimitiveType.Cube, new Vector3(0.25f, 0.12f, 0.18f));
            Pickup(items, "Module_Coupler", new Vector3(5.6f, 0.96f, 5f), "power_coupler", "Power coupler", "l3_coupler", partMat, PrimitiveType.Cylinder, new Vector3(0.12f, 0.06f, 0.12f));
            Pickup(items, "Module_Tuner", new Vector3(-9.4f, 0.96f, 12.4f), "antenna_tuner", "Antenna tuner", "l3_tuner", partMat, PrimitiveType.Cube, new Vector3(0.2f, 0.1f, 0.2f));
            Battery(items, new Vector3(4.3f, 0.96f, 5f), "l3_bat1");
            Battery(items, new Vector3(-8.4f, 0.96f, 12.4f), "l3_bat2");

            Note(items, "Note_Engineer", new Vector3(5.9f, 0.91f, 4.8f), 5f, "l3_engineer", "Engineer's shutdown procedure",
                "EMERGENCY SHUTDOWN - EXPERIMENTAL RIG\n\n1. Refit all three transmitter modules (RF amplifier, power coupler, antenna tuner).\n" +
                "2. Tune to the EMERGENCY CHANNEL (see the basement emergency log).\n3. TRANSMIT. The shutdown command only goes out on the emergency channel.\n" +
                "4. Pull the master shutdown lever.\n\nThe rig fights back when it is shut down. Do not stay in the chamber afterwards.", paper);
            NoteInteractable notebook = Note(items, "Note_MayaNotebook", new Vector3(-8.6f, 0.91f, 12.5f), -10f, "l3_notebook", "Maya's notebook",
                "Dad always said: if you're ever lost, find 103.7 and you'll find your way home.\n\n" +
                "Tonight I heard myself on 103.7. Talking. Asking Alex to stay.\n\nThat wasn't me.", paper);
            On(notebook, "onRead", events.Say, "ALEX|103.7. Same as the tape. Same as Dad's old station.|4");

            Locker(items, "Locker_Corridor", new Vector3(0.95f, 0f, 3f), -90f, locker);
            Locker(items, "Locker_Equip", new Vector3(11.45f, 0f, 5.5f), -90f, locker);
            Locker(items, "Locker_Antenna", new Vector3(-11.45f, 0f, 5.5f), 90f, locker);
            Locker(items, "Locker_Chamber", new Vector3(6.45f, 0f, 19f), -90f, locker);

            // Frequency puzzle.
            var freqGo = new GameObject("FrequencyPuzzle");
            freqGo.transform.SetParent(items, false);
            freqGo.transform.position = new Vector3(0f, 1.2f, 25f);
            var freq = freqGo.AddComponent<FrequencyPuzzle>();
            SetValue(freq, "id", "l3_frequency");
            var staticSource = freqGo.AddComponent<AudioSource>();
            staticSource.spatialBlend = 1f;
            staticSource.rolloffMode = AudioRolloffMode.Linear;
            staticSource.maxDistance = 14f;
            staticSource.playOnAwake = false;
            Set(freq, "staticSource", staticSource);
            ItemSlot ampSlot = Slot(items, "Slot_RFAmp", new Vector3(-1.2f, 1f, 24.95f), "l3_slot_rf", "rf_amp", "RF amplifier", "amplifier bay", slotMat, partMat, new Vector3(0.3f, 0.2f, 0.1f));
            ItemSlot couplerSlot = Slot(items, "Slot_Coupler", new Vector3(0f, 1f, 24.95f), "l3_slot_coupler", "power_coupler", "Power coupler", "coupler socket", slotMat, partMat, new Vector3(0.3f, 0.2f, 0.1f));
            ItemSlot tunerSlot = Slot(items, "Slot_Tuner", new Vector3(1.2f, 1f, 24.95f), "l3_slot_tuner", "antenna_tuner", "Antenna tuner", "tuner bay", slotMat, partMat, new Vector3(0.3f, 0.2f, 0.1f));
            Sign(signs, "Label_RF", "RF AMP", new Vector3(-1.2f, 1.25f, 24.94f), 0f, 1.3f, null, new Vector2(0.7f, 0.2f));
            Sign(signs, "Label_Coupler", "COUPLER", new Vector3(0f, 1.25f, 24.94f), 0f, 1.3f, null, new Vector2(0.7f, 0.2f));
            Sign(signs, "Label_Tuner", "TUNER", new Vector3(1.2f, 1.25f, 24.94f), 0f, 1.3f, null, new Vector2(0.7f, 0.2f));
            SetArray(freq, "modules", new Object[] { ampSlot, couplerSlot, tunerSlot });
            TextMeshPro display = Sign(signs, "FrequencyDisplay", "", new Vector3(0f, 1.75f, 24.97f), 0f, 3.2f, new Color(0.55f, 1f, 0.6f), new Vector2(2.4f, 0.8f));
            Set(freq, "display", display);
            (string label, float delta, float x)[] knobs = { ("-1.0", -1f, -1.25f), ("-0.1", -0.1f, -0.7f), ("+0.1", 0.1f, -0.15f), ("+1.0", 1f, 0.4f) };
            foreach (var k in knobs)
            {
                ActionButton knob = Button3D(items, "Knob_" + k.label, new Vector3(k.x, 1.06f, 23.0f), 0f, "l3_knob_" + k.label,
                    "Tune " + k.label + " MHz", metal, new Vector3(0.2f, 0.12f, 0.2f));
                On(knob, "onPressed", freq.Tune, k.delta);
                Sign(signs, "KnobLabel_" + k.label, k.label, new Vector3(k.x, 0.8f, 22.78f), 0f, 1.1f, null, new Vector2(0.5f, 0.2f));
            }
            ActionButton transmit = Button3D(items, "TransmitLever", new Vector3(1.15f, 1.2f, 23.1f), 0f, "l3_transmit", "Transmit", red, new Vector3(0.12f, 0.4f, 0.12f));
            On(transmit, "onPressed", freq.Transmit);
            Sign(signs, "TransmitLabel", "TRANSMIT", new Vector3(1.15f, 0.8f, 22.78f), 0f, 1.1f, null, new Vector2(0.7f, 0.2f));

            On(freq, "onModulesReady", events.SetObjective, "l3_tune|Tune the transmitter to the emergency channel, then transmit");
            On(freq, "onSolved", events.SetObjective, "l3_recording|Play Maya's final recording on the desk");

            // The Echo (active, alert).
            GameObject echoGo = Instantiate(echoPrefab, scene, new Vector3(0f, 0f, 19f), 180f);
            var echo = echoGo.GetComponent<EchoAIController>();
            Transform wpRoot = Group(null, "EchoWaypoints");
            Vector3[] wps =
            {
                new Vector3(0f, 0f, 19f), new Vector3(0f, 0f, 9f), new Vector3(7f, 0f, 9f), new Vector3(0f, 0f, 3f),
                new Vector3(-7f, 0f, 9f), new Vector3(0f, 0f, 12f), new Vector3(-4f, 0f, 21f), new Vector3(4f, 0f, 21f),
            };
            var wpTransforms = new Object[wps.Length];
            for (int i = 0; i < wps.Length; i++) wpTransforms[i] = Marker(wpRoot, "WP_" + i, wps[i], 0f);
            SetArray(echo, "waypoints", wpTransforms);
            SetValue(echo, "startActive", true);
            SetValue(echo, "settings.sightRange", 13f);
            SetValue(echo, "settings.sightAngle", 120f);
            SetValue(echo, "settings.hearingMultiplier", 1.3f);
            SetValue(echo, "settings.patrolSpeed", 1.5f);
            SetValue(echo, "settings.investigateSpeed", 2.3f);
            SetValue(echo, "settings.chaseSpeed", 3.5f);
            SetValue(echo, "settings.searchSeconds", 9f);
            SetValue(echo, "settings.loseSightSeconds", 3f);
            SetValue(echo, "settings.waypointPause", 1f);
            On(freq, "onSolved", echo.Vanish);
            On(freq, "onSolved", events.Say, "THE ECHO|STOP! You'll silence her! You'll silence ME!|3.5");

            // Final recording (cannot be skipped; player is held still while it plays).
            TapePlayer finalTape = Tape(items, "FinalRecording", new Vector3(-4.5f, 0.99f, 26.8f), 0f, "l3_final", "Maya's final recording", black, new[]
            {
                L("RECORDING", "[The broadcast clears. Maya's voice, close to the microphone.]", 3f),
                L("MAYA (FINAL RECORDING)", "Alex, if you're hearing this, you got out. I pushed you through the emergency exit.", 5.5f),
                L("MAYA (FINAL RECORDING)", "You didn't leave me behind. I stayed. The transmitter was overloading, and if the signal spread, it would keep calling people here.", 7f),
                L("MAYA (FINAL RECORDING)", "Something lives in the static. It copies voices. It will use mine. Don't believe it.", 5.5f),
                L("MAYA (FINAL RECORDING)", "This was never your fault. Please stop blaming yourself. Live, for both of us.", 6f),
                L("MAYA (FINAL RECORDING)", "I love you, big brother. Now shut it down... and go home.", 5f),
                L("RECORDING", "[The recording ends.]", 2f),
            }, requiredFlag: "puzzle:l3_frequency", missingFlagMessage: "Only static. The broadcast isn't running.", lockPlayer: true);
            SetValue(finalTape, "replayable", false);
            On(finalTape, "onFinished", events.Say, "ALEX|You saved me. All this time... I'm sorry, Maya. I hear you.|4.5");
            On(finalTape, "onFinished", events.SetObjective, "l3_shutdown|Pull the master shutdown lever");

            var finale = story.gameObject.AddComponent<FinaleSequence>();
            Set(finale, "towerPower", towerPower);
            Set(finale, "emergencyLights", emergency);
            Set(finale, "escapeDoor", escapeDoor);
            Set(finale, "echo", echo);
            Set(finale, "echoSpawn", Marker(story, "EchoHuntSpawn", new Vector3(0f, 0f, 14f), 0f));
            SetValue(finale, "huntDelay", 3f);
            ActionButton shutdown = Button3D(items, "MasterShutdown", new Vector3(2.6f, 1.3f, 24.95f), 0f, "l3_shutdown", "Pull master shutdown",
                red, new Vector3(0.14f, 0.45f, 0.14f), requiredFlag: "played:l3_final", lockedMessage: "Not yet. Listen to the recording first.", oneShot: true);
            On(shutdown, "onPressed", finale.ShutDownTransmitter);
            Sign(signs, "ShutdownLabel", "MASTER\nSHUTDOWN", new Vector3(2.6f, 1.75f, 24.94f), 0f, 1.3f, new Color(1f, 0.5f, 0.45f), new Vector2(0.8f, 0.4f));

            // Escape.
            LevelExit exit = Trigger<LevelExit>(story, "Exit_Outside", new Vector3(-15.2f, 1f, 25f), new Vector3(1.2f, 2f, 2.8f));
            SetValue(exit, "nextScene", GameFlowManager.EndingScene);
            SetValue(exit, "requiredFlag", FinaleSequence.ShutdownFlag);

            // Beats.
            TriggerZone chamberZone = Trigger<TriggerZone>(story, "Zone_Chamber", new Vector3(0f, 1f, 17f), new Vector3(3f, 2f, 1.5f));
            SetValue(chamberZone, "id", "l3_chamber_enter");
            On(chamberZone, "onEnter", events.PlayStatic);
            On(chamberZone, "onEnter", events.Say, "THE ECHO (MAYA'S VOICE)|Don't touch it, Alex. Stay here with me. Forever.|4");

            // ------------------------------------------------------------------ checkpoints + level
            CheckpointZone(story, "l3_chamber", new Vector3(0f, 1f, 15f), 0f, new Vector3(3f, 2f, 1.2f));
            CheckpointZone(story, "finale", new Vector3(-3f, 0.02f, 24.5f), -90f, Vector3.one, triggerEnabled: false);

            Transform spawn = Marker(story, "PlayerStart", new Vector3(0f, 0.02f, -1f), 0f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 3);
            SetValue(level, "levelName", "The Last Transmission");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l3_components");
            SetValue(level, "startObjectiveText", "Find the 3 transmitter modules: RF amplifier, power coupler, antenna tuner");
            SetLines(level, "introLines", new[]
            {
                L("ALEX", "The transmission tower. This is where it all started.", 3.5f),
                L("THE ECHO (MAYA'S VOICE)", "Alex... why won't you stay?", 3f),
            });
            SetValue(level, "flashlightDrain", true);
            SetValue(level, "drainPerSecond", 0.8f);
            SetValue(level, "ambience", Ambience.TowerInterference);
            SetValue(level, "ambienceVolume", 0.55f);

            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level3Builder] Built " + ScenePath);
        }
    }
}
