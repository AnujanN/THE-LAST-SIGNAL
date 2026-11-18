using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Hand-built fixtures for things no free model covers (fridge, bathtub, sink, safe, tape decks),
    /// detailing for the big machines (vents, exhaust, status lights, warning signs, cables) and light
    /// fittings above every ceiling lamp. Box stand-ins keep their colliders so gameplay is unchanged.
    /// </summary>
    public static class Fixtures
    {
        // ------------------------------------------------------------------ materials

        private static Material Enamel => Mat("Enamel", new Color(0.74f, 0.73f, 0.68f), smoothness: 0.6f);
        private static Material Chrome => Metal("Chrome", new Color(0.7f, 0.7f, 0.72f), 0.85f);
        private static Material Brass => Metal("Brass", new Color(0.55f, 0.42f, 0.18f), 0.7f);
        private static Material Dark => Mat("VentDark", new Color(0.035f, 0.035f, 0.04f), smoothness: 0.3f);
        private static Material SafeSteel => Mat("SafeSteel", new Color(0.1f, 0.13f, 0.11f), smoothness: 0.45f);

        private static Material Metal(string name, Color c, float smooth)
        {
            Material m = Mat(name, c, smoothness: smooth);
            m.SetFloat("_Metallic", 0.9f);
            return m;
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>A frame at the bottom centre of a box with +Z pointing out of its front.</summary>
        private static Transform Frame(Transform box, Vector3 front, out float width, out float height, out float depth)
        {
            Vector3 s = box.lossyScale;
            bool frontAlongX = Mathf.Abs(front.x) > 0.5f;
            width = frontAlongX ? s.z : s.x;
            depth = frontAlongX ? s.x : s.z;
            height = s.y;
            var f = new GameObject(box.name + "_Detail").transform;
            f.SetParent(box.parent, false);
            f.position = box.position - Vector3.up * s.y / 2f;
            f.rotation = Quaternion.LookRotation(front, Vector3.up);
            return f;
        }

        private static GameObject Part(Transform frame, string name, Vector3 pos, Vector3 size, Material mat)
        {
            GameObject go = Box(frame, name, pos, size, mat, collider: false);
            go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.On;
            return go;
        }

        private static GameObject Cyl(Transform frame, string name, Vector3 pos, float radius, float height, Material mat, Vector3 euler = default)
        {
            GameObject go = Prim(frame, name, PrimitiveType.Cylinder, pos, new Vector3(radius * 2f, height / 2f, radius * 2f), mat, euler, collider: false);
            go.isStatic = true;
            return go;
        }

        private static void Grime(Transform frame, Vector3 pos, Vector2 size, Dressing.Kind kind)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.name = "Grime";
            q.transform.SetParent(frame, false);
            q.transform.localPosition = pos;
            q.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // face +Z (out of the front)
            q.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = q.GetComponent<Renderer>();
            r.sharedMaterial = Dressing.DecalMaterial(kind);
            r.shadowCastingMode = ShadowCastingMode.Off;
            q.isStatic = true;
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform c in root)
            {
                Transform f = Find(c, name);
                if (f != null) return f;
            }
            return null;
        }

        private static Transform HideBox(Transform root, string name)
        {
            Transform box = Find(root, name);
            if (box == null) { Debug.LogWarning("[Fixtures] Missing " + name); return null; }
            if (box.GetComponent<Renderer>() is Renderer r) r.enabled = false;
            return box;
        }

        // ------------------------------------------------------------------ kitchen and bathroom

        public static void Fridge(Transform env, string name, Vector3 front)
        {
            Transform box = HideBox(env, name);
            if (box == null) return;
            Transform f = Frame(box, front, out float w, out float h, out float d);
            Part(f, "Body", new Vector3(0f, h / 2f, -0.03f), new Vector3(w - 0.02f, h - 0.02f, d - 0.06f), Enamel);
            Part(f, "KickGrille", new Vector3(0f, 0.06f, d / 2f - 0.03f), new Vector3(w - 0.1f, 0.09f, 0.02f), Dark);
            Part(f, "FreezerDoor", new Vector3(0f, h * 0.83f, d / 2f - 0.02f), new Vector3(w - 0.03f, h * 0.27f, 0.05f), Enamel);
            Part(f, "MainDoor", new Vector3(0f, h * 0.38f + 0.06f, d / 2f - 0.02f), new Vector3(w - 0.03f, h * 0.6f, 0.05f), Enamel);
            Part(f, "FreezerHandle", new Vector3(w / 2f - 0.09f, h * 0.83f, d / 2f + 0.04f), new Vector3(0.03f, 0.2f, 0.03f), Chrome);
            Part(f, "MainHandle", new Vector3(w / 2f - 0.09f, h * 0.55f, d / 2f + 0.04f), new Vector3(0.03f, 0.42f, 0.03f), Chrome);
            Part(f, "Badge", new Vector3(0f, h * 0.93f, d / 2f + 0.006f), new Vector3(0.16f, 0.03f, 0.005f), Chrome);
            Grime(f, new Vector3(-0.12f, h * 0.3f, d / 2f + 0.007f), new Vector2(0.4f, 0.7f), Dressing.Kind.Streaks);
            Grime(f, new Vector3(0.1f, 0.25f, d / 2f + 0.008f), new Vector2(0.7f, 0.4f), Dressing.Kind.BaseGrime);
        }

        public static void Bathtub(Transform env, string name, Vector3 front)
        {
            Transform box = HideBox(env, name);
            if (box == null) return;
            Transform f = Frame(box, front, out float w, out float h, out float d);
            float y0 = 0.08f, wallH = h - y0;
            Part(f, "WallFront", new Vector3(0f, y0 + wallH / 2f, d / 2f - 0.04f), new Vector3(w, wallH, 0.08f), Enamel);
            Part(f, "WallBack", new Vector3(0f, y0 + wallH / 2f, -d / 2f + 0.04f), new Vector3(w, wallH, 0.08f), Enamel);
            Part(f, "EndL", new Vector3(-w / 2f + 0.04f, y0 + wallH / 2f, 0f), new Vector3(0.08f, wallH, d), Enamel);
            Part(f, "EndR", new Vector3(w / 2f - 0.04f, y0 + wallH / 2f, 0f), new Vector3(0.08f, wallH, d), Enamel);
            Part(f, "Bottom", new Vector3(0f, y0 + 0.03f, 0f), new Vector3(w - 0.16f, 0.06f, d - 0.16f), Enamel);
            Part(f, "RimFront", new Vector3(0f, h - 0.01f, d / 2f - 0.03f), new Vector3(w + 0.03f, 0.04f, 0.1f), Enamel);
            Part(f, "RimBack", new Vector3(0f, h - 0.01f, -d / 2f + 0.03f), new Vector3(w + 0.03f, 0.04f, 0.1f), Enamel);
            Part(f, "RimL", new Vector3(-w / 2f + 0.03f, h - 0.01f, 0f), new Vector3(0.1f, 0.04f, d + 0.03f), Enamel);
            Part(f, "RimR", new Vector3(w / 2f - 0.03f, h - 0.01f, 0f), new Vector3(0.1f, 0.04f, d + 0.03f), Enamel);
            // Stagnant, murky water.
            Part(f, "Water", new Vector3(0f, h * 0.45f, 0f), new Vector3(w - 0.16f, 0.01f, d - 0.16f),
                Mat("MurkyWater", new Color(0.04f, 0.05f, 0.035f), smoothness: 0.95f));
            foreach (Vector2 c in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                Prim(f, "Foot", PrimitiveType.Sphere, new Vector3(c.x * (w / 2f - 0.12f), 0.05f, c.y * (d / 2f - 0.1f)), Vector3.one * 0.11f, Brass, collider: false);
            Cyl(f, "Tap", new Vector3(-w / 2f + 0.05f, h + 0.08f, 0f), 0.02f, 0.16f, Chrome);
            Part(f, "Spout", new Vector3(-w / 2f + 0.11f, h + 0.15f, 0f), new Vector3(0.14f, 0.03f, 0.03f), Chrome);
            Grime(f, new Vector3(0.2f, h * 0.5f, d / 2f + 0.002f), new Vector2(0.8f, 0.45f), Dressing.Kind.Streaks);
        }

        public static void Sink(Transform env, string name, Vector3 front)
        {
            Transform box = HideBox(env, name);
            if (box == null) return;
            Transform f = Frame(box, front, out float w, out float h, out float d);
            Cyl(f, "Pedestal", new Vector3(0f, 0.36f, -0.04f), 0.11f, 0.72f, Enamel);
            Part(f, "Basin", new Vector3(0f, h - 0.09f, 0f), new Vector3(w, 0.18f, d), Enamel);
            Part(f, "Bowl", new Vector3(0f, h + 0.002f, 0.03f), new Vector3(w - 0.12f, 0.004f, d - 0.17f),
                Mat("BasinShadow", new Color(0.18f, 0.18f, 0.16f), smoothness: 0.6f));
            Cyl(f, "Faucet", new Vector3(0f, h + 0.06f, -d / 2f + 0.07f), 0.015f, 0.12f, Chrome);
            Part(f, "FaucetSpout", new Vector3(0f, h + 0.11f, -d / 2f + 0.12f), new Vector3(0.025f, 0.025f, 0.1f), Chrome);
            Cyl(f, "KnobL", new Vector3(-0.12f, h + 0.02f, -d / 2f + 0.07f), 0.022f, 0.04f, Chrome);
            Cyl(f, "KnobR", new Vector3(0.12f, h + 0.02f, -d / 2f + 0.07f), 0.022f, 0.04f, Chrome);
            Grime(f, new Vector3(0f, h - 0.1f, d / 2f + 0.002f), new Vector2(0.5f, 0.25f), Dressing.Kind.Streaks);
        }

        // ------------------------------------------------------------------ the safe

        /// <summary>Heavy safe: body from the env box, door details on the door that disappears when unlocked.</summary>
        public static void Safe(Transform env, string bodyName, Transform items, string doorName, string dialName, Vector3 front)
        {
            Transform body = HideBox(env, bodyName);
            if (body == null) return;
            Transform f = Frame(body, front, out float w, out float h, out float d);
            Part(f, "Plinth", new Vector3(0f, 0.03f, 0f), new Vector3(w + 0.04f, 0.06f, d + 0.04f), Dark);
            Part(f, "Body", new Vector3(0f, 0.06f + (h - 0.06f) / 2f, -0.01f), new Vector3(w, h - 0.06f, d - 0.02f), SafeSteel);
            Part(f, "TopLip", new Vector3(0f, h - 0.015f, 0.005f), new Vector3(w + 0.02f, 0.03f, d), SafeSteel);
            Part(f, "Cavity", new Vector3(0f, h * 0.5f, d / 2f - 0.004f), new Vector3(w - 0.12f, h * 0.72f, 0.012f), Mat("Black", new Color(0.03f, 0.03f, 0.03f)));
            Cyl(f, "HingeTop", new Vector3(-w / 2f + 0.02f, h * 0.75f, d / 2f), 0.025f, 0.1f, Brass);
            Cyl(f, "HingeLow", new Vector3(-w / 2f + 0.02f, h * 0.25f, d / 2f), 0.025f, 0.1f, Brass);
            Part(f, "Plate", new Vector3(0f, h - 0.05f, d / 2f + 0.003f), new Vector3(0.26f, 0.035f, 0.004f), Brass);

            Transform door = HideBox(items, doorName);
            Transform dial = HideBox(items, dialName);
            if (door == null) return;
            // Visual children of the door (they vanish with it when the safe opens). Undo the box scale.
            // Undo the box scale on an unrotated holder first; rotating under a non-uniformly scaled parent would skew.
            Transform holder = new GameObject("DoorVisual").transform;
            holder.SetParent(door, false);
            Vector3 ds = door.localScale;
            holder.localScale = new Vector3(1f / ds.x, 1f / ds.y, 1f / ds.z);
            Transform v = new GameObject("Front").transform;
            v.SetParent(holder, false);
            v.rotation = Quaternion.LookRotation(front, Vector3.up);
            Part(v, "Slab", Vector3.zero, new Vector3(w - 0.1f, h * 0.78f, 0.05f), Mat("SafeDoorSteel", new Color(0.13f, 0.16f, 0.14f), smoothness: 0.5f));
            Part(v, "Inset", new Vector3(0f, 0f, 0.027f), new Vector3(w - 0.22f, h * 0.6f, 0.005f), SafeSteel);
            foreach (Vector2 b in new[] { new Vector2(-1f, -1f), new Vector2(1f, -1f), new Vector2(-1f, 1f), new Vector2(1f, 1f) })
                Cyl(v, "Bolt", new Vector3(b.x * (w / 2f - 0.1f), b.y * (h * 0.33f), 0.03f), 0.015f, 0.01f, Brass, new Vector3(90f, 0f, 0f));
            // Handle: a three-spoke wheel to the side of the dial.
            Vector3 hub = new Vector3(w * 0.22f, -0.08f, 0.05f);
            Cyl(v, "Hub", hub, 0.03f, 0.04f, Chrome, new Vector3(90f, 0f, 0f));
            for (int i = 0; i < 3; i++)
            {
                GameObject spoke = Part(v, "Spoke", hub + new Vector3(0f, 0f, 0.01f), new Vector3(0.16f, 0.018f, 0.018f), Chrome);
                spoke.transform.localRotation = Quaternion.Euler(0f, 0f, i * 60f);
            }
            if (dial != null)
            {
                // The combination dial (its interactable box stays, invisible).
                Vector3 dialLocal = v.InverseTransformPoint(dial.position);
                Cyl(v, "DialRing", new Vector3(dialLocal.x, dialLocal.y, 0.035f), 0.07f, 0.012f, Mat("Black", new Color(0.03f, 0.03f, 0.03f)), new Vector3(90f, 0f, 0f));
                Cyl(v, "Dial", new Vector3(dialLocal.x, dialLocal.y, 0.05f), 0.05f, 0.03f, Chrome, new Vector3(90f, 0f, 0f));
                Part(v, "DialMark", new Vector3(dialLocal.x, dialLocal.y + 0.075f, 0.043f), new Vector3(0.008f, 0.02f, 0.004f), Mat("LedRed", new Color(0.8f, 0.1f, 0.08f), emissive: true));
            }
        }

        // ------------------------------------------------------------------ tape decks

        /// <summary>Reel-to-reel tape deck drawn over a TapePlayer's box (which stays as the interactable).</summary>
        public static void TapeDeck(Transform deckBox, bool charred)
        {
            if (deckBox.GetComponent<Renderer>() is Renderer r) r.enabled = false;
            Transform reelBox = deckBox.Find("Reel");
            if (reelBox != null && reelBox.GetComponent<Renderer>() is Renderer rr) rr.enabled = false;
            var v = new GameObject("DeckVisual").transform;
            v.SetParent(deckBox, false);
            Vector3 s = deckBox.localScale;
            v.localScale = new Vector3(1f / s.x, 1f / s.y, 1f / s.z);
            v.localRotation = Quaternion.identity;
            float y0 = -s.y / 2f;
            Material body = charred ? Mat("Charred", new Color(0.05f, 0.045f, 0.04f), smoothness: 0.05f) : Mat("DeckBody", new Color(0.16f, 0.12f, 0.09f), smoothness: 0.4f);
            Material plate = charred ? body : Metal("BrushedAluminium", new Color(0.55f, 0.55f, 0.56f), 0.5f);
            Material reel = charred ? body : Metal("ReelSilver", new Color(0.62f, 0.62f, 0.64f), 0.7f);
            Part(v, "Body", new Vector3(0f, y0 + 0.07f, 0f), new Vector3(0.45f, 0.14f, 0.3f), body);
            Part(v, "Faceplate", new Vector3(0f, y0 + 0.142f, 0f), new Vector3(0.43f, 0.004f, 0.28f), plate);
            foreach (float x in new[] { -0.11f, 0.11f })
            {
                Cyl(v, "Reel", new Vector3(x, y0 + 0.152f, 0.03f), 0.075f, 0.012f, reel);
                Cyl(v, "Hub", new Vector3(x, y0 + 0.162f, 0.03f), 0.018f, 0.012f, Dark);
            }
            Part(v, "Tape", new Vector3(0f, y0 + 0.156f, -0.045f), new Vector3(0.22f, 0.003f, 0.008f), Mat("TapeBrown", new Color(0.18f, 0.1f, 0.05f), smoothness: 0.6f));
            for (int i = 0; i < 5; i++)
                Part(v, "Key", new Vector3(-0.12f + i * 0.06f, y0 + 0.148f, -0.115f), new Vector3(0.04f, 0.012f, 0.025f), i == 2 ? Mat("Red", new Color(0.6f, 0.08f, 0.06f)) : Dark);
            if (!charred)
                Part(v, "VUMeter", new Vector3(0.15f, y0 + 0.146f, -0.07f), new Vector3(0.07f, 0.004f, 0.035f), Mat("LedAmber", new Color(1f, 0.6f, 0.15f), emissive: true));
        }

        // ------------------------------------------------------------------ machines

        /// <summary>Vents on the sides, status lights, a warning sign, a cable to the wall and optionally an exhaust stack.</summary>
        public static void Machine(Transform env, string name, Vector3 front, bool exhaust, string warning = "DANGER\nHIGH VOLTAGE")
        {
            Transform box = Find(env, name);
            if (box == null) { Debug.LogWarning("[Fixtures] Missing machine " + name); return; }
            Transform f = Frame(box, front, out float w, out float h, out float d);
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 6; i++)
                {
                    GameObject slat = Part(f, "Vent", new Vector3(side * (w / 2f + 0.012f), h * 0.35f + i * 0.08f, -d * 0.1f), new Vector3(0.02f, 0.035f, d * 0.5f), Dark);
                    slat.transform.localRotation = Quaternion.Euler(0f, 0f, side * 25f);
                }
            string[] leds = { "LedRed", "LedGreen", "LedAmber" };
            Color[] cols = { new Color(0.9f, 0.1f, 0.08f), new Color(0.15f, 0.9f, 0.2f), new Color(1f, 0.6f, 0.1f) };
            for (int i = 0; i < 3; i++)
                Part(f, "Status", new Vector3(w / 2f - 0.12f - i * 0.07f, h - 0.1f, d / 2f + 0.006f), new Vector3(0.035f, 0.035f, 0.012f), Mat(leds[i], cols[i], emissive: true));
            if (!string.IsNullOrEmpty(warning))
                Sign(f, "Warning", warning, new Vector3(w / 2f + 0.025f, h * 0.62f, d * 0.15f), -90f, 1.2f, new Color(0.85f, 0.7f, 0.1f), new Vector2(0.6f, 0.3f));
            Part(f, "Cable", new Vector3(0.15f, 0.04f, -d / 2f - 0.35f), new Vector3(0.06f, 0.06f, 0.7f), Mat("Black", new Color(0.03f, 0.03f, 0.03f)));
            if (exhaust)
            {
                float top = h, ceiling = top + 1f;
                if (Physics.Raycast(box.position + Vector3.up * (h / 2f + 0.05f), Vector3.up, out RaycastHit hit, 4f, ~0, QueryTriggerInteraction.Ignore))
                    ceiling = hit.point.y - f.position.y;
                float len = Mathf.Max(0.2f, ceiling - top);
                Vector3 at = new Vector3(w / 2f - 0.3f, top + len / 2f, -d / 2f + 0.3f);
                Cyl(f, "Exhaust", at, 0.09f, len, Mat("Metal", new Color(0.5f, 0.5f, 0.52f), smoothness: 0.6f));
                Cyl(f, "ExhaustCollar", new Vector3(at.x, top + 0.08f, at.z), 0.12f, 0.12f, Dark);
            }
        }

        // ------------------------------------------------------------------ ceiling fittings

        /// <summary>A fluorescent fitting on the ceiling above every hanging lamp (so lights no longer float).</summary>
        public static void CeilingFittings(Transform lightingRoot)
        {
            if (lightingRoot == null) return;
            Physics.SyncTransforms();
            foreach (Light l in lightingRoot.GetComponentsInChildren<Light>(true))
            {
                if (l.type != LightType.Point || l.transform.position.y < 2.3f) continue;
                if (!Physics.Raycast(l.transform.position, Vector3.up, out RaycastHit hit, 1.2f, ~0, QueryTriggerInteraction.Ignore)) continue;
                GameObject fit = PropLibrary.Place(l.transform.parent, "mounted_fluorescent_lights", hit.point - Vector3.up * 0.04f, 0f, collider: false);
                if (fit == null) continue;
                fit.name = "Fitting_" + l.name;
                foreach (Renderer r in fit.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
    }
}
