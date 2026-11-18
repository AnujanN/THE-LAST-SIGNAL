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
    /// Level 4 - The Burned Wing (hard, new in the five-level version).
    /// The fire-ravaged broadcasting wing where Maya was trapped. The main corridor has collapsed, so
    /// the route detours through the burned west office. Studio A holds a scorched recorder with a
    /// fragment of Maya's last moments. The junction room re-routes emergency power to the tower stair
    /// door: the schematic says which feeds reach which lines, the fire marshal's report says what burned
    /// (only FEED B -> LINE 3 -> LIFT works). Solving it wakes The Echo into a hunt: run for the stairs.
    /// </summary>
    public static class Level4Builder
    {
        public const string SceneName = "Level04_BurnedWing";
        public const string ScenePath = "Assets/Scenes/Level04_BurnedWing.unity";
        private const float H = 3.2f;

        [MenuItem("The Last Signal/Build/Level 4 - The Burned Wing")]
        public static void Build()
        {
            GameObject playerPrefab = GamePrefabs.Player();
            GameObject hudPrefab = GamePrefabs.Hud();
            GameObject echoPrefab = GamePrefabs.Echo();
            var scene = NewScene(SceneName);
            Atmosphere(new Color(0.03f, 0.024f, 0.022f), new Color(0.02f, 0.013f, 0.009f), 0.036f);

            Material floor = Mat("Burned_Floor", new Color(0.22f, 0.2f, 0.19f));
            Material wall = Mat("Burned_Wall", new Color(0.24f, 0.22f, 0.21f));
            Material charred = Mat("Charred", new Color(0.05f, 0.045f, 0.04f), smoothness: 0.05f);
            Material metal = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Material wood = Mat("L1_Wood", new Color(0.36f, 0.25f, 0.16f));
            Material paper = Mat("Paper", new Color(0.85f, 0.82f, 0.7f));
            Material red = Mat("Red", new Color(0.6f, 0.08f, 0.06f));
            Material black = Mat("Black", new Color(0.03f, 0.03f, 0.03f));
            Material locker = Mat("L2_Locker", new Color(0.25f, 0.3f, 0.32f), smoothness: 0.35f);
            Material knobMat = Mat("Knob", new Color(0.12f, 0.12f, 0.13f), smoothness: 0.5f);

            // ------------------------------------------------------------------ geometry
            var envGo = new GameObject("Environment");
            Transform env = envGo.transform;
            Slab(env, "Floor", -9f, -3f, 10f, 27f, 0f, floor);
            Slab(env, "Ceiling", -9f, -3f, 10f, 27f, H, wall, ceiling: true);

            WallX(env, "Start_S", -2f, -1.5f, 1.5f, H, wall);
            Box(env, "FireDoorBehind", new Vector3(0f, 1.1f, -1.88f), new Vector3(1.5f, 2.2f, 0.08f), red);
            WallZ(env, "Corr_W", -1.5f, -2f, 18f, H, wall, 6f, 14.5f);
            WallZ(env, "Corr_E", 1.5f, -2f, 18f, H, wall, 15f);

            // Collapse: the corridor is blocked between the two office doors.
            Box(env, "Rubble", new Vector3(0f, 0.9f, 10.5f), new Vector3(2.9f, 1.8f, 2.4f), charred);
            Box(env, "Beam_1", new Vector3(-0.3f, 1.9f, 10f), new Vector3(0.25f, 0.25f, 3.2f), charred);
            Box(env, "Beam_2", new Vector3(0.5f, 1.4f, 11.2f), new Vector3(2.6f, 0.22f, 0.22f), charred);

            // West office (the detour), partly collapsed.
            WallX(env, "Office_S", 3f, -9f, -1.5f, H, wall);
            WallX(env, "Office_N", 17f, -9f, -1.5f, H, wall);
            WallZ(env, "Office_W", -9f, 3f, 17f, H, wall);
            Box(env, "OfficeCollapse", new Vector3(-5f, 0.65f, 10f), new Vector3(2.6f, 1.3f, 2.2f), charred);
            Box(env, "OfficeDesk", new Vector3(-7.4f, 0.4f, 5f), new Vector3(1.6f, 0.8f, 0.8f), charred);
            Box(env, "OfficeDesk2", new Vector3(-3.4f, 0.4f, 15.6f), new Vector3(1.6f, 0.8f, 0.8f), charred);
            Box(env, "FilingCabinet", new Vector3(-8.5f, 0.6f, 9f), new Vector3(0.6f, 1.2f, 0.8f), metal);

            // Studio A, where Maya was trapped.
            WallX(env, "Studio_S", 8f, 1.5f, 10f, H, wall);
            WallX(env, "Studio_N", 17f, 1.5f, 10f, H, wall);
            WallZ(env, "Studio_E", 10f, 8f, 17f, H, wall);
            Box(env, "StudioConsole", new Vector3(6f, 0.5f, 12.5f), new Vector3(3f, 1f, 1f), charred);
            Box(env, "RecorderDesk", new Vector3(8.6f, 0.45f, 9.6f), new Vector3(1.4f, 0.9f, 0.7f), charred);
            Box(env, "StudioChair", new Vector3(5.5f, 0.3f, 14f), new Vector3(0.6f, 0.6f, 0.6f), charred);

            // Junction room and the stairwell to the tower.
            WallX(env, "Junction_S", 18f, -7f, 7f, H, wall, 0f);
            WallZ(env, "Junction_W", -7f, 18f, 27f, H, wall);
            WallX(env, "Junction_N", 27f, -7f, 7f, H, wall);
            WallZ(env, "Junction_E", 7f, 18f, 27f, H, wall, 24f);
            WallX(env, "Stair_S", 22f, 7f, 10f, H, wall);
            WallX(env, "Stair_N", 26f, 7f, 10f, H, wall);
            WallZ(env, "Stair_E", 10f, 22f, 26f, H, wall);
            Box(env, "JunctionPanel", new Vector3(0f, 1.45f, 26.85f), new Vector3(4.2f, 1.5f, 0.2f), metal);
            Box(env, "CableTray", new Vector3(5.5f, 2.6f, 22.5f), new Vector3(0.4f, 0.15f, 9f), black);
            Box(env, "Crates", new Vector3(-5f, 0.5f, 25f), new Vector3(1.4f, 1f, 1.4f), wood);

            LevelDressing.Level4(env);
            BakeNavMesh(envGo, SceneName);

            // ------------------------------------------------------------------ lights + embers
            Transform lights = Group(null, "Lighting");
            foreach (Vector3 p in new[] { new Vector3(0f, 0.5f, 9.2f), new Vector3(-5f, 0.4f, 8.8f), new Vector3(6f, 0.7f, 11.8f) })
            {
                Light ember = PointLight(lights, "Embers", p, new Color(1f, 0.42f, 0.12f), 0.35f, 3.5f);
                ember.gameObject.AddComponent<FlickerLight>();
                Embers(lights, p + Vector3.up * 0.2f);
            }
            PointLight(lights, "Moonlight", new Vector3(-5f, 3f, 10f), new Color(0.45f, 0.55f, 0.8f), 0.3f, 6f);
            PointLight(lights, "Emergency_Red", new Vector3(0f, 3f, 22f), new Color(1f, 0.1f, 0.08f), 0.5f, 9f);
            Light work = PointLight(lights, "WorkLight", new Vector3(0f, 2.9f, 23f), new Color(1f, 0.92f, 0.8f), 1.1f, 10f);
            Light stair = PointLight(lights, "StairLight", new Vector3(8.5f, 2.9f, 24f), new Color(1f, 0.92f, 0.8f), 0.9f, 6f);
            var power = new GameObject("JunctionPower").AddComponent<PowerGroup>();
            SetValue(power, "powerFlag", "puzzle:l4_junction");
            SetArray(power, "lights", new Object[] { work, stair });
            Light arc = PointLight(lights, "ArcFlash", new Vector3(0f, 1.6f, 26.3f), new Color(0.75f, 0.85f, 1f), 3f, 7f);

            // ------------------------------------------------------------------ signs
            Transform signs = Group(null, "Signs");
            Sign(signs, "Sign_Studio", "STUDIO A - ON AIR", new Vector3(1.38f, 2.5f, 15f), 90f, 2f, new Color(0.45f, 0.18f, 0.14f));
            Sign(signs, "Sign_Stairs", "TOWER STAIRS", new Vector3(6.88f, 2.5f, 24f), -90f, 2.2f);
            Sign(signs, "Sign_Junction", "EMERGENCY JUNCTION J-4", new Vector3(0f, 2.45f, 26.74f), 0f, 1.8f, null, new Vector2(2.6f, 0.3f));

            // ------------------------------------------------------------------ story + interactables
            Transform story = Group(null, "Story");
            var events = story.gameObject.AddComponent<StoryEvents>();
            Transform items = Group(null, "Interactables");

            Door stairDoor = MakeDoor(items, "StairDoor", new Vector3(7f, 0f, 24f), -90f, "l4_stairs", "stair door", red,
                requiredFlag: "puzzle:l4_junction", flagMessage: "The stair door's maglock is engaged. No power reaches it.");

            Locker(items, "Locker_Office", new Vector3(-8.45f, 0f, 13f), 90f, locker);
            Locker(items, "Locker_Studio", new Vector3(9.45f, 0f, 15.6f), -90f, locker);
            Locker(items, "Locker_Junction", new Vector3(-6.45f, 0f, 20.5f), 90f, locker);
            Battery(items, new Vector3(-3.4f, 0.82f, 15.6f), "l4_bat1");
            Battery(items, new Vector3(-5f, 1.02f, 25f), "l4_bat2");

            NoteInteractable report = Note(items, "Note_FireReport", new Vector3(-7.4f, 0.81f, 5f), 8f, "l4_report", "Fire marshal's preliminary report",
                "BROADCAST WING - PRELIMINARY\n\nFEED A: melted at the panel. Unusable.\nFEED C: breaker scorched; shorts on contact.\n" +
                "WEST OFFICE: ceiling collapse - every line through it is severed.\nSTUDIO A: wiring burned out completely.\n\n" +
                "Cause: lightning strike on the mast, fire spread from the experimental rig.\n" +
                "One occupant unaccounted for. Search suspended.", paper);
            On(report, "onRead", events.AddClue, "Fire report: Feed A melted, Feed C shorts. Lines through the west office and Studio A are dead.");

            NoteInteractable schematic = WallNote(items, "Note_Schematic", new Vector3(-4f, 1.6f, 26.88f), 0f, "l4_schematic", "Junction J-4 schematic",
                "EMERGENCY JUNCTION J-4\n\nFEED A -> line 1 or 2\nFEED B -> line 2 or 3\nFEED C -> line 1 or 3\n\n" +
                "LINE 1: through Studio A\nLINE 2: through the west office ceiling\nLINE 3: along the east wall to the stairwell\n\n" +
                "LOAD: STUDIO / RIG / LIFT (stair door maglock + tower lift)", paper, new Vector2(0.6f, 0.55f));
            On(schematic, "onRead", events.AddClue, "Junction J-4: A->1/2, B->2/3, C->1/3. Line 1 Studio A, line 2 west office, line 3 east wall. LIFT powers the stairs.");

            TapePlayer recorder = Tape(items, "ScorchedRecorder", new Vector3(8.6f, 0.95f, 9.6f), 180f, "l4_recorder", "scorched recorder", black, new[]
            {
                L("RECORDING", "[Crackling flames. Someone coughing right against the microphone.]", 3f),
                L("MAYA (SCORCHED TAPE)", "Alex! The emergency exit - go! I'm right behind you!", 4f),
                L("MAYA (SCORCHED TAPE)", "...he's out. He's out. Okay. Okay.", 3.5f),
                L("MAYA (SCORCHED TAPE)", "The rig is still transmitting. If I leave now it keeps calling... it'll call him back here.", 5.5f),
                L("MAYA (SCORCHED TAPE)", "I'm going up to the tower to finish it. I'll record everything. Alex, don't come looking for-", 5.5f),
                L("RECORDING", "[The tape warps into silence.]", 2.5f),
            });
            On(recorder, "onFinished", events.Say, "ALEX|She went up. Alone. For me.|3");
            On(recorder, "onFinished", events.AddClue, "Maya's scorched tape: she got Alex out, then went up to the tower to stop the signal.");

            // Junction puzzle: three rotary selectors and an engage lever.
            var junctionGo = new GameObject("JunctionPuzzle");
            junctionGo.transform.SetParent(items, false);
            junctionGo.transform.position = new Vector3(0f, 1.45f, 26.6f);
            var junction = junctionGo.AddComponent<JunctionPuzzle>();
            SetValue(junction, "id", "l4_junction");
            Set(junction, "arcLight", arc);
            (string label, string[] options, float x)[] dials =
            {
                ("FEED", new[] { "A", "B", "C" }, -0.85f),
                ("LINE", new[] { "1", "2", "3" }, 0f),
                ("LOAD", new[] { "STUDIO", "RIG", "LIFT" }, 0.85f),
            };
            var selectors = new SelectorSwitch[dials.Length];
            for (int i = 0; i < dials.Length; i++)
            {
                GameObject body = Box(items, "Selector_" + dials[i].label, new Vector3(dials[i].x, 1.35f, 26.7f), new Vector3(0.22f, 0.22f, 0.1f), metal, isStatic: false);
                GameObject knob = Box(body.transform, "Knob", new Vector3(0f, 0f, -0.6f), new Vector3(0.25f, 0.85f, 0.4f), knobMat, collider: false, isStatic: false);
                TextMeshPro readout = Sign(signs, "Readout_" + dials[i].label, dials[i].label + "\n" + dials[i].options[0],
                    new Vector3(dials[i].x, 1.72f, 26.73f), 0f, 1.3f, new Color(0.75f, 0.72f, 0.65f), new Vector2(0.7f, 0.35f));
                selectors[i] = body.AddComponent<SelectorSwitch>();
                SetValue(selectors[i], "label", dials[i].label);
                SetStrings(selectors[i], "positions", dials[i].options);
                Set(selectors[i], "knob", knob.transform);
                Set(selectors[i], "readout", readout);
                SetValue(selectors[i], "degreesPerStep", 90f);
            }
            SetArray(junction, "selectors", selectors);
            SetInts(junction, "solution", new[] { 1, 2, 2 });
            ActionButton engage = Button3D(items, "EngageLever", new Vector3(1.75f, 1.3f, 26.7f), 0f, "l4_engage", "Throw the engage lever", red,
                new Vector3(0.12f, 0.42f, 0.12f));
            On(engage, "onPressed", junction.Engage);
            Sign(signs, "Label_Engage", "ENGAGE", new Vector3(1.75f, 1.62f, 26.73f), 0f, 1.1f, new Color(1f, 0.5f, 0.45f), new Vector2(0.6f, 0.15f));

            // ------------------------------------------------------------------ The Echo
            GameObject echoGo = Instantiate(echoPrefab, scene, new Vector3(0f, 0f, 22f), 180f);
            var echo = echoGo.GetComponent<EchoAIController>();
            Transform wpRoot = Group(null, "EchoWaypoints");
            Vector3[] wps =
            {
                new Vector3(0f, 0f, 22f), new Vector3(-4f, 0f, 24f), new Vector3(4f, 0f, 20.5f), new Vector3(0f, 0f, 15.5f),
                new Vector3(-5f, 0f, 14.5f), new Vector3(-6.5f, 0f, 6f), new Vector3(-3f, 0f, 5f), new Vector3(5f, 0f, 15f),
                new Vector3(6f, 0f, 10f), new Vector3(0f, 0f, 4f),
            };
            var wpTransforms = new Object[wps.Length];
            for (int i = 0; i < wps.Length; i++) wpTransforms[i] = Marker(wpRoot, "WP_" + i, wps[i], 0f);
            SetArray(echo, "waypoints", wpTransforms);
            SetValue(echo, "startActive", true);
            SetValue(echo, "settings.sightRange", 13f);
            SetValue(echo, "settings.sightAngle", 120f);
            SetValue(echo, "settings.hearingMultiplier", 1.35f);
            SetValue(echo, "settings.patrolSpeed", 1.45f);
            SetValue(echo, "settings.investigateSpeed", 2.3f);
            SetValue(echo, "settings.chaseSpeed", 3.5f);
            SetValue(echo, "settings.searchSeconds", 9f);
            SetValue(echo, "settings.loseSightSeconds", 3f);
            SetValue(echo, "settings.waypointPause", 1f);
            SetValue(echo, "settings.inspectChance", 0.65f);
            SetValue(echo, "settings.inspectSeconds", 5f);
            SetValue(echo, "settings.directorHintSeconds", 30f);
            SetValue(echo, "settings.lureInterval", 40f);

            // Escape run after the power is re-routed.
            var hunt = story.gameObject.AddComponent<EchoHuntTrigger>();
            Set(hunt, "echo", echo);
            Set(hunt, "spawn", Marker(story, "EchoHuntSpawn", new Vector3(-6.5f, 0f, 6f), 0f));
            SetValue(hunt, "delay", 2.5f);
            On(junction, "onSolved", stairDoor.Open);
            On(junction, "onSolved", events.SetObjective, "l4_run|The stair door is open. Get to the tower stairs - NOW!");
            On(junction, "onSolved", events.Say, "THE ECHO|You left me in the fire once. Not again!|3");
            On(junction, "onSolved", hunt.Begin);
            On(junction, "onSolved", events.Checkpoint, "l4_power");

            // ------------------------------------------------------------------ scripted beats
            TriggerZone collapse = Trigger<TriggerZone>(story, "Zone_Collapse", new Vector3(0f, 1f, 7.5f), new Vector3(3f, 2f, 1.5f));
            SetValue(collapse, "id", "l4_collapse");
            On(collapse, "onEnter", events.Say, "ALEX|The ceiling's come down. There has to be a way round - the offices.|3.5");
            On(collapse, "onEnter", events.SetObjectiveIfNotFlag, "puzzle:l4_junction|l4_detour|The corridor has collapsed. Go round through the west office.");

            TriggerZone studio = Trigger<TriggerZone>(story, "Zone_Studio", new Vector3(5.5f, 1f, 13f), new Vector3(7f, 2f, 7f));
            SetValue(studio, "id", "l4_studio");
            On(studio, "onEnter", events.PlayStatic);
            On(studio, "onEnter", events.Say, "THE ECHO (MAYA'S VOICE)|It was so hot, Alex. You didn't come back for me.|4");

            // Maya's silhouette in the smoke of the studio where she was trapped.
            ShadowSighting studioGhost = ScareKit.Apparition(story, "l4_studio_ghost", ScareKit.MayaGhost(), new Vector3(7.8f, 0f, 11.4f), -90f,
                glimpse: true, maxSeconds: 7f, vanishDistance: 2.5f);
            On(studio, "onEnter", studioGhost.Show);

            // West office: a burned beam gives way - loud enough to bring The Echo.
            Rigidbody beam = ScareKit.DropProp(story, "FallingBeam", new Vector3(-4.5f, 2.95f, 13.2f), new Vector3(0.25f, 0.25f, 2.6f), charred);
            ScareEvent beamFall = ScareKit.Scare(story, "l4_beam", new Vector3(-4.5f, 0.5f, 13.2f), 0.4f);
            ScareKit.Drops(beamFall, new Vector3(0f, -0.5f, 0.6f), beam);
            ScareKit.Voice(beamFall, "|[The ceiling groans. A burned beam crashes down.]|2.5", noiseRadius: 13f);
            TriggerZone officeBeam = Trigger<TriggerZone>(story, "Zone_OfficeBeam", new Vector3(-5f, 1f, 12.6f), new Vector3(6f, 2f, 1.5f));
            SetValue(officeBeam, "id", "l4_office_beam");
            On(officeBeam, "onEnter", beamFall.Play);

            TriggerZone junctionZone = Trigger<TriggerZone>(story, "Zone_Junction", new Vector3(0f, 1f, 19.5f), new Vector3(3f, 2f, 1.5f));
            SetValue(junctionZone, "id", "l4_junction_seen");
            On(junctionZone, "onEnter", events.SetObjectiveIfNotFlag, "puzzle:l4_junction|l4_power|The stair door has no power. Re-route it at the junction box.");

            LevelExit exit = Trigger<LevelExit>(story, "Exit_TowerStairs", new Vector3(9f, 1f, 24f), new Vector3(1.4f, 2f, 2.6f));
            SetValue(exit, "nextScene", GameFlowManager.Level5Scene);
            SetValue(exit, "requiredFlag", "puzzle:l4_junction");

            // ------------------------------------------------------------------ checkpoints + level
            CheckpointZone(story, "l4_office", new Vector3(-5f, 1f, 5f), 0f, new Vector3(3f, 2f, 2f));
            CheckpointZone(story, "l4_junction", new Vector3(0f, 1f, 18.9f), 0f, new Vector3(3f, 2f, 1f));
            CheckpointZone(story, "l4_power", new Vector3(5.5f, 0.02f, 24f), 90f, Vector3.one, triggerEnabled: false);

            Transform spawn = Marker(story, "PlayerStart", new Vector3(0f, 0.02f, -1f), 0f);
            var level = story.gameObject.AddComponent<LevelController>();
            SetValue(level, "levelNumber", 4);
            SetValue(level, "levelName", "The Burned Wing");
            Set(level, "startSpawn", spawn);
            SetValue(level, "startObjectiveId", "l4_through");
            SetValue(level, "startObjectiveText", "Get through the burned wing to the tower stairs");
            SetLines(level, "introLines", new[]
            {
                L("ALEX", "It still smells of smoke. After ten years.", 3f),
                L("ALEX", "This is where she was. The broadcasting wing.", 3f),
            });
            SetValue(level, "flashlightDrain", true);
            SetValue(level, "drainPerSecond", 0.75f);
            SetValue(level, "ambience", Ambience.RainAndWind);
            SetValue(level, "ambienceVolume", 0.4f);

            LevelDressing.Finish(4, items);
            Instantiate(playerPrefab, scene, spawn.position, 0f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();
            SaveScene(scene, ScenePath);
            Debug.Log("[Level4Builder] Built " + ScenePath);
        }

        /// <summary>Slow orange sparks drifting up from smouldering debris.</summary>
        private static void Embers(Transform parent, Vector3 position)
        {
            var go = new GameObject("EmberSparks");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            main.startColor = new Color(1f, 0.55f, 0.15f, 0.9f);
            main.gravityModifier = -0.05f;
            main.maxParticles = 30;
            var emission = ps.emission;
            emission.rateOverTime = 6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(1.2f, 0.2f, 1.2f);
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.7f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.25f, 0.05f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            colour.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = UnlitMat("Ember", new Color(1f, 0.6f, 0.2f, 1f), transparent: true);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
