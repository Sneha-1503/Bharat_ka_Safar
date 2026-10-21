using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// The terrain plan ("terrain plan.docx" + the T1–T10 recipes and the 35 bridge missions) as data, and the
    /// terrain builder that follows it:
    ///  • length per level: 1000 / 1100 / 1200 / 1400 / 1500
    ///  • 5 zones: Introduction 15%, Exploration 25%, Main Challenge 25%, Recovery 20%, Destination/Finale 15%
    ///  • each zone gets a terrain shape (T1–T10) from the mission's recipe and its own road material (state roads)
    ///  • the terrain is built as one piece per zone (so each zone can use a different road material)
    /// Bridges: exactly the 35 missions of the plan (Bridge()).
    /// </summary>
    public static class BKS_TerrainPlanData
    {
        public static readonly int[] Lengths = { 1000, 1100, 1200, 1400, 1500 };
        public static readonly string[] ZoneNames = { "Introduction", "Exploration", "MainChallenge", "Recovery", "Finale" };
        public static readonly float[] ZoneEnd = { 0.15f, 0.40f, 0.65f, 0.85f, 1.00f };

        public static int Length(int level) => Lengths[Mathf.Clamp(level, 1, 5) - 1];
        public static float ZoneStart(int z) => z == 0 ? 0f : ZoneEnd[z - 1];

        // ── state roads (plan section 6) ────────────────────────────────────────────────────────
        public static readonly Dictionary<string, string[]> StateRoads = new Dictionary<string, string[]>
        {
            {"Andhra Pradesh", new[]{"R6","R4","R5"}}, {"Arunachal Pradesh", new[]{"R4","R7","R10"}}, {"Assam", new[]{"R7","R9","R1"}},
            {"Bihar", new[]{"R1","R2","R3"}}, {"Chhattisgarh", new[]{"R7","R3","R4"}}, {"Goa", new[]{"R6","R4","R7"}},
            {"Gujarat", new[]{"R6","R8","R7"}}, {"Haryana", new[]{"R1","R3","R4"}}, {"Himachal Pradesh", new[]{"R4","R5","R10"}},
            {"Jharkhand", new[]{"R4","R5","R7"}}, {"Karnataka", new[]{"R2","R4","R7"}}, {"Kerala", new[]{"R6","R9","R4"}},
            {"Madhya Pradesh", new[]{"R2","R7","R4"}}, {"Maharashtra", new[]{"R1","R4","R9"}}, {"Manipur", new[]{"R3","R4","R7"}},
            {"Meghalaya", new[]{"R9","R4","R7"}}, {"Mizoram", new[]{"R4","R7","R9"}}, {"Nagaland", new[]{"R4","R3","R7"}},
            {"Odisha", new[]{"R6","R7","R2"}}, {"Punjab", new[]{"R1","R2","R3"}}, {"Rajasthan", new[]{"R8","R5","R2"}},
            {"Sikkim", new[]{"R4","R10"}}, {"Tamil Nadu", new[]{"R6","R2","R4"}}, {"Telangana", new[]{"R1","R2","R4"}},
            {"Tripura", new[]{"R3","R4","R7"}}, {"Uttar Pradesh", new[]{"R1","R2"}}, {"Uttarakhand", new[]{"R4","R10"}},
            {"West Bengal", new[]{"R1","R6","R9","R4"}},
        };

        /// <summary>Road material of zone z: the state's first road at the start, its other roads through the run.</summary>
        public static string ZoneRoad(MissionData m, int z)
        {
            var r = StateRoads.TryGetValue(m.StateName ?? "", out var list) ? list : new[] { "R3" };
            int[] pick = { 0, 0, 1, 2, r.Length - 1 };
            // snow / the last road only shows up in the later missions
            int i = Mathf.Min(pick[z], r.Length - 1);
            // snow (R10) only from mission 3 on: the early missions use the state's road before it
            if (m.MissionNumber <= 2 && r[i] == "R10" && i > 0) i--;
            return r[Mathf.Clamp(i, 0, r.Length - 1)];
        }

        // ── the 35 bridge missions (state, mission number) → type ───────────────────────────────
        // (the plan's list, mapped onto the game's actual mission numbers)
        private static readonly Dictionary<(string, int), string> BridgeMissions = new Dictionary<(string, int), string>
        {
            {("Uttar Pradesh",2),"B1"}, {("Uttar Pradesh",4),"B2"}, {("Uttar Pradesh",5),"B1"},
            {("Rajasthan",1),"B1"}, {("Rajasthan",4),"B1"},
            {("Haryana",4),"B2"}, {("Punjab",5),"B5"},
            {("Himachal Pradesh",2),"B3"}, {("Himachal Pradesh",4),"B3"},
            {("Uttarakhand",1),"B3"}, {("Uttarakhand",4),"B3"},
            {("Andhra Pradesh",1),"B2"}, {("Andhra Pradesh",4),"B2"},
            {("Tamil Nadu",2),"B1"}, {("Karnataka",4),"B6"},
            {("Kerala",2),"B4"}, {("Kerala",4),"B4"}, {("Telangana",4),"B2"},
            {("Arunachal Pradesh",1),"B3"}, {("Arunachal Pradesh",4),"B3"},
            {("Assam",1),"B5"}, {("Assam",2),"B4"}, {("Manipur",2),"B4"},
            {("Meghalaya",2),"B6"}, {("Meghalaya",3),"B6"}, {("Mizoram",3),"B6"},
            {("Nagaland",3),"B4"}, {("Sikkim",2),"B3"}, {("Sikkim",3),"B3"},
            {("Tripura",2),"B4"}, {("Bihar",4),"B2"}, {("Chhattisgarh",1),"B4"},
            {("Goa",3),"B6"}, {("Odisha",2),"B5"}, {("West Bengal",1),"B5"},
        };

        /// <summary>Bridge type for this mission ("B1".."B6"), or null when the mission has no bridge.</summary>
        public static string Bridge(MissionData m) =>
            BridgeMissions.TryGetValue((m.StateName ?? "", m.MissionNumber), out var t) ? t : null;

        // ── T1–T10 recipes ─────────────────────────────────────────────────────────────────────
        // location → its signature shape (from the recipe library)
        private static readonly (string place, string t)[] LocationShape =
        {
            ("Agra","T1"),("Amritsar","T1"),("Hyderabad","T1"),("Kochi","T1"),("Kolkata","T1"),("Patna","T1"),("Chennai","T1"),("Visakhapatnam","T1"),
            ("Kurukshetra","T2"),("Anandpur","T2"),("Jaipur","T2"),("Bodh Gaya","T2"),("Panaji","T2"),("Baga","T2"),("Dwarka","T2"),("Puri","T2"),
            ("Loktak","T3"),("Alappuzha","T3"),("Alleppey","T3"),("Umiam","T3"),("Neermahal","T3"),("Manali","T3"),("Chitrakote","T3"),
            ("Araku","T4"),("Kaziranga","T4"),("Kanyakumari","T4"),("Coorg","T4"),("Majuli","T4"),("Digha","T4"),("Jaisalmer","T4"),("Rajgir","T4"),
            ("Tirupati","T5"),("Shimla","T5"),("Tawang","T5"),("Ooty","T5"),("Mussoorie","T5"),("Gangtok","T5"),("Sela","T5"),
            ("Dawki","T6"),("Kalimpong","T6"),("Pelling","T6"),("Vijayawada","T6"),("Pinjore","T6"),
            ("Varanasi","T7"),("Nalanda","T7"),("Warangal","T7"),("Hampi","T7"),("Old Goa","T7"),("Sivasagar","T7"),("Khajuraho","T7"),("Konark","T7"),
            ("Nainital","T8"),("Tsomgo","T8"),("Pachmarhi","T8"),("Udaipur","T8"),("Gandikota","T8"),
            ("Spiti","T9"),("Netarhat","T9"),("Mahabaleshwar","T9"),("Jampui","T9"),("Bhulbhulaiya","T9"),("Lucknow","T9"),
            ("Dudhsagar","T10"),("Vantawng","T10"),("Pushkar","T10"),("Nathula","T10"),("Thekkady","T10"),("Mainpat","T10"),("Darjeeling","T10"),
        };

        // ── state terrain character ─────────────────────────────────────────────────────────────
        // amplitude = how big the hills are; shape pools per zone (Exploration, Main Challenge, Recovery, Finale)
        private class Character
        {
            public float amp; public float wave; public string[][] pools;
            public Character(float amp, float wave, string[] e, string[] m, string[] r, string[] f) { this.amp = amp; this.wave = wave; pools = new[] { e, m, r, f }; }
        }

        private static readonly Dictionary<string, Character> Characters = new Dictionary<string, Character>
        {
            {"plains",   new Character(0.70f, 0.6f, new[]{"T1","T2","T7"}, new[]{"T2","T4","T7","T3"}, new[]{"T1","T2"},       new[]{"T2","T4","T8"})},
            {"mountain", new Character(1.45f, 1.0f, new[]{"T4","T5"},      new[]{"T5","T8","T9","T3"}, new[]{"T6","T3"},       new[]{"T5","T8","T10"})},
            {"hills",    new Character(1.15f, 0.9f, new[]{"T2","T4"},      new[]{"T4","T5","T3","T9"}, new[]{"T6","T2","T3"},  new[]{"T5","T4","T8"})},
            {"coast",    new Character(0.85f, 0.7f, new[]{"T1","T2"},      new[]{"T4","T3","T2"},      new[]{"T2","T6"},       new[]{"T4","T8"})},
            {"desert",   new Character(1.00f, 1.3f, new[]{"T4","T2"},      new[]{"T4","T8","T9"},      new[]{"T2","T6"},       new[]{"T4","T10","T8"})},
            {"forest",   new Character(0.95f, 0.8f, new[]{"T2","T7","T4"}, new[]{"T3","T4","T7"},      new[]{"T2","T3"},       new[]{"T4","T5"})},
        };

        private static readonly Dictionary<string, string> StateCharacter = new Dictionary<string, string>
        {
            {"Uttar Pradesh","plains"},{"Bihar","plains"},{"Punjab","plains"},{"Haryana","plains"},{"Telangana","plains"},{"West Bengal","coast"},
            {"Himachal Pradesh","mountain"},{"Uttarakhand","mountain"},{"Sikkim","mountain"},{"Arunachal Pradesh","mountain"},
            {"Meghalaya","hills"},{"Mizoram","hills"},{"Nagaland","hills"},{"Manipur","hills"},{"Tripura","hills"},{"Jharkhand","hills"},{"Karnataka","hills"},
            {"Goa","coast"},{"Andhra Pradesh","coast"},{"Odisha","coast"},{"Tamil Nadu","coast"},{"Maharashtra","coast"},{"Kerala","coast"},
            {"Rajasthan","desert"},{"Gujarat","desert"},
            {"Assam","forest"},{"Chhattisgarh","forest"},{"Madhya Pradesh","forest"},
        };

        private static Character CharacterOf(MissionData m) =>
            Characters[StateCharacter.TryGetValue(m.StateName ?? "", out var c) ? c : "plains"];

        private static int Seed(MissionData m) => StableHash((m.StateName ?? "") + m.MissionNumber + (m.PlaceName ?? m.name));

        /// <summary>Shapes of the 5 zones (Introduction, Exploration, Main, Recovery, Finale) for this mission.</summary>
        public static string[] Recipe(MissionData m)
        {
            int level = Mathf.Clamp(m.MissionNumber, 1, 5);
            var ch = CharacterOf(m);
            var rng = new System.Random(Seed(m));
            var r = new string[5];
            r[0] = "T1";
            for (int z = 1; z < 5; z++)
            {
                var pool = ch.pools[z - 1].Where(t => Allowed(t, level)).ToArray();
                if (pool.Length == 0) pool = new[] { level <= 2 ? "T2" : "T4" };
                r[z] = pool[rng.Next(pool.Length)];
            }
            // never the same shape twice in a row (keeps the run varied)
            for (int z = 2; z < 5; z++)
                if (r[z] == r[z - 1]) { var pool = ch.pools[z - 1].Where(t => t != r[z - 1] && Allowed(t, level)).ToArray(); if (pool.Length > 0) r[z] = pool[rng.Next(pool.Length)]; }

            // the location's own shape (recipe library)
            string text = ((m.PlaceName ?? "") + " " + (m.name ?? "")).ToLowerInvariant();
            var hit = LocationShape.FirstOrDefault(p => text.Contains(p.place.ToLowerInvariant()));
            if (hit.t != null && Allowed(hit.t, level))
            {
                if (hit.t == "T10") r[4] = "T10";
                else if (hit.t == "T7" || hit.t == "T1" || hit.t == "T2") r[1] = hit.t;   // the start of the run
                else r[2] = hit.t;                                                     // the main challenge
            }
            return r;
        }

        // difficulty: big shapes only in the later missions
        private static bool Allowed(string t, int level)
        {
            if (t == "T10") return level >= 4;
            if (t == "T9") return level >= 3;
            if (t == "T5" || t == "T8" || t == "T6") return level >= 2;
            return true;
        }

        // ── profile ────────────────────────────────────────────────────────────────────────────
        /// <summary>Height offset for every unit of x (index 0 = left edge of the terrain, lead units before the start).</summary>
        public static float[] Profile(int count, float lead, int length, MissionData m, string[] recipe)
        {
            int level = Mathf.Clamp(m.MissionNumber, 1, 5);
            var ch = CharacterOf(m);
            var rng = new System.Random(Seed(m) ^ 0x5bd1e995);
            float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);
            float A = (3f + 2.2f * level) * ch.amp * Rand(0.85f, 1.15f);   // plains L1 ~4 … mountain L5 ~20
            float maxSlope = 0.30f + 0.04f * level + (ch.amp > 1.2f ? 0.05f : 0f);

            // zone borders move a little per mission so the rhythm is never the same
            var ends = (float[])ZoneEnd.Clone();
            for (int z = 0; z < 4; z++) ends[z] = Mathf.Clamp(ZoneEnd[z] + Rand(-0.03f, 0.03f), (z == 0 ? 0.10f : ends[z - 1] + 0.12f), 0.92f);

            var h = new float[count];
            float baseY = 0f;
            for (int z = 0; z < 5; z++)
            {
                float u0 = z == 0 ? 0f : ends[z - 1], u1 = ends[z];
                int i0 = Mathf.Max(0, Mathf.RoundToInt(lead + u0 * length)), i1 = Mathf.Min(count - 1, Mathf.RoundToInt(lead + u1 * length));
                float zoneLen = Mathf.Max(1f, i1 - i0);
                var p = new ShapeParams { A = A * Rand(0.8f, 1.25f), level = level, count = 2 + rng.Next(4), r1 = Rand(0f, 1f), r2 = Rand(0f, 1f), wave = ch.wave };
                float endY = 0f;
                for (int i = i0; i <= i1; i++)
                {
                    float u = (i - i0) / zoneLen;
                    float y = Shape(recipe[z], u, zoneLen, p);
                    h[i] = baseY + y;
                    endY = y;
                }
                baseY += endY;
            }
            for (int i = Mathf.RoundToInt(lead + length) + 1; i < count; i++) h[i] = baseY; // after the finish

            // small natural unevenness everywhere (no perfectly smooth curves)
            float f1 = Rand(0f, 6.28f), f2 = Rand(0f, 6.28f), n = 0.25f + 0.12f * level * ch.amp;
            for (int i = 0; i < count; i++) h[i] += n * (0.6f * Mathf.Sin(i * 0.11f / ch.wave + f1) + 0.4f * Mathf.Sin(i * 0.31f / ch.wave + f2));

            // steepness cap both ways so every vehicle can climb and nothing becomes a wall
            for (int pass = 0; pass < 3; pass++)
            {
                for (int i = 1; i < count; i++) h[i] = Mathf.Clamp(h[i], h[i - 1] - maxSlope, h[i - 1] + maxSlope);
                for (int i = count - 2; i >= 0; i--) h[i] = Mathf.Clamp(h[i], h[i + 1] - maxSlope, h[i + 1] + maxSlope);
            }

            // stay inside the terrain mesh (it is 80 high, the road sits 45 above its bottom)
            float max = h.Max(), min = h.Min();
            float k = Mathf.Min(1f, max > 0f ? 32f / max : 1f, min < 0f ? 32f / -min : 1f);
            if (k < 1f) for (int i = 0; i < count; i++) h[i] *= k;

            // flat, safe start (40 units) and flat finish (70 units: memory token + flag)
            int start = Mathf.RoundToInt(lead), finish = Mathf.RoundToInt(lead + length);
            float y0 = h[Mathf.Clamp(start, 0, count - 1)], y1 = h[Mathf.Clamp(finish - 70, 0, count - 1)];
            for (int i = 0; i < count; i++)
            {
                if (i <= start + 40) h[i] = y0;
                else if (i < start + 70) h[i] = Mathf.Lerp(y0, h[i], Smooth((i - start - 40) / 30f));
                if (i >= finish - 70) h[i] = y1;
            }
            return h;
        }

        private struct ShapeParams { public float A, r1, r2, wave; public int level, count; }

        private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
        private static float Bump(float u) { float s = Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI); return s * s; }

        /// <summary>One terrain shape over its zone. u = 0..1, returns the height relative to the zone start (0 at u = 0).</summary>
        private static float Shape(string t, float u, float len, ShapeParams p)
        {
            float A = p.A, x = u * len;
            switch (t)
            {
                case "T1": return 0.08f * A * Mathf.Sin(u * Mathf.PI * (2f + 2f * p.r1));                 // Flat Cruise
                case "T2":                                                                                   // Gentle Roll: 2-4 soft waves
                {
                    int n = 2 + (int)(p.r1 * 3f); float w = u * n; int i = Mathf.Min(n - 1, (int)w);
                    return (0.25f + 0.2f * p.r2) * A * Bump(w - i) * (0.7f + 0.3f * Mathf.Sin(i * 2.1f + p.r1 * 6f));
                }
                case "T3": return -(0.6f + 0.5f * p.r1) * A * Mathf.Pow(Bump(u), 0.7f + p.r2);              // Dip & Recover
                case "T4":                                                                                   // Rolling Adventure: 2-5 hills, each its own height
                {
                    int n = p.count; float w = u * n; int i = Mathf.Min(n - 1, (int)w);
                    float hgt = 0.35f + 0.45f * Mathf.Abs(Mathf.Sin(i * 1.7f + p.r1 * 9f));
                    return hgt * A * Bump(w - i);
                }
                case "T5": { float top = 0.7f + 0.2f * p.r1; return (1.0f + 0.5f * p.r2) * A * Smooth(u / top); }   // Long Climb
                case "T6": { float top = 0.7f + 0.2f * p.r1; return -(0.8f + 0.5f * p.r2) * A * Smooth(u / top); }  // Long Descent
                case "T7": { float per = (3f + 3f * p.r1) * p.wave; return (0.25f + 0.05f * p.level) * Bump((x / per) % 1f); } // Rough Bumps
                case "T8":                                                                                   // Plateau: climb → flat top → exit
                {
                    float c = 0.2f + 0.15f * p.r1, e = 0.7f + 0.15f * p.r2;
                    if (u < c) return A * Smooth(u / c);
                    if (u < e) return A;
                    return Mathf.Lerp(A, (0.2f + 0.4f * p.r1) * A, Smooth((u - e) / (1f - e)));
                }
                case "T9":                                                                                   // Technical: climb + dip + bumps + recovery
                {
                    float c = 0.25f + 0.1f * p.r1;
                    if (u < c) return 0.8f * A * Smooth(u / c);
                    if (u < 0.55f) return Mathf.Lerp(0.8f * A, -0.2f * A, Smooth((u - c) / (0.55f - c)));
                    if (u < 0.75f) return -0.2f * A + (0.3f + 0.05f * p.level) * Bump((x / (4f * p.wave)) % 1f);
                    return Mathf.Lerp(-0.2f * A, (0.2f + 0.3f * p.r2) * A, Smooth((u - 0.75f) / 0.25f));
                }
                case "T10":                                                                                  // Signature finale: steps up, reveal, gentle exit
                {
                    int steps = 2 + (int)(p.r1 * 2f);
                    if (u < 0.6f) { float s = u / 0.6f * steps; int st = Mathf.Min(steps - 1, (int)s); return 0.6f * A * (st + Smooth((s - st) / 0.6f)); }
                    float topY = 0.6f * A * steps;
                    if (u < 0.85f) return topY;
                    return Mathf.Lerp(topY, topY - 0.3f * A, Smooth((u - 0.85f) / 0.15f));
                }
                default: return 0f;
            }
        }

        // ── terrain builder ────────────────────────────────────────────────────────────────────
        /// <summary>
        /// Replace the scene's terrain with the plan terrain for this mission: one TerrainEditor2D per zone
        /// ("Terrain_1_Introduction" ...), each with its zone's road material. Returns false if nothing was built.
        /// </summary>
        public static bool BuildTerrain(Scene scene, MissionData m)
        {
            foreach (var e in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<TerrainEditor2D>(true)).ToList())
                if (e != null) Object.DestroyImmediate(e.gameObject);

            var start = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<StartPoint>(true)).FirstOrDefault();
            Vector3 sp = start != null ? start.transform.position : new Vector3(-5f, 4f, 0f);
            int level = Mathf.Clamp(m.MissionNumber, 1, 5);
            int length = Length(level);
            const int height = 80; const float topLocal = 45f; const float lead = 35f; const float tail = 35f;
            int total = Mathf.RoundToInt(lead + length + tail);
            var recipe = Recipe(m);
            float[] profile = Profile(total + 1, lead, length, m, recipe);
            float left = sp.x - lead, bottom = sp.y - 9f - topLocal;

            var holder = new GameObject("[BKS_Terrain]");
            SceneManager.MoveGameObjectToScene(holder, scene);
            for (int z = 0; z < 5; z++)
            {
                int a = z == 0 ? 0 : Mathf.RoundToInt(lead + ZoneStart(z) * length);
                int b = z == 4 ? total : Mathf.RoundToInt(lead + ZoneEnd[z] * length);
                int width = b - a + 1; // one column of overlap with the next piece, so the road has no gap
                var go = TerrainEditor2D.InstantiateTerrain2D(new Vector3(left + a, bottom, 0f), width, height, 1);
                go.name = $"Terrain_{z + 1}_{ZoneNames[z]}_{recipe[z]}";
                go.tag = "Ground"; // legacy crash sensors look for this tag
                SceneManager.MoveGameObjectToScene(go, scene);
                go.transform.SetParent(holder.transform, true);
                var t = go.GetComponent<TerrainEditor2D>();
                t.OrderInLayer = 10; t.CapOrderInLayer = 11;

                var verts = t.GetVertsPos();
                var top = new Dictionary<int, float>();
                foreach (var v in verts) { int x = Mathf.RoundToInt(v.x); if (!top.TryGetValue(x, out var y) || v.y > y) top[x] = v.y; }
                for (int i = 0; i < verts.Length; i++)
                {
                    int x = Mathf.RoundToInt(verts[i].x);
                    float hgt = Mathf.Max(4f, topLocal + profile[Mathf.Clamp(a + x, 0, profile.Length - 1)]);
                    if (verts[i].y > top[x] - 2f) verts[i].y += hgt - top[x];
                }
                // the plugin's Smart Cap crashes (OverflowException) when a piece ends up with no cap area,
                // so build these pieces with the plain cap (same look, tiled along the road)
                t.SmartCap = false;
                t.EditTerrain(verts, true);
                t.UpdateCollider2D();
                t.SetSortingLayerAndOrder();

                var (ground, cap) = BKS_GroundMaterialTool.Materials(ZoneRoad(m, z));
                int capTiles = Mathf.Max(4, Mathf.RoundToInt(width / 12f)); // plain cap tiling is per piece
                BKS_GroundMaterialTool.ApplyTo(t, ground, cap, 70, capTiles, 1f);
            }

            var gm = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<GameManager>(true)).FirstOrDefault();
            if (gm != null) { gm.targetDistance = Mathf.RoundToInt(sp.x + length - 40f); EditorUtility.SetDirty(gm); }
            return true;
        }

        public static int StableHash(string s) { unchecked { int h = 23; foreach (char c in s) h = h * 31 + c; return h; } }
    }
}
