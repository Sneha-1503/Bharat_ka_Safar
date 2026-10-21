using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Fixes car + terrain issues in ALL 5 Tamil Nadu scenes at once.
/// </summary>
public class TN_AllScenes_Fixer
{
    [MenuItem("Bharat Ka Safar/Fix ALL Tamil Nadu Scenes (Car Issues)")]
    public static void FixAllTNScenes()
    {
        string[] tnScenes = {
            "BKS_TN_M01_Madurai",
            "BKS_TN_M02_Chennai",
            "BKS_TN_M03_Mahabalipuram",
            "BKS_TN_M04_Ooty",
            "BKS_TN_M05_Kanyakumari"
        };

        int fixedCount = 0;

        foreach (string sceneName in tnScenes)
        {
            string[] guids = AssetDatabase.FindAssets($"{sceneName} t:Scene");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"[TN Fixer] Could not find scene: {sceneName}. Skipping.");
                continue;
            }

            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Debug.Log($"[TN Fixer] Processing: {sceneName}...");

            // ── 1. Disable our generated TN terrain (no visible colliders) ──────────
            string[] tnTerrainNames = {
                "TN_Madurai_Terrain",
                "TN_Chennai_Terrain",
                "TN_Mahabalipuram_Terrain",
                "TN_Ooty_Terrain",
                "TN_Kanyakumari_Terrain",
                "TN_Madurai_Terrain(Clone)"
            };
            foreach (string tName in tnTerrainNames)
            {
                GameObject tnTerrain = GameObject.Find(tName);
                if (tnTerrain != null && tnTerrain.activeSelf)
                {
                    tnTerrain.SetActive(false);
                    Debug.Log($"  → Disabled phantom terrain: {tName}");
                }
            }

            // ── 2. Fix Terrain Level0 — set Layer to Ground ──────────────────────
            GameObject terrainLv0 = GameObject.Find("Terrain Level0");
            if (terrainLv0 != null)
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer >= 0)
                    SetLayerRecursively(terrainLv0, groundLayer);

                // Also ensure its EdgeCollider2D is active
                EdgeCollider2D ec = terrainLv0.GetComponentInChildren<EdgeCollider2D>();
                if (ec != null) ec.enabled = true;

                Debug.Log($"  → Terrain Level0 layer set to Ground, collider enabled.");
            }

            // ── 3. Fix StartPoint — must be above terrain surface ────────────────
            GameObject startPoint = GameObject.Find("StartPoint");
            if (startPoint != null)
            {
                // Find the terrain's surface Y by raycasting from above
                float spawnX = startPoint.transform.position.x;
                float surfaceY = GetTerrainSurfaceY(spawnX);

                // Place StartPoint safely above terrain
                startPoint.transform.position = new Vector3(spawnX > 0 ? spawnX : -5f, surfaceY + 4f, 0f);
                Debug.Log($"  → StartPoint repositioned to Y={surfaceY + 4f}");
            }
            else
            {
                // Create one if missing
                GameObject sp = new GameObject("StartPoint");
                sp.transform.position = new Vector3(-5f, 6f, 0f);
                Debug.LogWarning($"  → StartPoint not found in {sceneName} — created at (-5, 6, 0).");
            }

            // ── 4. Fix FuelSpawner position ───────────────────────────────────────
            GameObject fuelSpawner = GameObject.Find("FuelSpawner");
            if (fuelSpawner != null)
            {
                Vector3 fp = fuelSpawner.transform.position;
                if (fp.y < 2f)
                    fuelSpawner.transform.position = new Vector3(fp.x, 4f, fp.z);
            }

            // ── 5. Ensure CoinSpawner is positioned above terrain ─────────────────
            GameObject coinSpawner = GameObject.Find("CoinSpawner");
            if (coinSpawner != null)
            {
                Vector3 cp = coinSpawner.transform.position;
                if (cp.y < 2f)
                    coinSpawner.transform.position = new Vector3(cp.x, 4f, cp.z);
            }

            // ── 6. Reset Time.timeScale (in case scene was frozen) ────────────────
            // This is saved as a scene note only — runtime timeScale is controlled by code

            // ── 7. Save the fixed scene ───────────────────────────────────────────
            EditorSceneManager.SaveScene(scene);
            fixedCount++;
            Debug.Log($"  ✅ {sceneName} fixed and saved!");
        }

        Debug.Log($"[TN Fixer] Done! Fixed {fixedCount}/{tnScenes.Length} Tamil Nadu scenes. Press Play on any to test!");
    }

    /// <summary>
    /// Tries to find the terrain surface Y at a given X using a Physics2D raycast from above.
    /// Falls back to 0 if nothing is hit.
    /// </summary>
    private static float GetTerrainSurfaceY(float x)
    {
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(x, 100f), Vector2.down, 200f, LayerMask.GetMask("Ground"));
        if (hit.collider != null)
            return hit.point.y;

        return 0f; // Fallback
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
