using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static LastSignal.EditorTools.BuildKit;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// A believable family saloon built in code (no model file): a smooth lofted body with a sloping bonnet, raked
    /// windscreen, rounded roof and boot, proper wheel arches; tinted glass you can see the cabin through (seats,
    /// dashboard, steering wheel on the RIGHT: right-hand drive); wheels with tyres, rims and wheel nuts; lamps,
    /// grille, number plates, door shut lines, handles and mirrors. Origin on the ground under the middle, +Z forward.
    /// </summary>
    public static class CarModel
    {
        private const float Length = 4.5f, Wheelbase = 2.66f, WheelRadius = 0.33f, Track = 0.775f;

        private struct Section
        {
            public float yBottom, yTop, wBottom, wTop, rBottom, rTop;
        }

        public static Transform Build(Transform parent, string name, Vector3 position, float yaw, Color paintColour, string plate)
        {
            Transform car = Group(parent, name, position);
            car.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Material paint = Mat("CarBody_" + ColorUtility.ToHtmlStringRGB(paintColour), paintColour, smoothness: 0.78f);
            Material glass = Mat("CarWindowGlass", new Color(0.03f, 0.035f, 0.045f, 0.55f), smoothness: 0.96f);
            EndingBuilder.MakeTransparent(glass);
            Material trim = Mat("CarTrimBlack", new Color(0.02f, 0.02f, 0.022f), smoothness: 0.3f);
            Material chrome = Mat("CarChrome", new Color(0.65f, 0.66f, 0.68f), smoothness: 0.9f);
            Material tyre = Mat("CarTyre", new Color(0.025f, 0.025f, 0.025f), smoothness: 0.15f);
            Material rim = Mat("CarRim", new Color(0.45f, 0.46f, 0.48f), smoothness: 0.75f);
            Material cabin = Mat("CarCabin", new Color(0.05f, 0.045f, 0.045f), smoothness: 0.15f);

            // Body: lofted from the rear bumper to the nose.
            Mesh body = Loft(BodySection, -Length / 2f, Length / 2f, 64, 6, "CarBody");
            Piece(car, "Body", body, paint);
            // Glasshouse (all glass), with a painted roof panel and pillars over it.
            Mesh house = Loft(HouseSection, -1.17f, 0.87f, 36, 5, "CarGlasshouse");
            Piece(car, "Glasshouse", house, glass);
            Mesh roof = Loft(RoofSection, -0.72f, 0.1f, 14, 5, "CarRoof");
            Piece(car, "Roof", roof, paint);
            Pillars(car, paint);
            // Fill under the sills between the wheel arches.
            Box(car, "Underbody", new Vector3(0f, 0.26f, 0f), new Vector3(1.62f, 0.14f, Wheelbase - 0.82f), trim, collider: false, isStatic: false);

            // Cabin (seen through the glass): dashboard, wheel on the right, seats, headrests, mirror.
            Box(car, "Dashboard", new Vector3(0f, 0.98f, 0.55f), new Vector3(1.5f, 0.14f, 0.38f), cabin, collider: false, isStatic: false);
            var wheel = new GameObject("SteeringWheel");
            wheel.transform.SetParent(car, false);
            wheel.transform.localPosition = new Vector3(0.38f, 1.05f, 0.3f);
            wheel.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            wheel.AddComponent<MeshFilter>().sharedMesh = Torus(0.17f, 0.018f);
            wheel.AddComponent<MeshRenderer>().sharedMaterial = trim;
            foreach (float x in new[] { -0.38f, 0.38f })
            {
                Box(car, "SeatBase", new Vector3(x, 0.72f, -0.25f), new Vector3(0.52f, 0.14f, 0.5f), cabin, collider: false, isStatic: false);
                Box(car, "SeatBack", new Vector3(x, 1.02f, -0.5f), new Vector3(0.5f, 0.62f, 0.12f), cabin, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                Box(car, "Headrest", new Vector3(x, 1.38f, -0.57f), new Vector3(0.26f, 0.16f, 0.1f), cabin, collider: false, isStatic: false);
            }
            Box(car, "RearSeat", new Vector3(0f, 0.88f, -0.88f), new Vector3(1.42f, 0.38f, 0.14f), cabin, collider: false, isStatic: false)
                .transform.localRotation = Quaternion.Euler(-14f, 0f, 0f);
            Box(car, "Mirror", new Vector3(0f, 1.33f, 0.18f), new Vector3(0.22f, 0.06f, 0.02f), trim, collider: false, isStatic: false);

            // Wheels.
            foreach (float x in new[] { -Track, Track })
                foreach (float z in new[] { -Wheelbase / 2f, Wheelbase / 2f })
                    Wheel(car, new Vector3(x, WheelRadius, z), x > 0f ? 1f : -1f, tyre, rim, chrome, trim);

            // Front: headlamps, grille, lower intake; back: tail lamps; number plates.
            Material lamp = Mat("CarHeadlamp", new Color(0.55f, 0.58f, 0.6f), smoothness: 0.95f);
            Material tail = Mat("CarTailLamp", new Color(0.32f, 0.02f, 0.02f), smoothness: 0.9f);
            Material amber = Mat("CarIndicator", new Color(0.45f, 0.25f, 0.02f), smoothness: 0.9f);
            foreach (float s in new[] { -1f, 1f })
            {
                Box(car, "Headlamp", new Vector3(s * 0.57f, 0.68f, 2.2f), new Vector3(0.36f, 0.12f, 0.06f), lamp, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(0f, -s * 6f, 0f);
                Box(car, "Indicator", new Vector3(s * 0.78f, 0.67f, 2.12f), new Vector3(0.08f, 0.1f, 0.06f), amber, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(0f, -s * 30f, 0f);
                Box(car, "TailLamp", new Vector3(s * 0.6f, 0.83f, -2.22f), new Vector3(0.38f, 0.14f, 0.05f), tail, collider: false, isStatic: false)
                    .transform.localRotation = Quaternion.Euler(0f, s * 6f, 0f);
            }
            Box(car, "Grille", new Vector3(0f, 0.67f, 2.23f), new Vector3(0.7f, 0.12f, 0.04f), trim, collider: false, isStatic: false);
            Box(car, "GrilleBar", new Vector3(0f, 0.67f, 2.25f), new Vector3(0.7f, 0.015f, 0.01f), chrome, collider: false, isStatic: false);
            Box(car, "Intake", new Vector3(0f, 0.38f, 2.24f), new Vector3(1.1f, 0.08f, 0.05f), trim, collider: false, isStatic: false);
            Box(car, "RubStrip_F", new Vector3(0f, 0.5f, 2.255f), new Vector3(1.5f, 0.035f, 0.02f), trim, collider: false, isStatic: false);
            Box(car, "RubStrip_R", new Vector3(0f, 0.55f, -2.255f), new Vector3(1.5f, 0.035f, 0.02f), trim, collider: false, isStatic: false);
            Plate(car, new Vector3(0f, 0.44f, 2.265f), 0f, plate, new Color(0.85f, 0.85f, 0.82f));
            Plate(car, new Vector3(0f, 0.62f, -2.265f), 180f, plate, new Color(0.85f, 0.72f, 0.12f));
            Prim(car, "Exhaust", PrimitiveType.Cylinder, new Vector3(-0.55f, 0.24f, -2.2f), new Vector3(0.06f, 0.08f, 0.06f), chrome, new Vector3(90f, 0f, 0f), collider: false);

            // Sides: door shut lines, handles, wing mirrors.
            foreach (float s in new[] { -1f, 1f })
            {
                foreach (float z in new[] { 0.86f, -0.27f, -1.13f })
                    Box(car, "ShutLine", new Vector3(s * 0.893f, 0.62f, z), new Vector3(0.006f, 0.62f, 0.008f), trim, collider: false, isStatic: false);
                Box(car, "SillLine", new Vector3(s * 0.885f, 0.33f, -0.13f), new Vector3(0.006f, 0.008f, 2.0f), trim, collider: false, isStatic: false);
                foreach (float z in new[] { 0.12f, -0.98f })
                    Box(car, "Handle", new Vector3(s * 0.9f, 0.86f, z), new Vector3(0.02f, 0.03f, 0.13f), chrome, collider: false, isStatic: false);
                Box(car, "MirrorArm", new Vector3(s * 0.92f, 1.0f, 0.78f), new Vector3(0.1f, 0.03f, 0.05f), trim, collider: false, isStatic: false);
                Box(car, "WingMirror", new Vector3(s * 1.0f, 1.03f, 0.75f), new Vector3(0.1f, 0.11f, 0.16f), paint, collider: false, isStatic: false);
            }

            // One box collider for the whole car.
            var col = car.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.72f, 0f);
            col.size = new Vector3(1.8f, 1.44f, Length);
            foreach (Transform t in car.GetComponentsInChildren<Transform>()) t.gameObject.isStatic = true;
            foreach (Renderer r in car.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.On;
            return car;
        }

        // ------------------------------------------------------------------ shape

        private static float Curve(float z, params float[] zy)
        {
            // zy = z0, y0, z1, y1, ... with z descending from the nose to the tail; smooth between points.
            if (z >= zy[0]) return zy[1];
            for (int i = 0; i + 3 < zy.Length; i += 2)
            {
                float za = zy[i], zb = zy[i + 2];
                if (z <= za && z >= zb)
                {
                    float k = Mathf.SmoothStep(0f, 1f, (za - z) / Mathf.Max(1e-4f, za - zb));
                    return Mathf.Lerp(zy[i + 1], zy[i + 3], k);
                }
            }
            return zy[zy.Length - 1];
        }

        private static Section BodySection(float z)
        {
            float top = Curve(z, 2.25f, 0.6f, 2.17f, 0.72f, 1.95f, 0.8f, 1.4f, 0.87f, 0.9f, 0.94f, -1.15f, 0.95f, -1.4f, 0.97f, -1.95f, 0.98f, -2.15f, 0.93f, -2.25f, 0.76f);
            float bottom = Curve(z, 2.25f, 0.32f, 2.0f, 0.25f, 1.75f, 0.2f, -1.75f, 0.2f, -2.0f, 0.25f, -2.25f, 0.34f);
            // Wheel arches.
            foreach (float zc in new[] { Wheelbase / 2f, -Wheelbase / 2f })
            {
                float d = Mathf.Abs(z - zc), r = WheelRadius + 0.07f;
                if (d < r) bottom = Mathf.Max(bottom, WheelRadius + Mathf.Sqrt(r * r - d * d));
            }
            float w = 0.89f;
            if (z > 1.85f) w = Mathf.Lerp(0.89f, 0.79f, Mathf.SmoothStep(0f, 1f, (z - 1.85f) / 0.4f));
            if (z < -1.9f) w = Mathf.Lerp(0.89f, 0.83f, Mathf.SmoothStep(0f, 1f, (-1.9f - z) / 0.35f));
            float h = Mathf.Max(0.05f, top - bottom);
            return new Section { yBottom = bottom, yTop = top, wBottom = w, wTop = w - 0.06f, rBottom = Mathf.Min(0.06f, h * 0.3f), rTop = Mathf.Min(0.15f, h * 0.45f) };
        }

        private static Section HouseSection(float z)
        {
            float top = Curve(z, 0.87f, 0.95f, 0.6f, 1.12f, 0.25f, 1.33f, 0.05f, 1.41f, -0.35f, 1.43f, -0.7f, 1.4f, -0.95f, 1.2f, -1.17f, 0.98f);
            float h = Mathf.Max(0.02f, top - 0.93f);
            return new Section { yBottom = 0.93f, yTop = top, wBottom = 0.84f, wTop = Mathf.Lerp(0.84f, 0.66f, Mathf.Clamp01(h / 0.45f)), rBottom = 0.01f, rTop = Mathf.Min(0.16f, h * 0.45f) };
        }

        private static Section RoofSection(float z)
        {
            float top = Curve(z, 0.1f, 1.415f, -0.35f, 1.44f, -0.72f, 1.415f);
            return new Section { yBottom = top - 0.06f, yTop = top + 0.008f, wBottom = 0.69f, wTop = 0.65f, rBottom = 0.01f, rTop = 0.03f };
        }

        /// <summary>Black B pillars between the front and rear side windows (the tinted glass reads as the rest).</summary>
        private static void Pillars(Transform car, Material paint)
        {
            Material black = Mat("CarTrimBlack", new Color(0.02f, 0.02f, 0.022f), smoothness: 0.3f);
            foreach (float s in new[] { -1f, 1f })
            {
                GameObject b = Box(car, "BPillar", new Vector3(s * 0.765f, 1.17f, -0.28f), new Vector3(0.025f, 0.46f, 0.08f), black, collider: false, isStatic: false);
                b.transform.localRotation = Quaternion.Euler(0f, 0f, s * 19f); // leaning in with the glass
                Box(car, "WindowSeal", new Vector3(s * 0.845f, 0.945f, -0.15f), new Vector3(0.02f, 0.02f, 2.0f), black, collider: false, isStatic: false);
            }
        }

        private static void Wheel(Transform car, Vector3 at, float side, Material tyre, Material rim, Material chrome, Material dark)
        {
            Transform w = Group(car, "Wheel", at);
            Prim(w, "Tyre", PrimitiveType.Cylinder, Vector3.zero, new Vector3(WheelRadius * 2f, 0.105f, WheelRadius * 2f), tyre, new Vector3(0f, 0f, 90f), collider: false);
            Prim(w, "Sidewall", PrimitiveType.Cylinder, new Vector3(side * 0.1f, 0f, 0f), new Vector3(WheelRadius * 1.86f, 0.01f, WheelRadius * 1.86f), tyre, new Vector3(0f, 0f, 90f), collider: false);
            Prim(w, "Rim", PrimitiveType.Cylinder, new Vector3(side * 0.104f, 0f, 0f), new Vector3(0.42f, 0.012f, 0.42f), rim, new Vector3(0f, 0f, 90f), collider: false);
            Prim(w, "RimDish", PrimitiveType.Cylinder, new Vector3(side * 0.112f, 0f, 0f), new Vector3(0.3f, 0.008f, 0.3f), dark, new Vector3(0f, 0f, 90f), collider: false);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                GameObject spoke = Box(w, "Spoke", new Vector3(side * 0.116f, 0f, 0f), new Vector3(0.012f, 0.19f, 0.04f), rim, collider: false, isStatic: false);
                spoke.transform.localRotation = Quaternion.Euler(a, 0f, 0f);
                spoke.transform.localPosition += spoke.transform.localRotation * new Vector3(0f, 0.09f, 0f);
            }
            Prim(w, "Hub", PrimitiveType.Cylinder, new Vector3(side * 0.12f, 0f, 0f), new Vector3(0.08f, 0.01f, 0.08f), chrome, new Vector3(0f, 0f, 90f), collider: false);
            // Dark arch liner behind the wheel.
            Prim(w, "ArchLiner", PrimitiveType.Cylinder, new Vector3(-side * 0.1f, 0.02f, 0f), new Vector3(WheelRadius * 2.3f, 0.06f, WheelRadius * 2.3f), dark, new Vector3(0f, 0f, 90f), collider: false);
        }

        private static void Plate(Transform car, Vector3 at, float yaw, string text, Color colour)
        {
            GameObject p = Box(car, "Plate", at, new Vector3(0.52f, 0.11f, 0.008f), Mat("Plate_" + ColorUtility.ToHtmlStringRGB(colour), colour, smoothness: 0.5f), collider: false, isStatic: false);
            p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var go = new GameObject("PlateText", typeof(RectTransform));
            go.transform.SetParent(car, false);
            go.transform.localPosition = at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0f, 0.006f);
            go.transform.localRotation = Quaternion.Euler(0f, yaw + 180f, 0f);
            var tmp = go.AddComponent<TextMeshPro>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = text;
            tmp.fontSize = 0.62f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.05f, 0.05f, 0.05f);
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            ((RectTransform)go.transform).sizeDelta = new Vector2(0.5f, 0.1f);
        }

        // ------------------------------------------------------------------ mesh building

        private static void Piece(Transform car, string name, Mesh mesh, Material m)
        {
            var go = new GameObject(name);
            go.transform.SetParent(car, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = m;
        }

        /// <summary>
        /// A closed tube along Z through rounded-trapezoid cross-sections (capped at both ends). Each section's ring
        /// runs bottom-right, top-right, top-left, bottom-left, with arcSteps points per rounded corner.
        /// </summary>
        private static Mesh Loft(Func<float, Section> sectionAt, float z0, float z1, int stations, int arcSteps, string name)
        {
            var v = new List<Vector3>();
            var tri = new List<int>();
            int ring = 0;
            for (int i = 0; i <= stations; i++)
            {
                float z = Mathf.Lerp(z0, z1, i / (float)stations);
                List<Vector2> pts = Ring(sectionAt(z), arcSteps);
                ring = pts.Count;
                foreach (Vector2 p in pts) v.Add(new Vector3(p.x, p.y, z));
            }
            for (int i = 0; i < stations; i++)
                for (int j = 0; j < ring; j++)
                {
                    int a = i * ring + j, b = (i + 1) * ring + j, c = i * ring + (j + 1) % ring, d = (i + 1) * ring + (j + 1) % ring;
                    tri.AddRange(new[] { a, c, b, c, d, b });
                }
            // End caps.
            int front = v.Count;
            v.Add(new Vector3(0f, Avg(v, stations * ring, ring), z1));
            for (int j = 0; j < ring; j++) tri.AddRange(new[] { front, stations * ring + j, stations * ring + (j + 1) % ring });
            int back = v.Count;
            v.Add(new Vector3(0f, Avg(v, 0, ring), z0));
            for (int j = 0; j < ring; j++) tri.AddRange(new[] { back, (j + 1) % ring, j });
            var mesh = new Mesh { name = name };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static float Avg(List<Vector3> v, int start, int count)
        {
            float y = 0f;
            for (int i = 0; i < count; i++) y += v[start + i].y;
            return y / count;
        }

        private static List<Vector2> Ring(Section s, int steps)
        {
            var pts = new List<Vector2>();
            void Arc(Vector2 centre, float r, float a0, float a1)
            {
                for (int k = 0; k <= steps; k++)
                {
                    float a = Mathf.Lerp(a0, a1, k / (float)steps) * Mathf.Deg2Rad;
                    pts.Add(centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r);
                }
            }
            Arc(new Vector2(s.wBottom - s.rBottom, s.yBottom + s.rBottom), s.rBottom, -90f, 0f);
            Arc(new Vector2(s.wTop - s.rTop, s.yTop - s.rTop), s.rTop, 0f, 90f);
            Arc(new Vector2(-s.wTop + s.rTop, s.yTop - s.rTop), s.rTop, 90f, 180f);
            Arc(new Vector2(-s.wBottom + s.rBottom, s.yBottom + s.rBottom), s.rBottom, 180f, 270f);
            return pts;
        }

        private static Mesh Torus(float radius, float tube)
        {
            const int seg = 32, side = 8;
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                Vector3 c = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius;
                for (int j = 0; j <= side; j++)
                {
                    float b = j / (float)side * Mathf.PI * 2f;
                    v.Add(c + (c.normalized * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b)) * tube);
                    if (i < seg && j < side)
                    {
                        int k = i * (side + 1) + j, n = k + side + 1;
                        tri.AddRange(new[] { k, n, k + 1, k + 1, n, n + 1 });
                    }
                }
            }
            var mesh = new Mesh { name = "CarWheelTorus" };
            mesh.SetVertices(v);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
