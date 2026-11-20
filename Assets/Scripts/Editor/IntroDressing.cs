using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Set pieces for the intro that give it the ending's finish: a lived-in flat (curtains, books, a laptop
    /// glowing on the table, a coat by the door, a radiator, dust in the lamp light), a storm sky that the
    /// lightning lights up, ground mist along the road, visible headlight beams in the rain, roadside reflector
    /// posts, a lit instrument cluster in the car, a lightning bolt, and a colour grade for each part.
    /// </summary>
    public static class IntroDressing
    {
        // ------------------------------------------------------------------ the flat

        /// <summary>
        /// Everything that makes the room someone's: room is the flat's group, p maps flat-local metres to the world.
        /// Window x 2.2..4.0 on the north wall (z = depth); the sofa faces it; the sideboard is on the east wall.
        /// </summary>
        public static void FlatDetails(Transform room, System.Func<float, float, float, Vector3> p, float width, float depth, float height)
        {
            var rng = new System.Random(1147);
            float R() => (float)rng.NextDouble();

            // Heavy curtains, half drawn, in folds; a rod above.
            Material curtain = Mat("Curtain", new Color(0.22f, 0.07f, 0.06f), smoothness: 0.05f);
            Material rod = Mat("CurtainRod", new Color(0.12f, 0.1f, 0.08f), smoothness: 0.5f);
            Box(room, "CurtainRod", p(3.1f, 2.38f, depth - 0.12f), new Vector3(2.9f, 0.03f, 0.03f), rod, collider: false);
            foreach (float x0 in new[] { 1.72f, 3.98f })
                for (int i = 0; i < 6; i++)
                {
                    float x = x0 + i * 0.075f;
                    GameObject fold = Box(room, "CurtainFold", p(x, 1.18f, depth - 0.13f + (i % 2) * 0.035f), new Vector3(0.09f, 2.36f, 0.025f), curtain, collider: false);
                    fold.transform.localRotation = Quaternion.Euler(0f, (i % 2 == 0 ? 14f : -14f), (R() - 0.5f) * 1.5f);
                }

            // Radiator under the window.
            Material paint = Mat("RadiatorPaint", new Color(0.72f, 0.7f, 0.66f), smoothness: 0.35f);
            for (int i = 0; i < 12; i++)
                Box(room, "RadiatorFin", p(2.45f + i * 0.11f, 0.45f, depth - 0.1f), new Vector3(0.06f, 0.55f, 0.08f), paint, collider: false);
            Box(room, "RadiatorPipe", p(3.05f, 0.15f, depth - 0.1f), new Vector3(1.5f, 0.03f, 0.03f), paint, collider: false);

            // Books: a leaning row on the floor by the bookcase, a stack on the coffee table, one face down open.
            Material[] covers =
            {
                Mat("BookRed", new Color(0.35f, 0.08f, 0.06f)), Mat("BookGreen", new Color(0.08f, 0.2f, 0.12f)),
                Mat("BookBlue", new Color(0.08f, 0.1f, 0.25f)), Mat("BookTan", new Color(0.55f, 0.45f, 0.3f)),
                Mat("BookBlack", new Color(0.05f, 0.05f, 0.05f)), Mat("BookCream", new Color(0.75f, 0.7f, 0.6f)),
            };
            float bz = 2.55f;
            for (int i = 0; i < 9; i++)
            {
                float h = 0.2f + R() * 0.09f, t = 0.025f + R() * 0.03f;
                GameObject b = Box(room, "Book", p(0.24f, h / 2f, bz), new Vector3(0.16f, h, t), covers[rng.Next(covers.Length)], collider: false);
                if (i == 8) b.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
                bz -= t + 0.004f;
            }
            float by = 0.45f;
            for (int i = 0; i < 3; i++)
            {
                float t = 0.03f + R() * 0.02f;
                Box(room, "Book", p(2.25f, by + t / 2f, 2.1f), new Vector3(0.22f, t, 0.16f), covers[rng.Next(covers.Length)], collider: false)
                    .transform.localRotation = Quaternion.Euler(0f, (R() - 0.5f) * 30f, 0f);
                by += t;
            }

            // A laptop open on the table, its screen the only cold light in here (a forum post about the station fire).
            Material shell = Mat("LaptopShell", new Color(0.12f, 0.12f, 0.13f), smoothness: 0.6f);
            Transform laptop = Group(room, "Laptop", p(3.05f, 0.455f, 1.85f));
            laptop.localRotation = Quaternion.Euler(0f, 192f, 0f);
            Box(laptop, "Base", new Vector3(0f, 0.008f, 0f), new Vector3(0.34f, 0.016f, 0.24f), shell, collider: false, isStatic: false);
            Transform lid = Group(laptop, "Lid", new Vector3(0f, 0.016f, 0.12f));
            lid.localRotation = Quaternion.Euler(-105f, 0f, 0f);
            Box(lid, "Back", new Vector3(0f, 0f, -0.115f), new Vector3(0.34f, 0.008f, 0.23f), shell, collider: false, isStatic: false);
            Box(lid, "Screen", new Vector3(0f, -0.005f, -0.115f), new Vector3(0.31f, 0.002f, 0.2f), Mat("LaptopScreen", new Color(0.35f, 0.45f, 0.6f), emissive: true, smoothness: 0.9f), collider: false, isStatic: false);
            Light glow = PointLight(room, "LaptopGlow", p(3.0f, 0.62f, 1.7f), new Color(0.55f, 0.68f, 1f), 0.12f, 1.8f);
            glow.shadows = LightShadows.None;

            // Takeaway box, a glass with a finger of whisky left, crumpled paper.
            Box(room, "TakeawayBox", p(2.35f, 0.47f, 1.8f), new Vector3(0.3f, 0.04f, 0.3f), Mat("Cardboard", new Color(0.5f, 0.38f, 0.24f)), collider: false)
                .transform.localRotation = Quaternion.Euler(0f, 23f, 0f);
            Material glass = Mat("TumblerGlass", new Color(0.7f, 0.75f, 0.8f, 0.25f), smoothness: 0.95f);
            EndingBuilder.MakeTransparent(glass);
            Prim(room, "Tumbler", PrimitiveType.Cylinder, p(2.6f, 0.49f, 2.15f), new Vector3(0.07f, 0.045f, 0.07f), glass, collider: false);
            Prim(room, "Whisky", PrimitiveType.Cylinder, p(2.6f, 0.465f, 2.15f), new Vector3(0.06f, 0.012f, 0.06f), Mat("Whisky", new Color(0.55f, 0.28f, 0.05f), smoothness: 0.9f), collider: false);
            Material paper = Mat("CrumpledPaper", new Color(0.8f, 0.78f, 0.72f));
            for (int i = 0; i < 4; i++)
                Prim(room, "CrumpledPaper", PrimitiveType.Sphere, p(1.6f + R() * 1.8f, 0.04f, 1.3f + R() * 0.9f), Vector3.one * (0.06f + R() * 0.03f), paper, new Vector3(R() * 90f, R() * 90f, 0f), collider: false);

            // A coat on a hook by the door, a ceiling light (off), a switch plate, a socket.
            Material coat = Mat("Coat", new Color(0.1f, 0.1f, 0.11f), smoothness: 0.08f);
            Box(room, "Hook", p(0.04f, 1.75f, 1.85f), new Vector3(0.04f, 0.04f, 0.04f), rod, collider: false);
            // The coat: shoulders on the hook, the body hanging straight, slightly turned.
            Box(room, "CoatShoulders", p(0.11f, 1.66f, 1.85f), new Vector3(0.12f, 0.1f, 0.4f), coat, collider: false).transform.localRotation = Quaternion.Euler(0f, 8f, 0f);
            Box(room, "CoatBody", p(0.1f, 1.2f, 1.85f), new Vector3(0.09f, 0.85f, 0.44f), coat, collider: false).transform.localRotation = Quaternion.Euler(0f, 8f, 1.5f);
            Prim(room, "CeilingRose", PrimitiveType.Cylinder, p(width / 2f, height - 0.01f, depth / 2f), new Vector3(0.12f, 0.01f, 0.12f), paint, collider: false);
            Prim(room, "Bulb", PrimitiveType.Sphere, p(width / 2f, height - 0.28f, depth / 2f), Vector3.one * 0.07f, Mat("DeadBulb", new Color(0.6f, 0.6f, 0.58f), smoothness: 0.8f), collider: false);
            Box(room, "Flex", p(width / 2f, height - 0.14f, depth / 2f), new Vector3(0.008f, 0.26f, 0.008f), Mat("Flex", new Color(0.05f, 0.05f, 0.05f)), collider: false);
            Box(room, "Socket", p(width - 0.01f, 0.3f, 3.6f), new Vector3(0.02f, 0.08f, 0.14f), paint, collider: false);

            // Picture hooks where frames used to hang (lighter squares on the wallpaper).
            Material ghost = EndingBuilder.Textured("WallGhost", "beige_wall_001", new Color(0.7f, 0.68f, 0.63f), 0.1f);
            Box(room, "UnfadedPatch", p(2.0f, 1.75f, 0.012f), new Vector3(0.5f, 0.38f, 0.004f), ghost, collider: false);
            Box(room, "UnfadedPatch", p(3.4f, 1.6f, 0.012f), new Vector3(0.3f, 0.42f, 0.004f), ghost, collider: false);
        }

        /// <summary>Slow dust drifting in the lamp light.</summary>
        public static void Dust(Transform parent, Vector3 at, Vector3 box, Color tint)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.02f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
            main.startColor = tint;
            main.maxParticles = 70;
            var emission = ps.emission;
            emission.rateOverTime = 7f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = box;
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.04f;
            noise.frequency = 0.3f;
            Fade(ps);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = EndingBuilder.SoftParticles("IntroDust", Color.white);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        /// <summary>Soft light blobs far outside the window (street lamps, windows, a crossing): bokeh behind the rain.</summary>
        public static void CityBokeh(Transform parent, Vector3 centre, int count, int seed)
        {
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            Material m = EndingBuilder.SoftParticles("CityBokeh", Color.white);
            for (int i = 0; i < count; i++)
            {
                Color c = R() < 0.55f ? new Color(1f, 0.62f, 0.3f, 0.5f) : R() < 0.5f ? new Color(0.65f, 0.75f, 1f, 0.4f) : new Color(1f, 0.25f, 0.2f, 0.4f);
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = "Bokeh";
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(parent, false);
                q.transform.position = centre + new Vector3((R() - 0.5f) * 40f, (R() - 0.6f) * 14f, R() * 25f);
                q.transform.localScale = Vector3.one * (0.8f + R() * 1.6f);
                var r = q.GetComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                var block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", c);
                r.SetPropertyBlock(block);
            }
        }

        // ------------------------------------------------------------------ the storm

        /// <summary>
        /// A dome of heavy, torn storm cloud (texture made here from layered noise) over the hill; nearly black,
        /// lit up by the lightning (IntroSequence tints it through flashSky).
        /// </summary>
        public static Renderer StormSky(Transform parent, Vector3 centre, float radius)
        {
            Texture2D clouds = CloudTexture();
            Material m = UnlitMat("StormClouds", new Color(0.05f, 0.055f, 0.07f));
            m.SetTexture("_BaseMap", clouds);
            m.SetTextureScale("_BaseMap", new Vector2(3f, 1.5f));
            EditorUtility.SetDirty(m);
            var go = new GameObject("StormSky");
            go.transform.SetParent(parent, false);
            go.transform.position = centre;
            go.AddComponent<MeshFilter>().sharedMesh = Dome(radius);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
        }

        private static Texture2D CloudTexture()
        {
            const string path = "Assets/Art/Textures/Generated/StormClouds.png";
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (existing != null) return existing;
            const int n = 512;
            var tex = new Texture2D(n, n, TextureFormat.RGB24, false);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = x / (float)n, v = y / (float)n, f = 0f, amp = 0.55f, freq = 3f;
                    for (int o = 0; o < 6; o++)
                    {
                        // Tileable noise: sample on a torus.
                        float a = u * Mathf.PI * 2f, b = v * Mathf.PI * 2f;
                        float nx = Mathf.Cos(a) * freq, ny = Mathf.Sin(a) * freq, nz = Mathf.Cos(b) * freq, nw = Mathf.Sin(b) * freq;
                        f += amp * (Mathf.PerlinNoise(nx + nz * 0.7f + 11f, ny + nw * 0.7f + 3f) * 0.5f + Mathf.PerlinNoise(nz + 5f, nw + ny * 0.3f + 7f) * 0.5f);
                        amp *= 0.5f;
                        freq *= 2f;
                    }
                    float c = Mathf.SmoothStep(0.25f, 0.85f, f);
                    // Darker underbellies, a few brighter torn edges.
                    float k = Mathf.Lerp(0.25f, 1f, c * c);
                    px[y * n + x] = new Color(k * 0.92f, k * 0.95f, k, 1f);
                }
            tex.SetPixels(px);
            tex.Apply();
            EnsureFolder("Assets/Art/Textures/Generated");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.maxTextureSize = 512;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>An inward-facing upper hemisphere (the horizon ring slightly below the eye line).</summary>
        private static Mesh Dome(float radius)
        {
            const int seg = 48, rings = 14;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float el = Mathf.Lerp(-8f, 90f, r / (float)rings) * Mathf.Deg2Rad;
                for (int s = 0; s <= seg; s++)
                {
                    float az = s / (float)seg * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(el) * Mathf.Sin(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Cos(az)) * radius);
                    uv.Add(new Vector2(s / (float)seg, r / (float)rings));
                    if (r < rings && s < seg)
                    {
                        int a = r * (seg + 1) + s, b = a + seg + 1;
                        tri.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b }); // wound to face inwards
                    }
                }
            }
            var mesh = new Mesh { name = "SkyDome" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Low mist lying along the road and between the trees.</summary>
        public static void GroundMist(Transform parent, Vector3 from, Vector3 to, Color tint)
        {
            var go = new GameObject("GroundMist");
            go.transform.SetParent(parent, false);
            go.transform.position = (from + to) / 2f + Vector3.up * 0.5f;
            go.transform.rotation = Quaternion.LookRotation(new Vector3(to.x - from.x, 0f, to.z - from.z));
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 24f;
            main.startSpeed = 0.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(5f, 10f);
            main.startColor = tint;
            main.maxParticles = 120;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 5f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            float len = Vector3.Distance(new Vector3(from.x, 0f, from.z), new Vector3(to.x, 0f, to.z));
            shape.scale = new Vector3(26f, 0.8f, len);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.05f, 0.05f);
            Fade(ps);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = EndingBuilder.SoftParticles("IntroMist", Color.white);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 10f;
        }

        /// <summary>A jagged lightning bolt far off over the hills (hidden; IntroSequence shapes and flashes it).</summary>
        public static LineRenderer Bolt(Transform parent)
        {
            var go = new GameObject("LightningBolt");
            go.transform.SetParent(parent, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 0;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 1.8f;
            lr.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.25f));
            lr.numCapVertices = 2;
            lr.sharedMaterial = UnlitMat("LightningBolt", new Color(0.85f, 0.9f, 1f) * 4f);
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.enabled = false;
            return lr;
        }

        // ------------------------------------------------------------------ the road and the car

        /// <summary>White posts with red reflectors along the verge, catching the headlights.</summary>
        public static void ReflectorPosts(Transform parent, Vector3 from, Vector3 to, Vector3 right)
        {
            Material post = Mat("ReflectorPost", new Color(0.75f, 0.75f, 0.72f), smoothness: 0.4f);
            Material red = Mat("Reflector", new Color(0.9f, 0.08f, 0.04f), emissive: true, smoothness: 0.9f);
            red.SetColor("_EmissionColor", new Color(0.5f, 0.03f, 0.02f));
            Transform posts = Group(parent, "ReflectorPosts");
            float len = Vector3.Distance(from, to);
            Vector3 dir = (to - from).normalized;
            for (float d = 8f; d < len; d += 22f)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 q = from + dir * (d + (side > 0f ? 11f : 0f)) + right * side * 3.3f;
                    q.y = EndingBuilder.H(q.x, q.z);
                    Box(posts, "Post", q + Vector3.up * 0.5f, new Vector3(0.1f, 1f, 0.1f), post, collider: false)
                        .transform.rotation = Quaternion.LookRotation(dir);
                    Box(posts, "Reflector", q + Vector3.up * 0.85f - dir * 0.055f, new Vector3(0.06f, 0.12f, 0.01f), red, collider: false)
                        .transform.rotation = Quaternion.LookRotation(dir);
                }
        }

        /// <summary>The headlight beams made visible by the rain: long soft cones fading out with distance.</summary>
        public static void HeadlightBeams(Transform car)
        {
            Mesh beam = BeamMesh(30f, 6.5f);
            Material m = EndingBuilder.SoftParticles("HeadlightBeam", new Color(1f, 0.95f, 0.85f));
            foreach (float x in new[] { -0.27f, 0.97f })
            {
                var go = new GameObject("Beam");
                go.transform.SetParent(car, false);
                go.transform.localPosition = new Vector3(x, -0.5f, 2.15f);
                go.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);
                go.AddComponent<MeshFilter>().sharedMesh = beam;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }

        /// <summary>An open cone along +Z, vertex alpha fading with length and towards the rim (no texture needed).</summary>
        private static Mesh BeamMesh(float length, float endRadius)
        {
            const int seg = 20, rings = 8;
            var v = new List<Vector3>();
            var col = new List<Color>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float t = r / (float)rings, rad = Mathf.Lerp(0.12f, endRadius, t);
                for (int s = 0; s <= seg; s++)
                {
                    float a = s / (float)seg * Mathf.PI * 2f;
                    v.Add(new Vector3(Mathf.Cos(a) * rad, Mathf.Sin(a) * rad, t * length));
                    col.Add(new Color(1f, 1f, 1f, 0.07f * (1f - t) * (1f - t)));
                    uv.Add(new Vector2(0.5f, 0.5f)); // centre of the soft dot: fully opaque, alpha from the vertex colour
                    if (r < rings && s < seg)
                    {
                        int i = r * (seg + 1) + s, j = i + seg + 1;
                        tri.AddRange(new[] { i, j, i + 1, i + 1, j, j + 1, i, i + 1, j, i + 1, j + 1, j });
                    }
                }
            }
            var mesh = new Mesh { name = "HeadlightBeam" };
            mesh.SetVertices(v);
            mesh.SetColors(col);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Instrument cluster behind the wheel: two lit gauges with needles, warning lamps; a charm swinging from the mirror.</summary>
        public static Transform Gauges(Transform cab)
        {
            Material face = Mat("GaugeFace", new Color(0.02f, 0.02f, 0.025f), smoothness: 0.8f);
            Material lit = UnlitMat("GaugeLight", new Color(0.95f, 0.55f, 0.18f));
            Material needle = UnlitMat("GaugeNeedle", new Color(1f, 0.3f, 0.12f));
            Transform cluster = Group(cab, "Cluster", new Vector3(0f, -0.25f, 0.68f));
            cluster.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            Box(cluster, "Hood", new Vector3(0f, 0.05f, 0.02f), new Vector3(0.36f, 0.02f, 0.1f), Mat("CarDash", new Color(0.035f, 0.035f, 0.04f), smoothness: 0.35f), collider: false, isStatic: false);
            foreach (float x in new[] { -0.08f, 0.08f })
            {
                Prim(cluster, "Gauge", PrimitiveType.Cylinder, new Vector3(x, 0f, 0f), new Vector3(0.11f, 0.003f, 0.11f), face, new Vector3(90f, 0f, 0f), collider: false);
                for (int i = 0; i < 9; i++)
                {
                    float a = (-120f + i * 30f) * Mathf.Deg2Rad;
                    Box(cluster, "Tick", new Vector3(x + Mathf.Sin(a) * 0.043f, Mathf.Cos(a) * 0.043f, -0.004f), new Vector3(0.002f, 0.008f, 0.001f), lit, collider: false, isStatic: false)
                        .transform.localRotation = Quaternion.Euler(0f, 0f, -(-120f + i * 30f));
                }
                Transform pivot = Group(cluster, "Needle", new Vector3(x, 0f, -0.005f));
                pivot.localRotation = Quaternion.Euler(0f, 0f, x < 0f ? 10f : -40f);
                Box(pivot, "Blade", new Vector3(0f, 0.02f, 0f), new Vector3(0.0025f, 0.04f, 0.001f), needle, collider: false, isStatic: false);
            }
            Box(cluster, "LampAmber", new Vector3(0f, -0.035f, -0.004f), new Vector3(0.008f, 0.006f, 0.001f), lit, collider: false, isStatic: false);
            var glowGo = new GameObject("ClusterGlow");
            glowGo.transform.SetParent(cluster, false);
            glowGo.transform.localPosition = new Vector3(0f, 0.02f, -0.12f);
            Light l = glowGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = 0.6f;
            l.intensity = 0.35f;
            l.color = new Color(1f, 0.55f, 0.2f);
            l.shadows = LightShadows.None;

            // A small charm hanging from the mirror on a cord (it swings with the car).
            Transform charm = Group(cab, "MirrorCharm", new Vector3(0.2f, 0.18f, 0.66f));
            Box(charm, "Cord", new Vector3(0f, -0.05f, 0f), new Vector3(0.002f, 0.1f, 0.002f), Mat("Flex", new Color(0.05f, 0.05f, 0.05f)), collider: false, isStatic: false);
            Prim(charm, "Charm", PrimitiveType.Sphere, new Vector3(0f, -0.11f, 0f), new Vector3(0.025f, 0.035f, 0.01f), Mat("CharmWood", new Color(0.3f, 0.18f, 0.08f), smoothness: 0.5f), collider: false);
            foreach (Renderer r in cluster.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            foreach (Renderer r in charm.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return charm;
        }

        // ------------------------------------------------------------------ grading

        /// <summary>
        /// The flat: lamp-warm highlights over cold blue shadows, soft focus on the city outside the window,
        /// bloom on the lamp, a little grain. Starts switched off; IntroSequence turns it on for the flat.
        /// </summary>
        public static GameObject FlatLook() => Look("IntroFlat", flat: true);

        /// <summary>The drive: cold, wet, high-contrast night with bloom on the lamps and beams.</summary>
        public static GameObject DriveLook() => Look("IntroDrive", flat: false);

        private static GameObject Look(string name, bool flat)
        {
            EnsureFolder(GeneratedDir);
            string path = GeneratedDir + "/" + name + "_PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null) AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(flat ? 0.85f : 0.9f);
            bloom.intensity.Override(flat ? 0.9f : 0.8f);
            bloom.scatter.Override(0.75f);
            bloom.tint.Override(flat ? new Color(1f, 0.85f, 0.7f) : new Color(0.85f, 0.9f, 1f));
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(flat ? 0.45f : 0.35f);
            ca.contrast.Override(flat ? 18f : 20f);
            ca.saturation.Override(flat ? -6f : -18f);
            var smh = profile.Add<SplitToning>(true);
            smh.shadows.Override(flat ? new Color(0.25f, 0.4f, 0.55f) : new Color(0.2f, 0.35f, 0.55f));
            smh.highlights.Override(flat ? new Color(0.75f, 0.55f, 0.35f) : new Color(0.6f, 0.6f, 0.65f));
            smh.balance.Override(flat ? -10f : -25f);
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(flat ? 4f : -14f);
            if (flat)
            {
                var dof = profile.Add<DepthOfField>(true);
                dof.mode.Override(DepthOfFieldMode.Gaussian);
                dof.gaussianStart.Override(4.5f);
                dof.gaussianEnd.Override(16f);
                dof.gaussianMaxRadius.Override(1f);
            }
            var grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium2);
            grain.intensity.Override(flat ? 0.2f : 0.25f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(flat ? 0.38f : 0.42f);
            vig.smoothness.Override(0.55f);
            AssetDatabase.CreateAsset(profile, path);
            foreach (VolumeComponent comp in profile.components) AssetDatabase.AddObjectToAsset(comp, profile);
            AssetDatabase.SaveAssets();
            var go = new GameObject(flat ? "FlatLook" : "DriveLook");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 2f;
            volume.sharedProfile = profile;
            go.SetActive(flat);
            return go;
        }

        private static void Fade(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }
    }
}
