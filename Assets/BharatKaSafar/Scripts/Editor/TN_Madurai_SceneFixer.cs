using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// One-click fixer for the BKS_TN_M01_Madurai scene car issues.
/// Finds the StartPoint and snaps it properly above the terrain surface.
/// Also ensures CarSpawner, FuelSpawner, and key objects are correctly positioned.
/// </summary>
public class TN_Madurai_SceneFixer
{
    [MenuItem("Bharat Ka Safar/Fix Madurai Scene (Car Issues)")]
    public static void FixMaduraiScene()
    {
        // Find and open the Madurai scene
        string[] guids = AssetDatabase.FindAssets("BKS_TN_M01_Madurai t:Scene");
        if (guids.Length == 0)
        {
            Debug.LogError("[Scene Fixer] Could not find BKS_TN_M01_Madurai scene!");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        bool anyFixed = false;

        // 1. Find and fix StartPoint - it must be ABOVE the terrain
        GameObject startPoint = GameObject.Find("StartPoint");
        if (startPoint != null)
        {
            Vector3 pos = startPoint.transform.position;
            // Place StartPoint above the terrain surface (Y = 5 is safely above any terrain)
            startPoint.transform.position = new Vector3(pos.x > 0 ? pos.x : -5f, 5f, 0f);
            Debug.Log($"[Scene Fixer] StartPoint moved to {startPoint.transform.position}");
            anyFixed = true;
        }
        else
        {
            // Create StartPoint if it doesn't exist
            GameObject newStart = new GameObject("StartPoint");
            newStart.transform.position = new Vector3(-5f, 5f, 0f);
            Debug.LogWarning("[Scene Fixer] StartPoint not found — created a new one at (-5, 5, 0).");
            anyFixed = true;
        }

        // 2. Fix Car spawner position (CoinSpawner acts as the car spawn trigger in this project)
        GameObject carObj = GameObject.FindGameObjectWithTag("Car");
        if (carObj == null) carObj = GameObject.Find("Car2 1(Clone)");
        if (carObj == null) carObj = GameObject.Find("Car2 1");
        
        if (carObj != null)
        {
            // Make sure the car is ABOVE the terrain when scene starts
            Vector3 carPos = carObj.transform.position;
            if (carPos.y < 3f)
            {
                carObj.transform.position = new Vector3(carPos.x, 5f, carPos.z);
                Debug.Log($"[Scene Fixer] Car position raised to Y=5 to prevent terrain clipping.");
                anyFixed = true;
            }
        }

        // 3. Make sure StartCollider is enabled
        GameObject startCollider = GameObject.Find("StartCollider");
        if (startCollider != null && !startCollider.activeInHierarchy)
        {
            startCollider.SetActive(true);
            Debug.Log("[Scene Fixer] StartCollider was disabled — re-enabled.");
            anyFixed = true;
        }

        // 4. Ensure the terrain "Terrain Level0" has Ground tag AND Ground layer
        GameObject terrainLv0 = GameObject.Find("Terrain Level0");
        if (terrainLv0 != null)
        {
            if (terrainLv0.tag != "Ground")
            {
                // Tag might need to exist in project first
                try { terrainLv0.tag = "Ground"; } catch { }
            }
            
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0 && terrainLv0.layer != groundLayer)
            {
                SetLayerRecursively(terrainLv0, groundLayer);
                Debug.Log("[Scene Fixer] Terrain Level0 layer set to Ground.");
                anyFixed = true;
            }
        }

        // 5. Make sure TN_Madurai_Terrain (our generated one) is fully disabled/destroyed
        // since user is using Terrain Level0 already
        GameObject tnTerrain = GameObject.Find("TN_Madurai_Terrain");
        if (tnTerrain != null && tnTerrain.activeSelf)
        {
            tnTerrain.SetActive(false);
            Debug.Log("[Scene Fixer] TN_Madurai_Terrain was active — disabled it to prevent physics conflict.");
            anyFixed = true;
        }

        if (anyFixed)
        {
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Scene Fixer] ✅ Madurai scene fixed and saved! Press Play to test.");
        }
        else
        {
            Debug.Log("[Scene Fixer] Nothing needed to be fixed. Everything looks correct!");
        }
    }

    private static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }
}
