using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Parallax backgrounds for the 140 missions from Assets/BharatKaSafar/Art/Parallax (5 layers per place + catalog.json).
    ///  1. "Prepare Parallax Images": sprite import settings (no mipmaps, Android ASTC 8x8 / 10x10 for sky + far, max 1024)
    ///  2. "Apply Parallax ...": in each mission scene the old camera background is switched off (not deleted) and
    ///     "[BKS_Parallax]" is added under the Main Camera with the place's 5 layers (BKS_ParallaxBackground).
    /// A mission is matched to its place by state + place name (falls back to the mission number).
    /// </summary>
    public static class BKS_ParallaxTool
    {
        private const string Root = "Assets/BharatKaSafar/Art/Parallax";
        private const string Holder = "[BKS_Parallax]";
        // per layer (sky → foreground): how much it moves with the road, how much it follows height
        private static readonly float[] Scroll = { 0.02f, 0.12f, 0.28f, 0.45f, 0.65f };
        private static readonly float[] Vertical = { 0.02f, 0.06f, 0.12f, 0.2f, 0.3f };
        private static readonly int[] Order = { -60, -59, -58, -57, -56 };

        [System.Serializable] private class Entry { public int number; public string folder; public string[] layers; public string state; public string place; }
        [System.Serializable] private class Wrap { public Entry[] items; }

        [MenuItem("Bharat Ka Safar/Parallax/1. Prepare Parallax Images")]
        public static void Prepare()
        {
            var paths = Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories).Select(p => p.Replace('\\', '/')).ToList();
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < paths.Count; i++)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("Parallax images", paths[i], i / (float)paths.Count)) break;
                    if (!(AssetImporter.GetAtPath(paths[i]) is TextureImporter ti)) continue;
                    ti.textureType = TextureImporterType.Sprite;
                    ti.spriteImportMode = SpriteImportMode.Single;
                    ti.mipmapEnabled = false;
                    ti.alphaIsTransparency = true;
                    ti.wrapMode = TextureWrapMode.Clamp;
                    ti.maxTextureSize = 1024;
                    // build size: 1024 is plenty on a phone (most of each layer sits behind the terrain);
                    // sky + far layers (01-, 02-) are soft anyway → stronger compression
                    string file = System.IO.Path.GetFileName(paths[i]);
                    bool far = file.StartsWith("01-") || file.StartsWith("02-");
                    var a = ti.GetPlatformTextureSettings("Android");
                    a.overridden = true; a.maxTextureSize = 1024;
                    a.format = far ? TextureImporterFormat.ASTC_10x10 : TextureImporterFormat.ASTC_8x8; a.compressionQuality = 50;
                    ti.SetPlatformTextureSettings(a);
                    ti.SaveAndReimport();
                }
            }
            finally { AssetDatabase.StopAssetEditing(); EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Parallax] {paths.Count} layer images prepared.");
        }

        [MenuItem("Bharat Ka Safar/Parallax/2. Apply Parallax To Open Scene")]
        public static void ApplyOpen()
        {
            var scene = SceneManager.GetActiveScene();
            var m = Missions().FirstOrDefault(x => x.GameplaySceneName == scene.name || x.DraftSceneName == scene.name);
            if (m == null) { Debug.LogWarning($"[Parallax] No mission uses scene '{scene.name}'."); return; }
            var e = Match(m, Catalog());
            if (e == null || !Apply(scene, e)) { Debug.LogWarning($"[Parallax] {scene.name}: no parallax set found."); return; }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Parallax] {scene.name} ← {e.folder}");
        }

        [MenuItem("Bharat Ka Safar/Parallax/3. Apply Parallax To ALL Missions")]
        public static void ApplyAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var catalog = Catalog();
            var missionPaths = Missions().Select(AssetDatabase.GetAssetPath).ToList();
            var scenePaths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !p.Contains("_Backup")).GroupBy(Path.GetFileNameWithoutExtension).ToDictionary(g => g.Key, g => g.First());
            int done = 0; var problems = new List<string>();
            try
            {
                for (int i = 0; i < missionPaths.Count; i++)
                {
                    var m = AssetDatabase.LoadAssetAtPath<MissionData>(missionPaths[i]);
                    string sceneName = !string.IsNullOrEmpty(m.GameplaySceneName) ? m.GameplaySceneName : m.DraftSceneName;
                    var e = Match(m, catalog);
                    if (EditorUtility.DisplayCancelableProgressBar("Parallax", $"{m.StateName}: {m.PlaceName}", i / (float)missionPaths.Count)) break;
                    if (e == null) { problems.Add($"{m.name}: no parallax set"); continue; }
                    if (string.IsNullOrEmpty(sceneName) || !scenePaths.TryGetValue(sceneName, out var path)) { problems.Add($"{m.name}: scene not found"); continue; }
                    var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                    if (!Apply(scene, e)) { problems.Add($"{sceneName}: no Main Camera / layers"); continue; }
                    EditorSceneManager.SaveScene(scene);
                    done++;
                }
            }
            finally { EditorUtility.ClearProgressBar(); }
            Debug.Log($"[Parallax] Applied to {done}/{missionPaths.Count} missions." + (problems.Count > 0 ? "\n" + string.Join("\n", problems) : ""));
        }

        private static bool Apply(Scene scene, Entry e)
        {
            var cam = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"))
                   ?? scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if (cam == null) return false;

            // old background: the sprites under the camera (Sky, hills, clouds…) are switched off, not deleted
            foreach (Transform c in cam.transform.Cast<Transform>().ToList())
            {
                if (c.name == Holder) { Object.DestroyImmediate(c.gameObject); continue; }
                if (c.GetComponentInChildren<SpriteRenderer>(true) != null && c.gameObject.activeSelf) c.gameObject.SetActive(false);
            }

            var holder = new GameObject(Holder);
            holder.transform.SetParent(cam.transform, false);
            holder.transform.localPosition = new Vector3(0f, 0f, 30f); // in front of the camera, behind everything else by sorting order
            var bg = holder.AddComponent<BKS_ParallaxBackground>();
            var view = BKS_ParallaxBackground.ViewSize(cam, holder.transform.localPosition.z);
            for (int i = 0; i < e.layers.Length && i < 5; i++)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{Root}/{e.folder}/{e.layers[i]}");
                if (sprite == null) continue;
                var go = new GameObject($"L{i + 1}_{Path.GetFileNameWithoutExtension(e.layers[i])}");
                go.transform.SetParent(holder.transform, false);
                go.transform.localPosition = new Vector3(0f, 0f, -i * 0.01f);
                float s = BKS_ParallaxBackground.FitScale(sprite, view, bg.cover);
                go.transform.localScale = new Vector3(s, s, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite; sr.sortingOrder = Order[i];
                bg.layers.Add(new BKS_ParallaxBackground.Layer { renderer = sr, scroll = Scroll[i], vertical = Vertical[i] });
            }
            EditorUtility.SetDirty(bg);
            return bg.layers.Count > 0;
        }

        private static List<Entry> Catalog()
        {
            string json = File.ReadAllText($"{Root}/catalog.json");
            return JsonUtility.FromJson<Wrap>("{\"items\":" + json + "}").items.ToList();
        }

        // same state; the place name decides, else the n-th place of that state
        private static Entry Match(MissionData m, List<Entry> catalog)
        {
            var list = catalog.Where(c => c.state == m.StateName).OrderBy(c => c.number).ToList();
            if (list.Count == 0) return null;
            string key = (m.PlaceName ?? "").ToLowerInvariant().Split(' ', '-', ',').FirstOrDefault(w => w.Length >= 4) ?? "";
            var hit = key.Length > 0 ? list.FirstOrDefault(c => (c.place + " " + c.folder).ToLowerInvariant().Contains(key)) : null;
            int k = Mathf.Clamp(m.MissionNumber - 1, 0, list.Count - 1);
            return hit ?? list[k];
        }

        private static List<MissionData> Missions() =>
            AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(m => m != null && !string.IsNullOrEmpty(m.StateName)).ToList();
    }
}
