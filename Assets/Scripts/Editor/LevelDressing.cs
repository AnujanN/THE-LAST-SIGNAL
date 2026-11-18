using UnityEngine;
using static LastSignal.EditorTools.PropLibrary;

namespace LastSignal.EditorTools
{
    /// <summary>
    /// Per-level set dressing: real props placed by hand around the existing furniture, doors and
    /// interactables (nothing blocks a route or something the player must use), then a grime pass.
    /// Called by each level builder before the NavMesh bake so large props are walked around.
    /// </summary>
    public static class LevelDressing
    {
        private static Rect Room(float xMin, float zMin, float xMax, float zMax) => Rect.MinMaxRect(xMin, zMin, xMax, zMax);

        private static Transform Group(Transform env) => BuildKit.Group(env, "Props");

        // Tipped-over furniture lies on its side a little above the floor.
        private static readonly Vector3 OnSide = new Vector3(0f, 0f, 88f);

        public static void Level1(Transform env)
        {
            Transform p = Group(env);
            Place(p, "cardboard_box_01", new Vector3(-11.0f, 0f, -5.2f), 10f);
            Place(p, "cardboard_box_01", new Vector3(-10.2f, 0f, -5.4f), -25f);
            Place(p, "painted_wooden_chair_01", new Vector3(-6.6f, 0.22f, -2.6f), 40f, OnSide);
            Place(p, "trashbag", new Vector3(-4.6f, 0f, 1.35f), 30f, collider: false);
            Place(p, "trashbag", new Vector3(3.4f, 0f, 1.4f), -60f, collider: false);
            Place(p, "wheelchair_01", new Vector3(-3.0f, 0f, -5.0f), 145f);
            Place(p, "drawer_cabinet", new Vector3(11.45f, 0f, 1.2f), -90f);
            Place(p, "painted_wooden_chair_01", new Vector3(8.0f, 0f, -0.15f), 180f);
            Place(p, "can_rusted", new Vector3(6.1f, 0f, -5.1f), 20f, collider: false);
            Place(p, "can_rusted", new Vector3(6.5f, 0.04f, -4.7f), 80f, new Vector3(90f, 0f, 0f), collider: false);
            Place(p, "street_rat", new Vector3(-1.0f, 0f, 12.6f), 30f, collider: false);
            Place(p, "steel_frame_shelves_01", new Vector3(-7.45f, 0f, 17.2f), 90f);
            Place(p, "television_02", new Vector3(6.6f, 0f, 15.3f), -30f);
            Place(p, "metal_office_desk", new Vector3(5.8f, 0f, 17.6f), -90f);

            Dressing.Apply(env, new[]
            {
                Room(-12f, -6f, -4f, 2f), Room(-4f, -6f, 4f, 2f), Room(4f, -6f, 12f, 2f), Room(-1.5f, 2f, 1.5f, 14f),
                Room(1.5f, 4f, 6f, 8f), Room(-8f, 14f, 8f, 24f),
            }, 101, new Dressing.Theme { ceiling = 3.2f, streaks = 16, baseGrime = 30, stains = 18, puddles = 3, mould = 4, cracks = 10, cobwebs = 10, papers = 22 });
        }

        public static void Level2(Transform env)
        {
            Transform p = Group(env);
            Place(p, "barrel_03", new Vector3(-14.0f, 0f, 8.0f), 0f);
            Place(p, "barrel_03", new Vector3(-13.2f, 0f, 8.3f), 50f);
            Place(p, "barrel_03", new Vector3(-14.2f, 0f, 13.8f), 120f);
            Place(p, "barrel_03", new Vector3(-6.3f, 0f, 22.3f), 10f);
            Place(p, "wooden_crate_01", new Vector3(-10.6f, 0f, 13.9f), 15f);
            Place(p, "wooden_crate_01", new Vector3(6.3f, 0f, 9.6f), -10f);
            Place(p, "wooden_crate_01", new Vector3(-7.2f, 0f, 26.0f), 35f);
            Place(p, "steel_frame_shelves_01", new Vector3(6.6f, 0f, 13.6f), -90f);
            Place(p, "trashbag", new Vector3(-12.0f, 0f, 6.4f), 10f, collider: false);
            Place(p, "trashbag", new Vector3(13.0f, 0f, 4.6f), 70f, collider: false);
            Place(p, "cardboard_box_01", new Vector3(-7.3f, 0f, 30.1f), 20f);
            Place(p, "street_rat", new Vector3(6.4f, 0f, 6.5f), 200f, collider: false);
            Place(p, "street_rat", new Vector3(-3.0f, 0f, 22.4f), 75f, collider: false);
            Place(p, "can_rusted", new Vector3(-2.1f, 0.04f, 18.0f), 0f, new Vector3(90f, 0f, 0f), collider: false);
            Place(p, "russian_food_cans_01", new Vector3(-9.6f, 0f, 9.0f), 40f, collider: false);

            Dressing.Apply(env, new[]
            {
                Room(-2f, -1f, 2f, 4f), Room(-15f, 4f, 15f, 7f), Room(-15f, 7f, -7f, 15f), Room(-1.5f, 7f, 1.5f, 25f),
                Room(1.5f, 9f, 7f, 15f), Room(-7f, 17f, -1.5f, 23f), Room(-8f, 25f, 8f, 33f), Room(8f, 27f, 11f, 31f),
            }, 202, new Dressing.Theme { ceiling = 3f, streaks = 24, baseGrime = 32, stains = 20, puddles = 14, mould = 14, cracks = 8, cobwebs = 12, papers = 8 });
        }

