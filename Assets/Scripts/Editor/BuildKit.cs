using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using LastSignal.Audio;
using LastSignal.GameFlow;
using LastSignal.Interaction;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Shared helpers for the scene builders: greybox geometry, interactables, UI widgets,
    /// serialized-field and UnityEvent wiring, NavMesh baking.
    /// </summary>
    public static class BuildKit
    {
        public const string MaterialDir = "Assets/Art/Materials/Greybox";
        public const string SpriteDir = "Assets/UI/HUD";
        public const string GeneratedDir = "Assets/Scenes/Generated";
        public const float DoorWidth = 1.6f;
        public const float DoorHeight = 2.2f;
        public const float WallThickness = 0.2f;

        // ================================================================== assets

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static Material Mat(string name, Color color, bool emissive = false, float smoothness = 0.15f)
        {
            EnsureFolder(MaterialDir);
            string path = MaterialDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            if (emissive)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 1.5f);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            }
            ApplySurface(m, name);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ================================================================== realistic surfaces

        public const string TextureDir = "Assets/Art/Textures/PolyHaven";

        /// <summary>Greybox material name -> photo-scanned Poly Haven texture (CC0), tint and smoothness.</summary>
        private static readonly Dictionary<string, (string tex, Color tint, float smooth)> Surfaces =
            new Dictionary<string, (string, Color, float)>
            {
                { "L1_Ground", ("asphalt_02", new Color(0.55f, 0.55f, 0.58f), 0.35f) },
                { "L1_Floor", ("concrete_floor_worn_001", new Color(0.8f, 0.78f, 0.75f), 0.2f) },
                { "L1_Wall", ("beige_wall_001", new Color(0.62f, 0.6f, 0.56f), 0.1f) },
                { "L1_Facade", ("brick_wall_001", new Color(0.5f, 0.48f, 0.48f), 0.15f) },
                { "L1_Wood", ("brown_planks_05", new Color(0.7f, 0.65f, 0.6f), 0.2f) },
                { "L1_Door", ("brown_planks_05", new Color(0.55f, 0.48f, 0.42f), 0.25f) },
                { "Fabric", ("dirty_carpet", new Color(0.55f, 0.45f, 0.48f), 0.05f) },
                { "Metal", ("rusty_metal_02", new Color(0.75f, 0.75f, 0.78f), 0.35f) },
                { "FusePanel", ("painted_metal_shutter", new Color(0.6f, 0.68f, 0.6f), 0.3f) },
                { "Red", ("painted_metal_shutter", new Color(0.65f, 0.15f, 0.12f), 0.3f) },
                { "L2_Concrete", ("damaged_concrete_floor_02", new Color(0.6f, 0.6f, 0.58f), 0.3f) },
                { "L2_Wall", ("concrete_wall_004", new Color(0.55f, 0.57f, 0.55f), 0.15f) },
                { "L2_Pipe", ("rusty_metal_02", new Color(0.7f, 0.6f, 0.55f), 0.4f) },
                { "L2_Locker", ("painted_metal_shutter", new Color(0.45f, 0.55f, 0.58f), 0.35f) },
                { "Generator", ("painted_metal_shutter", new Color(0.45f, 0.55f, 0.42f), 0.35f) },
                { "L3_Floor", ("concrete_slab_wall", new Color(0.55f, 0.55f, 0.6f), 0.25f) },
                { "L3_Wall", ("concrete_panels", new Color(0.55f, 0.56f, 0.62f), 0.15f) },
                { "L3_Dark", ("rusty_metal_02", new Color(0.35f, 0.35f, 0.4f), 0.4f) },
                { "L3_Door", ("painted_metal_shutter", new Color(0.45f, 0.48f, 0.55f), 0.35f) },
                { "Quarters_Floor", ("dirty_carpet", new Color(0.6f, 0.52f, 0.5f), 0.05f) },
                { "Quarters_Wall", ("plaster_grey_04", new Color(0.62f, 0.6f, 0.58f), 0.1f) },
                { "Quarters_Tiles", ("dirty_tiles", new Color(0.75f, 0.75f, 0.72f), 0.45f) },
                { "Burned_Floor", ("burned_ground_01", new Color(0.6f, 0.55f, 0.52f), 0.1f) },
                { "Burned_Wall", ("black_painted_planks", new Color(0.4f, 0.37f, 0.35f), 0.1f) },
                { "Ceiling", ("concrete_slab_wall", new Color(0.45f, 0.45f, 0.47f), 0.1f) },
            };

        private static void ApplySurface(Material m, string name)
        {
            if (!Surfaces.TryGetValue(name, out var s)) return;
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/" + s.tex + "_diff.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/" + s.tex + "_nor.jpg");
            if (diff == null) return;
            m.SetTexture("_BaseMap", diff);
            m.SetColor("_BaseColor", s.tint);
            m.SetFloat("_Smoothness", s.smooth);
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
        }

        public static bool IsTextured(Material m) => m != null && m.GetTexture("_BaseMap") != null;

        private static readonly Dictionary<string, Mesh> cubeCache = new Dictionary<string, Mesh>();

        /// <summary>
        /// Cube mesh whose UVs are scaled to the box's real size (metres / tile), so textures keep their
        /// real-world scale on every wall, floor and prop instead of stretching. Embedded in the scene.
        /// </summary>
        public static Mesh WorldCube(Vector3 size, float tileMetres = 2f)
        {
            string key = $"{size.x:F2}_{size.y:F2}_{size.z:F2}_{tileMetres:F1}";
            if (cubeCache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            var verts = new List<Vector3>();
            var norms = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            void Face(Vector3 n, Vector3 u, Vector3 v, float su, float sv)
            {
                int i = verts.Count;
                Vector3 c = n * 0.5f;
                verts.Add(c - u * 0.5f - v * 0.5f); verts.Add(c + u * 0.5f - v * 0.5f);
                verts.Add(c + u * 0.5f + v * 0.5f); verts.Add(c - u * 0.5f + v * 0.5f);
                for (int k = 0; k < 4; k++) norms.Add(n);
                uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(su, 0f));
                uvs.Add(new Vector2(su, sv)); uvs.Add(new Vector2(0f, sv));
                tris.AddRange(new[] { i, i + 2, i + 1, i, i + 3, i + 2 });
            }
            float t = Mathf.Max(0.1f, tileMetres);
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x / t, size.y / t);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x / t, size.y / t);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z / t, size.y / t);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z / t, size.y / t);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x / t, size.z / t);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x / t, size.z / t);

            var mesh = new Mesh { name = "WorldCube_" + key };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            cubeCache[key] = mesh;
            return mesh;
        }

        public static Material UnlitMat(string name, Color color, bool transparent = false)
        {
            EnsureFolder(MaterialDir);
            string path = MaterialDir + "/" + name + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find(transparent ? "Universal Render Pipeline/Particles/Unlit" : "Universal Render Pipeline/Unlit"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.SetColor("_BaseColor", color);
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        public enum SpriteShape { Disc, Ring, Square, Vignette, Soft }

        public static Sprite EnsureSprite(string name, SpriteShape shape)
        {
            EnsureFolder(SpriteDir);
            string path = SpriteDir + "/" + name + ".png";
            if (!File.Exists(path))
            {
                const int size = 128;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float c = (size - 1) / 2f;
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                    float a;
                    Color col = Color.white;
                    switch (shape)
                    {
                        case SpriteShape.Square: a = 1f; break;
                        case SpriteShape.Disc: a = Mathf.Clamp01(c - d); break;
                        case SpriteShape.Ring: a = Mathf.Clamp01(c - d) * Mathf.Clamp01(d - (c - 8f)); break;
                        case SpriteShape.Soft: a = Mathf.Pow(Mathf.Clamp01(1f - d / c), 2f); break;
                        default:
                            a = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((d / c - 0.55f) / 0.45f));
                            col = new Color(0.35f, 0f, 0f);
                            break;
                    }
                    col.a = a;
                    tex.SetPixel(x, y, col);
                }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ================================================================== scene

        private static string currentSceneName = "Scene";

        public static Scene NewScene(string sceneName = "Scene")
        {
            currentSceneName = sceneName;
            cubeCache.Clear();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            return scene;
        }

        /// <summary>
        /// Horror atmosphere: ambient light is cut hard (the flashlight must matter), fog thickened, and a
        /// colour-grading volume (desaturated, tinted, high contrast, vignette) is added for the scene.
        /// </summary>
        public static void Atmosphere(Color ambient, Color fogColor, float fogDensity, float darkness = 0.3f)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = ambient * darkness;
            RenderSettings.fog = fogDensity > 0f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor * 0.6f;
            RenderSettings.fogDensity = fogDensity * 1.6f;
            Color grade = ambient.maxColorComponent > 0f ? ambient / ambient.maxColorComponent : Color.white;
            PostFx(currentSceneName, Color.Lerp(Color.white, grade, 0.35f));
        }

        public static void PostFx(string sceneName, Color filter, float saturation = -35f, float contrast = 22f, float vignette = 0.42f)
        {
            EnsureFolder(GeneratedDir);
            string name = string.IsNullOrEmpty(sceneName) ? "Scene" : sceneName;
            string path = GeneratedDir + "/" + name + "_PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(-0.15f);
            ca.contrast.Override(contrast);
            ca.saturation.Override(saturation);
            ca.colorFilter.Override(filter);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.5f);
            vig.color.Override(Color.black);
            AssetDatabase.CreateAsset(profile, path);
            foreach (VolumeComponent c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
            AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        public static void SaveScene(Scene scene, string path)
        {
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, path);
        }

        public static GameObject Instantiate(GameObject prefab, Scene scene, Vector3 pos, float yaw)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            return go;
        }

        public static void EventSystemObject() =>
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        /// <summary>Bake a NavMesh from the colliders under root and save it as an asset.</summary>
        public static NavMeshSurface BakeNavMesh(GameObject root, string sceneName)
        {
            EnsureFolder(GeneratedDir);
            // Not "??": in the Editor GetComponent returns a fake-null object for missing components.
            NavMeshSurface surface = root.GetComponent<NavMeshSurface>();
            if (surface == null) surface = root.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.BuildNavMesh();
            string path = GeneratedDir + "/" + sceneName + "_NavMesh.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            return surface;
        }

        // ================================================================== geometry

        public static Transform Group(Transform parent, string name, Vector3 position = default)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            return go.transform;
        }

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat,
            bool collider = true, bool isStatic = true)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (IsTextured(mat)) go.GetComponent<MeshFilter>().sharedMesh = WorldCube(size);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = isStatic;
            return go;
        }

        public static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale,
            Material mat, Vector3 euler = default, bool collider = true)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        /// <summary>Floor or ceiling slab covering an XZ rectangle; top surface at y (floor) or bottom at y (ceiling).</summary>
        public static GameObject Slab(Transform parent, string name, float xMin, float zMin, float xMax, float zMax,
            float y, Material mat, bool ceiling = false, float thickness = 0.2f)
        {
            float cy = ceiling ? y + thickness / 2f : y - thickness / 2f;
            return Box(parent, name, new Vector3((xMin + xMax) / 2f, cy, (zMin + zMax) / 2f),
                new Vector3(xMax - xMin, thickness, zMax - zMin), mat);
        }

        /// <summary>Wall running along X at the given Z, with 1.6 m doorways centred on doorXs.</summary>
        public static void WallX(Transform parent, string name, float z, float x0, float x1, float height, Material mat,
            params float[] doorXs)
        {
            if (x1 < x0) { float t = x0; x0 = x1; x1 = t; }
            Segments(x0, x1, doorXs, (a, b) =>
                Box(parent, name, new Vector3((a + b) / 2f, height / 2f, z), new Vector3(b - a, height, WallThickness), mat),
                c => Box(parent, name + "_Lintel", new Vector3(c, DoorHeight + (height - DoorHeight) / 2f, z),
                    new Vector3(DoorWidth, height - DoorHeight, WallThickness), mat));
        }

        /// <summary>Wall running along Z at the given X, with 1.6 m doorways centred on doorZs.</summary>
        public static void WallZ(Transform parent, string name, float x, float z0, float z1, float height, Material mat,
            params float[] doorZs)
        {
            if (z1 < z0) { float t = z0; z0 = z1; z1 = t; }
            Segments(z0, z1, doorZs, (a, b) =>
                Box(parent, name, new Vector3(x, height / 2f, (a + b) / 2f), new Vector3(WallThickness, height, b - a), mat),
                c => Box(parent, name + "_Lintel", new Vector3(x, DoorHeight + (height - DoorHeight) / 2f, c),
                    new Vector3(WallThickness, height - DoorHeight, DoorWidth), mat));
        }

        private static void Segments(float start, float end, float[] doors, Action<float, float> wall, Action<float> lintel)
        {
            var list = new List<float>(doors ?? new float[0]);
            list.Sort();
            float cursor = start;
            foreach (float d in list)
            {
                float a = d - DoorWidth / 2f, b = d + DoorWidth / 2f;
                if (a - cursor > 0.01f) wall(cursor, a);
                lintel(d);
                cursor = b;
            }
            if (end - cursor > 0.01f) wall(cursor, end);
        }

        public static Light PointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            // Scenes now use dark, realistic albedo and physical falloff; scale the authored values so
            // working lamps still read as lit rooms (the darkness comes from lamps being off or broken).
            l.intensity = intensity * 4f;
            l.range = range;
            l.shadows = LightShadows.None;
            return l;
        }

        public static Light DirectionalLight(Transform parent, string name, Vector3 euler, Color color, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(euler);
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = color;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        /// <summary>World-space sign text (3D TextMeshPro).</summary>
        public static TextMeshPro Sign(Transform parent, string name, string text, Vector3 pos, float yaw,
            float fontSize = 3f, Color? color = null, Vector2? area = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color ?? new Color(0.34f, 0.33f, 0.3f); // unlit text: keep dim so it does not glow in the dark
            tmp.textWrappingMode = TextWrappingModes.Normal;
            ((RectTransform)go.transform).sizeDelta = area ?? new Vector2(2.4f, 0.8f);
            return tmp;
        }

        public static ParticleSystem Rain(Transform parent, Vector3 center, Vector3 size)
        {
            var go = new GameObject("Rain");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 14f;
            main.startSize = 0.04f;
            main.maxParticles = 1200;
            main.startColor = new Color(0.7f, 0.75f, 0.85f, 0.45f);
            var emission = ps.emission;
            emission.rateOverTime = 700f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = size;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 6f;
            r.sharedMaterial = UnlitMat("Rain", new Color(0.75f, 0.8f, 0.9f, 0.5f), transparent: true);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            return ps;
        }

        // ================================================================== interactables

        public static PickupItem Pickup(Transform parent, string name, Vector3 pos, string itemId, string displayName,
            string saveId, Material mat, PrimitiveType shape = PrimitiveType.Cube, Vector3? scale = null)
        {
            GameObject go = Prim(parent, name, shape, pos, scale ?? new Vector3(0.25f, 0.12f, 0.18f), mat);
            AddGrabTrigger(go);
            var p = go.AddComponent<PickupItem>();
            SetValue(p, "itemId", itemId);
            SetValue(p, "displayName", displayName);
            SetValue(p, "saveId", saveId);
            return p;
        }

        /// <summary>Generous trigger so small objects are easy to target on a phone.</summary>
        public static void AddGrabTrigger(GameObject go, float worldSize = 0.45f)
        {
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            Vector3 s = go.transform.lossyScale;
            box.size = new Vector3(worldSize / Mathf.Max(0.01f, s.x), worldSize / Mathf.Max(0.01f, s.y), worldSize / Mathf.Max(0.01f, s.z));
        }

        public static NoteInteractable Note(Transform parent, string name, Vector3 pos, float yaw, string id,
            string title, string body, Material mat)
        {
            GameObject go = Prim(parent, name, PrimitiveType.Cube, pos, new Vector3(0.22f, 0.01f, 0.3f), PaperArt.ForNote(mat), new Vector3(0f, yaw, 0f));
            AddGrabTrigger(go, 0.4f);
            var n = go.AddComponent<NoteInteractable>();
            SetValue(n, "id", id);
            SetValue(n, "title", title);
            SetValue(n, "body", body);
            return n;
        }

        /// <summary>Wall-mounted note/diagram (vertical).</summary>
        public static NoteInteractable WallNote(Transform parent, string name, Vector3 pos, float yaw, string id,
            string title, string body, Material mat, Vector2? size = null)
        {
            Vector2 s = size ?? new Vector2(0.5f, 0.4f);
            GameObject go = Prim(parent, name, PrimitiveType.Cube, pos, new Vector3(s.x, s.y, 0.02f), PaperArt.ForNote(mat), new Vector3(0f, yaw, 0f));
            var n = go.AddComponent<NoteInteractable>();
            SetValue(n, "id", id);
            SetValue(n, "title", title);
            SetValue(n, "body", body);
            return n;
        }

        /// <summary>Hinged door filling a 1.6 m doorway. pos = doorway centre on the floor; yaw 0 = door plane along X.</summary>
        public static Door MakeDoor(Transform parent, string name, Vector3 pos, float yaw, string id, string doorName, Material mat,
            string requiredItemId = null, string requiredItemName = null, string requiredFlag = null, string flagMessage = null,
            float openAngle = 100f)
        {
            Transform root = Group(parent, name, pos);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Transform hinge = Group(root, "Hinge", new Vector3(-0.75f, 0f, 0f));
            GameObject leaf = Box(hinge, "Leaf", new Vector3(0.75f, 1.075f, 0f), new Vector3(1.5f, 2.15f, 0.08f), mat, isStatic: false);
            Box(leaf.transform, "Handle", new Vector3(0.4f, 0f, 0.9f), new Vector3(0.06f, 0.03f, 1.6f), Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f), collider: false, isStatic: false);
            var obstacle = leaf.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = Vector3.one;
            var door = root.gameObject.AddComponent<Door>();
            Set(door, "hinge", hinge);
            SetValue(door, "id", id);
            SetValue(door, "doorName", doorName);
            SetValue(door, "openAngle", openAngle);
            if (!string.IsNullOrEmpty(requiredItemId))
            {
                SetValue(door, "requiredItemId", requiredItemId);
                SetValue(door, "requiredItemName", requiredItemName);
            }
            if (!string.IsNullOrEmpty(requiredFlag))
            {
                SetValue(door, "requiredFlag", requiredFlag);
                SetValue(door, "flagLockedMessage", flagMessage ?? "It won't open.");
            }
            return door;
        }

        public static ItemSlot Slot(Transform parent, string name, Vector3 pos, string id, string itemId, string itemName,
            string slotName, Material emptyMat, Material fullMat, Vector3? size = null)
        {
            Vector3 s = size ?? new Vector3(0.3f, 0.3f, 0.3f);
            Transform root = Group(parent, name, pos);
            GameObject empty = Box(root, "Empty", Vector3.zero, s, emptyMat, isStatic: false);
            GameObject full = Box(root, "Installed", Vector3.zero, s * 0.9f, fullMat, collider: false, isStatic: false);
            var slot = root.gameObject.AddComponent<ItemSlot>();
            SetValue(slot, "id", id);
            SetValue(slot, "requiredItemId", itemId);
            SetValue(slot, "requiredItemName", itemName);
            SetValue(slot, "slotName", slotName);
            Set(slot, "emptyVisual", empty);
            Set(slot, "installedVisual", full);
            // Keep a collider when the "Empty" visual hides.
            var col = root.gameObject.AddComponent<BoxCollider>();
            col.size = s * 1.3f;
            col.isTrigger = true;
            return slot;
        }

        public static ActionButton Button3D(Transform parent, string name, Vector3 pos, float yaw, string id, string prompt,
            Material mat, Vector3? size = null, string requiredFlag = null, string lockedMessage = null, bool oneShot = false)
        {
            GameObject go = Box(parent, name, pos, size ?? new Vector3(0.18f, 0.18f, 0.1f), mat, isStatic: false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var b = go.AddComponent<ActionButton>();
            SetValue(b, "id", id);
            SetValue(b, "prompt", prompt);
            if (!string.IsNullOrEmpty(requiredFlag)) SetValue(b, "requiredFlag", requiredFlag);
            if (!string.IsNullOrEmpty(lockedMessage)) SetValue(b, "lockedMessage", lockedMessage);
            SetValue(b, "oneShot", oneShot);
            return b;
        }

        public static TapePlayer Tape(Transform parent, string name, Vector3 pos, float yaw, string id, string deviceName,
            Material mat, SubtitleLine[] lines, string requiredItemId = null, int requiredCount = 1, string missingItemMessage = null,
            string requiredFlag = null, string missingFlagMessage = null, bool lockPlayer = false)
        {
            GameObject go = Box(parent, name, pos, new Vector3(0.45f, 0.18f, 0.3f), mat, isStatic: false);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Box(go.transform, "Reel", new Vector3(0f, 0.6f, 0f), new Vector3(0.6f, 0.3f, 0.5f), Mat("Black", new Color(0.05f, 0.05f, 0.05f)), collider: false, isStatic: false);
            var t = go.AddComponent<TapePlayer>();
            SetValue(t, "id", id);
            SetValue(t, "deviceName", deviceName);
            SetLines(t, "lines", lines);
            if (!string.IsNullOrEmpty(requiredItemId))
            {
                SetValue(t, "requiredItemId", requiredItemId);
                SetValue(t, "requiredCount", requiredCount);
                if (missingItemMessage != null) SetValue(t, "missingItemMessage", missingItemMessage);
            }
            if (!string.IsNullOrEmpty(requiredFlag))
            {
                SetValue(t, "requiredFlag", requiredFlag);
                if (missingFlagMessage != null) SetValue(t, "missingFlagMessage", missingFlagMessage);
            }
            SetValue(t, "lockPlayer", lockPlayer);
            Fixtures.TapeDeck(go.transform, charred: deviceName.ToLowerInvariant().Contains("scorched"));
            return t;
        }

        /// <summary>Hollow locker (0.9 x 0.8 x 2.1) the player can hide in. Opening faces local +Z.</summary>
        public static HidingSpot Locker(Transform parent, string name, Vector3 floorPos, float yaw, Material mat, string spotName = "locker")
        {
            Transform root = Group(parent, name, floorPos);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            bool wood = spotName != "locker";
            const float w = 1.0f, d = 0.9f, h = 2.1f, t = 0.05f;
            Material inside = Mat(wood ? "WardrobeInside" : "LockerInside", wood ? new Color(0.1f, 0.07f, 0.05f) : new Color(0.07f, 0.075f, 0.08f), smoothness: 0.2f);
            Material handleMat = Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f);
            Box(root, "Back", new Vector3(0f, h / 2f, -d / 2f), new Vector3(w, h, t), mat, isStatic: false);
            Box(root, "Left", new Vector3(-w / 2f, h / 2f, 0f), new Vector3(t, h, d), mat, isStatic: false);
            Box(root, "Right", new Vector3(w / 2f, h / 2f, 0f), new Vector3(t, h, d), mat, isStatic: false);
            Box(root, "Top", new Vector3(0f, h, 0f), new Vector3(w, t, d), mat, isStatic: false);
            Box(root, "Plinth", new Vector3(0f, 0.04f, 0f), new Vector3(w, 0.08f, d), mat, collider: false, isStatic: false);
            // Dark lining so the inside reads as a cramped box.
            Box(root, "Lining", new Vector3(0f, h / 2f, -d / 2f + t / 2f + 0.006f), new Vector3(w - 2f * t, h - 0.1f, 0.01f), inside, collider: false, isStatic: false);

            // Door on a hinge at the left front edge (visual only; HidingDoor swings it when you get in or out).
            // Solid below and above; louvred vents at eye height are all you can see through while hidden.
            Transform hinge = Group(root, "DoorHinge", new Vector3(-w / 2f, 0f, d / 2f));
            float dw = w - 0.02f, cx = dw / 2f + 0.01f, dt = 0.025f;
            Box(hinge, "DoorLower", new Vector3(cx, 0.75f, 0f), new Vector3(dw, 1.38f, dt), mat, collider: false, isStatic: false);
            Box(hinge, "DoorUpper", new Vector3(cx, 2.0f, 0f), new Vector3(dw, 0.18f, dt), mat, collider: false, isStatic: false);
            Box(hinge, "DoorStileL", new Vector3(0.07f, 1.67f, 0f), new Vector3(0.14f, 0.48f, dt), mat, collider: false, isStatic: false);
            Box(hinge, "DoorStileR", new Vector3(dw - 0.05f, 1.67f, 0f), new Vector3(0.14f, 0.48f, dt), mat, collider: false, isStatic: false);
            for (int i = 0; i < 5; i++)
            {
                GameObject v = Box(hinge, "Vent", new Vector3(cx, 1.47f + i * 0.09f, 0f), new Vector3(dw - 0.26f, 0.03f, 0.05f), mat, collider: false, isStatic: false);
                v.transform.localRotation = Quaternion.Euler(wood ? 35f : 25f, 0f, 0f);
            }
            Box(hinge, "Handle", new Vector3(dw - 0.09f, 1.15f, 0.03f), new Vector3(0.03f, 0.2f, 0.03f), handleMat, collider: false, isStatic: false);
            if (!wood)
            {
                Box(hinge, "NumberPlate", new Vector3(cx, 2.0f, 0.016f), new Vector3(0.16f, 0.07f, 0.006f),
                    Mat("LockerPlate", new Color(0.55f, 0.52f, 0.42f), smoothness: 0.5f), collider: false, isStatic: false);
                for (int i = 0; i < 3; i++)
                    Box(hinge, "LowVent", new Vector3(cx, 0.18f + i * 0.05f, 0.014f), new Vector3(dw - 0.4f, 0.015f, 0.006f), inside, collider: false, isStatic: false);
            }

            var trigger = root.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, h / 2f, 0.05f);
            trigger.size = new Vector3(w, h, d);
            var obstacle = root.gameObject.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = new Vector3(0f, h / 2f, 0f);
            obstacle.size = new Vector3(w, h, d);
            obstacle.carving = true;
            obstacle.carveOnlyStationary = true;
            Transform hide = Group(root, "HidePoint", new Vector3(0f, 0.02f, -0.05f));
            Transform exit = Group(root, "ExitPoint", new Vector3(0f, 0.02f, 1.0f));
            var spot = root.gameObject.AddComponent<HidingSpot>();
            SetValue(spot, "spotName", spotName);
            Set(spot, "hidePoint", hide);
            Set(spot, "exitPoint", exit);
            var door = root.gameObject.AddComponent<HidingDoor>();
            Set(door, "spot", spot);
            Set(door, "hinge", hinge);
            return spot;
        }

        public static BatteryPickup Battery(Transform parent, Vector3 pos, string saveId)
        {
            GameObject go = Prim(parent, "Battery_" + saveId, PrimitiveType.Cylinder, pos, new Vector3(0.07f, 0.07f, 0.07f),
                Mat("Battery", new Color(0.2f, 0.75f, 0.35f), emissive: true));
            AddGrabTrigger(go, 0.35f);
            var b = go.AddComponent<BatteryPickup>();
            SetValue(b, "saveId", saveId);
            return b;
        }

        public static T Trigger<T>(Transform parent, string name, Vector3 center, Vector3 size) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = size;
            return go.AddComponent<T>();
        }

        public static Checkpoint CheckpointZone(Transform parent, string id, Vector3 pos, float yaw, Vector3 size, bool triggerEnabled = true)
        {
            Checkpoint cp = Trigger<Checkpoint>(parent, "Checkpoint_" + id, pos, size);
            cp.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            SetValue(cp, "id", id);
            cp.GetComponent<Collider>().enabled = triggerEnabled;
            return cp;
        }

        public static Transform Marker(Transform parent, string name, Vector3 pos, float yaw)
        {
            Transform t = Group(parent, name, pos);
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        // ================================================================== UI

        public static RectTransform UIRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Stretch(RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
        }

        public static void Anchors(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform r, Vector2 anchor, Vector2 position, Vector2 size)
        {
            r.anchorMin = anchor;
            r.anchorMax = anchor;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = position;
            r.sizeDelta = size;
        }

        public static Image UIImage(RectTransform r, Sprite sprite, Color color, bool raycast)
        {
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static TMP_Text UIText(Transform parent, string name, string text, float fontSize, Vector2 anchor,
            Vector2 position, Vector2 size, TextAlignmentOptions align = TextAlignmentOptions.Center, bool wrap = false)
        {
            RectTransform r = UIRect(parent, name);
            Place(r, anchor, position, size);
            var tmp = r.gameObject.AddComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = new Color(1f, 1f, 1f, 0.92f);
            tmp.raycastTarget = false;
            tmp.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            return tmp;
        }

        /// <summary>Full-screen panel (dark backdrop) that blocks touches behind it.</summary>
        public static RectTransform UIPanel(Transform parent, string name, Color color)
        {
            RectTransform r = UIRect(parent, name);
            Stretch(r);
            UIImage(r, null, color, raycast: true);
            return r;
        }

        public static Button UIButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size,
            float fontSize, UnityAction onClick, Sprite sprite = null)
        {
            RectTransform r = UIRect(parent, name);
            Place(r, anchor, pos, size);
            Image img = UIImage(r, sprite ?? EnsureSprite("Square", SpriteShape.Square), new Color(0.07f, 0.07f, 0.08f, 0.88f), raycast: true);
            var b = r.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(2.2f, 2.1f, 1.9f);
            cb.pressedColor = new Color(4f, 3.5f, 2.6f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            cb.colorMultiplier = 1f;
            b.colors = cb;
            UIText(r, "Label", label, fontSize, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            if (onClick != null) UnityEventTools.AddPersistentListener(b.onClick, onClick);
            return b;
        }

        public static Slider UISlider(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            Sprite square = EnsureSprite("Square", SpriteShape.Square);
            Sprite disc = EnsureSprite("Circle", SpriteShape.Disc);
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources { standard = square, background = square, knob = disc });
            go.name = name;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            Place(r, anchor, pos, size);
            foreach (Image img in go.GetComponentsInChildren<Image>())
            {
                if (img.name == "Background") img.color = new Color(1f, 1f, 1f, 0.15f);
                else if (img.name == "Fill") img.color = new Color(1f, 0.85f, 0.55f, 0.85f);
                else if (img.name == "Handle") { img.color = Color.white; ((RectTransform)img.transform).sizeDelta = new Vector2(44f, 0f); }
            }
            return go.GetComponent<Slider>();
        }

        // ================================================================== serialized wiring

        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetInts(Object target, string field, int[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).intValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetBools(Object target, string field, bool[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).boolValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetStrings(Object target, string field, string[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).stringValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetLines(Object target, string field, SubtitleLine[] lines)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            p.arraySize = lines.Length;
            for (int i = 0; i < lines.Length; i++)
            {
                SerializedProperty e = p.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("speaker").stringValue = lines[i].speaker;
                e.FindPropertyRelative("text").stringValue = lines[i].text;
                e.FindPropertyRelative("seconds").floatValue = lines[i].seconds;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Set a primitive serialized value (string, bool, int, float, enum, Vector3, Color). Supports dotted paths.</summary>
        public static void SetValue(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = Prop(so, target, field);
            switch (value)
            {
                case null: if (p.propertyType == SerializedPropertyType.String) p.stringValue = ""; break;
                case string s: p.stringValue = s; break;
                case bool b: p.boolValue = b; break;
                case int i: if (p.propertyType == SerializedPropertyType.Float) p.floatValue = i; else p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case Enum en: p.intValue = Convert.ToInt32(en); break;
                case Vector3 v: p.vector3Value = v; break;
                case Color c: p.colorValue = c; break;
                case LayerMask lm: p.intValue = lm.value; break;
                default: throw new ArgumentException("Unsupported value type " + value.GetType());
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static SerializedProperty Prop(SerializedObject so, Object target, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            if (p == null) throw new ArgumentException($"{target.GetType().Name} has no serialized field '{field}'");
            return p;
        }

        // ================================================================== UnityEvent wiring

        private static UnityEventBase EventField(Component c, string field)
        {
            for (Type t = c.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo f = t.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (f != null) return (UnityEventBase)f.GetValue(c);
            }
            throw new ArgumentException($"{c.GetType().Name} has no event field '{field}'");
        }

        public static void On(Component c, string field, UnityAction action)
        {
            UnityEventTools.AddPersistentListener((UnityEvent)EventField(c, field), action);
            EditorUtility.SetDirty(c);
        }

        public static void On(Component c, string field, UnityAction<string> action, string arg)
        {
            UnityEventTools.AddStringPersistentListener(EventField(c, field), action, arg);
            EditorUtility.SetDirty(c);
        }

        public static void On(Component c, string field, UnityAction<int> action, int arg)
        {
            UnityEventTools.AddIntPersistentListener(EventField(c, field), action, arg);
            EditorUtility.SetDirty(c);
        }

        public static void On(Component c, string field, UnityAction<float> action, float arg)
        {
            UnityEventTools.AddFloatPersistentListener(EventField(c, field), action, arg);
            EditorUtility.SetDirty(c);
        }

        public static void On(Component c, string field, UnityAction<bool> action, bool arg)
        {
            UnityEventTools.AddBoolPersistentListener(EventField(c, field), action, arg);
            EditorUtility.SetDirty(c);
        }

        public static SubtitleLine L(string speaker, string text, float seconds) => new SubtitleLine(speaker, text, seconds);
    }
}
