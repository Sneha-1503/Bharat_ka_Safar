using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Makes every mission playable from Mission Select (and in the phone build):
    ///  • each MissionData whose draft scene exists gets that scene as its gameplay scene
    ///  • every Bharat Ka Safar scene (menus + all mission scenes, not _Backup) is added to Build Settings and switched on
    /// Scenes already in Build Settings (also the legacy ones) keep their order; new ones are added at the end.
    /// Menu: Bharat Ka Safar > Level > Make All Missions Playable
    /// </summary>
    public static class BKS_MakePlayableTool
    {
        [MenuItem("Bharat Ka Safar/Level/Make All Missions Playable")]
        public static void MakeAllPlayable()
        {
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).OrderBy(p => p).ToList();
            var byName = scenePaths.GroupBy(p => Path.GetFileNameWithoutExtension(p)).ToDictionary(g => g.Key, g => g.First());

            // 1. link missions to their scenes
            int linked = 0, missing = 0;
            foreach (var m in AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g))))
            {
                if (m == null || m.IsPlayable || string.IsNullOrEmpty(m.DraftSceneName)) continue;
                if (!byName.ContainsKey(m.DraftSceneName)) { missing++; Debug.LogWarning($"[Playable] {m.name}: scene '{m.DraftSceneName}' not found."); continue; }
                m.LinkDraftSceneForEditor();
                EditorUtility.SetDirty(m);
                linked++;
            }
            AssetDatabase.SaveAssets();

            // 2. Build Settings: keep existing entries, switch BKS ones on, add the rest
            var list = EditorBuildSettings.scenes.ToList();
            int added = 0, enabled = 0;
            foreach (var p in scenePaths)
            {
                int i = list.FindIndex(s => s.path == p);
                if (i < 0) { list.Add(new EditorBuildSettingsScene(p, true)); added++; }
                else if (!list[i].enabled) { list[i] = new EditorBuildSettingsScene(p, true); enabled++; }
            }
            EditorBuildSettings.scenes = list.ToArray();

            Debug.Log($"[Playable] {linked} missions linked to their scenes" + (missing > 0 ? $" ({missing} scenes missing)" : "") +
                      $". Build Settings: {added} scenes added, {enabled} switched on, {list.Count} total.");
        }
    }
}
