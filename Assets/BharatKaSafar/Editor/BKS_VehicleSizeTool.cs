using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Makes every vehicle in Assets/BharatKaSafar/Prefabs/Vehicles (BKS_*) 2 units longer, keeping its shape:
    /// the prefab root is scaled so the body (its BoxCollider2D) becomes (length + 2) units. Everything inside —
    /// body art, driver, wheels, colliders, joints — scales with it, so the hand adjustments stay as they are.
    /// Safe to run again: the scale is always computed from the body's own size (it does not grow twice).
    /// The Store (icons, catalog) is not touched.
    /// Menu: Bharat Ka Safar > Vehicle Maker > Make All Vehicles 2 Units Bigger
    /// </summary>
    public static class BKS_VehicleSizeTool
    {
        private const string Folder = "Assets/BharatKaSafar/Prefabs/Vehicles";
        private const float ExtraLength = 2f;

        [MenuItem("Bharat Ka Safar/Vehicle Maker/Make All Vehicles 2 Units Bigger")]
        public static void Run()
        {
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { Folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.IO.Path.GetFileName(p).StartsWith("BKS_")).ToList();
            int n = 0;
            foreach (var path in paths)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // the car body: the widest BoxCollider2D (the body collider)
                    var body = root.GetComponentsInChildren<BoxCollider2D>(true)
                        .OrderByDescending(b => b.size.x * Mathf.Abs(b.transform.localScale.x)).FirstOrDefault();
                    if (body == null) { Debug.LogWarning($"[Vehicles] {path}: no body collider, skipped."); continue; }

                    // body length at root scale 1
                    float baseLength = body.size.x * Mathf.Abs(body.transform.lossyScale.x) / Mathf.Abs(root.transform.localScale.x);
                    float scale = (baseLength + ExtraLength) / baseLength;
                    root.transform.localScale = new Vector3(scale, scale, 1f);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    Debug.Log($"[Vehicles] {System.IO.Path.GetFileNameWithoutExtension(path)}: {baseLength:0.0} → {baseLength + ExtraLength:0.0} units (scale {scale:0.00})");
                    n++;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Debug.Log($"[Vehicles] {n} vehicles are now 2 units longer.");
        }
    }
}
