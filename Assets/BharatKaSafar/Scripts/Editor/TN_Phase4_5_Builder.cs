using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.UI;
using BharatKaSafar.Core;
using BharatKaSafar.Gameplay;
using System.IO;

public class TN_Phase4_5_Builder
{
    [MenuItem("Bharat Ka Safar/Automate Phase 4 & 5 (Build UI & Prefabs)")]
    public static void BuildPhase4And5()
    {
        CreateTerrainPrefabs();
        DuplicateAndSetupMissionSelectScene();
        AssetDatabase.Refresh();
        Debug.Log("[BKS Builder] Phase 4 & 5 Automation Complete! UI Scene and Terrain Prefabs generated.");
    }

    private static void CreateTerrainPrefabs()
    {
        string folderPath = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        string[] prefabsToMake = {
            "TN_Terrain_Flat",
            "TN_Terrain_Flat_Long",
            "TN_Terrain_SmallSlopeUp",
            "TN_Terrain_SmallSlopeDown",
            "TN_Terrain_SmallHill",
            "TN_Terrain_Bridge"
        };

        foreach (string pName in prefabsToMake)
        {
            string fullPath = $"{folderPath}/{pName}.prefab";
            if (File.Exists(fullPath)) continue;

            // Create root object
            GameObject root = new GameObject(pName);
            root.layer = LayerMask.NameToLayer("Ground"); // Assign to ground layer if it exists
            
            // Add physics and module script
            root.AddComponent<EdgeCollider2D>();
            TN_TerrainModule module = root.AddComponent<TN_TerrainModule>();

            // Create Start and End points
            GameObject startPt = new GameObject("StartPoint");
            startPt.transform.SetParent(root.transform);
            startPt.transform.localPosition = new Vector3(-5f, 0, 0);

            GameObject endPt = new GameObject("EndPoint");
            endPt.transform.SetParent(root.transform);
            endPt.transform.localPosition = new Vector3(5f, 0, 0);

            module.startPoint = startPt.transform;
            module.endPoint = endPt.transform;

            // Save as Prefab
            PrefabUtility.SaveAsPrefabAsset(root, fullPath);
            GameObject.DestroyImmediate(root);
        }
    }

    private static void DuplicateAndSetupMissionSelectScene()
    {
        string originalScenePath = "";
        
        // Find the UP Mission Select scene to duplicate
        string[] guids = AssetDatabase.FindAssets("BKS_UP_MissionSelect t:Scene");
        if (guids.Length > 0)
        {
            originalScenePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        }
        else
        {
            // Try fallback path
            originalScenePath = "Assets/BharatKaSafar/Scenes/BKS_UP_MissionSelect.unity"; 
            if (!File.Exists(originalScenePath)) originalScenePath = "Assets/Scenes/BKS_UP_MissionSelect.unity";
        }

        if (!File.Exists(originalScenePath))
        {
            Debug.LogError("[BKS Builder] Could not find BKS_UP_MissionSelect.unity. Please rename the scene manually.");
            return;
        }

        string newScenePath = originalScenePath.Replace("BKS_UP_MissionSelect", "BKS_TN_MissionSelect");

        if (AssetDatabase.CopyAsset(originalScenePath, newScenePath))
        {
            // Open the new scene to modify it
            Scene newScene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

            // Find the UI script and swap the chapter data
            BKS_UPMissionSelectUI uiScript = GameObject.FindObjectOfType<BKS_UPMissionSelectUI>(true);
            if (uiScript != null)
            {
                ChapterData tnData = AssetDatabase.LoadAssetAtPath<ChapterData>("Assets/BharatKaSafar/ScriptableObjects/Chapters/TN_ChapterData.asset");
                if (tnData != null)
                {
                    SerializedObject serializedObject = new SerializedObject(uiScript);
                    SerializedProperty chapterProp = serializedObject.FindProperty("chapterData");
                    if (chapterProp != null)
                    {
                        chapterProp.objectReferenceValue = tnData;
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("[BKS Builder] Successfully assigned TN_ChapterData to the UI script.");
                    }
                }
            }

            // Save the modified scene
            EditorSceneManager.SaveScene(newScene);

            // Add it to Build Settings
            AddSceneToBuildSettings(newScenePath);
        }
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
            Debug.Log($"[BKS Builder] Added {scenePath} to Build Settings.");
        }
    }
}
