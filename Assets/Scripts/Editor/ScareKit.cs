using LastSignal.Environment;
using LastSignal.Interaction;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Builders for scripted fear moments: apparitions (Maya's ghost, The Echo's shadow), falling props,
    /// footsteps overhead, bursting lamps and slamming doors. Prefabs are made from the CC0 mannequin.
    /// </summary>
    public static class ScareKit
    {
        public const string MayaGhostPath = "Assets/Prefabs/Story/MayaGhost.prefab";
        public const string EchoShadowPath = "Assets/Prefabs/Story/EchoShadow.prefab";

        [MenuItem("The Last Signal/Build/Rebuild Apparition Prefabs")]
        public static void RebuildPrefabs()
        {
            EnsureFolder("Assets/Prefabs/Story");
            // Maya: a pale, see-through girl, a little shorter than Alex, standing still.
            Material ghost = UnlitMat("MayaGhost", new Color(0.72f, 0.8f, 0.9f, 0.2f), transparent: true);
            BuildFigure(MayaGhostPath, "MayaGhost", ghost, new Vector3(0.86f, 0.86f, 0.86f), "Idle_Loop", 0.4f);
            // The Echo's shadow: tall, thin, black, for glimpses before it is ever seen moving.
            Material skin = Mat("EchoSkin", new Color(0.03f, 0.028f, 0.03f), smoothness: 0.22f);
            BuildFigure(EchoShadowPath, "EchoShadow", skin, new Vector3(0.82f, 1.17f, 0.82f), "Idle_Loop", 1.1f);
        }

        public static GameObject MayaGhost() => Load(MayaGhostPath);
        public static GameObject EchoShadow() => Load(EchoShadowPath);

        private static GameObject Load(string path)
        {
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p == null) { RebuildPrefabs(); p = AssetDatabase.LoadAssetAtPath<GameObject>(path); }
            return p;
        }

        private static void BuildFigure(string path, string name, Material mat, Vector3 scale, string clipName, float time)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabs.EchoModelPath);
            var root = new GameObject(name);
            if (model != null)
            {
                var body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                body.transform.localScale = scale;
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(GamePrefabs.EchoModelPath))
                    if (o is AnimationClip c && (c.name == "Rig|" + clipName || c.name == clipName)) { c.SampleAnimation(body, time); break; }
                // A frozen pose: no animator needed.
                Animator anim = body.GetComponent<Animator>();
                if (anim != null) Object.DestroyImmediate(anim);
                foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                    smr.sharedMaterials = mats;
                    smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
            else
            {
                Prim(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 0.95f, 0.4f), mat, collider: false);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        /// <summary>A figure that appears on Show() and vanishes when approached, glimpsed, or after a while.</summary>
        public static ShadowSighting Apparition(Transform parent, string id, GameObject prefab, Vector3 pos, float yaw,
            bool glimpse = true, float maxSeconds = 6f, float vanishDistance = 3f, string whisper = null, bool silent = false)
        {
            Transform holder = Group(parent, "Apparition_" + id, Vector3.zero);
            var figure = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            figure.transform.SetParent(holder, false);
            figure.transform.position = pos;
            figure.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            var s = holder.gameObject.AddComponent<ShadowSighting>();
            SetValue(s, "id", id);
            Set(s, "figure", figure);
            SetValue(s, "vanishDistance", vanishDistance);
            SetValue(s, "maxSeconds", maxSeconds);
            SetValue(s, "vanishWhenSeen", glimpse);
            SetValue(s, "silentVanish", silent);
            if (!string.IsNullOrEmpty(whisper)) SetValue(s, "whisper", whisper);
            return s;
        }

        public static ScareEvent Scare(Transform parent, string id, Vector3 pos, float delay = 0f)
        {
            Transform t = Group(parent, "Scare_" + id, pos);
            var e = t.gameObject.AddComponent<ScareEvent>();
            SetValue(e, "id", id);
            SetValue(e, "delay", delay);
            return e;
        }

        /// <summary>A loose prop held in place (kinematic) until a ScareEvent drops it.</summary>
        public static Rigidbody DropProp(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, Vector3 euler = default)
        {
            GameObject go = Box(parent, name, pos, size, mat, isStatic: false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.mass = 4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            return rb;
        }

        public static Object[] Path(Transform parent, string name, params Vector3[] points)
        {
            Transform root = Group(parent, name, Vector3.zero);
            var result = new Object[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = Marker(root, "Step_" + i, points[i], 0f);
            return result;
        }

        public static void Lights(ScareEvent e, params Light[] lights) => SetArray(e, "burstLights", lights);
        public static void Drops(ScareEvent e, Vector3 impulse, params Rigidbody[] bodies)
        {
            SetArray(e, "dropObjects", bodies);
            SetValue(e, "dropImpulse", impulse);
        }
        public static void Slams(ScareEvent e, params Door[] doors) => SetArray(e, "slamDoors", doors);
        public static void Steps(ScareEvent e, Object[] path, float interval = 0.45f)
        {
            SetArray(e, "footstepPath", path);
            SetValue(e, "footstepInterval", interval);
        }
        public static void Voice(ScareEvent e, string subtitle, bool staticBurst = false, float noiseRadius = 0f)
        {
            if (!string.IsNullOrEmpty(subtitle)) SetValue(e, "subtitle", subtitle);
            SetValue(e, "playStatic", staticBurst);
            SetValue(e, "noiseRadius", noiseRadius);
        }
    }
}
