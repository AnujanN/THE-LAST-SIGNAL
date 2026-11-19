using LastSignal.Environment;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Life in the dead building: a mouse that darts across the floor, a rat in the walls, spiders on the
    /// walls and one that drops on its thread, and dying fluorescent tubes
    /// flickering far down the corridors. Creatures are built from simple shapes (the mouse is the
    /// Poly Haven rat when it is present) so they cost almost nothing on the phone.
    /// </summary>
    public static class Wildlife
    {
        private const string Dir = "Assets/Prefabs/Ambient";

        /// <summary>Called from LevelDressing.Finish, after the lights exist.</summary>
        public static void Apply(int level, Rect[] rooms, int seed)
        {
            if (rooms == null || rooms.Length == 0) return;
            Physics.SyncTransforms();
            var rng = new System.Random(seed * 7 + level);
            float R() => (float)rng.NextDouble();

            var root = new GameObject("AmbientLife");
            var life = root.AddComponent<AmbientLife>();

            Critter mouse = Instance<Critter>(MousePrefab(), root.transform);
            Spider dropper = Instance<Spider>(SpiderPrefab(true), root.transform);
            foreach (Component c in new Component[] { mouse, dropper }) c.gameObject.SetActive(false);
            Set(life, "mouse", mouse);
            Set(life, "dropSpider", dropper);

            // Spiders sitting on walls and ceilings, scuttling now and then.
            GameObject spider = SpiderPrefab(false);
            int spiders = Mathf.Clamp(rooms.Length + 2, 5, 12), placed = 0;
            for (int attempt = 0; attempt < spiders * 6 && placed < spiders; attempt++)
            {
                Rect room = rooms[rng.Next(rooms.Length)];
                var p = new Vector3(Mathf.Lerp(room.xMin + 0.5f, room.xMax - 0.5f, R()), 1.3f + R() * 1.2f, Mathf.Lerp(room.yMin + 0.5f, room.yMax - 0.5f, R()));
                Vector3 dir = R() < 0.3f ? Vector3.up : Quaternion.Euler(0f, R() * 360f, 0f) * Vector3.forward;
                if (!Physics.Raycast(p, dir, out RaycastHit h, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (h.collider.bounds.size.y < 1.5f && dir != Vector3.up) continue; // walls, not furniture
                var s = (GameObject)PrefabUtility.InstantiatePrefab(spider);
                s.transform.SetParent(root.transform, false);
                Vector3 n = h.normal;
                Vector3 fwd = Vector3.ProjectOnPlane(Quaternion.Euler(0f, R() * 360f, 0f) * Vector3.forward + Vector3.up * 0.3f, n);
                s.transform.SetPositionAndRotation(h.point + n * 0.008f, Quaternion.LookRotation(fwd.sqrMagnitude > 1e-4f ? fwd : Vector3.Cross(n, Vector3.right), n));
                placed++;
            }

            // Dying tubes far down the longest spaces.
            GameObject tube = TubePrefab();
            Rect first = rooms[0];
            var byLength = (Rect[])rooms.Clone();
            System.Array.Sort(byLength, (a, b) => Mathf.Max(b.width, b.height).CompareTo(Mathf.Max(a.width, a.height)));
            int tubes = 0;
            foreach (Rect room in byLength)
            {
                if (tubes >= 3) break;
                Vector2 c = room.center;
                if (room.width > room.height * 1.8f || room.height > room.width * 1.8f)
                {
                    // A corridor: at its end farthest from where the level starts.
                    bool alongX = room.width > room.height;
                    Vector2 a = alongX ? new Vector2(room.xMin + 1.2f, c.y) : new Vector2(c.x, room.yMin + 1.2f);
                    Vector2 b = alongX ? new Vector2(room.xMax - 1.2f, c.y) : new Vector2(c.x, room.yMax - 1.2f);
                    c = Vector2.Distance(a, first.center) > Vector2.Distance(b, first.center) ? a : b;
                }
                var probe = new Vector3(c.x, 1.5f, c.y);
                if (!Physics.Raycast(probe, Vector3.up, out RaycastHit ceil, 4f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var t = (GameObject)PrefabUtility.InstantiatePrefab(tube);
                t.transform.SetParent(root.transform, false);
                bool alongXRoom = room.width > room.height;
                t.transform.SetPositionAndRotation(ceil.point, Quaternion.Euler(0f, alongXRoom ? 0f : 90f, 0f));
                tubes++;
            }
        }

        private static T Instance<T>(GameObject prefab, Transform parent) where T : Component
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, false);
            return go.GetComponent<T>();
        }

        private static AudioSource Voice(GameObject go, float maxDistance)
        {
            var a = go.AddComponent<AudioSource>();
            a.playOnAwake = false;
            a.spatialBlend = 1f;
            a.rolloffMode = AudioRolloffMode.Linear;
            a.minDistance = 1f;
            a.maxDistance = maxDistance;
            return a;
        }

        private static void NoShadows(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = false;
        }

        private static GameObject Save(GameObject root, string name)
        {
            EnsureFolder(Dir);
            NoShadows(root);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Dir + "/" + name + ".prefab");
            Object.DestroyImmediate(root);
            return prefab;
        }

        // ------------------------------------------------------------------ mouse

        private static GameObject MousePrefab()
        {
            var root = new GameObject("Mouse");
            var model = new GameObject("Model").transform;
            model.SetParent(root.transform, false);
            model.localScale = Vector3.one * 2.1f; // a big sewer rat, easy to spot
            GameObject rat = PropLibrary.Prefab("street_rat");
            if (rat != null)
            {
                var r = (GameObject)PrefabUtility.InstantiatePrefab(rat);
                PrefabUtility.UnpackPrefabInstance(r, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                r.transform.SetParent(model, false);
                foreach (Collider c in r.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
                // Face +Z (the run direction) along the model's long axis.
                Bounds b = new Bounds(r.transform.position, Vector3.zero);
                foreach (Renderer rr in r.GetComponentsInChildren<Renderer>()) b.Encapsulate(rr.bounds);
                if (b.size.x > b.size.z) r.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            }
            else
            {
                Material fur = Mat("MouseFur", new Color(0.2f, 0.17f, 0.15f), smoothness: 0.2f);
                Prim(model, "Body", PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), new Vector3(0.07f, 0.06f, 0.13f), fur, collider: false);
                Prim(model, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.045f, 0.075f), new Vector3(0.045f, 0.04f, 0.06f), fur, collider: false);
                Prim(model, "Tail", PrimitiveType.Cylinder, new Vector3(0f, 0.02f, -0.13f), new Vector3(0.008f, 0.07f, 0.008f), fur, new Vector3(80f, 0f, 0f), collider: false);
            }
            var critter = root.AddComponent<Critter>();
            Set(critter, "model", model);
            Set(critter, "voice", Voice(root, 12f));
            return Save(root, "Mouse");
        }

        // ------------------------------------------------------------------ spiders

        private static GameObject SpiderPrefab(bool dropper)
        {
            var root = new GameObject(dropper ? "DropSpider" : "Spider");
            Material shell = Mat("SpiderShell", new Color(0.03f, 0.025f, 0.02f), smoothness: 0.55f);
            Material silk = Mat("SpiderSilk", new Color(0.8f, 0.8f, 0.78f), smoothness: 0.4f);
            Transform body = Group(root.transform, "Body");
            float s = dropper ? 1.2f : 1f;
            Prim(body, "Abdomen", PrimitiveType.Sphere, new Vector3(0f, 0.014f, -0.025f) * s, new Vector3(0.035f, 0.028f, 0.045f) * s, shell, collider: false);
            Prim(body, "Head", PrimitiveType.Sphere, new Vector3(0f, 0.011f, 0.01f) * s, new Vector3(0.025f, 0.018f, 0.028f) * s, shell, collider: false);
            var legs = new Transform[8];
            for (int i = 0; i < 8; i++)
            {
                float side = i < 4 ? -1f : 1f;
                int k = i % 4;
                Transform pivot = Group(body, "Leg" + i, new Vector3(side * 0.008f, 0.012f, 0.012f - k * 0.006f) * s);
                pivot.localRotation = Quaternion.Euler(0f, side < 0f ? -90f + (k - 1.5f) * -32f : 90f + (k - 1.5f) * 32f, 0f);
                // Upper segment rises out to a knee, lower segment angles down to the surface.
                Prim(pivot, "Femur", PrimitiveType.Cylinder, new Vector3(0f, 0.012f, 0.022f) * s, new Vector3(0.004f, 0.026f, 0.004f) * s, shell, new Vector3(58f, 0f, 0f), collider: false);
                Prim(pivot, "Tibia", PrimitiveType.Cylinder, new Vector3(0f, 0.006f, 0.058f) * s, new Vector3(0.0035f, 0.028f, 0.0035f) * s, shell, new Vector3(130f, 0f, 0f), collider: false);
                legs[i] = pivot;
            }
            var spider = root.AddComponent<Spider>();
            Set(spider, "body", body);
            SetArray(spider, "legs", legs);
            if (dropper)
            {
                GameObject thread = Prim(root.transform, "Thread", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.004f, 0.5f, 0.004f), silk, collider: false);
                Set(spider, "thread", thread.transform);
                SetBool(spider, "dropper", true);
                Set(spider, "voice", Voice(root, 6f));
            }
            return Save(root, dropper ? "DropSpider" : "Spider");
        }

        // ------------------------------------------------------------------ flickering tube

        private static GameObject TubePrefab()
        {
            var root = new GameObject("FlickerTube");
            Material metal = Mat("TubeHousing", new Color(0.16f, 0.17f, 0.16f), smoothness: 0.35f);
            Material glass = Mat("TubeGlow", new Color(0.8f, 0.95f, 0.85f), emissive: true, smoothness: 0.8f);
            const float drop = 0.3f;
            Box(root.transform, "Housing", new Vector3(0f, -drop, 0f), new Vector3(1.3f, 0.05f, 0.16f), metal, collider: false, isStatic: false);
            GameObject tubeGo = Prim(root.transform, "Tube", PrimitiveType.Cylinder, new Vector3(0f, -drop - 0.045f, 0f), new Vector3(0.045f, 0.6f, 0.045f), glass, new Vector3(0f, 0f, 90f), collider: false);
            for (int i = -1; i <= 1; i += 2)
                Box(root.transform, "Chain", new Vector3(i * 0.5f, -drop / 2f, 0f), new Vector3(0.012f, drop, 0.012f), metal, collider: false, isStatic: false);
            // One end hangs lower: a fitting that came loose.
            root.transform.GetChild(0).localRotation = Quaternion.Euler(0f, 0f, 4f);
            tubeGo.transform.localRotation = Quaternion.Euler(0f, 0f, 94f);

            var lightGo = new GameObject("Light");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, -drop - 0.15f, 0f);
            var lamp = lightGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(0.8f, 0.95f, 0.85f);
            lamp.range = 6f;
            lamp.intensity = 1.8f;
            lamp.shadows = LightShadows.None;
            lamp.lightmapBakeType = LightmapBakeType.Realtime;
            SetLightLayers(lamp, LitLayers);

            var flicker = root.AddComponent<DistantFlicker>();
            Set(flicker, "lamp", lamp);
            Set(flicker, "tube", tubeGo.GetComponent<Renderer>());
            Set(flicker, "buzz", Voice(root, 14f));
            return Save(root, "FlickerTube");
        }

        // ------------------------------------------------------------------ serialized helpers

        private static void SetEnum(Object o, string field, int value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).enumValueIndex = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object o, string field, float value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object o, string field, bool value)
        {
            var so = new SerializedObject(o);
            so.FindProperty(field).boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
