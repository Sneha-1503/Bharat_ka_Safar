using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using BharatKaSafar.Core;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;

/// <summary>
/// Level expansion tool: builds a NEW mission (M06, M07, ...) for any chapter.
///  1. Copies a template mission scene
///  2. Removes its old terrain and lays out a new terrain from modular TN_TerrainModule prefabs
///  3. Moves the destination / finish to the end of the new track
///  4. Creates the MissionData asset, appends it to the ChapterData, links nextMissionID
///  5. Adds the scene to Build Settings
/// Open via: Bharat Ka Safar > Level Expansion > Mission Builder
/// </summary>
public class BKS_MissionExpansionBuilder : EditorWindow
{
    private const string TerrainPrefabFolder = "Assets/BharatKaSafar/Prefabs/Terrain/TamilNadu/Core";

    [System.Serializable]
    private class Segment
    {
        public GameObject prefab;
        public string stageTitle = "";
        public string stageDialogue = "";
    }

    // Mission
    private ChapterData chapter;
    private SceneAsset templateScene;
    private string missionID = "TN_M06";
    private string missionName = "New Safar";
    private string sceneName = "BKS_TN_M06_NewMission";
    private string storyText = "";
    private string objectiveText = "Reach the destination";
    private int coinReward = 1200;
    private int maxStars = 15;
    private string missionAssetFolder = "Assets/BharatKaSafar/ScriptableObjects/missions/TamilNadu";

    // Terrain
    private string terrainRootName = "Expansion_Terrain";
    private List<Segment> segments = new List<Segment>();
    private Vector2 scroll;

    [MenuItem("Bharat Ka Safar/Level Expansion/Mission Builder")]
    public static void Open() => GetWindow<BKS_MissionExpansionBuilder>("Mission Builder");

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("1. Mission", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        chapter = (ChapterData)EditorGUILayout.ObjectField("Chapter", chapter, typeof(ChapterData), false);
        if (EditorGUI.EndChangeCheck() && chapter != null) SuggestFromChapter();

        templateScene = (SceneAsset)EditorGUILayout.ObjectField(
            new GUIContent("Template Scene", "Existing mission scene to copy (HUD, car, camera, background)."),
            templateScene, typeof(SceneAsset), false);
        missionID = EditorGUILayout.TextField("Mission ID", missionID);
        missionName = EditorGUILayout.TextField("Mission Name", missionName);
        sceneName = EditorGUILayout.TextField("Scene Name", sceneName);
        objectiveText = EditorGUILayout.TextField("Objective", objectiveText);
        EditorGUILayout.LabelField("Story");
        storyText = EditorGUILayout.TextArea(storyText, GUILayout.MinHeight(40));
        coinReward = EditorGUILayout.IntField("Coin Reward", coinReward);
        maxStars = EditorGUILayout.IntField("Max Safar Stars", maxStars);
        missionAssetFolder = EditorGUILayout.TextField("MissionData Folder", missionAssetFolder);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("2. Terrain Layout", EditorStyles.boldLabel);
        terrainRootName = EditorGUILayout.TextField("Terrain Root Name", terrainRootName);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Preset: Easy")) ApplyPreset(Easy);
        if (GUILayout.Button("Preset: Medium")) ApplyPreset(Medium);
        if (GUILayout.Button("Preset: Hard")) ApplyPreset(Hard);
        if (GUILayout.Button("Random")) ApplyPreset(RandomLayout());
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            s.prefab = (GameObject)EditorGUILayout.ObjectField($"#{i + 1}", s.prefab, typeof(GameObject), false);
            if (GUILayout.Button("▲", GUILayout.Width(22)) && i > 0) { segments.Reverse(i - 1, 2); }
            if (GUILayout.Button("▼", GUILayout.Width(22)) && i < segments.Count - 1) { segments.Reverse(i, 2); }
            if (GUILayout.Button("X", GUILayout.Width(22))) { segments.RemoveAt(i); EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
            EditorGUILayout.EndHorizontal();
            s.stageTitle = EditorGUILayout.TextField("Stage Title (optional)", s.stageTitle);
            if (!string.IsNullOrEmpty(s.stageTitle))
                s.stageDialogue = EditorGUILayout.TextField("Stage Dialogue", s.stageDialogue);
            EditorGUILayout.EndVertical();
        }
        if (GUILayout.Button("+ Add Segment")) segments.Add(new Segment());

        EditorGUILayout.Space();
        GUI.enabled = chapter != null && templateScene != null && segments.Count > 0;
        if (GUILayout.Button("BUILD MISSION", GUILayout.Height(32))) Build();
        GUI.enabled = true;

        EditorGUILayout.EndScrollView();
    }

