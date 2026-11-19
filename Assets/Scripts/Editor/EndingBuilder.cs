using System.Collections.Generic;
using System.IO;
using LastSignal.Environment;
using LastSignal.GameFlow;
using LastSignal.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// The ending: dawn on a grassy hill above the radio station, three months later. A weathered granite
    /// headstone for Maya with engraved letters, flowers in a jar and a candle in a lantern; long grass and
    /// wildflowers; a treeline and an old fence; the station and its lattice tower (red beacon blinking) in the
    /// morning haze. The sun rises behind the tower (EndingSequence animates it), mist drifts and burns off,
    /// dust glints in the light; filmic grading with bloom and a soft depth of field. Sound: dawn birdsong,
    /// wind in the grass, a quiet music bed. No credits (they are on the main menu).
    /// </summary>
    public static class EndingBuilder
    {
        private const string GeneratedTex = "Assets/Art/Textures/Generated";

        // Ground height: gentle rolls, rising into hills behind the station; flat (0) at the memorial.
        private static float Raw(float x, float z) =>
            0.35f * Mathf.PerlinNoise(x * 0.08f + 3.1f, z * 0.08f + 7.7f)
            + 1.1f * Mathf.PerlinNoise(x * 0.025f + 11f, z * 0.025f + 2f)
            + Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((z - 30f) / 70f)) * 7f * Mathf.PerlinNoise(x * 0.02f + 5f, 1.3f)
            - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((z - 55f) / 25f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(1f - Mathf.Abs(x + 28f) / 25f)) * 2.5f;
        private static readonly float Origin = Raw(0f, 0f);
        public static float H(float x, float z)
        {
            // Keep a small level patch round the memorial.
            float flat = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((new Vector2(x, z).magnitude - 1.5f) / 4f));
            return (Raw(x, z) - Origin) * flat;
        }

        public static void Build(string path, System.Func<string, GameObject> canvas)
        {
            NewScene("Ending");
            Sky();
            Transform env = Group(null, "Environment");
            Light sun = DirectionalLight(env, "Sun", new Vector3(1f, 165f, 0f), new Color(1f, 0.45f, 0.22f), 0.5f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.8f;
            RenderSettings.sun = sun;

            Ground(env);
            Vector3 stone = new Vector3(0f, 0f, 0f);
            Headstone(env, stone);
            GameObject cassette = Cassette(env, stone + new Vector3(0.22f, 0.13f, -0.22f));
            Flowers(env, stone + new Vector3(-0.3f, 0.1f, -0.24f));
            Candle(env, stone + new Vector3(0.36f, 0.1f, -0.18f));
            Grass(env);
            Trees(env);
            Fence(env);
            Station(env);
            ParticleSystem mist = Mist(env);
            Dust(env, stone);
            PostFx();

            // Camera: from down the slope, drifting in to the headstone, then lifting to the sunrise.
            Transform rig = Group(null, "CameraRig");
            var camGo = new GameObject("EndingCamera") { tag = "MainCamera" };
            camGo.transform.SetParent(rig, false);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 400f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
            Transform start = Marker(null, "CamStart", new Vector3(-2.6f, H(-2.6f, -9f) + 1.55f, -9f), 0f);
            start.rotation = Quaternion.LookRotation(new Vector3(-6f, 3.5f, 60f) - start.position) * Quaternion.Euler(0f, 0f, 0f);
            Transform end = Marker(null, "CamEnd", new Vector3(0.25f, 0.95f, -1.75f), 0f);
            end.rotation = Quaternion.LookRotation(stone + new Vector3(0.02f, 0.62f, 0f) - end.position);
            Transform final = Marker(null, "CamFinal", new Vector3(0.6f, 1.75f, -2.6f), 0f);
            final.rotation = Quaternion.LookRotation(new Vector3(-24f, 9f, 88f) - final.position);
            rig.SetPositionAndRotation(start.position, start.rotation);

            // Sound: wind in the grass and a quiet music bed (birdsong is the AudioManager's dawn ambience).
            AudioSource wind = Loop(rig, "Wind", "Assets/Audio/Resources/Sounds/wind.ogg");
            AudioSource music = Loop(rig, "Music", "Assets/SourceFiles/SoundFX/Music_Ethereal.ogg");
            if (music != null) music.loop = false;

            // UI: black fade, subtitles, THE END, menu button.
            GameObject root = canvas("EndingCanvas");
            var c = new Vector2(0.5f, 0.5f);
            var story = root.AddComponent<StoryHUD>();
            RectTransform subtitlePanel = UIRect(root.transform, "Subtitles");
            Place(subtitlePanel, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1100f, 120f));
            UIImage(subtitlePanel, EnsureSprite("Square", SpriteShape.Square), new Color(0f, 0f, 0f, 0.45f), raycast: false);
            TMP_Text subtitleText = UIText(subtitlePanel, "Text", "", 34, c, Vector2.zero, new Vector2(1060f, 110f), TextAlignmentOptions.Center, wrap: true);
            Set(story, "subtitlePanel", subtitlePanel.gameObject);
            Set(story, "subtitleText", subtitleText);
            subtitlePanel.gameObject.SetActive(false);

            TMP_Text theEnd = UIText(root.transform, "TheEnd", "THE END", 110, c, new Vector2(0f, 40f), new Vector2(1400f, 200f));
            theEnd.characterSpacing = 30f;
            theEnd.color = new Color(1f, 0.96f, 0.9f);
            var theEndGroup = theEnd.gameObject.AddComponent<CanvasGroup>();
            RectTransform fadeRect = UIRect(root.transform, "Fade");
            Stretch(fadeRect);
            UIImage(fadeRect, null, Color.black, raycast: false);
            var fadeGroup = fadeRect.gameObject.AddComponent<CanvasGroup>();
            fadeGroup.blocksRaycasts = false;
            theEnd.transform.SetAsLastSibling(); // THE END stays over the final fade to black
            Button menuButton = UIButton(root.transform, "MainMenuButton", "MAIN MENU", new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(420f, 95f), 36, null);
            menuButton.transform.SetAsLastSibling();

            var ending = root.AddComponent<EndingSequence>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(menuButton.onClick, ending.ReturnToMenu);
            Set(ending, "cameraRig", rig);
            Set(ending, "cameraStart", start);
            Set(ending, "cameraEnd", end);
            Set(ending, "cameraFinal", final);
            SetLines(ending, "lines", new[]
            {
                L("", "Three months later.", 3.5f),
                L("ALEX", "I listened to the whole tape again, Maya. Every word.", 4.5f),
                L("ALEX", "You got me out. You stayed. And I'm done blaming myself - I promised you I'd live.", 6f),
                L("", "[Alex sets the old cassette beside the memorial.]", 3.5f),
                L("ALEX", "I heard you this time, Maya. Goodbye.", 5f),
            });
            SetValue(ending, "cassetteLine", 3);
            SetValue(ending, "dollySeconds", 21f);
            SetValue(ending, "ambientStart", new Color(0.36f, 0.3f, 0.3f));
            SetValue(ending, "fogStart", new Color(0.62f, 0.46f, 0.42f));
            Set(ending, "cassette", cassette);
            Set(ending, "theEnd", theEndGroup);
            Set(ending, "fade", fadeGroup);
            Set(ending, "menuButton", menuButton.gameObject);
            Set(ending, "sun", sun);
            Set(ending, "mist", mist);
            if (wind != null) Set(ending, "wind", wind);
            if (music != null) Set(ending, "music", music);

            EventSystemObject();
            SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
            Debug.Log("[EndingBuilder] Built " + path);
        }

        // ------------------------------------------------------------------ sky, light, grading

        private static void Sky()
        {
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/SunriseSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, MaterialDir + "/SunriseSky.mat");
            }
            sky.SetFloat("_SunDisk", 2f); // high quality disk
            sky.SetFloat("_SunSize", 0.045f);
            sky.SetFloat("_SunSizeConvergence", 6f);
            sky.SetFloat("_AtmosphereThickness", 1.05f);
            sky.SetColor("_SkyTint", new Color(0.4f, 0.46f, 0.62f));
            sky.SetColor("_GroundColor", new Color(0.32f, 0.27f, 0.24f));
            sky.SetFloat("_Exposure", 1.1f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.28f, 0.22f, 0.22f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.55f, 0.38f, 0.32f);
            RenderSettings.fogDensity = 0.03f;
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        }

        private static void PostFx()
        {
            EnsureFolder(GeneratedDir);
            string p = GeneratedDir + "/Ending_PostFX.asset";
            if (AssetDatabase.LoadAssetAtPath<Object>(p) != null) AssetDatabase.DeleteAsset(p);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.75f);
            bloom.scatter.Override(0.72f);
            bloom.tint.Override(new Color(1f, 0.88f, 0.75f));
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.25f);
            ca.contrast.Override(14f);
            ca.saturation.Override(6f);
            ca.colorFilter.Override(new Color(1f, 0.96f, 0.9f));
            var wb = profile.Add<WhiteBalance>(true);
            wb.temperature.Override(12f);
            var dof = profile.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(9f);
            dof.gaussianEnd.Override(45f);
            dof.gaussianMaxRadius.Override(0.7f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.3f);
            vig.smoothness.Override(0.55f);
            AssetDatabase.CreateAsset(profile, p);
            foreach (VolumeComponent comp in profile.components) AssetDatabase.AddObjectToAsset(comp, profile);
            AssetDatabase.SaveAssets();
            var go = new GameObject("PostFX");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            volume.sharedProfile = profile;
        }

        private static AudioSource Loop(Transform parent, string name, string clipPath)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
            if (clip == null) return null;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            s.volume = 0f;
            return s;
        }

        // ------------------------------------------------------------------ ground and grass

        private static Material Textured(string name, string tex, Color tint, float smooth)
        {
            Material m = Mat(name, tint, smoothness: smooth);
            var diff = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/" + tex + "_diff.jpg");
            var nor = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureDir + "/" + tex + "_nor.jpg");
            if (diff != null) m.SetTexture("_BaseMap", diff);
            if (nor != null)
            {
                m.SetTexture("_BumpMap", nor);
                m.SetFloat("_BumpScale", 1f);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetColor("_BaseColor", tint);
            EditorUtility.SetDirty(m);
            return m;
        }

        private static void Ground(Transform env)
        {
            const float x0 = -90f, x1 = 90f, z0 = -40f, z1 = 160f, step = 2f;
            int nx = Mathf.RoundToInt((x1 - x0) / step) + 1, nz = Mathf.RoundToInt((z1 - z0) / step) + 1;
            var verts = new Vector3[nx * nz];
            var uvs = new Vector2[nx * nz];
            for (int iz = 0; iz < nz; iz++)
            for (int ix = 0; ix < nx; ix++)
            {
                float x = x0 + ix * step, z = z0 + iz * step;
                verts[iz * nx + ix] = new Vector3(x, H(x, z), z);
                uvs[iz * nx + ix] = new Vector2(x, z) / 4f;
            }
            var tris = new List<int>();
            for (int iz = 0; iz < nz - 1; iz++)
            for (int ix = 0; ix < nx - 1; ix++)
            {
                int a = iz * nx + ix, b = a + 1, c = a + nx, d = c + 1;
                tris.AddRange(new[] { a, c, b, b, c, d });
            }
            var mesh = new Mesh { name = "Hillside", indexFormat = IndexFormat.UInt32, vertices = verts, uv = uvs, triangles = tris.ToArray() };
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            var go = new GameObject("Hillside");
            go.transform.SetParent(env, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = Textured("EndingGrass", "aerial_grass_rock", new Color(0.78f, 0.82f, 0.7f), 0.08f);
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
            go.isStatic = true;
        }

        /// <summary>Tufts of long grass (crossed quads with a drawn blade texture), denser near the memorial.</summary>
        private static void Grass(Transform env)
        {
            Material m = GrassMaterial();
            var rng = new System.Random(77);
            float R() => (float)rng.NextDouble();
            var quads = new List<CombineInstance>();
            Mesh quad = BladeQuad();
            for (int i = 0; i < 2600; i++)
            {
                // Mostly within 14 m, some scattered further out.
                float radius = i < 2000 ? Mathf.Sqrt(R()) * 14f : 14f + R() * 26f;
                float ang = R() * Mathf.PI * 2f;
                float x = Mathf.Cos(ang) * radius, z = Mathf.Sin(ang) * radius + 2f;
                if (new Vector2(x, z).magnitude < 0.75f) continue; // keep the stone clear
                if (Mathf.PerlinNoise(x * 0.3f + 4f, z * 0.3f) < 0.32f) continue; // patchy, like real grass
                float h = 0.35f + R() * 0.35f, w = 0.45f + R() * 0.35f;
                Vector3 p = new Vector3(x, H(x, z) - 0.02f, z);
                float yaw = R() * 180f;
                for (int k = 0; k < 2; k++)
                    quads.Add(new CombineInstance { mesh = quad, transform = Matrix4x4.TRS(p, Quaternion.Euler(R() * 8f - 4f, yaw + k * 90f, R() * 8f - 4f), new Vector3(w, h, 1f)) });
            }
            // Split into a few meshes so far-away chunks can be culled.
            for (int start = 0, chunk = 0; start < quads.Count; start += 1200, chunk++)
            {
                var mesh = new Mesh { name = "Grass_" + chunk, indexFormat = IndexFormat.UInt32 };
                mesh.CombineMeshes(quads.GetRange(start, Mathf.Min(1200, quads.Count - start)).ToArray(), true, true);
                mesh.RecalculateBounds();
                var go = new GameObject("Grass_" + chunk);
                go.transform.SetParent(env, false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.AddComponent<MeshRenderer>();
                r.sharedMaterial = m;
                r.shadowCastingMode = ShadowCastingMode.Off;
                go.isStatic = true;
            }
        }

        private static Mesh bladeQuad;
        private static Mesh BladeQuad()
        {
            if (bladeQuad != null) return bladeQuad;
            bladeQuad = new Mesh { name = "GrassQuad" };
            bladeQuad.vertices = new[] { new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f) };
            bladeQuad.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) };
            // Normals pointing up so the tufts take the sky and sun light like the ground does.
            bladeQuad.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            bladeQuad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            return bladeQuad;
        }

        private static Material GrassMaterial()
        {
            EnsureFolder(GeneratedTex);
            string path = GeneratedTex + "/GrassBlades_v1.png";
            if (!File.Exists(path))
            {
                Texture2D tex = DrawBlades(256);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
                var ti = (TextureImporter)AssetImporter.GetAtPath(path);
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.maxTextureSize = 256;
                ti.SaveAndReimport();
            }
            Material m = Mat("EndingGrassBlades", new Color(0.85f, 0.9f, 0.75f), smoothness: 0.15f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(path));
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.45f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", (float)CullMode.Off);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            m.SetOverrideTag("RenderType", "TransparentCutout");
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A tuft: tapering, curving blades in mixed greens and straw, with a few tiny wildflowers.</summary>
        private static Texture2D DrawBlades(int n)
        {
            var rng = new System.Random(5);
            float R() => (float)rng.NextDouble();
            var px = new Color[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color(0.3f, 0.4f, 0.2f, 0f);
            for (int b = 0; b < 26; b++)
            {
                float baseX = 0.15f + R() * 0.7f, height = 0.45f + R() * 0.53f, bend = (R() - 0.5f) * 0.35f, width = 0.012f + R() * 0.014f;
                Color c = Color.Lerp(new Color(0.22f, 0.36f, 0.12f), new Color(0.55f, 0.55f, 0.28f), R() * R());
                for (float t = 0f; t < 1f; t += 0.5f / n)
                {
                    float x = baseX + bend * t * t, y = t * height, half = width * (1f - t * 0.9f);
                    Color shade = c * (0.7f + 0.45f * t);
                    shade.a = 1f;
                    for (int dx = -4; dx <= 4; dx++)
                    {
                        float px01 = x + dx / (float)n;
                        if (Mathf.Abs(px01 - x) > half) continue;
                        int ix = Mathf.RoundToInt(px01 * (n - 1)), iy = Mathf.RoundToInt(y * (n - 1));
                        if (ix >= 0 && ix < n && iy >= 0 && iy < n) px[iy * n + ix] = shade;
                    }
                }
            }
            // A few small flowers on top of some stems.
            Color[] petals = { new Color(0.95f, 0.95f, 0.9f, 1f), new Color(0.95f, 0.82f, 0.3f, 1f), new Color(0.7f, 0.55f, 0.85f, 1f) };
            for (int f = 0; f < 4; f++)
            {
                Vector2 c = new Vector2(0.2f + R() * 0.6f, 0.5f + R() * 0.4f);
                Color col = petals[f % petals.Length];
                for (int y = -4; y <= 4; y++)
                for (int x = -4; x <= 4; x++)
                {
                    if (x * x + y * y > 14) continue;
                    int ix = Mathf.RoundToInt(c.x * (n - 1)) + x, iy = Mathf.RoundToInt(c.y * (n - 1)) + y;
                    if (ix >= 0 && ix < n && iy >= 0 && iy < n) px[iy * n + ix] = x * x + y * y < 3 ? new Color(0.9f, 0.7f, 0.2f, 1f) : col;
                }
            }
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, true);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        // ------------------------------------------------------------------ memorial

        private static void Headstone(Transform env, Vector3 at)
        {
            Material granite = Textured("EndingGranite", "rock_boulder_dry", new Color(0.72f, 0.72f, 0.7f), 0.25f);
            Transform root = Group(env, "Memorial", at);
            root.localRotation = Quaternion.Euler(-2f, 3f, 1.5f); // settled a little over the years
            // Base slab half sunk in the grass.
            GameObject slab = Box(root, "Base", new Vector3(0f, 0.04f, 0.02f), new Vector3(1.05f, 0.16f, 0.5f), granite);
            // The stone: rounded top, bevelled edges.
            var stone = new GameObject("Headstone");
            stone.transform.SetParent(root, false);
            stone.transform.localPosition = new Vector3(0f, 0.12f, 0.06f);
            Mesh mesh = RoundTopSlab(0.72f, 1.02f, 0.16f);
            stone.AddComponent<MeshFilter>().sharedMesh = mesh;
            stone.AddComponent<MeshRenderer>().sharedMaterial = granite;
            stone.AddComponent<MeshCollider>().sharedMesh = mesh;

            // Engraved inscription (dark, slightly sunk into the face).
            var text = new GameObject("Inscription", typeof(RectTransform));
            text.transform.SetParent(stone.transform, false);
            text.transform.localPosition = new Vector3(0f, 0.58f, -0.081f);
            var tmp = text.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = "<size=150%><cspace=0.25em>MAYA</cspace></size>\n\n<i>She stayed so that\nI could leave.</i>";
            tmp.fontSize = 0.6f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.13f, 0.12f, 0.11f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ((RectTransform)text.transform).sizeDelta = new Vector2(0.62f, 0.6f);
            // A thin lighter rim under the letters reads as a chiselled edge in low sun.
            var rim = Object.Instantiate(text, stone.transform);
            rim.name = "InscriptionEdge";
            rim.transform.localPosition = text.transform.localPosition + new Vector3(0f, -0.004f, 0.0015f);
            rim.GetComponent<TextMeshPro>().color = new Color(0.62f, 0.6f, 0.56f, 0.6f);

        }

        /// <summary>A slab with a semicircular top: front/back faces and a smooth rim, about the bottom-centre.</summary>
        private static Mesh RoundTopSlab(float w, float h, float t)
        {
            var outline = new List<Vector2>();
            float r = w / 2f, straight = h - r;
            outline.Add(new Vector2(-r, 0f));
            outline.Add(new Vector2(r, 0f));
            outline.Add(new Vector2(r, straight));
            for (int i = 1; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI;
                outline.Add(new Vector2(Mathf.Cos(a) * r, straight + Mathf.Sin(a) * r));
            }
            outline.Add(new Vector2(-r, straight));
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            // Front (facing -Z) and back faces as fans from the centre.
            foreach (float zs in new[] { -t / 2f, t / 2f })
            {
                int c = v.Count;
                v.Add(new Vector3(0f, h * 0.45f, zs));
                uv.Add(new Vector2(0.5f, h * 0.45f) / 0.6f);
                foreach (Vector2 p in outline) { v.Add(new Vector3(p.x, p.y, zs)); uv.Add(new Vector2(p.x + 0.5f, p.y) / 0.6f); }
                for (int i = 0; i < outline.Count; i++)
                {
                    int a = c + 1 + i, b = c + 1 + (i + 1) % outline.Count;
                    if (zs < 0f) tri.AddRange(new[] { c, b, a }); else tri.AddRange(new[] { c, a, b });
                }
            }
            // Rim: one quad per outline edge.
            float run = 0f;
            for (int i = 0; i < outline.Count; i++)
            {
                Vector2 p = outline[i], q = outline[(i + 1) % outline.Count];
                int s = v.Count;
                v.Add(new Vector3(p.x, p.y, -t / 2f)); v.Add(new Vector3(q.x, q.y, -t / 2f));
                v.Add(new Vector3(p.x, p.y, t / 2f)); v.Add(new Vector3(q.x, q.y, t / 2f));
                float len = Vector2.Distance(p, q);
                uv.Add(new Vector2(run, 0f) / 0.6f); uv.Add(new Vector2(run + len, 0f) / 0.6f);
                uv.Add(new Vector2(run, t) / 0.6f); uv.Add(new Vector2(run + len, t) / 0.6f);
                run += len;
                tri.AddRange(new[] { s, s + 1, s + 2, s + 2, s + 1, s + 3 });
            }
            var mesh = new Mesh { name = "Headstone" };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static GameObject Cassette(Transform env, Vector3 at)
        {
            Transform c = Group(env, "Cassette", at);
            c.localRotation = Quaternion.Euler(-72f, 12f, 0f); // leaning against the base
            Material shell = Mat("CassetteShell", new Color(0.1f, 0.1f, 0.11f), smoothness: 0.55f);
            Material label = Mat("CassetteLabel", new Color(0.86f, 0.82f, 0.72f), smoothness: 0.1f);
            Material window = Mat("CassetteWindow", new Color(0.25f, 0.2f, 0.17f), smoothness: 0.8f);
            Box(c, "Shell", Vector3.zero, new Vector3(0.1f, 0.064f, 0.012f), shell, collider: false, isStatic: false);
            Box(c, "Label", new Vector3(0f, 0.008f, -0.0062f), new Vector3(0.088f, 0.04f, 0.001f), label, collider: false, isStatic: false);
            Box(c, "Window", new Vector3(0f, 0.004f, -0.0068f), new Vector3(0.045f, 0.014f, 0.001f), window, collider: false, isStatic: false);
            foreach (float x in new[] { -0.019f, 0.019f })
                Prim(c, "Reel", PrimitiveType.Cylinder, new Vector3(x, 0.004f, -0.0072f), new Vector3(0.011f, 0.0006f, 0.011f), shell, new Vector3(90f, 0f, 0f), collider: false);
            var t = new GameObject("Handwriting", typeof(RectTransform));
            t.transform.SetParent(c, false);
            t.transform.localPosition = new Vector3(0f, 0.021f, -0.0072f);
            var tmp = t.AddComponent<TextMeshPro>();
            TMP_FontAsset hand = PaperArt.HandwritingFont();
            if (hand != null) tmp.font = hand;
            tmp.text = "for Alex";
            tmp.fontSize = 0.075f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.12f, 0.12f, 0.35f);
            ((RectTransform)t.transform).sizeDelta = new Vector2(0.08f, 0.015f);
            foreach (Renderer r in c.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return c.gameObject;
        }

        private static void Flowers(Transform env, Vector3 at)
        {
            Transform f = Group(env, "Flowers", at);
            Material glass = Mat("JarGlass", new Color(0.75f, 0.85f, 0.85f, 0.35f), smoothness: 0.95f);
            MakeTransparent(glass);
            Material stem = Mat("Stem", new Color(0.2f, 0.32f, 0.12f), smoothness: 0.1f);
            Material water = Mat("JarWater", new Color(0.6f, 0.7f, 0.7f, 0.25f), smoothness: 0.95f);
            MakeTransparent(water);
            Prim(f, "Jar", PrimitiveType.Cylinder, new Vector3(0f, 0.07f, 0f), new Vector3(0.09f, 0.07f, 0.09f), glass, collider: false);
            Prim(f, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.045f, 0f), new Vector3(0.082f, 0.045f, 0.082f), water, collider: false);
            Material[] blooms =
            {
                Mat("PetalWhite", new Color(0.95f, 0.94f, 0.9f), smoothness: 0.2f),
                Mat("PetalYellow", new Color(0.98f, 0.8f, 0.25f), smoothness: 0.2f),
                Mat("PetalPink", new Color(0.92f, 0.62f, 0.68f), smoothness: 0.2f),
            };
            Material centre = Mat("FlowerCentre", new Color(0.55f, 0.38f, 0.12f), smoothness: 0.1f);
            var rng = new System.Random(3);
            for (int i = 0; i < 8; i++)
            {
                float lean = 6f + (float)rng.NextDouble() * 16f, dir = i * 45f + (float)rng.NextDouble() * 20f;
                Quaternion q = Quaternion.Euler(0f, dir, 0f) * Quaternion.Euler(lean, 0f, 0f);
                float len = 0.22f + (float)rng.NextDouble() * 0.12f;
                Vector3 top = new Vector3(0f, 0.02f, 0f) + q * Vector3.up * len;
                GameObject s = Prim(f, "Stem", PrimitiveType.Cylinder, (new Vector3(0f, 0.02f, 0f) + top) / 2f, new Vector3(0.006f, len / 2f, 0.006f), stem, collider: false);
                s.transform.localRotation = q;
                Transform head = Group(f, "Bloom", top);
                head.localRotation = q * Quaternion.Euler(-20f, 0f, 0f);
                Material petal = blooms[i % blooms.Length];
                for (int k = 0; k < 6; k++)
                {
                    GameObject p = Prim(head, "Petal", PrimitiveType.Sphere, Quaternion.Euler(0f, k * 60f, 0f) * new Vector3(0f, 0f, 0.022f),
                        new Vector3(0.022f, 0.006f, 0.034f), petal, collider: false);
                    p.transform.localRotation = Quaternion.Euler(0f, k * 60f, 0f) * Quaternion.Euler(-12f, 0f, 0f);
                }
                Prim(head, "Centre", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.016f, centre, collider: false);
            }
            foreach (Renderer r in f.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void Candle(Transform env, Vector3 at)
        {
            Transform c = Group(env, "Lantern", at);
            Material metal = Mat("LanternMetal", new Color(0.12f, 0.12f, 0.12f), smoothness: 0.5f);
            Material glass = Mat("LanternGlass", new Color(0.9f, 0.9f, 0.85f, 0.18f), smoothness: 0.95f);
            MakeTransparent(glass);
            Material wax = Mat("CandleWax", new Color(0.92f, 0.88f, 0.78f), smoothness: 0.35f);
            Material flameMat = UnlitMat("CandleFlame", new Color(1f, 0.75f, 0.35f));
            Box(c, "Base", new Vector3(0f, 0.01f, 0f), new Vector3(0.09f, 0.02f, 0.09f), metal, collider: false, isStatic: false);
            Box(c, "Roof", new Vector3(0f, 0.17f, 0f), new Vector3(0.095f, 0.015f, 0.095f), metal, collider: false, isStatic: false);
            Prim(c, "Handle", PrimitiveType.Cylinder, new Vector3(0f, 0.195f, 0f), new Vector3(0.012f, 0.012f, 0.012f), metal, collider: false);
            foreach (Vector2 k in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
                Box(c, "Post", new Vector3(k.x * 0.042f, 0.09f, k.y * 0.042f), new Vector3(0.006f, 0.15f, 0.006f), metal, collider: false, isStatic: false);
            Box(c, "Glass", new Vector3(0f, 0.09f, 0f), new Vector3(0.082f, 0.14f, 0.082f), glass, collider: false, isStatic: false);
            Prim(c, "Candle", PrimitiveType.Cylinder, new Vector3(0f, 0.05f, 0f), new Vector3(0.03f, 0.03f, 0.03f), wax, collider: false);
            GameObject flame = Prim(c, "Flame", PrimitiveType.Sphere, new Vector3(0f, 0.1f, 0f), new Vector3(0.012f, 0.03f, 0.012f), flameMat, collider: false);
            var lightGo = new GameObject("CandleLight");
            lightGo.transform.SetParent(c, false);
            lightGo.transform.localPosition = new Vector3(0f, 0.11f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.7f, 0.4f);
            l.range = 1.3f;
            l.intensity = 0.6f;
            l.shadows = LightShadows.None;
            foreach (Renderer r in c.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            var cf = c.gameObject.AddComponent<CandleFlame>();
            Set(cf, "glow", l);
            Set(cf, "flame", flame.transform);
        }

        private static void MakeTransparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
        }

        // ------------------------------------------------------------------ the land around

        private static void Trees(Transform env)
        {
            Material bark = Mat("Bark", new Color(0.16f, 0.13f, 0.1f), smoothness: 0.05f);
            Material[] leaves =
            {
                Mat("Foliage", new Color(0.12f, 0.18f, 0.08f), smoothness: 0.05f),
                Mat("FoliageLight", new Color(0.18f, 0.24f, 0.1f), smoothness: 0.05f),
                Mat("FoliageDark", new Color(0.08f, 0.12f, 0.06f), smoothness: 0.05f),
            };
            Material needles = Mat("Needles", new Color(0.07f, 0.12f, 0.08f), smoothness: 0.05f);
            Mesh cone = Cone();
            var rng = new System.Random(41);
            float R() => (float)rng.NextDouble();
            Transform trees = Group(env, "Trees");
            for (int i = 0; i < 52; i++)
            {
                float x, z;
                if (i < 40) { x = -78f + i * 4f + (R() - 0.5f) * 3.5f; z = 36f + (R() - 0.5f) * 16f + Mathf.Abs(x) * 0.15f; }
                else { float side = i % 2 == 0 ? -1f : 1f; x = side * (18f + R() * 14f); z = 6f + R() * 22f; }
                if (Mathf.Abs(x + 26f) < 10f && z > 28f) continue; // keep the view of the station open
                float s = 0.75f + R() * 0.7f;
                Transform t = Group(trees, "Tree", new Vector3(x, H(x, z) - 0.1f, z));
                t.localRotation = Quaternion.Euler(0f, R() * 360f, 0f);
                if (R() < 0.3f)
                {
                    // Conifer: a trunk and stacked, drooping cones.
                    Prim(t, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.2f * s, 0f), new Vector3(0.22f * s, 1.2f * s, 0.22f * s), bark, collider: false);
                    for (int k = 0; k < 4; k++)
                    {
                        var tier = new GameObject("Tier");
                        tier.transform.SetParent(t, false);
                        float w = (2.6f - k * 0.55f) * s;
                        tier.transform.localPosition = new Vector3(0f, (1.2f + k * 1.5f) * s, 0f);
                        tier.transform.localScale = new Vector3(w, 2.4f * s, w);
                        tier.transform.localRotation = Quaternion.Euler(0f, R() * 360f, 0f);
                        tier.AddComponent<MeshFilter>().sharedMesh = cone;
                        tier.AddComponent<MeshRenderer>().sharedMaterial = needles;
                    }
                    continue;
                }
                // Broadleaf: a slightly leaning trunk, two limbs, and a crown of many overlapping clumps.
                float h = 2.6f * s;
                GameObject trunk = Prim(t, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, h / 2f, 0f), new Vector3(0.3f * s, h / 2f, 0.3f * s), bark, collider: false);
                trunk.transform.localRotation = Quaternion.Euler((R() - 0.5f) * 8f, 0f, (R() - 0.5f) * 8f);
                for (int k = 0; k < 2; k++)
                {
                    float dir = k == 0 ? -1f : 1f;
                    GameObject limb = Prim(t, "Limb", PrimitiveType.Cylinder, new Vector3(dir * 0.55f * s, h * 0.95f, 0f), new Vector3(0.14f * s, 0.8f * s, 0.14f * s), bark, collider: false);
                    limb.transform.localRotation = Quaternion.Euler(0f, R() * 40f, dir * -38f);
                }
                Vector3 crown = new Vector3(0f, h + 1.6f * s, 0f);
                Vector3 radii = new Vector3(2.4f, 1.6f, 2.4f) * s;
                int clumps = 11 + rng.Next(6);
                for (int k = 0; k < clumps; k++)
                {
                    Vector3 d = new Vector3((R() - 0.5f) * 2f, (R() - 0.35f) * 2f, (R() - 0.5f) * 2f).normalized * Mathf.Sqrt(R());
                    Vector3 o = crown + Vector3.Scale(d, radii);
                    float r = (0.9f + R() * 0.9f) * s;
                    GameObject leaf = Prim(t, "Leaves", PrimitiveType.Sphere, o, new Vector3(r * (1f + R() * 0.3f), r * (0.7f + R() * 0.25f), r), leaves[rng.Next(leaves.Length)], collider: false);
                    leaf.transform.localRotation = Quaternion.Euler(R() * 30f, R() * 360f, R() * 30f);
                }
            }
        }

        private static Mesh coneMesh;
        /// <summary>A unit cone (radius 0.5, height 1, base at y 0) with a drooping, uneven edge.</summary>
        private static Mesh Cone()
        {
            if (coneMesh != null) return coneMesh;
            const int seg = 10;
            var v = new List<Vector3> { new Vector3(0f, 1f, 0f) };
            for (int i = 0; i < seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float r = 0.5f * (0.85f + 0.15f * Mathf.PerlinNoise(i * 0.9f, 0.3f));
                v.Add(new Vector3(Mathf.Cos(a) * r, -0.06f * (i % 2), Mathf.Sin(a) * r));
            }
            var tri = new List<int>();
            for (int i = 0; i < seg; i++) tri.AddRange(new[] { 0, 1 + (i + 1) % seg, 1 + i });
            coneMesh = new Mesh { name = "TreeCone" };
            coneMesh.SetVertices(v);
            coneMesh.SetTriangles(tri, 0);
            coneMesh.RecalculateNormals();
            coneMesh.RecalculateBounds();
            return coneMesh;
        }

        private static void Fence(Transform env)
        {
            Material wood = Textured("FenceWood", "brown_planks_05", new Color(0.55f, 0.52f, 0.48f), 0.08f);
            Transform fence = Group(env, "Fence");
            // An old post-and-rail fence wandering down the slope towards the station.
            Vector3 prev = Vector3.zero;
            for (int i = 0; i < 18; i++)
            {
                float z = -4f + i * 3.2f, x = -6.5f - i * 0.9f + Mathf.Sin(i * 0.7f) * 0.6f;
                var p = new Vector3(x, H(x, z), z);
                GameObject post = Box(fence, "Post", p + Vector3.up * 0.6f, new Vector3(0.11f, 1.2f, 0.11f), wood, collider: false);
                post.transform.localRotation = Quaternion.Euler((i * 37 % 7) - 3f, i * 13f, (i * 53 % 9) - 4f);
                if (i > 0 && i % 5 != 3) // a few rails have fallen
                    foreach (float y in new[] { 0.45f, 0.95f })
                    {
                        Vector3 a = prev + Vector3.up * y, b = p + Vector3.up * y;
                        GameObject rail = Box(fence, "Rail", (a + b) / 2f, new Vector3(0.06f, 0.08f, Vector3.Distance(a, b)), wood, collider: false);
                        rail.transform.localRotation = Quaternion.LookRotation(b - a);
                    }
                prev = p;
            }
        }

        /// <summary>The radio station and its lattice mast on the far slope, hazy in the morning light.</summary>
        private static void Station(Transform env)
        {
            Material dark = Mat("DistantStation", new Color(0.12f, 0.11f, 0.11f), smoothness: 0.05f);
            Material steel = Mat("MastSteel", new Color(0.18f, 0.18f, 0.2f), smoothness: 0.2f);
            Transform st = Group(env, "RadioStation", new Vector3(-30f, H(-30f, 85f), 85f));
            Box(st, "Building", new Vector3(0f, 4f, 0f), new Vector3(16f, 8f, 10f), dark, collider: false);
            Box(st, "Parapet", new Vector3(0f, 8.3f, 0f), new Vector3(16.4f, 0.6f, 10.4f), dark, collider: false);
            Box(st, "Annex", new Vector3(10f, 2.5f, 1f), new Vector3(6f, 5f, 7f), dark, collider: false);
            // Lattice mast: four legs tapering in, cross-braced.
            Transform mast = Group(st, "Mast", new Vector3(6f, 0f, 3f));
            const float height = 34f, baseHalf = 1.6f, topHalf = 0.35f;
            Vector3 Leg(int i, float y)
            {
                float half = Mathf.Lerp(baseHalf, topHalf, y / height);
                float sx = i == 0 || i == 3 ? -1f : 1f, sz = i < 2 ? -1f : 1f;
                return new Vector3(sx * half, y, sz * half);
            }
            void Strut(Vector3 a, Vector3 b, float thick)
            {
                GameObject s = Box(mast, "Steel", (a + b) / 2f, new Vector3(thick, thick, Vector3.Distance(a, b)), steel, collider: false);
                s.transform.localRotation = Quaternion.LookRotation(b - a);
            }
            for (int i = 0; i < 4; i++) Strut(Leg(i, 0f), Leg(i, height), 0.16f);
            for (float y = 0f; y < height - 1f; y += 3f)
                for (int i = 0; i < 4; i++)
                {
                    int j = (i + 1) % 4;
                    Strut(Leg(i, y), Leg(j, y + 3f), 0.07f);
                    Strut(Leg(i, y + 3f), Leg(j, y + 3f), 0.07f);
                }
            Strut(new Vector3(0f, height, 0f), new Vector3(0f, height + 5f, 0f), 0.1f); // antenna
            // Red aircraft beacon at the very top.
            Material red = Mat("BeaconRed", new Color(0.9f, 0.1f, 0.06f), emissive: true, smoothness: 0.6f);
            GameObject bulb = Prim(mast, "Beacon", PrimitiveType.Sphere, new Vector3(0f, height + 5.2f, 0f), Vector3.one * 0.5f, red, collider: false);
            var lightGo = new GameObject("BeaconLight");
            lightGo.transform.SetParent(mast, false);
            lightGo.transform.localPosition = new Vector3(0f, height + 5.2f, 0f);
            var l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.12f, 0.08f);
            l.range = 8f;
            l.intensity = 3f;
            l.shadows = LightShadows.None;
            var blink = mast.gameObject.AddComponent<BlinkLight>();
            Set(blink, "lamp", l);
            Set(blink, "bulb", bulb.GetComponent<Renderer>());
            foreach (Renderer r in st.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ air

        private static Material SoftParticles(string name, Color c)
        {
            Material m = UnlitMat(name, c, transparent: true);
            Sprite soft = EnsureSprite("SoftDot", SpriteShape.Soft);
            if (soft != null) m.SetTexture("_BaseMap", soft.texture);
            return m;
        }

        /// <summary>Low morning mist drifting over the grass (EndingSequence thins it as the sun climbs).</summary>
        private static ParticleSystem Mist(Transform env)
        {
            var go = new GameObject("Mist");
            go.transform.SetParent(env, false);
            go.transform.position = new Vector3(-6f, 0.6f, 22f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 26f;
            main.startSpeed = 0.12f;
            main.startSize = new ParticleSystem.MinMaxCurve(7f, 13f);
            main.startColor = new Color(1f, 0.88f, 0.78f, 0.3f);
            main.maxParticles = 70;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.rateOverTime = 2.6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(70f, 0.6f, 50f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.x = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SoftParticles("EndingMist", Color.white);
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.sortingFudge = 10f;
            return ps;
        }

        /// <summary>Dust and seeds hanging in the low sunlight around the stone.</summary>
        private static void Dust(Transform env, Vector3 around)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(env, false);
            go.transform.position = around + new Vector3(0f, 1f, -0.5f);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.03f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.008f, 0.02f);
            main.startColor = new Color(1f, 0.93f, 0.8f, 0.8f);
            main.maxParticles = 90;
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(3f, 1.6f, 3f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.05f;
            noise.frequency = 0.3f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = SoftParticles("EndingDust", new Color(1f, 0.95f, 0.85f));
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
        }
    }
}
