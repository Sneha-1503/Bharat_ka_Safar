using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Diary;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Safar Collection (BKS_SafarDiary) in the travel-journal look:
    ///  • state list on the LEFT page of the book, memory tokens on the RIGHT page (positions are fractions of the
    ///    screen read from the background art; move them in the Canvas if needed)
    ///  • no white boxes: panel / token backgrounds transparent
    ///  • state strips = parchment tickets with an ink border, selected = gold
    ///  • state title on the right page in dark ink; token names in ink
    /// The token grid size is fitted at runtime (BKS_DiaryScreen) so all tokens of a state fit on the page.
    /// Menu: Bharat Ka Safar > UI > Style Safar Collection
    /// </summary>
    public static class BKS_DiaryStyler
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_SafarDiary.unity";
        // book pages on the background (anchor min/max as screen fractions)
        private static readonly Vector2 LeftMin = new Vector2(0.115f, 0.15f), LeftMax = new Vector2(0.465f, 0.80f);
        private static readonly Vector2 RightMin = new Vector2(0.53f, 0.15f), RightMax = new Vector2(0.885f, 0.80f);
        private static readonly Color Ink = new Color(0.36f, 0.21f, 0.08f);

        [MenuItem("Bharat Ka Safar/UI/Style Safar Collection")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_DiaryScreen>(true)).FirstOrDefault();
            if (screen == null) { Debug.LogError("[Diary] No BKS_DiaryScreen in BKS_SafarDiary."); return; }
            var so = new SerializedObject(screen);
            var chapterList = so.FindProperty("chapterList").objectReferenceValue as RectTransform;
            var tokenGrid = so.FindProperty("tokenGrid").objectReferenceValue as RectTransform;
            var chapterTemplate = so.FindProperty("chapterTemplate").objectReferenceValue as Component;
            var tokenTemplate = so.FindProperty("tokenTemplate").objectReferenceValue as Component;
            var chapterTitle = so.FindProperty("chapterTitle").objectReferenceValue as TMP_Text;
            var canvas = screen.GetComponentInParent<Canvas>().rootCanvas.transform;
            var round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            // panels on the pages
            var statesPanel = TopPanel(chapterList, canvas);
            var tokensPanel = TopPanel(tokenGrid, canvas);
            if (statesPanel != null) { Fit(statesPanel, LeftMin, LeftMax); Clear(statesPanel); }
            if (tokensPanel != null) { Fit(tokensPanel, RightMin, RightMax); Clear(tokensPanel); }
            var scroll = chapterList != null ? chapterList.GetComponentInParent<ScrollRect>(true) : null;
            if (scroll != null && scroll.viewport != null)
            {
                var vimg = scroll.viewport.GetComponent<Image>(); if (vimg != null) vimg.color = new Color(1f, 1f, 1f, 0.01f); // still masks
            }

            // state list spacing
            var vlg = chapterList != null ? chapterList.GetComponent<VerticalLayoutGroup>() : null;
            if (vlg != null)
            {
                vlg.padding = new RectOffset(12, 12, 10, 10); vlg.spacing = 8f;
                vlg.childControlWidth = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
            }

            // state strip
            if (chapterTemplate != null)
            {
                var rt = (RectTransform)chapterTemplate.transform;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 74f);
                var img = rt.GetComponent<Image>(); if (img != null) { img.sprite = round; img.type = Image.Type.Sliced; img.color = Color.white; }
                var ol = rt.GetComponent<Outline>(); if (ol == null) ol = rt.gameObject.AddComponent<Outline>();
                ol.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.8f); ol.effectDistance = new Vector2(2.5f, -2.5f);
                foreach (var t in rt.GetComponentsInChildren<TMP_Text>(true)) { t.color = Ink; t.enableAutoSizing = true; t.fontSizeMin = 14; t.fontSizeMax = 34; }
            }
            so.FindProperty("chapterOn").colorValue = new Color(1f, 0.78f, 0.30f);
            so.FindProperty("chapterOff").colorValue = new Color(0.97f, 0.90f, 0.71f);

            // state title on the right page
            if (chapterTitle != null)
            {
                chapterTitle.color = Ink; chapterTitle.fontStyle = FontStyles.Bold;
                chapterTitle.enableAutoSizing = true; chapterTitle.fontSizeMin = 18; chapterTitle.fontSizeMax = 52;
            }

            // tokens: no white box, ink names; the grid sits under the title and fills the rest of the page
            if (tokenGrid != null)
            {
                tokenGrid.anchorMin = new Vector2(0.02f, 0.02f); tokenGrid.anchorMax = new Vector2(0.98f, 0.86f);
                tokenGrid.offsetMin = tokenGrid.offsetMax = Vector2.zero;
                var g = tokenGrid.GetComponent<GridLayoutGroup>(); if (g != null) { g.padding = new RectOffset(6, 6, 6, 6); g.childAlignment = TextAnchor.MiddleCenter; }
            }
            if (tokenTemplate != null)
            {
                var timg = tokenTemplate.GetComponent<Image>(); if (timg != null) timg.color = new Color(1f, 1f, 1f, 0f);
                foreach (var t in tokenTemplate.GetComponentsInChildren<TMP_Text>(true)) { t.color = Ink; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = 26; }
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Diary] Safar Collection styled: states on the left page, tokens on the right page. Adjust StatesPanel / TokensPanel in the Canvas if the pages differ.");
        }

        // the panel that is a direct child of the canvas and contains t
        private static RectTransform TopPanel(Transform t, Transform canvas)
        {
            if (t == null) return null;
            while (t.parent != null && t.parent != canvas) t = t.parent;
            return t as RectTransform;
        }

        private static void Fit(RectTransform r, Vector2 min, Vector2 max)
        {
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private static void Clear(RectTransform panel)
        {
            var img = panel.GetComponent<Image>(); if (img != null) img.color = new Color(1f, 1f, 1f, 0f);
        }
    }
}