        public static void Level3(Transform env)
        {
            Transform p = Group(env);
            Place(p, "drawer_cabinet", new Vector3(-2.5f, 0f, 24.35f), 180f);
            Place(p, "painted_wooden_chair_01", new Vector3(4.1f, 0f, 6.9f), 20f);
            Place(p, "painted_wooden_chair_01", new Vector3(6.3f, 0.22f, 7.4f), 100f, OnSide);
            Place(p, "television_02", new Vector3(5.6f, 0f, 11.4f), 180f);
            Place(p, "cardboard_box_01", new Vector3(7.9f, 0f, 23.8f), 5f);
            Place(p, "cardboard_box_01", new Vector3(6.7f, 0f, 23.4f), -30f);
            Place(p, "trashbag", new Vector3(8.2f, 0f, 11.3f), 0f, collider: false);
            Place(p, "wheelchair_01", new Vector3(0.95f, 0f, 19.5f), 200f);
            Place(p, "wooden_bookshelf_worn", new Vector3(-7.0f, 0f, 1.4f), 0f);
            Place(p, "street_rat", new Vector3(-4.6f, 0f, 10.3f), 140f, collider: false);
            Place(p, "can_rusted", new Vector3(3.2f, 0.04f, 4.0f), 30f, new Vector3(90f, 0f, 0f), collider: false);

            Dressing.Apply(env, new[]
            {
                Room(-1.5f, -2f, 1.5f, 1f), Room(-1.5f, 1f, 1.5f, 25f), Room(-9f, 1f, -1.5f, 9.5f), Room(-9f, 9.5f, -1.5f, 14f),
                Room(-9f, 14f, -1.5f, 25f), Room(1.5f, 1f, 9f, 12f), Room(1.5f, 12f, 9f, 20f), Room(1.5f, 20f, 9f, 25f),
            }, 303, new Dressing.Theme { ceiling = 3f, streaks = 14, baseGrime = 30, stains = 22, puddles = 3, mould = 10, cracks = 8, cobwebs = 14, papers = 16, scratches = 3 });
        }

        public static void Level4(Transform env)
        {
            Transform p = Group(env);
            Place(p, "painted_wooden_chair_01", new Vector3(-6.0f, 0.22f, 14.0f), 30f, OnSide, charredLook: true);
            Place(p, "painted_wooden_chair_01", new Vector3(4.0f, 0.22f, 10.2f), 160f, OnSide, charredLook: true);
            Place(p, "steel_frame_shelves_01", new Vector3(-8.45f, 0f, 6.8f), 90f, new Vector3(0f, 0f, -6f), charredLook: true);
            Place(p, "metal_office_desk", new Vector3(-5.6f, 0f, 22.9f), 90f, charredLook: true);
            Place(p, "wooden_crate_01", new Vector3(2.6f, 0f, 9.0f), 25f, charredLook: true);
            Place(p, "barrel_03", new Vector3(5.8f, 0f, 19.2f), 0f, charredLook: true);
            Place(p, "television_02", new Vector3(7.3f, 0f, 16.2f), 200f, charredLook: true);
            Place(p, "wheelchair_01", new Vector3(-0.85f, 0f, 4.6f), 20f, charredLook: true);
            Place(p, "cardboard_box_01", new Vector3(3.3f, 0f, 25.8f), 40f, charredLook: true);

            Dressing.Apply(env, new[]
            {
                Room(-1.5f, -2f, 1.5f, 2f), Room(-1.5f, 2f, 1.5f, 9f), Room(-1.5f, 12f, 1.5f, 18f), Room(-9f, 3f, -1.5f, 17f),
                Room(1.5f, 8f, 10f, 17f), Room(-7f, 18f, 7f, 27f), Room(7f, 22f, 10f, 26f),
            }, 404, new Dressing.Theme { ceiling = 3.2f, streaks = 10, baseGrime = 30, stains = 20, puddles = 2, cracks = 14, cobwebs = 6, soot = 30, papers = 6, scratches = 2, debris = 60 });
        }

        public static void Level5(Transform env)
        {
            Transform p = Group(env);
            Place(p, "steel_frame_shelves_01", new Vector3(7.5f, 0f, 13.4f), 180f);
            Place(p, "cardboard_box_01", new Vector3(2.5f, 0f, 13.2f), 10f);
            Place(p, "cardboard_box_01", new Vector3(3.3f, 0f, 13.4f), -35f);
            Place(p, "metal_office_desk", new Vector3(-6.0f, 0f, 4.7f), 0f);
            Place(p, "painted_wooden_chair_01", new Vector3(-5.0f, 0.22f, 18.0f), 60f, OnSide);
            Place(p, "trashbag", new Vector3(1.0f, 0f, 13.8f), 0f, collider: false);
            Place(p, "television_02", new Vector3(5.8f, 0f, 26.8f), 160f);
            Place(p, "street_rat", new Vector3(-12.5f, 0f, 24.0f), 90f, collider: false);
            Place(p, "can_rusted", new Vector3(-9.6f, 0.04f, 5.2f), 0f, new Vector3(90f, 0f, 0f), collider: false);

            Dressing.Apply(env, new[]
            {
                Room(-1.5f, -2f, 1.5f, 1f), Room(-1.5f, 1f, 1.5f, 16f), Room(1.5f, 4f, 12f, 14f), Room(-12f, 4f, -1.5f, 14f),
                Room(-7f, 16f, 7f, 28f), Room(-16f, 23.5f, -7f, 26.5f),
            }, 505, new Dressing.Theme { ceiling = 3.4f, streaks = 18, baseGrime = 30, stains = 20, puddles = 3, mould = 4, cracks = 12, cobwebs = 12, papers = 14 });
        }
    }
}
