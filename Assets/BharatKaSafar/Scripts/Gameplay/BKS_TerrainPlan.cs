using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Gameplay
{
    [CreateAssetMenu(menuName = "Bharat Ka Safar/Terrain Plan")]
    public class BKS_TerrainPlan : ScriptableObject
    {
        public int stateOrder;
        public string stateName;
        public int missionNumber;
        public MissionData mission;
        public BKS_TerrainDifficulty difficulty;
        public Material groundMaterial;
        [Min(20)] public float safeStartLength = 220;
        [Min(20)] public float safeFinishLength = 100;
        [Min(20)] public float recoveryLength = 100;
        public BKS_TerrainSection[] sections;
        public bool playtested;
        [TextArea] public string testNotes;

        public static BKS_TerrainDifficulty DifficultyFor(int stateOrder, int missionNumber)
        {
            if (stateOrder < 1 || stateOrder > 28 || missionNumber < 1 || missionNumber > 5)
                throw new System.ArgumentOutOfRangeException("State must be 1–28 and mission 1–5.");
            int[,] tiers = { {0,0,1,0,1}, {0,1,1,1,2}, {1,1,2,2,2}, {1,2,3,2,3} };
            return (BKS_TerrainDifficulty)tiers[(stateOrder - 1) / 7, missionNumber - 1];
        }

        public Vector2[] BuildPoints(float spacing = 1f)
        {
            spacing = Mathf.Clamp(spacing, 0.25f, 2f);
            var points = new List<Vector2> { Vector2.zero };
            float x = 0;
            AppendFlat(points, ref x, safeStartLength, spacing);
            if (sections != null)
                for (int i = 0; i < sections.Length; i++)
                {
                    var section = sections[i];
                    if (section != null)
                    {
                        int count = Mathf.CeilToInt(section.length / spacing);
                        for (int j = 1; j <= count; j++)
                        {
                            float t = j / (float)count;
                            points.Add(new Vector2(x + section.length * t, section.Height(t)));
                        }
                        x += section.length;
                    }
                    // Regular recovery roads: Easy 3 flats per challenge, Medium 1,
                    // Hard alternating, Super Complex after pairs of challenges.
                    int flats = difficulty == BKS_TerrainDifficulty.Easy ? 3
                        : difficulty == BKS_TerrainDifficulty.Medium ? 1 : (i % 2 == 1 ? 1 : 0);
                    if (i < sections.Length - 1)
                        AppendFlat(points, ref x, flats * recoveryLength, spacing);
                }
            AppendFlat(points, ref x, safeFinishLength, spacing);
            return points.ToArray();
        }

        private static void AppendFlat(List<Vector2> points, ref float x, float length, float spacing)
        {
            if (length <= 0) return;
            int count = Mathf.CeilToInt(length / spacing);
            float start = x;
            for (int i = 1; i <= count; i++) points.Add(new Vector2(start + length * i / count, 0));
            x += length;
        }
    }
}
