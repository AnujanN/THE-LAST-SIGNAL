using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Makes rooms look abandoned instead of clean: water streaks and grime along walls, floor stains,
    /// puddles, mould, cracks, cobwebs in corners, soot (burned wing), claw scratches, scattered papers and
    /// small debris. Decals are generated textures on quads placed by raycasting against the real walls and
    /// floors, then merged into one mesh per material (a handful of draw calls per level on the phone).
    /// </summary>
    public static class Dressing
    {
        private const string Dir = "Assets/Art/Textures/Generated/Grime";
        private const string Ver = "_v1";

        public enum Kind { Streaks, BaseGrime, Stain, Puddle, Mould, Cracks, Cobweb, Soot, Scratches, Papers, Strands }

        /// <summary>Rooms and seed of the last level dressed (used by the wildlife pass in Finish).</summary>
        public static Rect[] LastRooms { get; private set; } = new Rect[0];
        public static int LastSeed { get; private set; }

        public class Theme
        {
            public float ceiling = 3.2f;
            public int streaks = 18, baseGrime = 30, stains = 22, puddles = 4, mould = 0, cracks = 10, cobwebs = 10,
                soot = 0, scratches = 0, papers = 18, debris = 0;
            public Color stainTint = new Color(0.25f, 0.2f, 0.15f);
        }

        /// <summary>Rooms as (xMin, zMin, xMax, zMax) in world space.</summary>
        public static void Apply(Transform envRoot, Rect[] rooms, int seed, Theme t)
        {
            Physics.SyncTransforms();
            LastRooms = rooms;
            LastSeed = seed;
            var rng = new System.Random(seed);
            var holder = new GameObject("Grime").transform;
            holder.SetParent(envRoot, false);
            var batches = new Dictionary<Kind, List<CombineInstance>>();
            Mesh quad = QuadMesh();

            void Add(Kind k, Vector3 pos, Quaternion rot, Vector2 size, Mesh mesh = null)
            {
                if (!batches.TryGetValue(k, out var list)) batches[k] = list = new List<CombineInstance>();
                list.Add(new CombineInstance { mesh = mesh ?? quad, transform = Matrix4x4.TRS(pos, rot, new Vector3(size.x, size.y, 1f)) });
            }

            float R() => (float)rng.NextDouble();
            Vector3 RandomPoint(out Rect room)
            {
                room = rooms[rng.Next(rooms.Length)];
                return new Vector3(Mathf.Lerp(room.xMin + 0.4f, room.xMax - 0.4f, R()), 1.2f, Mathf.Lerp(room.yMin + 0.4f, room.yMax - 0.4f, R()));
            }

            bool Floor(Vector3 p, out Vector3 hitPoint)
            {
                hitPoint = default;
                if (!Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out RaycastHit h, 3f, ~0, QueryTriggerInteraction.Ignore)) return false;
                if (h.normal.y < 0.9f || h.point.y > 0.1f || !h.collider.transform.IsChildOf(envRoot)) return false;
                hitPoint = h.point;
                return true;
            }

            bool Wall(Vector3 p, out RaycastHit wall)
            {
                float a = R() * Mathf.PI * 2f;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                if (!Physics.Raycast(p, dir, out wall, 7f, ~0, QueryTriggerInteraction.Ignore)) return false;
                // Real walls only: tall, part of the level, and visible (not a hidden furniture stand-in).
                var shown = wall.collider.GetComponent<Renderer>();
                return Mathf.Abs(wall.normal.y) < 0.1f && wall.collider.bounds.size.y >= 2f && wall.collider.transform.IsChildOf(envRoot)
                    && shown != null && shown.enabled;
            }

            Quaternion OnWall(Vector3 normal, float roll = 0f) => Quaternion.LookRotation(-normal) * Quaternion.Euler(0f, 0f, roll);
            Quaternion OnFloor(float yaw) => Quaternion.Euler(90f, yaw, 0f);

            // Walls: water streaks from the ceiling, grime along the bottom, mould, cracks, scratches.
            for (int i = 0; i < t.streaks; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                {
                    float h = 1.2f + R() * 1.4f;
                    Add(Kind.Streaks, new Vector3(w.point.x, t.ceiling - h / 2f, w.point.z) + w.normal * 0.012f, OnWall(w.normal), new Vector2(0.6f + R() * 1.2f, h));
                }
            for (int i = 0; i < t.baseGrime; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                    Add(Kind.BaseGrime, new Vector3(w.point.x, 0.3f, w.point.z) + w.normal * 0.01f, OnWall(w.normal), new Vector2(1.4f + R() * 1.6f, 0.6f));
            for (int i = 0; i < t.mould; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                    Add(Kind.Mould, new Vector3(w.point.x, Random(rng, 0.4f, t.ceiling - 0.6f), w.point.z) + w.normal * 0.014f, OnWall(w.normal, R() * 360f), Vector2.one * (0.6f + R() * 0.9f));
            for (int i = 0; i < t.cracks; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                    Add(Kind.Cracks, new Vector3(w.point.x, Random(rng, 0.8f, t.ceiling - 0.4f), w.point.z) + w.normal * 0.016f, OnWall(w.normal, R() * 360f), Vector2.one * (0.5f + R() * 0.8f));
            for (int i = 0; i < t.soot; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                {
                    float h = 1.5f + R() * 1.5f;
                    Add(Kind.Soot, new Vector3(w.point.x, t.ceiling - h / 2f, w.point.z) + w.normal * 0.018f, OnWall(w.normal), new Vector2(1.2f + R() * 1.6f, h));
                }
            for (int i = 0; i < t.scratches; i++)
                if (Wall(RandomPoint(out _), out RaycastHit w))
                    Add(Kind.Scratches, new Vector3(w.point.x, Random(rng, 0.9f, 1.8f), w.point.z) + w.normal * 0.02f, OnWall(w.normal, Random(rng, -25f, 25f)), new Vector2(0.45f, 0.6f));

            // Cobwebs: a web stretched across the upper corners of (nearly) every room, its plane cutting
            // both walls and the ceiling, with loose strands hanging beside it.
            float webChance = Mathf.Clamp01(0.55f + t.cobwebs * 0.025f);
            var patterns = new List<int>();
            int NextPattern()
            {
                if (patterns.Count == 0)
                {
                    for (int i = 0; i < WebPatterns; i++) patterns.Add(i);
                    for (int i = patterns.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (patterns[i], patterns[j]) = (patterns[j], patterns[i]); }
                }
                int v = patterns[patterns.Count - 1];
                patterns.RemoveAt(patterns.Count - 1);
                return v;
            }
            foreach (Rect room in rooms)
                for (int cx = 0; cx < 2; cx++)
                for (int cz = 0; cz < 2; cz++)
                {
                    if (R() > webChance) continue;
                    var inward = new Vector3(cx == 0 ? 1f : -1f, 0f, cz == 0 ? 1f : -1f);
                    var probe = new Vector3(cx == 0 ? room.xMin : room.xMax, 1.5f, cz == 0 ? room.yMin : room.yMax) + inward * 0.6f;
                    if (!Physics.Raycast(probe, Vector3.up, out RaycastHit ceil, 4f, ~0, QueryTriggerInteraction.Ignore) || ceil.normal.y > -0.9f) continue;
                    float top = ceil.point.y;
                    var high = new Vector3(probe.x, top - 0.15f, probe.z);
                    if (!Physics.Raycast(high, new Vector3(-inward.x, 0f, 0f), out RaycastHit wx, 1.8f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(wx.normal.x) < 0.9f) continue;
                    if (!Physics.Raycast(high, new Vector3(0f, 0f, -inward.z), out RaycastHit wz, 1.8f, ~0, QueryTriggerInteraction.Ignore) || Mathf.Abs(wz.normal.z) < 0.9f) continue;
                    var corner = new Vector3(wx.point.x, top, wz.point.z);
                    float size = 1.4f + R() * 0.9f;
                    Vector3 n = (inward + Vector3.down).normalized;
                    // Centre of the triangle the web plane cuts from the corner; the quad's overhang hides in the walls.
                    Vector3 centre = corner + n * (size / 2.83f);
                    Add(Kind.Cobweb, centre, Quaternion.LookRotation(-n) * Quaternion.Euler(0f, 0f, R() * 360f), Vector2.one * size, WebQuad(NextPattern()));
                    for (int k = 0; k < 2; k++)
                    {
                        float len = 0.35f + R() * 0.6f;
                        Vector3 at = corner + new Vector3(inward.x * (0.3f + R() * 0.9f), -len / 2f - 0.01f, inward.z * (0.3f + R() * 0.9f));
                        Vector3 facing = -Flat(at - corner);
                        Add(Kind.Strands, at, Quaternion.LookRotation(facing), new Vector2(0.25f, len));
                    }
                }

            // Floors: stains, puddles, scattered papers.
            for (int i = 0; i < t.stains; i++)
                if (Floor(RandomPoint(out _), out Vector3 f))
                    Add(Kind.Stain, f + Vector3.up * (0.004f + i * 0.00005f), OnFloor(R() * 360f), Vector2.one * (0.8f + R() * 1.6f));
            for (int i = 0; i < t.puddles; i++)
                if (Floor(RandomPoint(out _), out Vector3 f))
                {
                    float yaw = R() * 360f;
                    var size = new Vector2(1f + R() * 1.6f, 0.8f + R() * 1.2f);
                    Add(Kind.Puddle, f + Vector3.up * 0.007f, OnFloor(yaw), size);
                    PuddleZone(f, yaw, size);
                }
            for (int i = 0; i < t.papers; i++)
                if (Floor(RandomPoint(out _), out Vector3 f))
                    Add(Kind.Papers, f + Vector3.up * (0.009f + i * 0.0001f), OnFloor(R() * 360f), new Vector2(0.21f, 0.29f) * (0.8f + R() * 0.4f));

            foreach (var kv in batches) Combine(holder, kv.Key, kv.Value);

            // Small debris chunks (real colliders off; they are just clutter).
            if (t.debris > 0)
            {
                var debris = new GameObject("Debris").transform;
                debris.SetParent(envRoot, false);
                Material chunk = Mat("Charred", new Color(0.05f, 0.045f, 0.04f), smoothness: 0.05f);
                Material plaster = Mat("Rubble", new Color(0.32f, 0.3f, 0.28f), smoothness: 0.1f);
                for (int i = 0; i < t.debris; i++)
                    if (Floor(RandomPoint(out _), out Vector3 f))
                    {
                        float s = 0.06f + R() * 0.22f;
                        GameObject c = Box(debris, "Chunk", f + Vector3.up * s * 0.4f, new Vector3(s, s * (0.4f + R() * 0.6f), s * (0.6f + R())), R() < 0.6f ? chunk : plaster, collider: false);
                        c.transform.rotation = Quaternion.Euler(R() * 30f, R() * 360f, R() * 30f);
                        c.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                    }
            }
        }

        private static float Random(System.Random rng, float a, float b) => Mathf.Lerp(a, b, (float)rng.NextDouble());

        /// <summary>
        /// Walking through a puddle splashes (and carries a little further). The trigger lives outside the
        /// environment so the NavMesh bake ignores it, on Ignore Raycast so it never blocks aiming.
        /// </summary>
        private static void PuddleZone(Vector3 at, float yaw, Vector2 size)
        {
            GameObject holder = GameObject.Find("WaterZones") ?? new GameObject("WaterZones");
            var go = new GameObject("Puddle");
            go.transform.SetParent(holder.transform, false);
            go.transform.SetPositionAndRotation(at + Vector3.up * 0.2f, Quaternion.Euler(0f, yaw, 0f));
            go.layer = 2; // Ignore Raycast
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(size.x * 0.8f, 0.4f, size.y * 0.8f);
            var zone = go.AddComponent<LastSignal.Enemy.SurfaceNoiseZone>();
            SetValue(zone, "noiseMultiplier", 1.3f);
        }

        private static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }
        private static int CountOf(Dictionary<Kind, List<CombineInstance>> b, Kind k) => b.TryGetValue(k, out var l) ? l.Count : 0;

        private static void Combine(Transform holder, Kind kind, List<CombineInstance> parts)
        {
            if (parts.Count == 0) return;
            var mesh = new Mesh { name = "Grime_" + kind, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            var go = new GameObject("Grime_" + kind);
            go.transform.SetParent(holder, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = DecalMaterial(kind);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = true;
            go.isStatic = true;
        }

        private static Mesh quadCache;
        private static Mesh QuadMesh()
        {
            if (quadCache != null) return quadCache;
            var m = new Mesh { name = "GrimeQuad" };
            m.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
            m.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            m.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            m.tangents = new[] { new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f), new Vector4(1f, 0f, 0f, -1f) };
            m.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            quadCache = m;
            return m;
        }

        // ------------------------------------------------------------------ materials and textures

        public static Material DecalMaterial(Kind kind)
        {
            EnsureFolder(MaterialDir);
            string path = MaterialDir + "/Grime_" + kind + ".mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, path);
            }
            Texture2D tex = kind == Kind.Papers ? AssetDatabase.LoadAssetAtPath<Texture2D>(PaperArt.WrittenPath) : Texture(kind);
            if (kind == Kind.Papers) PaperArt.WrittenMaterial(); // make sure the paper texture exists
            tex = kind == Kind.Papers ? AssetDatabase.LoadAssetAtPath<Texture2D>(PaperArt.WrittenPath) : tex;
            m.SetTexture("_BaseMap", tex);
            Color c;
            float smooth = 0.1f;
            switch (kind)
            {
                case Kind.Puddle: c = new Color(0.02f, 0.025f, 0.03f, 0.85f); smooth = 0.95f; break;
                case Kind.Mould: c = new Color(0.12f, 0.14f, 0.08f, 0.9f); break;
                case Kind.Cobweb:
                case Kind.Strands: c = new Color(0.86f, 0.86f, 0.83f, 0.9f); smooth = 0.3f; break;
                case Kind.Papers: c = new Color(0.8f, 0.78f, 0.72f, 1f); break;
                case Kind.Soot: c = new Color(0.02f, 0.02f, 0.02f, 0.95f); break;
                case Kind.Scratches: c = new Color(0.08f, 0.06f, 0.05f, 0.9f); break;
                default: c = new Color(0.12f, 0.1f, 0.07f, 0.85f); break;
            }
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", smooth);
            // Webs hang in the air: visible from both sides.
            m.SetFloat("_Cull", (float)(kind == Kind.Cobweb || kind == Kind.Strands ? CullMode.Off : CullMode.Back));
            m.SetShaderPassEnabled("ShadowCaster", false);
            if (kind == Kind.Puddle)
            {
                // Wet patches: opaque cut-out shape so the shine stays inside the puddle.
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.35f);
                m.SetFloat("_ZWrite", 1f);
                m.SetFloat("_SrcBlend", (float)BlendMode.One);
                m.SetFloat("_DstBlend", (float)BlendMode.Zero);
                m.SetOverrideTag("RenderType", "TransparentCutout");
                m.EnableKeyword("_ALPHATEST_ON");
                m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                // Plain alpha blending with no specular or reflections: URP's default "preserve specular"
                // premultiplied mode would add shine over the whole quad, including its transparent parts.
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_BlendModePreserveSpecular", 0f);
                m.SetFloat("_AlphaClip", 0f);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SpecularHighlights", 0f);
                m.SetFloat("_EnvironmentReflections", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.DisableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)RenderQueue.Transparent - 50;
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D Texture(Kind kind)
        {
            EnsureFolder(Dir);
            bool web = kind == Kind.Cobweb || kind == Kind.Strands;
            string path = Dir + "/" + kind + (kind == Kind.Cobweb ? "_v4" : web ? "_v2" : Ver) + ".png";
            if (!File.Exists(path))
            {
                Texture2D tex = kind == Kind.Cobweb ? WebAtlas() : Generate(kind, web ? 512 : 256);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = kind == Kind.Cobweb ? WebCell * WebColumns : web ? 512 : 256;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static Texture2D Generate(Kind kind, int n)
        {
            var rng = new System.Random((int)kind * 97 + 13);
            float R() => (float)rng.NextDouble();
            float ox = R() * 50f, oy = R() * 50f;
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);

            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float u = x / (float)(n - 1), v = y / (float)(n - 1);
                float cu = u - 0.5f, cv = v - 0.5f, r = Mathf.Sqrt(cu * cu + cv * cv) * 2f;
                float noise = Mathf.PerlinNoise(ox + u * 6f, oy + v * 6f), fine = Mathf.PerlinNoise(ox + u * 25f, oy + v * 25f);
                float a = 0f;
                switch (kind)
                {
                    case Kind.Streaks:
                    {
                        // Vertical drips running down from the top, uneven widths and lengths.
                        float col = Mathf.PerlinNoise(ox + u * 18f, 3f);
                        float len = 0.25f + 0.75f * Mathf.PerlinNoise(ox + u * 9f, 7f);
                        float fromTop = 1f - v;
                        a = Mathf.Clamp01((col - 0.35f) * 2.2f) * Mathf.Clamp01(1f - fromTop / len) * (0.6f + 0.4f * fine);
                        a *= Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 6f);
                        break;
                    }
                    case Kind.BaseGrime:
                        a = Mathf.Pow(1f - v, 1.6f) * (0.5f + 0.5f * noise) * Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 5f);
                        break;
                    case Kind.Stain:
                    case Kind.Puddle:
                    {
                        float edge = 0.55f + 0.35f * noise;
                        a = Mathf.Clamp01((edge - r) * (kind == Kind.Puddle ? 9f : 3f));
                        if (kind == Kind.Stain) a *= 0.45f + 0.55f * fine;
                        break;
                    }
                    case Kind.Mould:
                    {
                        float blob = Mathf.Clamp01((0.6f + 0.3f * noise - r) * 3f);
                        float speck = Mathf.PerlinNoise(ox + u * 60f, oy + v * 60f);
                        a = blob * Mathf.Clamp01((speck - 0.35f) * 3f);
                        break;
                    }
                    case Kind.Soot:
                        a = Mathf.Pow(v, 0.9f) * (0.55f + 0.45f * noise) * Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 3f);
                        break;
                }
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }

            if (kind == Kind.Cracks) DrawCracks(px, n, rng);
            if (kind == Kind.Strands) DrawStrands(px, n, rng);
            if (kind == Kind.Scratches) DrawScratches(px, n, rng);

            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        private static void Line(Color[] px, int n, Vector2 a, Vector2 b, float width, float alpha)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) * n * 1.5f) + 1;
            for (int s = 0; s <= steps; s++)
            {
                Vector2 p = Vector2.Lerp(a, b, s / (float)steps) * (n - 1);
                int w = Mathf.CeilToInt(width);
                for (int dy = -w; dy <= w; dy++)
                for (int dx = -w; dx <= w; dx++)
                {
                    int x = Mathf.RoundToInt(p.x) + dx, y = Mathf.RoundToInt(p.y) + dy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float k = Mathf.Clamp01(width + 0.5f - d) * alpha;
                    Color c = px[y * n + x];
                    c.a = Mathf.Max(c.a, k);
                    px[y * n + x] = c;
                }
            }
        }

        private static void DrawCracks(Color[] px, int n, System.Random rng)
        {
            void Branch(Vector2 p, float angle, float length, int depth)
            {
                while (length > 0.02f)
                {
                    float step = 0.03f + (float)rng.NextDouble() * 0.03f;
                    angle += ((float)rng.NextDouble() - 0.5f) * 0.9f;
                    Vector2 q = p + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * step;
                    Line(px, n, p, q, depth == 0 ? 1.2f : 0.7f, 0.95f);
                    if (depth < 2 && rng.NextDouble() < 0.18) Branch(q, angle + ((float)rng.NextDouble() - 0.5f) * 2f, length * 0.5f, depth + 1);
                    p = q;
                    length -= step;
                    if (p.x < 0.02f || p.y < 0.02f || p.x > 0.98f || p.y > 0.98f) break;
                }
            }
            for (int i = 0; i < 3; i++) Branch(new Vector2(0.5f, 0.5f), (float)rng.NextDouble() * Mathf.PI * 2f, 0.5f, 0);
        }

        // ------------------------------------------------------------------ cobweb patterns

        private const int WebColumns = 4, WebRows = 4, WebPatterns = WebColumns * WebRows, WebCell = 512;
        private static readonly Mesh[] webQuads = new Mesh[WebPatterns];

        /// <summary>The shared quad with its UVs on one cell of the web atlas.</summary>
        private static Mesh WebQuad(int v)
        {
            if (webQuads[v] != null) return webQuads[v];
            float inset = 2f / (WebCell * WebColumns);
            float u0 = (v % WebColumns) / (float)WebColumns + inset, u1 = (v % WebColumns + 1) / (float)WebColumns - inset;
            float v0 = (v / WebColumns) / (float)WebRows + inset, v1 = (v / WebColumns + 1) / (float)WebRows - inset;
            Mesh m = Object.Instantiate(QuadMesh());
            m.name = "WebQuad" + v;
            m.uv = new[] { new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u0, v1), new Vector2(u1, v1) };
            webQuads[v] = m;
            return m;
        }

        /// <summary>16 different webs (8 styles, two variations each) in one texture: one draw call per level.</summary>
        private static Texture2D WebAtlas()
        {
            int n = WebCell, w = n * WebColumns, h = n * WebRows;
            var atlas = new Texture2D(w, h, TextureFormat.RGBA32, true);
            var clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color(1f, 1f, 1f, 0f);
            atlas.SetPixels(clear);
            for (int v = 0; v < WebPatterns; v++)
            {
                var px = new Color[n * n];
                for (int i = 0; i < px.Length; i++) px[i] = new Color(1f, 1f, 1f, 0f);
                DrawWeb(px, n, v % 8, new System.Random(4099 + v * 131));
                atlas.SetPixels((v % WebColumns) * n, (v / WebColumns) * n, n, n, px);
            }
            atlas.Apply();
            return atlas;
        }

        private static void DrawWeb(Color[] px, int n, int style, System.Random rng)
        {
            float R() => (float)rng.NextDouble();
            float w = n / 256f;                 // line widths scale with the cell size
            float spoke = 0.75f * w, cross = 0.5f * w, loose = 0.45f * w;

            void Haze(Vector2 c, float radius, float strength)
            {
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x / (float)n, y / (float)n), c);
                    float a = Mathf.Clamp01(1f - d / radius) * strength * Mathf.PerlinNoise(x * 0.04f + style * 7f, y * 0.04f);
                    Color p = px[y * n + x];
                    p.a = Mathf.Max(p.a, a);
                    px[y * n + x] = p;
                }
            }

            // A thread that sags towards "down" (the web's lower side) in the middle.
            void Sag(Vector2 a, Vector2 b, float sag, float width, float alpha)
            {
                Vector2 prev = a;
                for (int k = 1; k <= 6; k++)
                {
                    float t = k / 6f;
                    Vector2 q = Vector2.Lerp(a, b, t) + Vector2.down * (sag * 4f * t * (1f - t));
                    Line(px, n, prev, q, width, alpha);
                    prev = q;
                }
            }

            Vector2[] Spokes(Vector2 c, int count, float jitter)
            {
                var d = new Vector2[count];
                float start = R() * Mathf.PI * 2f;
                for (int i = 0; i < count; i++)
                {
                    float a = start + i / (float)count * Mathf.PI * 2f + (R() - 0.5f) * jitter;
                    d[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                }
                return d;
            }

            // Orb: spokes from a hub and a spiral of cross threads; holes = chance a segment is missing.
            void Orb(Vector2 c, int count, float reach, float spacing, float holes, float sag, float skipFrom = -1f, float skipTo = -1f)
            {
                Vector2[] d = Spokes(c, count, 0.35f);
                float[] len = new float[count];
                for (int i = 0; i < count; i++)
                {
                    len[i] = reach * (0.75f + R() * 0.35f);
                    Line(px, n, c, c + d[i] * len[i], spoke, 0.9f);
                }
                for (float r = spacing * 1.2f; r < reach * 0.85f; r += spacing * (0.8f + R() * 0.5f))
                    for (int i = 0; i < count; i++)
                    {
                        int j = (i + 1) % count;
                        if (R() < holes || r > len[i] || r > len[j]) continue;
                        float ang = Mathf.Atan2(d[i].y, d[i].x);
                        if (skipFrom >= 0f && ang > skipFrom && ang < skipTo) continue;
                        Sag(c + d[i] * r, c + d[j] * (r + spacing * 0.15f), sag * r, cross, 0.8f);
                    }
            }

            void LooseThreads(int count, Vector2 around, float spread)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 a = around + new Vector2(R() - 0.5f, R() - 0.5f) * spread;
                    Sag(a, a + new Vector2(R() - 0.5f, -0.1f - R() * 0.3f) * 0.6f, 0.02f, loose, 0.65f);
                }
            }

            Vector2 centre = new Vector2(0.5f, 0.5f) + new Vector2(R() - 0.5f, R() - 0.5f) * 0.08f;
            switch (style)
            {
                case 0: // classic orb, evenly spaced
                    Haze(centre, 0.25f, 0.08f);
                    Orb(centre, 11 + rng.Next(4), 0.5f, 0.065f, 0.06f, 0.08f);
                    break;
                case 1: // orb with the hub pushed to one side and a torn sector
                {
                    Vector2 c = new Vector2(0.35f + R() * 0.1f, 0.58f + R() * 0.1f);
                    float from = R() * 6f - 3f;
                    Orb(c, 9 + rng.Next(3), 0.55f, 0.06f, 0.1f, 0.12f, from, from + 1.4f);
                    LooseThreads(3, c, 0.5f);
                    break;
                }
                case 2: // tangle web: random threads between a few anchor clusters, dust caught in it
                {
                    Haze(centre, 0.35f, 0.06f);
                    // Anchors spread all round, threads criss-crossing between them and to a few knots inside.
                    var anchors = new Vector2[7 + rng.Next(3)];
                    float start = R() * Mathf.PI * 2f;
                    for (int i = 0; i < anchors.Length; i++)
                    {
                        float a = start + (i + (R() - 0.5f) * 0.6f) / anchors.Length * Mathf.PI * 2f, r = 0.32f + R() * 0.14f;
                        anchors[i] = centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                    }
                    var knots = new Vector2[4];
                    for (int i = 0; i < knots.Length; i++) knots[i] = centre + new Vector2(R() - 0.5f, R() - 0.5f) * 0.35f;
                    for (int i = 0; i < 18; i++)
                    {
                        int a = rng.Next(anchors.Length), b = (a + 2 + rng.Next(anchors.Length - 3)) % anchors.Length;
                        Sag(anchors[a], anchors[b], 0.015f, cross, 0.75f);
                    }
                    for (int i = 0; i < 12; i++)
                        Sag(anchors[rng.Next(anchors.Length)], knots[rng.Next(knots.Length)], 0.01f, cross, 0.7f);
                    foreach (Vector2 k in knots) Line(px, n, k, k + new Vector2(0.003f, 0f), 1.6f * w, 0.55f); // caught dust
                    break;
                }
                case 3: // sheet web: irregular rings with few radials over a thin dusty film
                {
                    Haze(centre, 0.42f, 0.14f);
                    Vector2[] d = Spokes(centre, 7, 0.6f);
                    foreach (Vector2 dir in d) Line(px, n, centre, centre + dir * 0.48f, cross, 0.8f);
                    for (float r = 0.08f; r < 0.45f; r += 0.07f + R() * 0.04f)
                        for (int i = 0; i < d.Length; i++)
                            Line(px, n, centre + d[i] * (r * (0.8f + R() * 0.4f)), centre + d[(i + 1) % d.Length] * (r * (0.8f + R() * 0.4f)), loose, 0.7f);
                    break;
                }
                case 4: // broken orb: many missing segments, a ragged hole, torn ends hanging
                {
                    Vector2 hole = centre + new Vector2(R() - 0.5f, R() - 0.5f) * 0.3f;
                    Orb(centre, 13 + rng.Next(3), 0.5f, 0.05f, 0.3f, 0.1f);
                    for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                        if (Vector2.Distance(new Vector2(x / (float)n, y / (float)n), hole) < 0.09f + 0.03f * Mathf.PerlinNoise(x * 0.05f, y * 0.05f))
                            px[y * n + x].a = 0f;
                    LooseThreads(5, hole, 0.2f);
                    break;
                }
                case 5: // old, slack web: few spokes, deep sag, dusty
                    Haze(centre, 0.3f, 0.1f);
                    Orb(centre, 6 + rng.Next(2), 0.5f, 0.08f, 0.15f, 0.35f);
                    LooseThreads(6, centre, 0.7f);
                    break;
                case 6: // tent web strung between three anchors, criss-crossed
                {
                    var t = new Vector2[3];
                    for (int i = 0; i < 3; i++)
                    {
                        float a = R() * 0.6f + i * Mathf.PI * 2f / 3f;
                        t[i] = new Vector2(0.5f, 0.5f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * (0.38f + R() * 0.08f);
                    }
                    // Frame threads stretch with a little slack; the anchors carry short tie-off threads.
                    for (int i = 0; i < 3; i++)
                    {
                        Sag(t[i], t[(i + 1) % 3], 0.02f + R() * 0.03f, spoke, 0.85f);
                        Vector2 out1 = (t[i] - new Vector2(0.5f, 0.5f)).normalized;
                        Line(px, n, t[i], t[i] + out1 * (0.05f + R() * 0.06f), loose, 0.7f);
                    }
                    for (int k = 0; k < 18; k++)
                    {
                        int e1 = rng.Next(3), e2 = (e1 + 1 + rng.Next(2)) % 3;
                        Vector2 a = Vector2.Lerp(t[e1], t[(e1 + 1) % 3], R()), b = Vector2.Lerp(t[e2], t[(e2 + 1) % 3], R());
                        Sag(a, b, 0.02f, cross, 0.75f);
                    }
                    break;
                }
                default: // small tight orb held out on long guy lines
                {
                    Vector2 c = centre + new Vector2(R() - 0.5f, R() - 0.5f) * 0.1f;
                    Orb(c, 14 + rng.Next(4), 0.26f, 0.035f, 0.05f, 0.05f);
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI / 2f + R() * 0.8f;
                        Line(px, n, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.2f, c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.6f, spoke, 0.8f);
                    }
                    break;
                }
            }
        }

        private static void DrawStrands(Color[] px, int n, System.Random rng)
        {
            // A few threads hanging from the top edge, with dust caught on them.
            for (int i = 0; i < 4; i++)
            {
                float x = 0.2f + i * 0.2f + ((float)rng.NextDouble() - 0.5f) * 0.1f;
                float len = 0.5f + (float)rng.NextDouble() * 0.5f;
                Vector2 prev = new Vector2(x, 1f);
                for (float v = 1f; v > 1f - len; v -= 0.05f)
                {
                    Vector2 next = new Vector2(x + Mathf.Sin(v * 9f + i) * 0.02f, v - 0.05f);
                    Line(px, n, prev, next, 0.8f * n / 256f, 0.9f);
                    prev = next;
                }
                Line(px, n, prev, prev + new Vector2(0f, -0.01f), 2.5f * n / 256f, 0.8f); // dust clump
            }
        }

        private static void DrawScratches(Color[] px, int n, System.Random rng)
        {
            // Four long parallel gouges, as if something dragged its fingers down the wall.
            for (int i = 0; i < 4; i++)
            {
                float x = 0.25f + i * 0.15f + ((float)rng.NextDouble() - 0.5f) * 0.03f;
                Vector2 prev = new Vector2(x, 0.95f);
                for (float v = 0.95f; v > 0.05f; v -= 0.04f)
                {
                    Vector2 next = new Vector2(x + Mathf.Sin(v * 6f + i) * 0.02f, v - 0.04f);
                    Line(px, n, prev, next, Mathf.Lerp(0.6f, 1.6f, Mathf.Sin(v * Mathf.PI)), 0.95f);
                    prev = next;
                }
            }
        }
    }
}
