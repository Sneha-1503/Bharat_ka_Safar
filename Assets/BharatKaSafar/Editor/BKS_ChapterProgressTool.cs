using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Home;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Adds ONLY the chapter progress to the State Map chapter panel: "Missions 2 / 5" + a gold bar + "Stars 9 / 15"
    /// (filled by ChapterDetailPanel). Nothing else in the scene is changed. Move "ChapterProgress" in the Canvas as you like.
    /// Menu: Bharat Ka Safar > Home > Add Missions + Stars To Chapter Panel
    /// </summary>
    public static class BKS_ChapterProgressTool
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/Home/BKS_StateMap.unity";
        private static readonly Color Gold = new Color(1f, 0.82f, 0.35f);

        [MenuItem("Bharat Ka Safar/Home/Add Missions + Stars To Chapter Panel")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var panel = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ChapterDetailPanel>(true)).FirstOrDefault();
            if (panel == null) { Debug.LogError("[Chapter Progress] No ChapterDetailPanel in BKS_StateMap."); return; }
            var prt = (RectTransform)panel.transform;
            var font = panel.GetComponentsInChildren<TMP_Text>(true).Select(t => t.font).FirstOrDefault(f => f != null);

            // between the description and the status strip
            var block = Rect(prt, "ChapterProgress", new Vector2(0f, 22f), new Vector2(560f, 80f));
            var progress = Text(block, "ProgressText", "Missions  0 / 5", new Vector2(-140f, 20f), new Vector2(280f, 36f), Color.white, font);
            progress.alignment = TextAlignmentOptions.Left;
            var stars = Text(block, "StarsText", "Stars  0 / 15", new Vector2(140f, 20f), new Vector2(280f, 36f), Gold, font);
            stars.alignment = TextAlignmentOptions.Right;
            var bg = Img(block, "ProgressBar", new Vector2(0f, -18f), new Vector2(540f, 22f));
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            var fill = Img(bg.rectTransform, "Fill", Vector2.zero, Vector2.zero);
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one; fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            fill.color = Gold; fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillAmount = 0.4f;

            var so = new SerializedObject(panel);
            so.FindProperty("progressText").objectReferenceValue = progress;
            so.FindProperty("starsText").objectReferenceValue = stars;
            so.FindProperty("progressFill").objectReferenceValue = fill;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = block.gameObject;
            Debug.Log("[Chapter Progress] Missions + Stars added to the chapter panel (only that). Move 'ChapterProgress' if needed.");
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var t = parent.Find(name) as RectTransform;
            if (t == null) { t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); t.SetParent(parent, false); }
            t.anchorMin = t.anchorMax = new Vector2(0.5f, 0.5f); t.anchoredPosition = pos; t.sizeDelta = size;
            return t;
        }

        private static Image Img(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var t = Rect(parent, name, pos, size);
            var img = t.GetComponent<Image>(); if (img == null) img = t.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            if (img.sprite == null) img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return img;
        }

        private static TextMeshProUGUI Text(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color color, TMP_FontAsset font)
        {
            var t = Rect(parent, name, pos, size);
            var tmp = t.GetComponent<TextMeshProUGUI>(); if (tmp == null) tmp = t.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text; tmp.color = color; tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true; tmp.fontSizeMin = 14f; tmp.fontSizeMax = 30f; tmp.raycastTarget = false;
            return tmp;
        }
    }
}
