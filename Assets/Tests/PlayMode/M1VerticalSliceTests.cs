using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using LastSignal.Inventory;
using LastSignal.Player;
using LastSignal.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LastSignal.Tests
{
    /// <summary>
    /// M1 checks: movement, sprint, crouch + headroom, pickup + flashlight, switch,
    /// joystick output and pause. Uses the real Player prefab on a generated floor.
    /// </summary>
    public class M1VerticalSliceTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameObject player;
        private PlayerInputRouter router;
        private PlayerController controller;
        private CharacterController cc;
        private PlayerInteractor interactor;
        private InventoryManager inventory;
        private FlashlightController flashlight;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            SpawnCube(new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f)); // floor, top at y = 0

#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
#else
            GameObject prefab = null;
#endif
            Assert.IsNotNull(prefab, "Player prefab missing at " + PlayerPrefabPath);
            player = Track(Object.Instantiate(prefab, new Vector3(0f, 0.05f, 0f), Quaternion.identity));
            router = player.GetComponent<PlayerInputRouter>();
            router.KeyboardMouseEnabled = false; // only simulated input in tests
            controller = player.GetComponent<PlayerController>();
            cc = player.GetComponent<CharacterController>();
            interactor = player.GetComponent<PlayerInteractor>();
            inventory = player.GetComponent<InventoryManager>();
            flashlight = player.GetComponentInChildren<FlashlightController>();

            // Let the controller settle onto the floor before any test places targets relative to the camera.
            float timeout = Time.realtimeSinceStartup + 2f;
            int groundedFrames = 0;
            while (groundedFrames < 5 && Time.realtimeSinceStartup < timeout)
            {
                yield return new WaitForFixedUpdate();
                groundedFrames = controller.IsGrounded ? groundedFrames + 1 : 0;
            }
            yield return null;
            Assert.IsTrue(controller.IsGrounded, "Player did not land on the test floor");
            Assert.Less(player.transform.position.y, 0.1f, "Player should stand on the floor, not on leftovers");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (router != null) router.SetMobileMove(Vector2.zero);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            foreach (GameObject go in spawned)
                if (go != null) Object.Destroy(go);
            spawned.Clear();
            // Destroy is deferred to the end of the frame; make sure objects are gone
            // before the next test spawns a player in the same place.
            yield return null;
            yield return new WaitForFixedUpdate();
        }

        [Test]
        public void Prefab_ReferencesAreWired()
        {
            Assert.IsNotNull(router);
            Assert.IsNotNull(controller);
            Assert.IsNotNull(interactor);
            Assert.IsNotNull(inventory);
            Assert.IsNotNull(flashlight);
            Assert.IsNotNull(player.GetComponentInChildren<Camera>());
            Assert.IsNotNull(player.GetComponentInChildren<Light>(true), "Flashlight spot light missing");
        }

        [UnityTest]
        public IEnumerator Move_Forward_MovesPlayer()
        {
            float startZ = player.transform.position.z;
            router.SetMobileMove(Vector2.up);
            yield return new WaitForSeconds(0.6f);
            router.SetMobileMove(Vector2.zero);
            Assert.Greater(player.transform.position.z - startZ, 0.6f);
            Assert.IsTrue(controller.IsGrounded, "Player should stay on the floor");
        }

        [UnityTest]
        public IEnumerator Sprint_IsFasterThanWalk()
        {
            float walk = 0f, sprint = 0f;
            yield return Measure(false, d => walk = d);
            yield return Measure(true, d => sprint = d);
            Assert.Greater(sprint, walk * 1.3f, $"walk {walk:F2} m, sprint {sprint:F2} m");
        }

        private IEnumerator Measure(bool withSprint, System.Action<float> result)
        {
            if (withSprint != router.MobileSprintToggled) router.ToggleMobileSprint();
            Vector3 start = player.transform.position;
            router.SetMobileMove(Vector2.up);
            yield return new WaitForSeconds(0.8f);
            result(Vector3.Distance(start, player.transform.position));
            router.SetMobileMove(Vector2.zero);
            if (router.MobileSprintToggled) router.ToggleMobileSprint();
            yield return new WaitForSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator Crouch_LowersHeight_AndHeadroomBlocksStanding()
        {
            router.Press(PlayerAction.Crouch);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(controller.IsCrouching);
            Assert.Less(cc.height, 1.1f);

            // Bottom of the slab at 1.3 m: too low to stand, high enough to stay crouched.
            GameObject ceiling = SpawnCube(player.transform.position + Vector3.up * 1.55f, new Vector3(3f, 0.5f, 3f));
            yield return new WaitForFixedUpdate();

            router.Press(PlayerAction.Crouch);
            yield return new WaitForSeconds(0.3f);
            Assert.IsTrue(controller.IsCrouching, "Should not stand up under a low ceiling");

            Object.Destroy(ceiling);
            yield return new WaitForFixedUpdate();
            router.Press(PlayerAction.Crouch);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(controller.IsCrouching);
            Assert.Greater(cc.height, 1.7f);
        }

        [UnityTest]
        public IEnumerator Flashlight_RequiresPickup_ThenToggles()
        {
            Light spot = flashlight.GetComponent<Light>();
            router.Press(PlayerAction.Flashlight);
            yield return null;
            yield return null;
            Assert.IsFalse(flashlight.IsOn, "Flashlight must not work before it is picked up");

            GameObject item = SpawnInFrontOfCamera(1.3f);
            item.AddComponent<PickupItem>().Configure("flashlight", "Flashlight");
            yield return WaitForPhysicsThenFrame();
            Assert.IsNotNull(interactor.Current, "Interactor should target the pickup. " + DescribeView() + ", item " + item.transform.position);
            Assert.AreEqual("Pick up Flashlight", interactor.Current.GetPrompt(interactor));

            router.Press(PlayerAction.Interact);
            yield return null;
            yield return null;
            Assert.IsTrue(inventory.Has("flashlight"));
            Assert.IsFalse(item.activeSelf, "Pickup should be hidden after collecting");
            Assert.IsTrue(flashlight.IsOwned);

            router.Press(PlayerAction.Flashlight);
            yield return null;
            yield return null;
            Assert.IsTrue(flashlight.IsOn);
            Assert.IsTrue(spot.enabled);

            router.Press(PlayerAction.Flashlight);
            yield return null;
            yield return null;
            Assert.IsFalse(flashlight.IsOn);
            Assert.IsFalse(spot.enabled);
        }

        [UnityTest]
        public IEnumerator Switch_TogglesLight_ThroughInteractor()
        {
            GameObject box = SpawnInFrontOfCamera(1.5f);
            var lamp = Track(new GameObject("Lamp")).AddComponent<Light>();
            var sw = box.AddComponent<SwitchInteractable>();
            typeof(SwitchInteractable).GetField("lights", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(sw, new[] { lamp });
            yield return WaitForPhysicsThenFrame();
            Assert.IsTrue(lamp.enabled);
            Assert.AreSame(sw, interactor.Current, DescribeView());
            Assert.AreEqual("Turn off light", sw.GetPrompt(interactor));

            router.Press(PlayerAction.Interact);
            yield return null;
            yield return null;
            Assert.IsFalse(sw.IsOn);
            Assert.IsFalse(lamp.enabled);
            Assert.AreEqual("Turn on light", sw.GetPrompt(interactor));
        }

        [UnityTest]
        public IEnumerator Joystick_DragToEdge_GivesFullForwardInput()
        {
            Track(new GameObject("EventSystem", typeof(EventSystem)));
            var canvasGo = Track(new GameObject("Canvas", typeof(RectTransform), typeof(Canvas)));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var area = new GameObject("Area", typeof(RectTransform), typeof(Image));
            area.transform.SetParent(canvasGo.transform, false);
            var areaRect = (RectTransform)area.transform;
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = areaRect.offsetMax = Vector2.zero;
            var baseGo = new GameObject("Base", typeof(RectTransform));
            baseGo.transform.SetParent(area.transform, false);
            var knobGo = new GameObject("Knob", typeof(RectTransform));
            knobGo.transform.SetParent(baseGo.transform, false);

            area.SetActive(false); // assign fields before Awake
            var stick = area.AddComponent<VirtualJoystick>();
            typeof(VirtualJoystick).GetField("baseRect", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stick, (RectTransform)baseGo.transform);
            typeof(VirtualJoystick).GetField("knob", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(stick, (RectTransform)knobGo.transform);
            area.SetActive(true);
            stick.Router = router;
            yield return null;

            Vector2 centre = RectTransformUtility.WorldToScreenPoint(null, areaRect.position);
            var e = new PointerEventData(EventSystem.current) { pointerId = 1, position = centre };
            stick.OnPointerDown(e);
            e.position = centre + new Vector2(0f, Screen.height); // far beyond the radius
            stick.OnDrag(e);
            yield return null;
            yield return null;

            Assert.AreEqual(0f, stick.Value.x, 0.01f);
            Assert.AreEqual(1f, stick.Value.y, 0.01f);
            Assert.AreEqual(1f, router.Move.y, 0.01f);

            stick.OnPointerUp(e);
            yield return null;
            yield return null;
            Assert.AreEqual(Vector2.zero, router.Move);
        }

        [UnityTest]
        public IEnumerator Pause_FreezesTime_AndBlocksInput()
        {
            var pause = Track(new GameObject("Pause")).AddComponent<PauseController>();
            yield return null;

            router.SetMobileMove(Vector2.up);
            router.Press(PlayerAction.Pause);
            yield return null;
            yield return null;
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.AreEqual(Vector2.zero, router.Move, "Movement must be blocked while paused");

            router.Press(PlayerAction.Pause);
            yield return null;
            yield return null;
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        private GameObject SpawnInFrontOfCamera(float distance)
        {
            Transform cam = player.GetComponentInChildren<Camera>().transform;
            return SpawnCube(cam.position + cam.forward * distance, Vector3.one * 0.4f);
        }

        /// <summary>
        /// CreatePrimitive spawns at the origin. Without an explicit sync the physics scene keeps that
        /// pose until the next fixed step, and the CharacterController gets pushed out of the phantom cube.
        /// </summary>
        private GameObject SpawnCube(Vector3 position, Vector3 scale)
        {
            GameObject go = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            go.transform.position = position;
            go.transform.localScale = scale;
            Physics.SyncTransforms();
            return go;
        }

        /// <summary>
        /// Colliders moved from code only reach the physics scene on the next fixed step,
        /// so wait for one before expecting the interactor's ray to see them.
        /// </summary>
        private static IEnumerator WaitForPhysicsThenFrame()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
        }

        private string DescribeView()
        {
            Transform cam = player.GetComponentInChildren<Camera>().transform;
            string hit = Physics.Raycast(cam.position, cam.forward, out RaycastHit h, 5f, ~0, QueryTriggerInteraction.Collide)
                ? $"{h.collider.name} at {h.distance:F2} m"
                : "nothing";
            return $"cam {cam.position} fwd {cam.forward}, player {player.transform.position}, ray hit {hit}, cursor {Cursor.lockState}";
        }

        private GameObject Track(GameObject go)
        {
            spawned.Add(go);
            return go;
        }
    }
}
