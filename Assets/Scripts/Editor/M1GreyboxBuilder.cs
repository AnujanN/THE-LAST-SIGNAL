using LastSignal.Interaction;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// M1 vertical-slice test room (dev scene, not in the build list): movement, crouch overhang,
    /// step/ramp, light switch and the flashlight pickup. Uses the shared Player and HUD prefabs.
    /// </summary>
    public static class M1GreyboxBuilder
    {
        public const string ScenePath = "Assets/Scenes/Dev/M1_Greybox.unity";

        [MenuItem("The Last Signal/Dev/Build M1 Greybox Scene")]
        public static void BuildScene() => Build(false);

        [MenuItem("The Last Signal/Dev/Rebuild Player + HUD Prefabs and M1 Scene")]
        public static void RebuildAll() => Build(true);

        private static void Build(bool rebuildPrefabs)
        {
            GameObject playerPrefab = GamePrefabs.Player(rebuildPrefabs);
            GameObject hudPrefab = GamePrefabs.Hud(rebuildPrefabs);

            var scene = NewScene("M1_Greybox");
            Atmosphere(new Color(0.07f, 0.07f, 0.08f), Color.black, 0f);

            Material floor = Mat("Greybox_Floor", new Color(0.32f, 0.32f, 0.34f));
            Material wall = Mat("Greybox_Wall", new Color(0.55f, 0.55f, 0.57f));
            Material prop = Mat("Greybox_Prop", new Color(0.45f, 0.36f, 0.26f));
            Material accent = Mat("Greybox_Accent", new Color(0.9f, 0.75f, 0.2f));
            Material lampMat = Mat("Greybox_Lamp", new Color(1f, 0.93f, 0.75f), emissive: true);

            Transform env = Group(null, "Environment");
            const float h = 3f;
            Slab(env, "Floor_A", -6f, -6f, 6f, 6f, 0f, floor);
            Slab(env, "Ceiling_A", -6f, -6f, 6f, 6f, h, wall, ceiling: true);
            WallX(env, "Wall_N", 6f, -6f, 6f, h, wall);
            WallX(env, "Wall_S", -6f, -6f, 6f, h, wall);
            WallZ(env, "Wall_W", -6f, -6f, 6f, h, wall);
            WallZ(env, "Wall_E", 6f, -6f, 6f, h, wall, 0f);
            Box(env, "LowOverhang", new Vector3(-4.5f, 1.3f + (h - 1.3f) / 2f, -4f), new Vector3(3f, h - 1.3f, 3.6f), wall);
            Box(env, "Step", new Vector3(-3f, 0.125f, 3.5f), new Vector3(1.5f, 0.25f, 1.5f), prop);
            Box(env, "Ramp", new Vector3(2.5f, 0.35f, 4.2f), new Vector3(1.6f, 0.2f, 3f), prop).transform.rotation = Quaternion.Euler(-14f, 0f, 0f);
            Box(env, "Crate_1", new Vector3(3.5f, 0.5f, -3.5f), Vector3.one, prop);
            Box(env, "Crate_2", new Vector3(4.2f, 0.4f, -2.3f), new Vector3(0.8f, 0.8f, 0.8f), prop);
            Box(env, "LampFixture", new Vector3(0f, h - 0.05f, 0f), new Vector3(0.6f, 0.08f, 0.6f), lampMat, collider: false);
            Light lamp = PointLight(env, "CeilingLamp", new Vector3(0f, h - 0.3f, 0f), new Color(1f, 0.9f, 0.75f), 1.6f, 11f);

            Slab(env, "Floor_B", 6f, -3f, 12f, 3f, 0f, floor);
            Slab(env, "Ceiling_B", 6f, -3f, 12f, 3f, h, wall, ceiling: true);
            WallX(env, "WallB_N", 3f, 6f, 12f, h, wall);
            WallX(env, "WallB_S", -3f, 6f, 12f, h, wall);
            WallZ(env, "WallB_E", 12f, -3f, 3f, h, wall);
            Box(env, "Table", new Vector3(10.6f, 0.375f, 0f), new Vector3(0.8f, 0.75f, 1.4f), prop);

            Transform interactables = Group(null, "Interactables");
            Pickup(interactables, "Pickup_Flashlight", new Vector3(10.6f, 0.8f, 0f), "flashlight", "Flashlight", "", accent,
                PrimitiveType.Cylinder, new Vector3(0.09f, 0.13f, 0.09f)).transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            GameObject plate = Box(interactables, "LightSwitch", new Vector3(5.86f, 1.3f, -1.3f), new Vector3(0.06f, 0.26f, 0.18f), wall, isStatic: false);
            GameObject handle = Box(interactables, "SwitchHandle", new Vector3(5.81f, 1.3f, -1.3f), new Vector3(0.07f, 0.09f, 0.045f), prop, collider: false, isStatic: false);
            var sw = plate.AddComponent<SwitchInteractable>();
            SetArray(sw, "lights", new Object[] { lamp });
            Set(sw, "handle", handle.transform);
            SetValue(sw, "handleOnEuler", new Vector3(0f, 0f, 25f));
            SetValue(sw, "handleOffEuler", new Vector3(0f, 0f, -25f));

            Instantiate(playerPrefab, scene, new Vector3(-2.5f, 0f, 1.5f), 90f);
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            EventSystemObject();

            SaveScene(scene, ScenePath);
            Debug.Log("[M1GreyboxBuilder] Built " + ScenePath);
        }
    }
}
