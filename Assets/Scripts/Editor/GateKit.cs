using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Northpoint's gate and fence, shared by the intro's drive (gate shut, the car stops at it) and Level 1's yard
    /// (gate standing open where Alex drove in): two concrete posts, two leaves of steel bars hung on hinges, a chain,
    /// the red "NORTHPOINT RADIO / STATION CLOSED / NO ENTRY" sign, and see-through chain-link fencing on steel posts.
    /// The gate faces along its local +Z (the way in); the sign is on the outside (-Z).
    /// </summary>
    public static class GateKit
    {
        public const float HalfOpening = 3.3f;
        private const float LeafWidth = 2.9f;

        /// <summary>The gate at ground point `at`, facing `facing` (+Z = in). openDegrees 0 = shut; leaves swing inwards.</summary>
        public static Transform Gate(Transform parent, Vector3 at, Quaternion facing, float openDegrees)
        {
            Transform g = Group(parent, "Gate");
            g.SetPositionAndRotation(at, facing);
            Material concrete = EndingBuilder.Textured("GatePost", "concrete_wall_004", new Color(0.5f, 0.5f, 0.48f), 0.1f);
            Material steel = Mat("GateSteel", new Color(0.22f, 0.2f, 0.18f), smoothness: 0.35f);
            foreach (float x in new[] { -HalfOpening, HalfOpening })
            {
                Box(g, "Post", new Vector3(x, 1.1f, 0f), new Vector3(0.45f, 2.2f, 0.45f), concrete);
                Box(g, "PostCap", new Vector3(x, 2.24f, 0f), new Vector3(0.55f, 0.08f, 0.55f), concrete, collider: false);
            }
            for (int side = 0; side < 2; side++)
            {
                float sgn = side == 0 ? -1f : 1f; // left leaf hangs on the left post and reaches right
                Transform hinge = Group(g, side == 0 ? "LeafLeft" : "LeafRight", new Vector3(sgn * (HalfOpening - 0.24f), 0f, 0f));
                hinge.localRotation = Quaternion.Euler(0f, side == 0 ? -openDegrees : openDegrees, 0f);
                float dir = -sgn; // along the leaf, away from the hinge
                Box(hinge, "Rail", new Vector3(dir * LeafWidth / 2f, 1.6f, 0f), new Vector3(LeafWidth, 0.07f, 0.07f), steel, collider: false, isStatic: false);
                Box(hinge, "Rail", new Vector3(dir * LeafWidth / 2f, 0.35f, 0f), new Vector3(LeafWidth, 0.07f, 0.07f), steel, collider: false, isStatic: false);
                Box(hinge, "Stile", new Vector3(dir * 0.04f, 0.95f, 0f), new Vector3(0.07f, 1.4f, 0.07f), steel, collider: false, isStatic: false);
                Box(hinge, "Stile", new Vector3(dir * (LeafWidth - 0.04f), 0.95f, 0f), new Vector3(0.07f, 1.4f, 0.07f), steel, collider: false, isStatic: false);
                for (int i = 1; i < 9; i++)
                    Box(hinge, "Bar", new Vector3(dir * i * LeafWidth / 9f, 0.95f, 0f), new Vector3(0.035f, 1.3f, 0.035f), steel, collider: false, isStatic: false);
                Box(hinge, "Brace", new Vector3(dir * LeafWidth / 2f, 0.97f, 0f), new Vector3(Mathf.Sqrt(LeafWidth * LeafWidth + 1.25f * 1.25f), 0.045f, 0.04f), steel, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(1.25f, LeafWidth) * Mathf.Rad2Deg * -dir);
                if (side == 0)
                {
                    // The sign, on the outside of the left leaf.
                    Box(hinge, "SignBoard", new Vector3(dir * 1.45f, 1.1f, -0.06f), new Vector3(1.6f, 0.8f, 0.03f), Mat("SignWhite", new Color(0.7f, 0.68f, 0.62f)), collider: false, isStatic: false);
                    var sign = new GameObject("SignText", typeof(RectTransform));
                    sign.transform.SetParent(hinge, false);
                    sign.transform.localPosition = new Vector3(dir * 1.45f, 1.1f, -0.08f);
                    var tmp = sign.AddComponent<TextMeshPro>();
                    if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
                    tmp.text = "<b>NORTHPOINT RADIO</b>\n<size=70%>STATION CLOSED\nNO ENTRY</size>";
                    tmp.fontSize = 1.6f;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = new Color(0.55f, 0.08f, 0.06f);
                    tmp.textWrappingMode = TextWrappingModes.NoWrap;
                    ((RectTransform)sign.transform).sizeDelta = new Vector2(1.5f, 0.7f);
                }
                else
                {
                    // The chain: wrapped shut, or hanging from the open leaf where it was cut.
                    Box(hinge, "Chain", new Vector3(dir * (LeafWidth - 0.06f), openDegrees > 1f ? 0.95f : 1.1f, -0.03f), new Vector3(0.12f, openDegrees > 1f ? 0.45f : 0.22f, 0.04f), steel, collider: false, isStatic: false);
                }
            }
            return g;
        }

        /// <summary>Chain-link fencing from a to b: steel posts every 2.5 m or less, a top rail, mesh panels you can see through.</summary>
        public static void Fence(Transform parent, Vector3 a, Vector3 b, float height, System.Func<Vector3, float> groundY, bool solid)
        {
            Transform f = Group(parent, "ChainLinkFence");
            Material steel = Mat("FenceSteel", new Color(0.3f, 0.3f, 0.31f), smoothness: 0.4f);
            Material mesh = ChainLink();
            float len = Vector3.Distance(new Vector3(a.x, 0f, a.z), new Vector3(b.x, 0f, b.z));
            int bays = Mathf.Max(1, Mathf.CeilToInt(len / 2.5f));
            Vector3 prev = Vector3.zero;
            for (int i = 0; i <= bays; i++)
            {
                Vector3 p = Vector3.Lerp(a, b, i / (float)bays);
                p.y = groundY(p);
                Box(f, "FencePost", p + Vector3.up * height / 2f, new Vector3(0.07f, height, 0.07f), steel, collider: false);
                if (i > 0)
                {
                    Vector3 mid = (prev + p) / 2f, d = p - prev;
                    float w = new Vector2(d.x, d.z).magnitude;
                    Quaternion r = Quaternion.LookRotation(new Vector3(d.z, 0f, -d.x)); // panel faces across the line
                    GameObject rail = Box(f, "TopRail", mid + Vector3.up * (height - 0.03f), new Vector3(w, 0.045f, 0.045f), steel, collider: false);
                    rail.transform.rotation = r;
                    var quad = new GameObject("Mesh");
                    quad.transform.SetParent(f, false);
                    quad.transform.SetPositionAndRotation(mid + Vector3.up * (height / 2f), r);
                    quad.AddComponent<MeshFilter>().sharedMesh = Panel(w, height - 0.08f);
                    var mr = quad.AddComponent<MeshRenderer>();
                    mr.sharedMaterial = mesh;
                    mr.shadowCastingMode = ShadowCastingMode.On;
                    quad.isStatic = true;
                    if (solid)
                    {
                        var col = quad.AddComponent<BoxCollider>();
                        col.size = new Vector3(w, height, 0.1f);
                    }
                }
                prev = p;
            }
        }

        /// <summary>A flat panel in its local XY (width x height), UVs in quarter-metres so the diamonds stay true size.</summary>
        private static Mesh Panel(float w, float h)
        {
            var m = new Mesh { name = "ChainLinkPanel" };
            m.SetVertices(new List<Vector3> { new Vector3(-w / 2f, -h / 2f, 0f), new Vector3(w / 2f, -h / 2f, 0f), new Vector3(-w / 2f, h / 2f, 0f), new Vector3(w / 2f, h / 2f, 0f) });
            m.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(w * 4f, 0f), new Vector2(0f, h * 4f), new Vector2(w * 4f, h * 4f) });
            m.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Galvanised chain-link: a diamond wire pattern, cut out (alpha clip) and seen from both sides.</summary>
        private static Material ChainLink()
        {
            const string texPath = "Assets/Art/Textures/Generated/ChainLink.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex == null)
            {
                const int n = 128;
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
                var px = new Color[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        // Two families of diagonal wires, one diamond per tile; a little thicker where they cross.
                        float u = Mathf.Repeat((x + y) / (float)n, 1f), v = Mathf.Repeat((x - y) / (float)n, 1f);
                        float d1 = Mathf.Min(u, 1f - u) * n, d2 = Mathf.Min(v, 1f - v) * n;
                        float wire = Mathf.Max(Mathf.Clamp01(3.2f - d1 * 1.4f), Mathf.Clamp01(3.2f - d2 * 1.4f));
                        float shade = 0.75f + 0.25f * Mathf.Clamp01(1f - Mathf.Min(d1, d2) / 2f);
                        px[y * n + x] = new Color(0.62f * shade, 0.63f * shade, 0.64f * shade, wire);
                    }
                t.SetPixels(px);
                t.Apply();
                EnsureFolder("Assets/Art/Textures/Generated");
                System.IO.File.WriteAllBytes(texPath, t.EncodeToPNG());
                Object.DestroyImmediate(t);
                AssetDatabase.ImportAsset(texPath);
                var imp = (TextureImporter)AssetImporter.GetAtPath(texPath);
                imp.alphaIsTransparency = true;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.mipmapEnabled = true;
                imp.SaveAndReimport();
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            }
            Material m = Mat("ChainLink", new Color(0.75f, 0.76f, 0.78f), smoothness: 0.55f);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_AlphaClip", 1f);
            m.SetFloat("_Cutoff", 0.45f);
            m.EnableKeyword("_ALPHATEST_ON");
            m.SetFloat("_Cull", 0f); // both sides
            m.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
