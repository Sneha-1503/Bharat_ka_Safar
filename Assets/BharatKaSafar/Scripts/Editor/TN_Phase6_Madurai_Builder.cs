using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase6_Madurai_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 6 (Build Madurai Level)")]
    public static void BuildMaduraiLevel()
    {
        string originalScenePath = "Assets/Scenes/Level1.unity"; // Fallback
        
        // Find a valid gameplay scene to use as a template
        string[] guids = AssetDatabase.FindAssets("BKS_UP_M01_Agra t:Scene");
        if (guids.Length > 0)
        {
            originalScenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        }
        else
        {
            // If they don't have BKS_UP_M01_Agra, try finding Level1
            guids = AssetDatabase.FindAssets("Level1 t:Scene");
            if (guids.Length > 0) originalScenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        }

        string newScenePath = "Assets/BharatKaSafar/Scenes/BKS_TN_M01_Madurai.unity";
        
        // Ensure Scenes folder exists
        if (!Directory.Exists("Assets/BharatKaSafar/Scenes"))
            Directory.CreateDirectory("Assets/BharatKaSafar/Scenes");

        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // 1. Delete old generic terrain
            GameObject[] rootObjects = newScene.GetRootGameObjects();
            foreach (GameObject obj in rootObjects)
            {
                if (obj.name.Contains("Terrain") || obj.GetComponent("Terrain2DGenerator") != null || obj.name.Contains("LevelElements"))
                {
                    GameObject.DestroyImmediate(obj);
                }
            }

            // 2. Create New Terrain Root
            GameObject terrainRoot = new GameObject("TN_Madurai_Terrain");
            
            // Define the layout requested in Phase 6:
            // START -> Long Flat -> Small Hill -> Flat -> Small Dip (SlopeDown+SlopeUp) -> Flat -> Finish (Flat)
            string[] layout = {
                "TN_Terrain_Flat",
                "TN_Terrain_Flat_Long",
                "TN_Terrain_SmallHill",
                "TN_Terrain_Flat",
                "TN_Terrain_SmallSlopeDown",
                "TN_Terrain_SmallSlopeUp",
                "TN_Terrain_Flat_Long",
                "TN_Terrain_Flat"
            };

            Transform previousEnd = null;
            float currentX = 0f;

            for (int i = 0; i < layout.Length; i++)
            {
                string prefabPath = $"Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core/{layout[i]}.prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null)
                {
                    GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    piece.transform.SetParent(terrainRoot.transform);
                    
                    TN_TerrainModule module = piece.GetComponent<TN_TerrainModule>();
                    
                    if (previousEnd != null && module != null && module.startPoint != null)
                    {
                        Vector3 offset = previousEnd.position - module.startPoint.position;
                        piece.transform.position += offset;
                    }
                    else
                    {
                        piece.transform.position = new Vector3(currentX, 0, 0);
                    }

                    if (module != null && module.endPoint != null)
                    {
                        previousEnd = module.endPoint;
                        currentX = module.endPoint.position.x; // Fallback if next piece doesn't have a start point
                    }

                    // 3. Add Stage Triggers at specific intervals
                    if (i == 1) CreateStageTrigger(piece.transform, "STAGE 1: MARKET STREET", "Market kaafi busy hai! Speed control karo aur obstacles se bachkar niklo.");
                    if (i == 3) CreateStageTrigger(piece.transform, "STAGE 2: CLEAR THE ROUTE", "Raasta block hai! Horn use karo aur Safar Star collect karo!");
                    if (i == 6) CreateStageTrigger(piece.transform, "STAGE 3: TEMPLE AHEAD", "Meenakshi Temple dikh raha hai! Bas thoda aur aage!");
                }
            }

            // 4. Save and Add to Build Settings
            EditorSceneManager.SaveScene(newScene);
            AddSceneToBuildSettings(newScenePath);
            
            Debug.Log("[BKS Builder] Phase 6 Complete! BKS_TN_M01_Madurai created, generic terrain removed, modular Madurai layout generated, and Stage Triggers added!");
        }
        else
        {
            Debug.LogError("[BKS Builder] Failed to duplicate the template gameplay scene.");
        }
    }

    private static void CreateStageTrigger(Transform parent, string title, string dialogue)
    {
        GameObject triggerObj = new GameObject($"StageTrigger_{title.Split(':')[0].Replace(" ", "")}");
        triggerObj.transform.SetParent(parent);
        triggerObj.transform.localPosition = new Vector3(0, 2f, 0); // Put it slightly above ground

        BoxCollider2D box = triggerObj.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 10f); // Tall trigger to ensure car hits it

        TN_StageTrigger trigger = triggerObj.AddComponent<TN_StageTrigger>();
        trigger.stageTitle = title;
        trigger.stageDialogue = dialogue;
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        bool exists = false;
        foreach (var s in buildScenes)
        {
            if (s.path == scenePath) { exists = true; break; }
        }

        if (!exists)
        {
            EditorBuildSettingsScene[] newScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
            System.Array.Copy(buildScenes, newScenes, buildScenes.Length);
            newScenes[buildScenes.Length] = new EditorBuildSettingsScene(scenePath, true);
            EditorBuildSettings.scenes = newScenes;
        }
    }
}
