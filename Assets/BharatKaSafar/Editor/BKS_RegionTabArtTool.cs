using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Home;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Region tab art on the State Map (All India / North / West / South / East / North-East):
    /// blue framed button for the normal tabs, gold one for the selected tab (Art/UI/StateMap/RegionTab_Normal / _Selected,
    /// 9-sliced so they fit any tab width). Text: gold on blue, dark navy on gold. Only the tabs change.
    /// Menu: Bharat Ka Safar > Home > Region Tab Art (State Map)
    /// </summary>
    public static class BKS_RegionTabArtTool
    {
        private const string Scene = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private const string Art = "Assets/BharatKaSafar/Art/UI/StateMap/";

        [MenuItem("Bharat Ka Safar/Home/Region Tab Art (State Map)")]
        public static void Apply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var normal = Prepare("RegionTab_Normal.png", new Vector4(60f, 48f, 60f, 48f));
            var selected = Prepare("RegionTab_Selected.png", new Vector4(66f, 48f, 66f, 48f));
            if (normal == null || selected == null) { Debug.LogError("[RegionTabs] Tab art missing in " + Art); return; }

            var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
            var tabs = Object.FindFirstObjectByType<BKS_MapRegionTabs>(FindObjectsInactive.Include);
            if (tabs == null) { Debug.LogError("[RegionTabs] BKS_MapRegionTabs not found in the State Map."); return; }

            var so = new SerializedObject(tabs);
            so.FindProperty("tabNormalSprite").objectReferenceValue = normal;
            so.FindProperty("tabSelectedSprite").objectReferenceValue = selected;
            so.FindProperty("tabTextNormal").colorValue = new Color(1f, 0.86f, 0.45f);
            so.FindProperty("tabTextSelected").colorValue = new Color(0.12f, 0.12f, 0.35f);
            var regions = so.FindProperty("regions");
            for (int i = 0; i < regions.arraySize; i++)
            {
                var tab = regions.GetArrayElementAtIndex(i).FindPropertyRelative("tab").objectReferenceValue as Button;
                var img = tab != null ? tab.targetGraphic as Image : null;
                if (img == null) continue;
                Undo.RecordObject(img, "Region Tab Art");
                img.sprite = i == 0 ? selected : normal; img.type = Image.Type.Sliced; img.color = Color.white;
                img.pixelsPerUnitMultiplier = 2.4f; // frame ~20 units thick on the tab
                var tr = (RectTransform)tab.transform;
                if (tr.rect.height < 56f) { Undo.RecordObject(tr, "Region Tab Art"); tr.sizeDelta = new Vector2(tr.sizeDelta.x, tr.sizeDelta.y + (56f - tr.rect.height)); }
            }
            so.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RegionTabs] Tab art set (blue = normal, gold = selected).");
        }

        private static Sprite Prepare(string file, Vector4 border)
        {
            string path = Art + file;
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite || imp.spriteBorder != border)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.spriteBorder = border;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
