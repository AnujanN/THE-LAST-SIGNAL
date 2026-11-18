using System.IO;
using UnityEditor;
using UnityEngine;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Horror rendering settings: real-time shadows for the flashlight (an additional/spot light) on both the Low
    /// and Medium presets (owner's decision), sized for phones, and a generated flashlight cookie texture.
    /// </summary>
    public static class RenderingSetup
    {
        public const string CookiePath = "Assets/Art/Textures/Generated/FlashlightCookie.png";

        [MenuItem("The Last Signal/Build/Apply Rendering Settings")]
        public static void Apply()
        {
            Configure("Assets/Settings/Rendering/URP_Low.asset", atlas: 512, tier: 256);
            Configure("Assets/Settings/Rendering/URP_Medium.asset", atlas: 1024, tier: 512);
            AssetDatabase.SaveAssets();
            Debug.Log("[RenderingSetup] Flashlight shadows enabled on Low and Medium; cookie ready.");
        }

        private static void Configure(string path, int atlas, int tier)
        {
            var asset = AssetDatabase.LoadMainAssetAtPath(path);
            if (asset == null) { Debug.LogWarning("[RenderingSetup] Missing " + path); return; }
            var so = new SerializedObject(asset);
            void SetInt(string field, int v) { var p = so.FindProperty(field); if (p != null) p.intValue = v; }
            void SetBool(string field, bool v) { var p = so.FindProperty(field); if (p != null) p.boolValue = v; }
            SetBool("m_AdditionalLightShadowsSupported", true);
            SetInt("m_AdditionalLightsShadowmapResolution", atlas);
            SetInt("m_AdditionalLightsShadowResolutionTierLow", tier / 2);
            SetInt("m_AdditionalLightsShadowResolutionTierMedium", tier);
            SetInt("m_AdditionalLightsShadowResolutionTierHigh", tier);
            SetBool("m_SoftShadowsSupported", false);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Flashlight beam pattern: bright core, a slightly darker ring, a brighter rim and soft falloff.</summary>
        public static Texture2D EnsureCookie()
        {
            if (!File.Exists(CookiePath))
            {
                BuildKit.EnsureFolder(Path.GetDirectoryName(CookiePath).Replace('\\', '/'));
                const int size = 256;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float c = (size - 1) / 2f;
                var rng = new System.Random(7);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float r = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                    float core = Mathf.Clamp01(1f - r * 1.6f);
                    float ring = 0.75f + 0.25f * Mathf.Sin(r * 22f) * Mathf.Clamp01(1f - r);
                    float rim = Mathf.Exp(-Mathf.Pow((r - 0.72f) * 9f, 2f)) * 0.35f;
                    float falloff = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01((r - 0.6f) / 0.4f));
                    float grain = 0.95f + 0.05f * (float)rng.NextDouble();
                    float v = Mathf.Clamp01((0.55f * ring + core * 0.6f + rim) * falloff * grain);
                    if (r >= 0.98f) v = 0f; // cookie edge must be black for spot lights
                    tex.SetPixel(x, y, new Color(v, v, v, v));
                }
                File.WriteAllBytes(CookiePath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(CookiePath);
                var importer = (TextureImporter)AssetImporter.GetAtPath(CookiePath);
                importer.textureType = TextureImporterType.Cookie;
                importer.textureShape = TextureImporterShape.Texture2D;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(CookiePath);
        }
    }
}
