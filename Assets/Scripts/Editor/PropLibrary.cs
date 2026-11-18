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
        private const string Version = "props-v3";

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
            // Models with several texture sets keep their material slot names so each slot gets its own textures.
            List<string> sets = TextureSets(folder);
            ModelImporterMaterialImportMode mode = sets.Count > 1
                ? ModelImporterMaterialImportMode.ImportViaMaterialDescription : ModelImporterMaterialImportMode.None;
            if (importer.materialImportMode != mode || importer.importAnimation)
            {
                importer.materialImportMode = mode;
                importer.importAnimation = false;
                importer.SaveAndReimport();
            }

            Material mat = PropMaterial(sets.Count > 0 ? sets[0] : id, folder);
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
            // One texture set: merge all sub-meshes into one so each copy is a single draw call on the phone.
            if (sets.Count <= 1)
                foreach (MeshFilter mf in inst.GetComponentsInChildren<MeshFilter>())
                    if (mf.sharedMesh != null && mf.sharedMesh.subMeshCount > 1)
                        mf.sharedMesh = SingleSubmesh(mf.sharedMesh, id);
            foreach (Renderer r in inst.GetComponentsInChildren<Renderer>())
            {
                if (sets.Count <= 1 && r.sharedMaterials.Length > 1) r.sharedMaterials = new[] { mat };
                Material[] slots = r.sharedMaterials;
                var mats = new Material[slots.Length];
                for (int i = 0; i < mats.Length; i++)
                    mats[i] = sets.Count > 1 && slots[i] != null ? PropMaterial(BestSet(sets, slots[i].name), folder) : mat;
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

        private static Mesh SingleSubmesh(Mesh source, string id)
        {
            string dir = PrefabDir + "/Meshes";
            EnsureFolder(dir);
            string path = dir + "/" + id + "_" + source.name.Replace('/', '_').Replace(':', '_') + ".asset";
            var tris = new List<int>();
            for (int i = 0; i < source.subMeshCount; i++) tris.AddRange(source.GetTriangles(i));
            Mesh m = Object.Instantiate(source);
            m.name = source.name + "_merged";
            m.subMeshCount = 1;
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        /// <summary>Texture set base names in a model folder ("name" for name_diff_1k.jpg/png).</summary>
        private static List<string> TextureSets(string folder)
        {
            var sets = new List<string>();
            string dir = folder + "/textures";
            if (!Directory.Exists(dir)) return sets;
            foreach (string f in Directory.GetFiles(dir))
            {
                string name = Path.GetFileName(f);
                int k = name.IndexOf("_diff_1k", System.StringComparison.Ordinal);
                if (k > 0 && !name.EndsWith(".meta")) sets.Add(name.Substring(0, k));
            }
            sets.Sort((a, b) => a.Length.CompareTo(b.Length));
            return sets;
        }

        /// <summary>The texture set whose name best matches a material slot name (longest shared prefix).</summary>
        private static string BestSet(List<string> sets, string slot)
        {
            string s = slot.ToLowerInvariant();
            string best = sets[0];
            int bestScore = -1;
            foreach (string set in sets)
            {
                string t = set.ToLowerInvariant();
                int n = 0;
                while (n < s.Length && n < t.Length && s[n] == t[n]) n++;
                int score = n * 10 - Mathf.Abs(t.Length - s.Length);
                if (score > bestScore) { bestScore = score; best = set; }
            }
            return best;
        }

        private static Texture2D LoadAny(string basePath, bool normal)
        {
            foreach (string ext in new[] { ".jpg", ".png", ".exr" })
            {
                Texture2D t = Load(basePath + ext, normal);
                if (t != null) return t;
            }
            return null;
        }

        private static Material PropMaterial(string id, string folder)
        {
            string tex = folder + "/textures/" + id;
            Texture2D diff = LoadAny(tex + "_diff_1k", normal: false);
            Texture2D nor = LoadAny(tex + "_nor_gl_1k", normal: true);
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
