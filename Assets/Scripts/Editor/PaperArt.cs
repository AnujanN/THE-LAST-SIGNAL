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

        /// <summary>Number of paper styles: 0 aged page, 1 lined notebook, 2 graph paper, 3 office memo,
        /// 4 crumpled brown scrap, 5 water-damaged page with a burnt corner.</summary>
        public const int Styles = 6;

        [MenuItem("The Last Signal/Build/Rebuild Paper Art")]
        public static void Rebuild()
        {
            for (int st = 0; st < Styles; st++)
            {
                foreach (string path in new[] { PlainPath(st), WrittenPathFor(st) })
                    if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
            }
            if (File.Exists(FontAssetPath)) AssetDatabase.DeleteAsset(FontAssetPath);
            for (int st = 0; st < Styles; st++) { PaperSprite(st); WrittenMaterial(st); }
            HandwritingFont();
        }

        private static string PlainPath(int style) => style == 0 ? PaperPath : Dir + "/Paper_" + style + "_v1.png";
        private static string WrittenPathFor(int style) => style == 0 ? WrittenPath : Dir + "/PaperWritten_" + style + "_v1.png";

        /// <summary>Same note id, same paper, every build (string.GetHashCode is not stable).</summary>
        public static int StyleFor(string id)
        {
            int h = 17;
            foreach (char c in id ?? "") h = unchecked(h * 31 + c);
            return Mathf.Abs(h % Styles);
        }

        /// <summary>Ink for each style: black, blue biro, pencil, typewriter black, red-brown, smudged black.</summary>
        public static Color Ink(int style)
        {
            switch (style)
            {
                case 1: return new Color(0.09f, 0.14f, 0.42f);
                case 2: return new Color(0.27f, 0.27f, 0.29f);
                case 3: return new Color(0.06f, 0.06f, 0.07f);
                case 4: return new Color(0.3f, 0.07f, 0.05f);
                case 5: return new Color(0.12f, 0.12f, 0.13f);
                default: return new Color(0.11f, 0.09f, 0.13f);
            }
        }

        // ------------------------------------------------------------------ textures

        /// <summary>Plain paper of a style (note reader background and the sheet Alex holds up).</summary>
        public static Sprite PaperSprite(int style = 0)
        {
            EnsureTexture(PlainPath(style), written: false, seed: 7 + style * 13, asSprite: true, style: style);
            return AssetDatabase.LoadAssetAtPath<Sprite>(PlainPath(style));
        }

        /// <summary>Material for notes lying in the world (paper with handwriting scribbles).</summary>
        public static Material WrittenMaterial(int style = 0)
        {
            EnsureTexture(WrittenPathFor(style), written: true, seed: 21 + style * 17, asSprite: false, style: style);
            Material m = Mat(style == 0 ? "PaperWritten" : "PaperWritten_" + style, new Color(0.95f, 0.93f, 0.88f), smoothness: 0.05f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(WrittenPathFor(style)));
            m.SetTextureScale("_BaseMap", Vector2.one);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Plain sheet for a note while Alex holds it up (the real words are drawn on top as text).</summary>
        public static Material HeldMaterial(int style)
        {
            PaperSprite(style);
            Material m = Mat("PaperHeld_" + style, new Color(1f, 1f, 1f), smoothness: 0.05f);
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PlainPath(style)));
            // The cube's top face maps the texture upside down relative to the page (top of the page = +Z).
            m.SetTextureScale("_BaseMap", new Vector2(1f, -1f));
            m.SetTextureOffset("_BaseMap", new Vector2(0f, 1f));
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Paper notes in the world get a written-paper look; sticky notes etc. keep their own material.</summary>
        public static Material ForNote(Material requested, int style = 0) =>
            requested != null && requested.name == "Paper" ? WrittenMaterial(style) : requested;

        private static void EnsureTexture(string path, bool written, int seed, bool asSprite, int style)
        {
            EnsureFolder(Dir);
            if (!File.Exists(path))
            {
                Texture2D tex = Generate(512, 704, seed, written, style);
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
            // World notes are small on screen; the reader / held sheet needs the full size.
            int max = asSprite ? 512 : 256;
            if (importer.maxTextureSize != max) { importer.maxTextureSize = max; changed = true; }
            // Mipmaps on: the plain sheet is also used in 3D (held up to the face).
            if (!importer.mipmapEnabled) { importer.mipmapEnabled = true; changed = true; }
            if (changed) importer.SaveAndReimport();
        }

        private static Texture2D Generate(int w, int h, int seed, bool written, int style)
        {
            var rng = new System.Random(seed);
            float R() => (float)rng.NextDouble();
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            Color paper, stain = new Color(0.55f, 0.4f, 0.22f), grime = new Color(0.3f, 0.24f, 0.17f);
            int stains;
            switch (style)
            {
                case 1: paper = new Color(0.92f, 0.91f, 0.85f); stains = 2; break;
                case 2: paper = new Color(0.88f, 0.89f, 0.86f); stains = 2; break;
                case 3: paper = new Color(0.86f, 0.85f, 0.8f); stains = 3; break;
                case 4: paper = new Color(0.7f, 0.58f, 0.4f); stains = 5; grime = new Color(0.22f, 0.15f, 0.09f); break;
                case 5: paper = new Color(0.74f, 0.73f, 0.68f); stains = 2; stain = new Color(0.42f, 0.38f, 0.3f); break;
                default: paper = new Color(0.86f, 0.8f, 0.66f); stains = 4; break;
            }

            var sc = new Vector2[stains];
            var sr = new float[stains];
            for (int i = 0; i < stains; i++) { sc[i] = new Vector2(R(), R()); sr[i] = 0.06f + R() * (style == 5 ? 0.3f : 0.12f); }
            float ox = R() * 100f, oy = R() * 100f;
            float aspect = h / (float)w;

            // Crumple creases for the brown scrap: random straight lines across the sheet.
            var creases = new Vector4[style == 4 ? 14 : 0];
            for (int i = 0; i < creases.Length; i++) creases[i] = new Vector4(R(), R(), R() * Mathf.PI, 0.4f + R() * 0.6f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                float fibre = Mathf.PerlinNoise(ox + u * 40f, oy + v * 6f) * 0.5f + Mathf.PerlinNoise(ox + u * 9f, oy + v * 9f) * 0.5f;
                Color c = paper * (0.88f + 0.12f * fibre) + new Color(1f, 1f, 1f) * ((float)rng.NextDouble() - 0.5f) * 0.035f;

                // Printed rules and grids.
                if (style == 1)
                {
                    float rule = Mathf.Repeat(v - 0.02f, 0.04f);
                    if (v < 0.86f && rule < 0.0028f) c = Color.Lerp(c, new Color(0.45f, 0.6f, 0.8f), 0.55f);
                    if (Mathf.Abs(u - 0.14f) < 0.0022f) c = Color.Lerp(c, new Color(0.85f, 0.35f, 0.35f), 0.6f);
                }
                else if (style == 2)
                {
                    bool minor = Mathf.Repeat(u, 0.04f) < 0.003f || Mathf.Repeat(v * aspect, 0.04f) < 0.003f;
                    bool major = Mathf.Repeat(u, 0.2f) < 0.004f || Mathf.Repeat(v * aspect, 0.2f) < 0.004f;
                    if (major) c = Color.Lerp(c, new Color(0.45f, 0.6f, 0.65f), 0.45f);
                    else if (minor) c = Color.Lerp(c, new Color(0.55f, 0.7f, 0.72f), 0.28f);
                }
                else if (style == 3)
                {
                    // Printed letterhead band and two rules under it.
                    if (v > 0.9f && v < 0.95f && u > 0.08f && u < 0.92f) c = Color.Lerp(c, new Color(0.2f, 0.22f, 0.3f), 0.75f);
                    if ((Mathf.Abs(v - 0.875f) < 0.0016f || Mathf.Abs(v - 0.865f) < 0.0012f) && u > 0.08f && u < 0.92f)
                        c = Color.Lerp(c, new Color(0.2f, 0.22f, 0.3f), 0.6f);
                }

                // Dirty, uneven edges.
                float d = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v) * aspect);
                float edgeNoise = Mathf.PerlinNoise(ox + u * 14f, oy + v * 14f);
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / (0.06f + edgeNoise * 0.08f)));
                c = Color.Lerp(grime, c, (style == 4 ? 0.3f : 0.45f) + (style == 4 ? 0.7f : 0.55f) * edge);

                // Stains (water damage: one big tide mark).
                for (int i = 0; i < stains; i++)
                {
                    float r = Vector2.Distance(new Vector2(u, v * aspect), new Vector2(sc[i].x, sc[i].y * aspect));
                    float wobble = (Mathf.PerlinNoise(ox + u * 20f + i, oy + v * 20f) - 0.5f) * (style == 5 ? 0.08f : 0.02f);
                    float ring = Mathf.Exp(-Mathf.Pow((r - sr[i] + wobble) / (style == 5 ? 0.012f : 0.006f), 2f)) * 0.35f;
                    float blot = r < sr[i] ? (style == 5 ? 0.18f : 0.07f) * Mathf.PerlinNoise(ox + u * 7f + i * 3f, oy + v * 7f) : 0f;
                    c = Color.Lerp(c, stain, (ring + blot) * (i % 2 == 0 ? 1f : 0.6f));
                }

                // Folds (pages), crumples (the scrap).
                if (style == 0 || style == 1 || style == 3)
                {
                    float fold1 = Mathf.Exp(-Mathf.Pow((v - 0.5f) / 0.0025f, 2f));
                    float fold1b = Mathf.Exp(-Mathf.Pow((v - 0.5035f) / 0.003f, 2f));
                    float fold2 = style == 0 ? Mathf.Exp(-Mathf.Pow((u - 0.33f) / 0.0025f, 2f)) * 0.7f : 0f;
                    c *= 1f - 0.12f * (fold1 + fold2);
                    c += new Color(1f, 1f, 1f) * 0.04f * fold1b;
                }
                foreach (Vector4 k in creases)
                {
                    Vector2 dir = new Vector2(Mathf.Cos(k.z), Mathf.Sin(k.z));
                    Vector2 rel = new Vector2(u - k.x, (v - k.y) * aspect);
                    float dist = Mathf.Abs(rel.x * dir.y - rel.y * dir.x);
                    if (Mathf.Abs(Vector2.Dot(rel, dir)) > k.w * 0.6f) continue;
                    c *= 1f - 0.16f * Mathf.Exp(-Mathf.Pow(dist / 0.003f, 2f));
                    c += new Color(1f, 1f, 1f) * 0.05f * Mathf.Exp(-Mathf.Pow((dist - 0.006f) / 0.003f, 2f));
                }

                // Burnt corner (top right) on the water-damaged page.
                float alpha = 1f;
                if (style == 5)
                {
                    float burn = (1f - u) + (1f - v) * aspect * 0.8f - 0.32f - 0.1f * Mathf.PerlinNoise(ox + u * 12f, oy + v * 12f);
                    if (burn < 0f) alpha = 0f;
                    else if (burn < 0.05f) c = Color.Lerp(new Color(0.05f, 0.04f, 0.03f), c, burn / 0.05f);
                }

                // Torn edges: ragged; notebook pages have punched holes, the scrap a torn-off bottom.
                float tear = 0.006f + Mathf.PerlinNoise(ox + u * 60f, oy + v * 60f) * 0.012f;
                if (d <= tear) alpha = 0f;
                if (style == 1)
                {
                    float hy = Mathf.Repeat(v * aspect, 0.25f) - 0.125f;
                    if (Mathf.Sqrt((u - 0.06f) * (u - 0.06f) + hy * hy) < 0.022f) alpha = 0f;
                }
                if (style == 4 && v < 0.08f + 0.06f * Mathf.PerlinNoise(ox + u * 18f, 3f)) alpha = 0f;
                c.a = alpha;
                px[y * w + x] = c;
            }

            if (written) Scribble(px, w, h, rng, style);
            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Handwriting-like strokes (typed lines for the memo) so notes lying in the world look written on.</summary>
        private static void Scribble(Color[] px, int w, int h, System.Random rng, int style)
        {
            Color ink = Ink(style);
            float step = style == 1 ? 0.04f : style == 3 ? 0.035f : 0.055f;
            float top = style == 3 ? 0.82f : style == 1 ? 0.82f : 0.88f;
            float left = style == 1 ? 0.17f : 0.1f;
            float bottom = style == 4 ? 0.2f : 0.12f;
            for (float row = top; row > bottom; row -= step)
            {
                float x = left + (float)rng.NextDouble() * 0.05f;
                float end = 0.9f - (float)rng.NextDouble() * (row < bottom + 0.1f ? 0.5f : 0.15f);
                float phase = (float)rng.NextDouble() * 10f;
                while (x < end)
                {
                    float word = 0.05f + (float)rng.NextDouble() * 0.12f;
                    for (float t = 0f; t < word && x + t < end; t += 0.0012f)
                    {
                        float yy = style == 3
                            ? row + ((Mathf.Repeat((x + t) * 90f, 1f) < 0.7f) ? Mathf.Sin((x + t) * 900f) * 0.003f : 0f)
                            : row + Mathf.Sin((x + t) * 260f + phase) * 0.006f + Mathf.Sin((x + t) * 41f) * 0.003f;
                        if (style == 3 && Mathf.Repeat((x + t) * 90f, 1f) >= 0.7f) continue; // gaps between typed letters
                        int cx = Mathf.RoundToInt((x + t) * w), cy = Mathf.RoundToInt(yy * h);
                        for (int oy = -1; oy <= 1; oy++)
                        for (int oxp = 0; oxp <= 1; oxp++)
                        {
                            int ix = cx + oxp, iy = cy + oy;
                            if (ix < 0 || iy < 0 || ix >= w || iy >= h) continue;
                            Color p = px[iy * w + ix];
                            if (p.a <= 0f) continue;
                            float k = (oy == 0 ? 0.85f : 0.35f) * (style == 2 ? 0.7f : 1f);
                            px[iy * w + ix] = Color.Lerp(p, new Color(ink.r, ink.g, ink.b, 1f), k);
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