    // ── Presets ──────────────────────────────────────────────────────────────
    private static readonly string[] Easy =
        { "Flat", "SmallSlopeUp", "SmallHill", "SmallSlopeDown", "Flat_Long", "Valley", "Flat", "SmallHill", "Flat_Long", "Flat" };
    private static readonly string[] Medium =
        { "Flat", "SmallHill", "Flat_Long", "Valley", "Bridge", "Jump", "Landing", "Downhill", "RockyClimb", "LargeHill", "Flat_Long", "Flat" };
    private static readonly string[] Hard =
        { "Flat", "SteepClimb", "Downhill", "Jump", "Landing", "RockyClimb", "LargeHill", "Valley", "Bridge", "SteepClimb", "Jump", "Landing", "LargeHill", "Downhill", "Flat_Long", "Flat" };

    private static string[] RandomLayout()
    {
        string[] pool = { "SmallHill", "Flat_Long", "Valley", "Bridge", "Downhill", "RockyClimb", "LargeHill", "SteepClimb", "SmallSlopeUp", "SmallSlopeDown", "Jump" };
        var list = new List<string> { "Flat" };
        for (int i = 0; i < 12; i++)
        {
            string p = pool[Random.Range(0, pool.Length)];
            list.Add(p);
            if (p == "Jump") list.Add("Landing");
        }
        list.Add("Flat_Long");
        list.Add("Flat");
        return list.ToArray();
    }

