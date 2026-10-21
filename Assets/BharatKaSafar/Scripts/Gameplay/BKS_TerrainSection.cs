using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    public enum BKS_TerrainDifficulty { Easy, Medium, Hard, SuperComplex }
    [CreateAssetMenu(menuName = "Bharat Ka Safar/Terrain Section")]
    public class BKS_TerrainSection : ScriptableObject
    {
        public BKS_TerrainDifficulty difficulty;
        [Min(20)] public float length = 100;
        [Range(0, 40)] public float slopeDegrees = 10;
        [Range(1, 4)] public int hills = 1;
        public bool valley;
        public GameObject referencePrefab;
        [TextArea] public string designNotes;

        public float Height(float t)
        {
            // Smooth, grounded joins. Max derivative is tan(slopeDegrees).
            float amplitude = length * Mathf.Tan(slopeDegrees * Mathf.Deg2Rad) / (Mathf.PI * Mathf.Max(1, hills));
            return (valley ? -1 : 1) * amplitude * 0.5f * (1 - Mathf.Cos(2 * Mathf.PI * hills * Mathf.Clamp01(t)));
        }
    }
}
