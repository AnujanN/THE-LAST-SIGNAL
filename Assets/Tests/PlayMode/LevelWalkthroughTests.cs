using System.Collections;
using System.Linq;
using LastSignal.Enemy;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.Puzzles;
using LastSignal.Save;
using LastSignal.UI;
using LastSignal.Utilities;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace LastSignal.Tests
{
    /// <summary>
    /// Loads the real level scenes and drives each critical path in order (pickups, puzzles,
    /// doors, recordings, exits) to prove every level can be completed without softlocks,
    /// that objectives advance, and that the scene flow reaches the ending.
    /// Interactions are invoked directly rather than by aiming the camera.
    /// </summary>
    public class LevelWalkthroughTests
    {
        private GameFlowManager flow;
        private PlayerInteractor interactor;
        private PlayerController player;
        private InventoryManager inventory;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            yield return null;
            flow = GameFlowManager.Instance;
            Assert.IsNotNull(flow, "GameFlowManager should bootstrap automatically");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            ModalStack.Reset();
            HidingSpot.ResetStatic();
            SaveManager.Delete();
            yield return null;
        }

        private IEnumerator StartLevel(string scene)
        {
            flow.StartFresh(scene);
            yield return WaitForScene(scene);
            CachePlayer();
            interactor.InputRouter.KeyboardMouseEnabled = false;
        }

        private IEnumerator WaitForScene(string scene, float timeout = 20f)
        {
            float end = Time.realtimeSinceStartup + timeout;
            while ((SceneManager.GetActiveScene().name != scene || flow.IsLoading) && Time.realtimeSinceStartup < end)
                yield return null;
            Assert.AreEqual(scene, SceneManager.GetActiveScene().name, "Scene did not load: " + scene);
            Assert.IsFalse(flow.IsLoading);
            yield return null;
            yield return null;
        }

        private void CachePlayer()
        {
            interactor = Object.FindAnyObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "Player missing from scene");
            player = interactor.GetComponent<PlayerController>();
            inventory = interactor.GetComponent<InventoryManager>();
        }

        private static T Get<T>(string name) where T : Component
        {
            T found = Object.FindObjectsByType<T>(FindObjectsInactive.Include).FirstOrDefault(c => c.name == name);
            Assert.IsNotNull(found, typeof(T).Name + " '" + name + "' not found");
            return found;
        }

        private static string Objective => ObjectiveManager.Instance.CurrentId;

        private IEnumerator WaitUntilTrue(System.Func<bool> condition, float timeoutSeconds, string what)
        {
            float end = Time.realtimeSinceStartup + timeoutSeconds;
            while (!condition() && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue(condition(), "Timed out waiting for: " + what);
        }

        private IEnumerator WalkInto(Vector3 position)
        {
            player.Teleport(position);
            yield return new WaitForFixedUpdate();
            yield return null;
            yield return null;
        }

        // ================================================================== Level 1

        [UnityTest, Timeout(120000)]
        public IEnumerator Level1_CriticalPath_ReachesLevel2()
        {
            yield return StartLevel(GameFlowManager.Level1Scene);
            Assert.AreEqual("l1_enter", Objective);

            Get<PickupItem>("Pickup_Flashlight").Interact(interactor);
            Assert.IsTrue(inventory.Has("flashlight"));
            Assert.AreEqual("l1_search", Objective);

            Door control = Get<Door>("ControlDoor");
            control.Interact(interactor);
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(control.IsOpen, "Control room must stay locked without power");

            foreach (string t in new[] { "Tape_1", "Tape_2", "Tape_3" }) Get<PickupItem>(t).Interact(interactor);
            Assert.AreEqual(3, inventory.Count("tape_fragment"));
            Assert.AreEqual("l1_power", Objective);

            BreakerPuzzle breaker = Get<BreakerPuzzle>("BreakerPuzzle");
            Assert.IsFalse(breaker.TryRestorePower(), "Lever must fail without the fuse");
            Get<PickupItem>("Pickup_MainFuse").Interact(interactor);
            ItemSlot socket = Get<ItemSlot>("FuseSocket");
            socket.Interact(interactor);
            Assert.IsTrue(socket.IsInstalled);
            Assert.IsFalse(breaker.TryRestorePower(), "Lever must fail with the starting breaker pattern");

            // Storm rule (sticker) + circuit diagram (office): tower circuits B3, B5, B6 down, the rest up.
            bool[] solution = { true, true, false, true, false, false };
            for (int i = 0; i < solution.Length; i++) Get<BreakerSwitch>("Breaker_B" + (i + 1)).Set(!solution[i]);
            Assert.IsFalse(breaker.TryRestorePower(), "Inverted pattern must trip");
            for (int i = 0; i < solution.Length; i++) Get<BreakerSwitch>("Breaker_B" + (i + 1)).Set(solution[i]);
            Assert.IsTrue(breaker.TryRestorePower());
            Assert.IsTrue(WorldFlags.Has("power:station"));
            Assert.AreEqual("l1_tape", Objective);
            Assert.IsTrue(Get<Light>("Corridor").enabled, "Powered lights should come on");
            Assert.IsTrue(SaveManager.HasSave, "Power checkpoint should save");

            control.Interact(interactor);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(control.IsOpen, "Control room needs the keypad code, not just power");
            // Keypad code = the time the last person signed out on the storm night (visitor log: 02:47).
            CodeLock keypad = Get<CodeLock>("ControlKeypad");
            Assert.IsFalse(keypad.TrySubmit("1234"), "Wrong code must be rejected");
            Assert.IsTrue(keypad.TrySubmit("0247"));
            Assert.IsTrue(WorldFlags.Has("code:l1_control"));
            yield return WaitUntilTrue(() => control.IsOpen, 3f, "control door opens after the code");

            TapePlayer tape = Get<TapePlayer>("TapePlayer");
            Time.timeScale = 20f;
            tape.Interact(interactor);
            yield return WaitUntilTrue(() => tape.HasPlayed, 15f, "level 1 tape finished");
            Time.timeScale = 1f;
            Assert.AreEqual("l1_key", Objective);

            PickupItem key = Get<PickupItem>("Pickup_BasementKey");
            yield return null;
            Assert.IsTrue(key.gameObject.activeSelf, "Key should appear after the tape");
            key.Interact(interactor);
            Assert.AreEqual("l1_basement", Objective);

            Door basement = Get<Door>("BasementDoor");
            basement.Interact(interactor);
            yield return WaitUntilTrue(() => basement.IsOpen, 3f, "basement door open");
            Assert.AreEqual("l1_down", Objective);

            yield return WalkInto(new Vector3(4.6f, 0.05f, 6f));
            yield return WaitForScene(GameFlowManager.Level2Scene);
            CachePlayer();
            Assert.IsTrue(inventory.Has("flashlight"), "Flashlight should carry over to Level 2");
            Assert.AreEqual("l2_find_gen", Objective);
        }

        // ================================================================== Level 2

        [UnityTest, Timeout(120000)]
        public IEnumerator Level2_CriticalPath_ReachesLevel3()
        {
            yield return StartLevel(GameFlowManager.Level2Scene);
            EchoAIController echo2 = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo2.Started, 5f, "Echo start-up");
            echo2.Deactivate();
            Assert.IsTrue(interactor.Flashlight.DrainEnabled, "Battery should drain in Level 2");

            GeneratorPuzzle generator = Get<GeneratorPuzzle>("GeneratorPuzzle");
            generator.PressStep(0);
            Assert.AreEqual(0, generator.Progress, "Start-up must not progress with parts missing");

            foreach (string p in new[] { "Part_SparkPlug", "Part_Fuse30A", "Part_DriveBelt" }) Get<PickupItem>(p).Interact(interactor);
            foreach (string s in new[] { "Slot_SparkPlug", "Slot_Fuse", "Slot_Belt" }) Get<ItemSlot>(s).Interact(interactor);
            Assert.AreEqual("l2_start", Objective);

            generator.PressStep(1);
            Assert.AreEqual(0, generator.Progress, "Wrong first step should reset");
            generator.PumpPrimer();
            Assert.AreEqual(0, generator.Pumps, "Priming does nothing with the fuel off");
            generator.PressStep(0);
            generator.PressStep(1);
            Assert.AreEqual(0, generator.Progress, "Choke without fuel pressure stalls");
            generator.PressStep(0);
            for (int i = 0; i < 6; i++) generator.PumpPrimer();
            Assert.AreEqual(0, generator.Progress, "Over-priming floods and resets");
            generator.PressStep(0);
            generator.PressStep(2);
            Assert.AreEqual(0, generator.Progress, "Out-of-order step should reset");

            // Torn card (wall) + missing piece (pump room): fuel, prime into the green, choke, starter.
            generator.PressStep(0);
            for (int i = 0; i < 3; i++) generator.PumpPrimer();
            Assert.IsTrue(generator.PressureInGreen);
            generator.PressStep(1);
            generator.PressStep(2);
            Assert.IsTrue(generator.IsSolved);
            Assert.IsTrue(WorldFlags.Has("power:generator"));
            Assert.AreEqual("l2_tape", Objective);

            ActionButton lift = Get<ActionButton>("LiftButton");
            lift.Interact(interactor);
            yield return null;
            Assert.IsFalse(lift.Used, "Elevator must wait for the recording");

            TapePlayer tape = Get<TapePlayer>("EmergencyRecorder");
            Time.timeScale = 20f;
            tape.Interact(interactor);
            yield return WaitUntilTrue(() => tape.HasPlayed, 15f, "level 2 recording finished");
            Assert.AreEqual("l2_lift", Objective);

            lift.Interact(interactor);
            Assert.IsTrue(lift.Used);
            yield return WaitForScene(GameFlowManager.Level3Scene, 30f);
            Time.timeScale = 1f;
            Assert.AreEqual("l3q_explore", Objective);
        }

        // ================================================================== Level 3 (The Living Quarters)

        [UnityTest, Timeout(120000)]
        public IEnumerator Level3_Quarters_CriticalPath_ReachesLevel4()
        {
            yield return StartLevel(GameFlowManager.Level3Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started, 5f, "Echo start-up");
            echo.Deactivate();
            Assert.AreEqual("l3q_explore", Objective);

            Door fireDoor = Get<Door>("FireDoor");
            fireDoor.Interact(interactor);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(fireDoor.IsOpen, "Fire door needs Dad's key");

            // Logbook: the safe combination is a date (the storm night), month first.
            Get<NoteInteractable>("Note_DadLogbook").Interact(interactor);
            Object.FindAnyObjectByType<StoryHUD>().CloseNote();
            Assert.AreEqual("l3q_safe", Objective);
            Assert.IsTrue(WorldFlags.Has("clue:Dad's safe: the night everything ended, month first (MM DD)."), "Journal should record the clue");

            CodeLock safe = Get<CodeLock>("SafeDial");
            PickupItem key = Get<PickupItem>("Pickup_FireKey");
            Assert.IsFalse(key.gameObject.activeSelf, "Key is locked in the safe");
            Assert.IsFalse(safe.TrySubmit("1410"), "Day-first is wrong (month first)");
            Assert.IsTrue(safe.TrySubmit("1014"));
            yield return null;
            Assert.IsTrue(key.gameObject.activeSelf, "Key appears when the safe opens");
            Assert.IsTrue(SaveManager.HasSave, "Safe checkpoint should save");

            key.Interact(interactor);
            Assert.AreEqual("l3q_fire", Objective);
            fireDoor.Interact(interactor);
            yield return WaitUntilTrue(() => fireDoor.IsOpen, 3f, "fire door open");
            Assert.AreEqual("l3q_exit", Objective);

            yield return WalkInto(new Vector3(0f, 0.05f, 26.2f));
            yield return WaitForScene(GameFlowManager.Level4Scene);
            CachePlayer();
            Assert.AreEqual("l4_through", Objective);
        }

        // ================================================================== Level 4 (The Burned Wing)

        [UnityTest, Timeout(120000)]
        public IEnumerator Level4_BurnedWing_CriticalPath_ReachesLevel5()
        {
            yield return StartLevel(GameFlowManager.Level4Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started, 5f, "Echo start-up");
            echo.Deactivate();

            Door stairs = Get<Door>("StairDoor");
            stairs.Interact(interactor);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(stairs.IsOpen, "Stair door has no power yet");

            JunctionPuzzle junction = Get<JunctionPuzzle>("JunctionPuzzle");
            SelectorSwitch feed = Get<SelectorSwitch>("Selector_FEED");
            SelectorSwitch line = Get<SelectorSwitch>("Selector_LINE");
            SelectorSwitch load = Get<SelectorSwitch>("Selector_LOAD");
            Assert.IsFalse(junction.TryEngage(), "Starting route must fail");
            // Feed A is melted: A -> line 2 -> lift still fails.
            feed.Set(0); line.Set(1); load.Set(2);
            Assert.IsFalse(junction.TryEngage());
            // Feed C -> line 3 is wired but C shorts.
            feed.Set(2); line.Set(2); load.Set(2);
            Assert.IsFalse(junction.TryEngage());
            // Using the selectors through the interactor cycles them.
            feed.Interact(interactor);
            Assert.AreEqual("A", feed.Current, "Selectors wrap around");
            feed.Interact(interactor);
            Assert.AreEqual("B", feed.Current);
            Assert.IsTrue(junction.TryEngage(), "FEED B -> LINE 3 -> LIFT is the only live route");
            Assert.IsTrue(WorldFlags.Has("puzzle:l4_junction"));
            yield return WaitUntilTrue(() => stairs.IsOpen, 3f, "stair door opens when powered");
            Assert.AreEqual("l4_run", Objective);

            yield return WalkInto(new Vector3(9f, 0.05f, 24f));
            yield return WaitForScene(GameFlowManager.Level5Scene);
            CachePlayer();
            Assert.AreEqual("l3_components", Objective);
        }

        // ================================================================== Level 3

        [UnityTest, Timeout(120000)]
        public IEnumerator Level5_Tower_CriticalPath_ReachesEnding()
        {
            yield return StartLevel(GameFlowManager.Level5Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started, 5f, "Echo start-up");
            echo.Deactivate();

            FrequencyPuzzle freq = Get<FrequencyPuzzle>("FrequencyPuzzle");
            freq.Transmit();
            Assert.IsFalse(freq.IsSolved, "Cannot transmit without modules");

            foreach (string m in new[] { "Module_RFAmp", "Module_Coupler", "Module_Tuner" }) Get<PickupItem>(m).Interact(interactor);
            foreach (string s in new[] { "Slot_RFAmp", "Slot_Coupler", "Slot_Tuner" }) Get<ItemSlot>(s).Interact(interactor);
            Assert.AreEqual("l3_tune", Objective);

            freq.Transmit();
            Assert.IsFalse(freq.IsSolved, "Wrong frequency must not solve");
            for (int i = 0; i < 7; i++) freq.Tune(1f);
            for (int i = 0; i < 2; i++) freq.Tune(0.1f);
            Assert.AreEqual(103.7f, freq.Frequency, 0.001f);
            freq.Transmit();
            Assert.IsTrue(freq.IsSolved);
            Assert.AreEqual("l3_recording", Objective);
            Assert.IsFalse(echo.gameObject.activeSelf, "The Echo withdraws during the final recording");

            ActionButton shutdown = Get<ActionButton>("MasterShutdown");
            shutdown.Interact(interactor);
            Assert.IsFalse(shutdown.Used, "Shutdown must wait for the final recording");

            TapePlayer finalTape = Get<TapePlayer>("FinalRecording");
            Time.timeScale = 20f;
            finalTape.Interact(interactor);
            yield return null;
            Assert.IsTrue(player.MovementLocked, "Player is held still during the final recording");
            yield return WaitUntilTrue(() => finalTape.HasPlayed, 20f, "final recording finished");
            Time.timeScale = 1f;
            Assert.IsFalse(player.MovementLocked);
            Assert.IsFalse(finalTape.CanInteract(interactor), "Final recording is not replayable/skippable");
            Assert.AreEqual("l3_shutdown", Objective);

            shutdown.Interact(interactor);
            Assert.IsTrue(WorldFlags.Has(FinaleSequence.ShutdownFlag));
            Assert.AreEqual("l3_escape", Objective);
            Door escape = Get<Door>("EscapeDoor");
            yield return WaitUntilTrue(() => escape.IsOpen, 3f, "escape door open");
            Assert.IsFalse(Get<Light>("Chamber").enabled, "Tower lights die at shutdown");

            yield return WalkInto(new Vector3(-15.2f, 0.05f, 25f));
            yield return WaitForScene(GameFlowManager.EndingScene);
            Assert.IsNotNull(Object.FindAnyObjectByType<EndingSequence>());
            Assert.IsFalse(SaveManager.HasSave, "Finishing the game clears the save");
        }

        // ================================================================== The Echo

        /// <summary>
        /// Regression: after a catch, retrying at the Level 3 chamber checkpoint used to put The Echo
        /// right in front of the player. It must restart far away and give a short grace period.
        /// </summary>
        [UnityTest, Timeout(90000)]
        public IEnumerator Echo_RestartsFarAway_AfterCheckpointRetry()
        {
            yield return StartLevel(GameFlowManager.Level5Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started, 5f, "Echo start-up");

            yield return WalkInto(new Vector3(0f, 0.05f, 15f));
            yield return WaitUntilTrue(() => flow.Live.checkpointId == "l3_chamber", 3f, "chamber checkpoint reached");

            flow.RetryCheckpoint();
            yield return WaitForScene(GameFlowManager.Level5Scene);
            CachePlayer();
            interactor.InputRouter.KeyboardMouseEnabled = false;
            echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started, 5f, "Echo restart");

            float distance = Vector3.Distance(echo.transform.position, player.transform.position);
            Assert.Greater(distance, 14f, "The Echo must restart far from the checkpoint spawn (was " + distance.ToString("F1") + " m)");
            Assert.IsTrue(echo.InGracePeriod, "Grace period after a restart");
            Assert.IsFalse(echo.CanSeePlayerNow);
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(flow.IsGameOver, "Player must not be caught right after respawning");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Echo_Chases_LosesHiddenPlayer_Investigates_AndCatches()
        {
            yield return StartLevel(GameFlowManager.Level2Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started && !echo.InGracePeriod, 8f, "Echo start-up grace period over");

            // This test covers sight and search; hiding-spot inspection has its own tests.
            echo.Settings.inspectChance = 0f;
            HidingSpot locker = Get<HidingSpot>("Locker_A2");
            yield return ChaseThenHide(echo, locker);

            // Hiding -> it cannot see the player and gives up to search; no catch.
            Assert.IsFalse(echo.CanSeePlayer(), "Hidden player must not be visible");
            yield return WaitUntilTrue(() => echo.Current == EchoAIController.State.Search, 6f, "search after losing the player");
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(flow.IsGameOver, "A hidden player must never be caught");
            locker.Leave();

            // Noise -> investigate.
            yield return WalkInto(new Vector3(0f, 0.05f, 18f));
            echo.Deactivate();
            echo.WarpTo(new Vector3(-12f, 0f, 5.5f));
            echo.Activate();
            yield return null;
            NoiseSystem.Emit(new Vector3(-8f, 0f, 5.5f), 9f);
            yield return null;
            Assert.AreEqual(EchoAIController.State.Investigate, echo.Current);

            // Standing still in front of it -> caught -> game over -> retry restores play.
            yield return WalkInto(new Vector3(-9f, 0.05f, 5.5f));
            echo.WarpTo(new Vector3(-11f, 0f, 5.5f));
            echo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return WaitUntilTrue(() => flow.IsGameOver, 8f, "caught by The Echo");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(ModalStack.AnyOpen, "Game over screen should be shown");
            flow.RetryCheckpoint();
            yield return WaitForScene(GameFlowManager.Level2Scene);
            Assert.IsFalse(flow.IsGameOver);
            Assert.IsFalse(ModalStack.AnyOpen);
        }

        /// <summary>Level 2 corridor: The Echo sees the player and chases; the player hides in a locker.</summary>
        private IEnumerator ChaseThenHide(EchoAIController echo, HidingSpot locker)
        {
            yield return WalkInto(new Vector3(3f, 0.05f, 5.5f));
            echo.Activate();
            echo.WarpTo(new Vector3(-5f, 0f, 5.5f));
            echo.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            yield return WaitUntilTrue(() => echo.Current == EchoAIController.State.Chase, 3f, "chase after seeing the player");
            locker.Enter(player);
            Assert.IsTrue(HidingSpot.PlayerHidden);
        }

        private IEnumerator WaitForInspectionAtLocker(EchoAIController echo, HidingSpot locker)
        {
            yield return WaitUntilTrue(() => echo.Current == EchoAIController.State.Inspect, 8f, "The Echo decides to check the hiding spot");
            yield return WaitUntilTrue(() => echo.IsInspectingPlayer
                && Vector3.Distance(echo.transform.position, locker.InspectPosition) < 1.2f, 10f, "The Echo arrives at the locker");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Echo_InspectsHidingSpot_HoldingBreathKeepsPlayerSafe()
        {
            yield return StartLevel(GameFlowManager.Level2Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started && !echo.InGracePeriod, 8f, "Echo start-up grace period over");
            echo.Settings.inspectChance = 1f;
            echo.Settings.inspectSeconds = 3f;
            HidingSpot locker = Get<HidingSpot>("Locker_A2");
            PlayerBreath breath = player.GetComponent<PlayerBreath>();
            Assert.IsNotNull(breath, "Player prefab needs PlayerBreath");

            yield return ChaseThenHide(echo, locker);
            yield return WaitForInspectionAtLocker(echo, locker);
            interactor.InputRouter.SetHoldBreath(true);
            yield return null;
            yield return null;
            Assert.IsTrue(breath.Holding, "Holding the breath button while hidden holds the breath");

            yield return WaitUntilTrue(() => echo.Current != EchoAIController.State.Inspect, 8f, "The Echo gives up the inspection");
            interactor.InputRouter.SetHoldBreath(false);
            Assert.IsFalse(flow.IsGameOver, "Holding breath must keep a hidden player safe");
            Assert.IsTrue(HidingSpot.PlayerHidden);
            Assert.Greater(breath.Breath01, 0f, "Breath should not have run out in a 3 s inspection");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator Echo_InspectsHidingSpot_FindsBreathingPlayer()
        {
            yield return StartLevel(GameFlowManager.Level2Scene);
            EchoAIController echo = Get<EchoAIController>("Echo");
            yield return WaitUntilTrue(() => echo.Started && !echo.InGracePeriod, 8f, "Echo start-up grace period over");
            echo.Settings.inspectChance = 1f;
            echo.Settings.inspectSeconds = 3f;
            HidingSpot locker = Get<HidingSpot>("Locker_A2");

            yield return ChaseThenHide(echo, locker);
            yield return WaitForInspectionAtLocker(echo, locker);
            yield return WaitUntilTrue(() => flow.IsGameOver, 4f, "breathing player found in the locker");
            Assert.IsFalse(HidingSpot.PlayerHidden, "The Echo pulls the player out of the hiding spot");
            flow.RetryCheckpoint();
            yield return WaitForScene(GameFlowManager.Level2Scene);
            Assert.IsFalse(flow.IsGameOver);
        }
    }
}
