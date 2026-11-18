using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Realistic notes: generated aged-paper textures (fibres, coffee rings, water stains, fold creases,
    /// dirty torn edges), a "written" variant with handwriting scribbles for notes lying in the world,
    /// and a TextMeshPro font asset built from the Caveat handwriting font (SIL OFL).
    /// </summary>
    public static class PaperArt
    {
        private const string Dir = "Assets/Art/Textures/Generated";
        public const string PaperPath = Dir + "/AgedPaper_v1.png";
        public const string WrittenPath = Dir + "/AgedPaperWritten_v1.png";
        public const string FontSourcePath = "Assets/Art/Fonts/Caveat/Caveat.ttf";
        public const string FontAssetPath = "Assets/Art/Fonts/Caveat/Caveat SDF.asset";

        [MenuItem("The Last Signal/Build/Rebuild Paper Art")]
        public static void Rebuild()
        {
            if (File.Exists(PaperPath)) AssetDatabase.DeleteAsset(PaperPath);
            if (File.Exists(WrittenPath)) AssetDatabase.DeleteAsset(WrittenPath);
            if (File.Exists(FontAssetPath)) AssetDatabase.DeleteAsset(FontAssetPath);
            PaperSprite();
            WrittenMaterial();
            HandwritingFont();
        }

        // ------------------------------------------------------------------ textures

        public static Sprite PaperSprite()
        {
            EnsureTexture(PaperPath, written: false, seed: 7, asSprite: true);
            return AssetDatabase.LoadAssetAtPath<Sprite>(PaperPath);
        }

        /// <summary>Material for notes lying in the world (paper with handwriting scribbles).</summary>
        public static Material WrittenMaterial()
        {
            EnsureTexture(WrittenPath, written: true, seed: 21, asSprite: false);
            Material m = Mat("PaperWritten", new Color(0.95f, 0.93f, 0.88f), smoothness: 0.05f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(WrittenPath));
            m.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Paper notes in the world get the written-paper look; sticky notes etc. keep their own material.</summary>
        public static Material ForNote(Material requested) =>
            requested != null && requested.name == "Paper" ? WrittenMaterial() : requested;

        private static void EnsureTexture(string path, bool written, int seed, bool asSprite)
        {
            EnsureFolder(Dir);
            if (!File.Exists(path))
            {
                Texture2D tex = Generate(512, 704, seed, written);
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool changed = false;
            TextureImporterType type = asSprite ? TextureImporterType.Sprite : TextureImporterType.Default;
            if (importer.textureType != type) { importer.textureType = type; changed = true; }
            if (asSprite && importer.spriteImportMode != SpriteImportMode.Single) { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }
            if (!importer.alphaIsTransparency) { importer.alphaIsTransparency = true; changed = true; }
            if (importer.maxTextureSize != 512) { importer.maxTextureSize = 512; changed = true; }
            if (importer.mipmapEnabled == asSprite) { importer.mipmapEnabled = !asSprite; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static Texture2D Generate(int w, int h, int seed, bool written)
        {
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color paper = new Color(0.86f, 0.8f, 0.66f);
            Color stain = new Color(0.55f, 0.4f, 0.22f);
            Color grime = new Color(0.3f, 0.24f, 0.17f);

            // Coffee rings and water blotches.
            int stains = 4;
            var sc = new Vector2[stains];
            var sr = new float[stains];
            for (int i = 0; i < stains; i++) { sc[i] = new Vector2(R(), R()); sr[i] = 0.06f + R() * 0.12f; }
            float ox = R() * 100f, oy = R() * 100f;

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                float fibre = Mathf.PerlinNoise(ox + u * 40f, oy + v * 6f) * 0.5f + Mathf.PerlinNoise(ox + u * 9f, oy + v * 9f) * 0.5f;
                Color c = paper * (0.88f + 0.12f * fibre) + new Color(1f, 1f, 1f) * ((float)rng.NextDouble() - 0.5f) * 0.035f;

                // Dirty, uneven edges.
                float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v) * (w / (float)h));
                float edgeNoise = Mathf.PerlinNoise(ox + u * 14f, oy + v * 14f);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / (0.06f + edgeNoise * 0.08f)));
                c = Color.Lerp(grime, c, 0.45f + 0.55f * edge);

                // Stains.
                for (int i = 0; i < stains; i++)
                {
                    float r = Vector2.Distance(new Vector2(u, v * (h / (float)w)), new Vector2(sc[i].x, sc[i].y * (h / (float)w)));
                    float wobble = (Mathf.PerlinNoise(ox + u * 20f + i, oy + v * 20f) - 0.5f) * 0.02f;
                    float ring = Mathf.Exp(-Mathf.Pow((r - sr[i] + wobble) / 0.006f, 2f)) * 0.35f;
                    float blot = r < sr[i] ? 0.07f * Mathf.PerlinNoise(ox + u * 7f + i * 3f, oy + v * 7f) : 0f;
                    c = Color.Lerp(c, stain, (ring + blot) * (i % 2 == 0 ? 1f : 0.6f));
                }

                // Fold creases (a dark line with a lighter edge beside it).
                float fold1 = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.0025f, 2f));
                float fold1b = Mathf.Exp(-Mathf.Pow((v - 0.5035f) / 0.003f, 2f));
                float fold2 = Mathf.Exp(-Mathf.Pow((u - 0.33f) / 0.0025f, 2f)) * 0.7f;
                c *= 1f - 0.12f * (fold1 + fold2);
                c += new Color(1f, 1f, 1f) * 0.04f * fold1b;

                // Torn edge: ragged alpha.
                float tear = 0.006f + Mathf.PerlinNoise(ox + u * 60f, oy + v * 60f) * 0.012f;
                c.a = d > tear ? 1f : 0f;
                px[y * w + x] = c;
            }

            if (written) Scribble(px, w, h, rng);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Wavy handwriting-like ink strokes so notes lying in the world look written on.</summary>
        private static void Scribble(Color[] px, int w, int h, System.Random rng)
        {
            Color ink = new Color(0.1f, 0.09f, 0.14f);
            for (float row = 0.88f; row > 0.12f; row -= 0.055f)
            {
                float x = 0.1f + (float)rng.NextDouble() * 0.05f;
                float end = 0.9f - (float)rng.NextDouble() * (row < 0.2f ? 0.5f : 0.15f);
                float phase = (float)rng.NextDouble() * 10f;
                while (x < end)
                {
                    float word = 0.05f + (float)rng.NextDouble() * 0.12f;
                    for (float t = 0f; t < word && x + t < end; t += 0.0012f)
                    {
                        float yy = row + Mathf.Sin((x + t) * 260f + phase) * 0.006f + Mathf.Sin((x + t) * 41f) * 0.003f;
                        int cx = Mathf.RoundToInt((x + t) * w), cy = Mathf.RoundToInt(yy * h);
                        for (int oy = -1; oy <= 1; oy++)
                        for (int oxp = 0; oxp <= 1; oxp++)
                        {
                            int ix = cx + oxp, iy = cy + oy;
                            if (ix < 0 || iy < 0 || ix >= w || iy >= h) continue;
                            Color p = px[iy * w + ix];
                            if (p.a <= 0f) continue;
                            px[iy * w + ix] = Color.Lerp(p, new Color(ink.r, ink.g, ink.b, 1f), oy == 0 ? 0.85f : 0.35f);
                        }
                    }
                    x += word + 0.015f + (float)rng.NextDouble() * 0.02f;
                }
            }
        }

        // ------------------------------------------------------------------ font

        /// <summary>TextMeshPro font asset for the Caveat handwriting font (null if the font is missing).</summary>
        public static TMP_FontAsset HandwritingFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null) return existing;
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (font == null) return null;
            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (asset == null) return null;
            asset.name = "Caveat SDF";
            // Pre-fill the atlas with everything the notes use so nothing is generated on the phone.
            string chars = "";
            for (char c = (char)32; c < 127; c++) chars += c;
            asset.TryAddCharacters(chars + "‘’“”–—…°", out _);
            AssetDatabase.CreateAsset(asset, FontAssetPath);
            if (asset.atlasTextures != null)
                foreach (Texture2D t in asset.atlasTextures)
                    if (t != null) { t.name = "Caveat SDF Atlas"; AssetDatabase.AddObjectToAsset(t, asset); }
            if (asset.material != null)
            {
                asset.material.name = "Caveat SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        }
    }
}
