using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// The old family photo from the intro (and the one on the wall in Level 3): Dad, Alex and Maya in front of
    /// Northpoint and its mast one summer evening, the sun low behind the tower. It is rendered once in the editor
    /// from the same station, hill and treeline as the ending, then aged like a print from a cheap camera:
    /// soft focus, faded blacks, warm cast, grain, a light leak, vignette and a yellowed white border.
    /// </summary>
    public static class PhotoArt
    {
        public const string PhotoPath = "Assets/Art/Textures/Generated/FamilyPhoto.png";
        private const string Version = "photo-v1";
        private const int W = 1200, Hgt = 900;

        [MenuItem("The Last Signal/Build/Render Family Photo")]
        public static void RenderMenu() => FamilyPhoto(force: true);

        public static Sprite FamilyPhoto(bool force = false)
        {
            if (!force && AssetImporter.GetAtPath(PhotoPath) is TextureImporter existing && existing.userData == Version)
            {
                Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(PhotoPath);
                if (s != null) return s;
            }
            Texture2D shot = Shoot();
            Texture2D print = Age(shot);
            Object.DestroyImmediate(shot);
            EnsureFolder(Path.GetDirectoryName(PhotoPath).Replace('\\', '/'));
            File.WriteAllBytes(PhotoPath, print.EncodeToPNG());
            Object.DestroyImmediate(print);
            AssetDatabase.ImportAsset(PhotoPath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(PhotoPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.userData = Version;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(PhotoPath);
        }

        /// <summary>A lit material showing the photo (so it sits in the room's light instead of glowing).</summary>
        public static Material PrintMaterial()
        {
            Sprite photo = FamilyPhoto();
            Material m = Mat("FamilyPhotoPrint", Color.white, smoothness: 0.35f);
            if (photo != null) m.SetTexture("_BaseMap", photo.texture);
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>The photo in a thin dark frame on a wall (yaw 0 = facing -Z), no collider.</summary>
        public static void Framed(Transform parent, Vector3 pos, float yaw, Vector2 size)
        {
            Transform frame = Group(parent, "FramedPhoto", pos);
            frame.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Material wood = Mat("PhotoFrameWood", new Color(0.12f, 0.08f, 0.06f), smoothness: 0.4f);
            Box(frame, "Frame", new Vector3(0f, 0f, 0.004f), new Vector3(size.x + 0.05f, size.y + 0.05f, 0.012f), wood, collider: false);
            GameObject print = Prim(frame, "Print", PrimitiveType.Quad, new Vector3(0f, 0f, -0.004f), new Vector3(size.x, size.y, 1f), PrintMaterial(), collider: false);
            print.isStatic = true;
        }

        // ------------------------------------------------------------------ the snapshot

        private static Texture2D Shoot()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Transform env = Group(null, "Environment");

            // A clear summer evening.
            Material sky = AssetDatabase.LoadAssetAtPath<Material>(MaterialDir + "/PhotoSky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, MaterialDir + "/PhotoSky.mat");
            }
            sky.SetFloat("_SunDisk", 2f);
            sky.SetFloat("_SunSize", 0.06f);
            sky.SetFloat("_SunSizeConvergence", 4f);
            sky.SetFloat("_AtmosphereThickness", 1.25f);
            sky.SetColor("_SkyTint", new Color(0.45f, 0.5f, 0.62f));
            sky.SetColor("_GroundColor", new Color(0.35f, 0.3f, 0.25f));
            sky.SetFloat("_Exposure", 1.35f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.5f, 0.5f, 0.56f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.29f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.26f, 0.22f, 0.17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.86f, 0.74f, 0.6f);
            RenderSettings.fogDensity = 0.007f;

            // Low sun behind the tower: the family stands half in silhouette, rim-lit.
            Light sun = DirectionalLight(env, "Sun", new Vector3(7f, 192f, 0f), new Color(1f, 0.74f, 0.48f), 1.9f);
            sun.shadows = LightShadows.Soft;
            RenderSettings.sun = sun;
            SetLightLayers(sun, 0xFFFFFFFFu);
            Light fill = DirectionalLight(env, "Fill", new Vector3(15f, 10f, 0f), new Color(0.8f, 0.74f, 0.68f), 0.13f);
            fill.shadows = LightShadows.None;
            SetLightLayers(fill, 0xFFFFFFFFu);

            EndingBuilder.Ground(env);
            EndingBuilder.Grass(env, -22f, 44f);
            EndingBuilder.Trees(env);
            EndingBuilder.Fence(env);
            EndingBuilder.Station(env);

            // The three of them, a few steps in front of the camera, the station and mast behind.
            Vector3 camPos = new Vector3(-21.5f, EndingBuilder.H(-21.5f, 33f) + 1.25f, 33f);
            Vector3 aim = new Vector3(-25.5f, EndingBuilder.H(-25.5f, 80f) + 8.5f, 80f);
            Figure(env, "Dad", null, new Vector3(-23.2f, 0f, 41.4f), 1.0f, 0.4f, 14f);
            Figure(env, "Alex", Mannequin(new Color(0.3f, 0.16f, 0.13f)), new Vector3(-22.55f, 0f, 41.2f), 0.84f, 1.1f, -4f);
            Figure(env, "Maya", Mannequin(new Color(0.42f, 0.38f, 0.3f)), new Vector3(-22.0f, 0f, 41.0f), 0.72f, 2.3f, -14f);

            var camGo = new GameObject("PhotoCamera");
            camGo.transform.SetPositionAndRotation(camPos, Quaternion.LookRotation(aim - camPos) * Quaternion.Euler(0f, 0f, -1.2f));
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 50f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 500f;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.85f);
            bloom.intensity.Override(1.3f);
            bloom.scatter.Override(0.75f);
            var ca = profile.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.45f);
            ca.contrast.Override(8f);
            var volumeGo = new GameObject("PhotoFX");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var rt = new RenderTexture(W, Hgt, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            cam.Render(); // the first frame can miss the volume / shadows
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(W, Hgt, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, Hgt), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            rt.Release();
            Object.DestroyImmediate(rt);
            Object.DestroyImmediate(profile);
            return tex;
        }

        private static GameObject mannequinModel;
        private static Material mannequinMat;
        private static GameObject Mannequin(Color clothes)
        {
            mannequinModel = AssetDatabase.LoadAssetAtPath<GameObject>(GamePrefabs.EchoModelPath);
            mannequinMat = Mat("PhotoClothes_" + ColorUtility.ToHtmlStringRGB(clothes), clothes, smoothness: 0.1f);
            return mannequinModel;
        }

        /// <summary>
        /// A standing figure on the ground at (x, z), facing the camera, frozen in an idle pose. No model = the
        /// grown man (the Mixamo body Alex uses, with the player's materials); otherwise a tinted mannequin.
        /// </summary>
        private static void Figure(Transform env, string name, GameObject model, Vector3 at, float scale, float poseTime, float yaw)
        {
            at.y = EndingBuilder.H(at.x, at.z);
            bool mannequin = model != null;
            GameObject body;
            if (mannequin)
            {
                body = (GameObject)PrefabUtility.InstantiatePrefab(model);
                PrefabUtility.UnpackPrefabInstance(body, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                body.transform.SetParent(env, false);
            }
            else
            {
                if (!MixamoSetup.HasAlex) return;
                body = MixamoSetup.Body(MixamoSetup.AlexModel, null, env);
                GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
                SkinnedMeshRenderer[] src = player != null ? player.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0];
                foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
                    foreach (SkinnedMeshRenderer s in src)
                        if (s.name == smr.name) { smr.sharedMaterials = s.sharedMaterials; break; }
            }
            if (body == null) return;
            body.name = name;
            body.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, (mannequin ? 0f : 180f) + yaw, 0f));
            body.transform.localScale = Vector3.one * scale;
            AnimationClip idle = null;
            string clipPath = mannequin ? GamePrefabs.EchoModelPath : "Assets/Art/Models/Mixamo/Alex/Idel.fbx";
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(clipPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview") && (!mannequin || c.name == "Idle_Loop" || c.name == "Rig|Idle_Loop")) { idle = c; break; }
            if (idle != null)
            {
                Animator anim = body.GetComponent<Animator>();
                if (anim != null && anim.avatar != null && anim.avatar.isHuman)
                {
                    AnimationMode.StartAnimationMode();
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(body, idle, poseTime % Mathf.Max(0.1f, idle.length));
                    AnimationMode.EndSampling();
                    // Keep the sampled pose: copy every bone's transform before leaving animation mode.
                    Transform[] bones = body.GetComponentsInChildren<Transform>();
                    var pos = new Vector3[bones.Length];
                    var rot = new Quaternion[bones.Length];
                    for (int i = 0; i < bones.Length; i++) { pos[i] = bones[i].localPosition; rot[i] = bones[i].localRotation; }
                    AnimationMode.StopAnimationMode();
                    for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = pos[i]; bones[i].localRotation = rot[i]; }
                    body.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, (mannequin ? 0f : 180f) + yaw, 0f));
                }
                else idle.SampleAnimation(body, poseTime);
            }
            foreach (SkinnedMeshRenderer smr in body.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                smr.updateWhenOffscreen = true;
                smr.shadowCastingMode = ShadowCastingMode.On;
                if (mannequin)
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = mannequinMat;
                    smr.sharedMaterials = mats;
                }
            }
        }

        // ------------------------------------------------------------------ ageing the print

        private static Texture2D Age(Texture2D src)
        {
            int border = 46;
            int w = W + border * 2, h = Hgt + border * 2;
            Color[] s = src.GetPixels();
            Color[] blur = new Color[s.Length];
            // Soft focus: a small box blur, mixed back with the original.
            for (int y = 0; y < Hgt; y++)
                for (int x = 0; x < W; x++)
                {
                    Color acc = Color.clear;
                    int n = 0;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            int xx = Mathf.Clamp(x + dx, 0, W - 1), yy = Mathf.Clamp(y + dy, 0, Hgt - 1);
                            acc += s[yy * W + xx];
                            n++;
                        }
                    blur[y * W + x] = Color.Lerp(s[y * W + x], acc / n, 0.65f);
                }

            var rng = new System.Random(1014);
            var outPx = new Color[w * h];
            Color paper = new Color(0.9f, 0.87f, 0.78f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float grain = ((float)rng.NextDouble() - 0.5f) * 0.06f;
                    int ix = x - border, iy = y - border;
                    if (ix < 0 || iy < 0 || ix >= W || iy >= Hgt)
                    {
                        // Yellowed paper border, a little darker towards the edges and at one corner.
                        float edge = Mathf.Min(Mathf.Min(x, w - 1 - x), Mathf.Min(y, h - 1 - y)) / (float)border;
                        float stain = Mathf.Clamp01(1f - new Vector2(x - w, y).magnitude / (w * 0.6f)) * 0.08f;
                        Color p = paper * (0.9f + 0.1f * Mathf.Clamp01(edge)) - new Color(stain, stain * 1.4f, stain * 2.2f, 0f);
                        outPx[y * w + x] = p + new Color(grain, grain, grain) * 0.4f;
                        continue;
                    }
                    Color c = blur[iy * W + ix];
                    float u = ix / (float)W, v = iy / (float)Hgt;
                    // Fade: lifted blacks, softer whites, a little less colour, a warm cast.
                    float lum = c.r * 0.3f + c.g * 0.59f + c.b * 0.11f;
                    c = Color.Lerp(new Color(lum, lum, lum), c, 0.72f);
                    c = new Color(c.r * 1.05f, c.g * 0.97f, c.b * 0.8f);
                    c = c * 0.8f + new Color(0.11f, 0.09f, 0.08f);
                    // Light leak from the right edge (orange), vignette into the corners.
                    float leak = Mathf.Pow(Mathf.Clamp01((u - 0.78f) / 0.22f), 2f) * (0.6f + 0.4f * Mathf.Sin(v * 3.2f));
                    c += new Color(0.22f, 0.09f, 0.0f) * leak;
                    float d = new Vector2(u - 0.5f, (v - 0.5f) * 0.8f).magnitude;
                    c *= 1f - Mathf.SmoothStep(0.3f, 0.75f, d) * 0.45f;
                    c += new Color(grain, grain, grain);
                    c.a = 1f;
                    outPx[y * w + x] = c;
                }
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.SetPixels(outPx);
            tex.Apply();
            return tex;
        }
    }
}
