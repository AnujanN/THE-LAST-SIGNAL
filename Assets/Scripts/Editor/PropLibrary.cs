using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Real props (Poly Haven, CC0) in Assets/Art/Models/Props/&lt;id&gt;/: builds a URP material from each
    /// model's colour + normal maps and a prefab with a fitted box collider, then places them in levels.
    /// Burned variants use a darkened copy of the material.
    /// </summary>
    public static class PropLibrary
    {
        public const string ModelDir = "Assets/Art/Models/Props";
        public const string PrefabDir = "Assets/Prefabs/Props";
        private const string Version = "props-v1";

        private static readonly Dictionary<string, Material> charred = new Dictionary<string, Material>();

        [MenuItem("The Last Signal/Build/Rebuild Prop Prefabs")]
        public static void RebuildAll()
        {
            if (!AssetDatabase.IsValidFolder(ModelDir)) return;
            foreach (string dir in Directory.GetDirectories(ModelDir))
                BuildPrefab(Path.GetFileName(dir), force: true);
        }

        public static GameObject Prefab(string id)
        {
            string path = PrefabDir + "/" + id + ".prefab";
            GameObject p = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (p != null && AssetImporter.GetAtPath(path) is AssetImporter ai && ai.userData == Version) return p;
            return BuildPrefab(id, force: false);
        }

        private static GameObject BuildPrefab(string id, bool force)
        {
            string folder = ModelDir + "/" + id;
            string fbx = folder + "/" + id + ".fbx";
            var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
            if (importer == null) return null;
            if (importer.materialImportMode != ModelImporterMaterialImportMode.None || importer.importAnimation)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.None;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }

            Material mat = PropMaterial(id, folder);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            if (model == null) return null;

            EnsureFolder(PrefabDir);
            var root = new GameObject(id);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            PrefabUtility.UnpackPrefabInstance(inst, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            inst.name = "Model";
            inst.transform.SetParent(root.transform, false);
            var bounds = new Bounds();
            bool first = true;
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            {
                var mats = new Material[r.sharedMaterials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = mat;
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                if (first) { bounds = r.bounds; first = false; } else bounds.Encapsulate(r.bounds);
            }
            // Sit the model on the floor at the root's origin.
            inst.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            var box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, bounds.extents.y, 0f);
            box.size = bounds.size;

            string path = PrefabDir + "/" + id + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            AssetImporter.GetAtPath(path).userData = Version;
            AssetDatabase.WriteImportSettingsIfDirty(path);
            return prefab;
        }

        private static Material PropMaterial(string id, string folder)
        {
            string tex = folder + "/textures/" + id;
            Texture2D diff = Load(tex + "_diff_1k.jpg", normal: false);
            Texture2D nor = Load(tex + "_nor_gl_1k.exr", normal: true);
            // Slightly darkened: decades of dust and grime.
            Material m = Mat("Prop_" + id, new Color(0.74f, 0.72f, 0.68f), smoothness: 0.2f);
            if (diff != null) m.SetTexture("_BaseMap", diff);
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D Load(string path, bool normal)
        {
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return null;
            bool changed = false;
            TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (ti.textureType != type) { ti.textureType = type; changed = true; }
            if (ti.maxTextureSize != 1024) { ti.maxTextureSize = 1024; changed = true; }
            if (ti.textureCompression != TextureImporterCompression.Compressed) { ti.textureCompression = TextureImporterCompression.Compressed; changed = true; }
            if (changed) ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>
        /// Replace a box stand-in with a real prop. The box keeps its collider (so surfaces that items sit on,
        /// routes and the NavMesh are unchanged) but is hidden; the prop is fitted to its footprint and height,
        /// tiled along long counters and stacked for piles of crates.
        /// </summary>
        public static void Dress(Transform env, string boxName, string id, int tiles = 1, int stack = 1,
            bool fitHeight = true, float yawOffset = 0f, bool charredLook = false)
        {
            Transform box = FindDeep(env, boxName);
            GameObject prefab = Prefab(id);
            if (box == null || prefab == null) { Debug.LogWarning("[PropLibrary] Cannot dress " + boxName + " with " + id); return; }
            var bc = prefab.GetComponent<BoxCollider>();
            Vector3 n = bc != null ? bc.size : Vector3.one;
            Vector3 size = box.lossyScale;
            size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
            var r = box.GetComponent<Renderer>();
            if (r != null) r.enabled = false;

            bool boxLongX = size.x >= size.z, propLongX = n.x >= n.z;
            float yaw = box.eulerAngles.y + (boxLongX == propLongX ? 0f : 90f) + yawOffset;
            float longLen = boxLongX ? size.x : size.z, shortLen = boxLongX ? size.z : size.x;
            float propLong = propLongX ? n.x : n.z, propShort = propLongX ? n.z : n.x;
            float sLong = longLen / tiles / Mathf.Max(0.01f, propLong), sShort = shortLen / Mathf.Max(0.01f, propShort);
            float layer = size.y / stack;
            float sY = fitHeight ? layer / Mathf.Max(0.01f, n.y) : Mathf.Clamp((sLong + sShort) * 0.5f, 0.75f, 1.25f);
            Vector3 scale = propLongX ? new Vector3(sLong, sY, sShort) : new Vector3(sShort, sY, sLong);
            Vector3 longDir = boxLongX ? box.right : box.forward;
            Vector3 bottom = box.position - Vector3.up * size.y / 2f;
            for (int t = 0; t < tiles; t++)
            for (int k = 0; k < stack; k++)
            {
                float offset = (t + 0.5f) / tiles - 0.5f;
                Vector3 pos = bottom + longDir * offset * longLen + Vector3.up * (k * (fitHeight ? layer : n.y * sY));
                GameObject go = Place(box.parent, id, pos, yaw + (k % 2 == 1 ? 7f : 0f), collider: false, charredLook: charredLook);
                if (go != null) go.transform.localScale = scale;
            }
        }

        /// <summary>Hide a box stand-in that a dressed prop already covers (keeps its collider).</summary>
        public static void Hide(Transform env, string boxName)
        {
            Transform box = FindDeep(env, boxName);
            if (box != null && box.GetComponent<Renderer>() is Renderer r) r.enabled = false;
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name) return t;
            foreach (Transform c in t)
            {
                Transform f = FindDeep(c, name);
                if (f != null) return f;
            }
            return null;
        }

        /// <summary>Place a prop (world position, yaw; optional tilt for fallen things; charred = burned look).</summary>
        public static GameObject Place(Transform parent, string id, Vector3 pos, float yaw, Vector3 tilt = default,
            float scale = 1f, bool collider = true, bool charredLook = false)
        {
            GameObject prefab = Prefab(id);
            if (prefab == null) return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(tilt.x, yaw, tilt.z);
            go.transform.localScale = Vector3.one * scale;
            go.isStatic = true;
            foreach (Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            if (!collider) { var c = go.GetComponent<Collider>(); if (c != null) Object.DestroyImmediate(c); }
            if (charredLook)
            {
                Material burned = Charred(id);
                foreach (Renderer r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = burned;
                    r.sharedMaterials = mats;
                }
            }
            return go;
        }

        private static Material Charred(string id)
        {
            if (charred.TryGetValue(id, out Material m) && m != null) return m;
            Material src = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/Prop_" + id + ".mat");
            m = Mat("Prop_" + id + "_Charred", new Color(0.22f, 0.2f, 0.19f), smoothness: 0.08f);
            if (src != null)
            {
                m.SetTexture("_BaseMap", src.GetTexture("_BaseMap"));
                m.SetTexture("_BumpMap", src.GetTexture("_BumpMap"));
                if (src.GetTexture("_BumpMap") != null) m.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(m);
            charred[id] = m;
            return m;
        }
    }
}