    private void ApplyPreset(string[] names)
    {
        segments.Clear();
        foreach (var n in names)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{TerrainPrefabFolder}/TN_Terrain_{n}.prefab");
            if (prefab == null) Debug.LogWarning($"[Mission Builder] Missing terrain prefab TN_Terrain_{n}");
            segments.Add(new Segment { prefab = prefab });
        }
        // Default stage triggers at roughly 1/4, 1/2, 3/4 and near the end
        int c = segments.Count;
        segments[Mathf.Min(1, c - 1)].stageTitle = "STAGE 1";
        segments[c / 2].stageTitle = "STAGE 2";
        segments[c * 3 / 4].stageTitle = "STAGE 3";
        segments[Mathf.Max(0, c - 2)].stageTitle = "FINAL EVENT: DESTINATION SPOTTED";
    }

    private void SuggestFromChapter()
    {
        int next = chapter.MissionCount + 1;
        missionID = $"{chapter.ChapterID}_M{next:00}";
        sceneName = $"BKS_{chapter.ChapterID}_M{next:00}_NewMission";
        var last = chapter.GetMission(chapter.MissionCount - 1);
        if (last != null)
        {
            coinReward = last.CoinReward + 200;
            maxStars = last.MaxSafarStars;
            string lastAssetPath = AssetDatabase.GetAssetPath(last);
            if (!string.IsNullOrEmpty(lastAssetPath)) missionAssetFolder = Path.GetDirectoryName(lastAssetPath).Replace('\\', '/');
            string[] g = AssetDatabase.FindAssets($"{last.GameplaySceneName} t:Scene");
            if (g.Length > 0) templateScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(g[0]));
        }
    }

    // ── Build ────────────────────────────────────────────────────────────────
    private void Build()
    {
        if (chapter.GetMissionByID(missionID) != null &&
            !EditorUtility.DisplayDialog("Mission exists", $"{missionID} is already in this chapter. Rebuild its scene?", "Rebuild", "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string templatePath = AssetDatabase.GetAssetPath(templateScene);
        string newScenePath = $"{Path.GetDirectoryName(templatePath).Replace('\\', '/')}/{sceneName}.unity";

        if (File.Exists(newScenePath))
        {
            if (!EditorUtility.DisplayDialog("Scene exists", $"{newScenePath} already exists. Overwrite it?", "Overwrite", "Cancel")) return;
            AssetDatabase.DeleteAsset(newScenePath);
        }
        if (!AssetDatabase.CopyAsset(templatePath, newScenePath))
        {
            Debug.LogError($"[Mission Builder] Could not copy {templatePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(newScenePath, OpenSceneMode.Single);

        RemoveOldTerrain();
        Vector3 trackEnd = BuildTerrain(out Vector3 trackStart);
        MoveDestination(trackEnd);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddSceneToBuildSettings(newScenePath);

        CreateMissionData();

        Debug.Log($"[Mission Builder] Built {missionID} → {newScenePath}. Track length: {trackEnd.x - trackStart.x:0} units.");
    }

    private static void RemoveOldTerrain()
    {
        // Delete every root that holds modular terrain pieces from the template
        var roots = Object.FindObjectsByType<TN_TerrainModule>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Select(m => m.transform.root.gameObject)
            .Distinct()
            .ToList();
        foreach (var r in roots) Object.DestroyImmediate(r);

        if (roots.Count == 0)
            Debug.LogWarning("[Mission Builder] Template scene has no TN_TerrainModule terrain. Delete its old ground manually if it overlaps the new track.");
    }

    private Vector3 BuildTerrain(out Vector3 start)
    {
        var root = new GameObject(terrainRootName);
        Transform previousEnd = null;
        start = Vector3.zero;
        Vector3 end = Vector3.zero;

        for (int i = 0; i < segments.Count; i++)
        {
            var s = segments[i];
            if (s.prefab == null) continue;

            var piece = (GameObject)PrefabUtility.InstantiatePrefab(s.prefab, root.transform);
            var module = piece.GetComponent<TN_TerrainModule>();

            if (previousEnd != null && module != null && module.startPoint != null)
                piece.transform.position += previousEnd.position - module.startPoint.position;
            else
                piece.transform.position = new Vector3(end.x, 0f, 0f);

            if (i == 0 && module != null && module.startPoint != null) start = module.startPoint.position;

            if (module != null && module.endPoint != null)
            {
                previousEnd = module.endPoint;
                end = module.endPoint.position;
            }

            if (!string.IsNullOrEmpty(s.stageTitle)) CreateStageTrigger(piece.transform, s.stageTitle, s.stageDialogue);
        }
        return end;
    }

    private static void MoveDestination(Vector3 trackEnd)
    {
        var dest = Object.FindFirstObjectByType<BKS_Destination>(FindObjectsInactive.Include);
        if (dest == null)
        {
            Debug.LogWarning("[Mission Builder] No BKS_Destination in template scene. Add a finish manually.");
            return;
        }
        // Place a little before the very end so the car hits it on flat ground
        dest.transform.position = new Vector3(trackEnd.x - 5f, trackEnd.y + 2f, dest.transform.position.z);
    }

    private static void CreateStageTrigger(Transform parent, string title, string dialogue)
    {
        var go = new GameObject($"StageTrigger_{title.Split(':')[0].Replace(" ", "")}");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0, 2f, 0);

        var box = go.AddComponent<BoxCollider2D>();
        box.isTrigger = true;
        box.size = new Vector2(2f, 15f);

        var trigger = go.AddComponent<TN_StageTrigger>();
        trigger.stageTitle = title;
        trigger.stageDialogue = dialogue;
    }

    private void CreateMissionData()
    {
        if (!AssetDatabase.IsValidFolder(missionAssetFolder)) Directory.CreateDirectory(missionAssetFolder);
        AssetDatabase.Refresh();

        string path = $"{missionAssetFolder}/{missionID}.asset";
        var mission = AssetDatabase.LoadAssetAtPath<MissionData>(path);
        if (mission == null)
        {
            mission = ScriptableObject.CreateInstance<MissionData>();
            AssetDatabase.CreateAsset(mission, path);
        }

        int number = chapter.GetMissionByID(missionID) is MissionData existing
            ? existing.MissionNumber
            : chapter.MissionCount + 1;

        mission.InitForEditor(missionID, chapter.ChapterID, number, missionName, storyText, sceneName,
            objectiveText, coinReward, maxStars, 0.6f, 0.8f, 0.5f, "", 80 + number * 10);
        EditorUtility.SetDirty(mission);

        // Append to chapter and link the previous mission to unlock this one
        var so = new SerializedObject(chapter);
        var list = so.FindProperty("missions");
        bool alreadyListed = false;
        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == mission) alreadyListed = true;

        if (!alreadyListed)
        {
            if (list.arraySize > 0 && list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue is MissionData prev)
            {
                var prevSo = new SerializedObject(prev);
                prevSo.FindProperty("nextMissionID").stringValue = missionID;
                prevSo.ApplyModifiedPropertiesWithoutUndo();
            }
            list.arraySize++;
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = mission;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        AssetDatabase.SaveAssets();
    }

    private static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == scenePath)) return;
        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
