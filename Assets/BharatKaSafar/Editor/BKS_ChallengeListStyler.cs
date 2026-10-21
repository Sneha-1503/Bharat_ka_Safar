using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Challenges;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Restyles the Challenges list (BKS_Challenges scene) to fit the travel-journal book:
    /// each challenge is a warm parchment "ticket" strip with a brown ink border —
    ///   [ gold badge ] Title ...................  [ reward ]  [ CLAIM ]
    ///                  [==== progress  12 / 30 ====]
    /// Rows are 118 high, 10 apart, with padding inside the page. The CLAIM button turns green when ready,
    /// brown while in progress and grey when done (BKS_ChallengeRowView). Everything stays editable in the Canvas.
    /// Menu: Bharat Ka Safar > UI > Style Challenge List
    /// </summary>
    public static class BKS_ChallengeListStyler
    {
        private const string ScenePath = "Assets/BharatKaSafar/Scenes/BKS_Challenges.unity";
        private static readonly Color Parchment = new Color(0.98f, 0.91f, 0.72f);
        private static readonly Color Ink = new Color(0.36f, 0.21f, 0.08f);
        private static readonly Color Gold = new Color(0.93f, 0.68f, 0.18f);
        private static readonly Color Teal = new Color(0.10f, 0.52f, 0.55f);

        [MenuItem("Bharat Ka Safar/UI/Style Challenge List")]
        public static void Run()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var rowView = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<BKS_ChallengeRowView>(true)).FirstOrDefault();
            if (rowView == null) { Debug.LogError("[Challenges] No RowTemplate (BKS_ChallengeRowView) in BKS_Challenges."); return; }
            var round = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            var knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var row = (RectTransform)rowView.transform;
            var font = row.GetComponentsInChildren<TMP_Text>(true).Select(t => t.font).FirstOrDefault(f => f != null);

            // list: padding inside the page, rows close together, full width
            var content = row.parent as RectTransform;
            var vlg = content != null ? content.GetComponent<VerticalLayoutGroup>() : null;
            if (vlg != null)
            {
                vlg.padding = new RectOffset(14, 14, 12, 12);
                vlg.spacing = 10f;
                vlg.childControlWidth = true; vlg.childForceExpandWidth = true;
                vlg.childControlHeight = false; vlg.childForceExpandHeight = false;
                vlg.childAlignment = TextAnchor.UpperCenter;
            }

            // the strip
            row.sizeDelta = new Vector2(row.sizeDelta.x, 118f);
            var bg = row.GetComponent<Image>();
            if (bg == null) bg = row.gameObject.AddComponent<Image>();
            bg.sprite = round; bg.type = Image.Type.Sliced; bg.color = Parchment;
            var outline = Comp<Outline>(row);
            outline.effectColor = new Color(Ink.r, Ink.g, Ink.b, 0.85f); outline.effectDistance = new Vector2(3f, -3f);

            // star badge on the left (the game's star art)
            var badge = Child(row, "Badge");
            Place(badge, new Vector2(0.015f, 0.12f), new Vector2(0.10f, 0.88f));
            var bImg = Comp<Image>(badge);
            bImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/BharatKaSafar/BKS sprite/home/star.png") ?? knob;
            bImg.color = Color.white; bImg.preserveAspect = true; bImg.raycastTarget = false;
            var oldStar = badge.Find("Star"); if (oldStar != null) Object.DestroyImmediate(oldStar.gameObject);
            badge.SetAsFirstSibling();

            // title
            var title = row.Find("Title") as RectTransform;
            if (title != null)
            {
                Place(title, new Vector2(0.12f, 0.50f), new Vector2(0.68f, 0.94f));
                var t = title.GetComponent<TMP_Text>();
                t.color = Ink; t.fontStyle = FontStyles.Bold; t.alignment = TextAlignmentOptions.MidlineLeft;
                t.enableAutoSizing = true; t.fontSizeMin = 16; t.fontSizeMax = 40;
            }

            // progress bar under the title
            var bar = row.Find("ProgressBar") as RectTransform;
            if (bar != null)
            {
                Place(bar, new Vector2(0.12f, 0.14f), new Vector2(0.68f, 0.42f));
                var barImg = bar.GetComponent<Image>(); if (barImg != null) { barImg.sprite = round; barImg.type = Image.Type.Sliced; barImg.color = new Color(Ink.r, Ink.g, Ink.b, 0.28f); }
                var fill = bar.Find("Fill") as RectTransform;
                if (fill != null)
                {
                    Place(fill, Vector2.zero, Vector2.one);
                    var f = fill.GetComponent<Image>();
                    if (f != null) { f.sprite = round; f.type = Image.Type.Filled; f.fillMethod = Image.FillMethod.Horizontal; f.color = Teal; }
                }
                var prog = bar.Find("Progress") as RectTransform;
                if (prog != null)
                {
                    Place(prog, Vector2.zero, Vector2.one);
                    var p = prog.GetComponent<TMP_Text>();
                    p.color = Color.white; p.fontStyle = FontStyles.Bold; p.alignment = TextAlignmentOptions.Center;
                    p.enableAutoSizing = true; p.fontSizeMin = 12; p.fontSizeMax = 26;
                    p.outlineWidth = 0.2f; p.outlineColor = new Color32(60, 35, 10, 255);
                    prog.SetAsLastSibling();
                }
            }

            // reward
            var reward = row.Find("Reward") as RectTransform;
            if (reward != null)
            {
                Place(reward, new Vector2(0.69f, 0.14f), new Vector2(0.82f, 0.86f));
                var r = reward.GetComponent<TMP_Text>();
                r.color = new Color(0.62f, 0.38f, 0.05f); r.fontStyle = FontStyles.Bold; r.alignment = TextAlignmentOptions.Center;
                r.enableAutoSizing = true; r.fontSizeMin = 12; r.fontSizeMax = 28;
            }

            // claim button
            var claim = row.Find("ClaimButton") as RectTransform;
            if (claim != null)
            {
                Place(claim, new Vector2(0.835f, 0.20f), new Vector2(0.985f, 0.80f));
                var c = claim.GetComponent<Image>(); if (c != null) { c.sprite = round; c.type = Image.Type.Sliced; c.color = Color.white; }
                var label = claim.Find("Label") as RectTransform;
                if (label != null)
                {
                    Place(label, Vector2.zero, Vector2.one);
                    var l = label.GetComponent<TMP_Text>();
                    l.color = Color.white; l.fontStyle = FontStyles.Bold; l.alignment = TextAlignmentOptions.Center;
                    l.enableAutoSizing = true; l.fontSizeMin = 10; l.fontSizeMax = 26;
                }
            }

            // row colours: parchment normally, warm glow when ready, faded when done
            var so = new SerializedObject(rowView);
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("normalColor").colorValue = Parchment;
            so.FindProperty("readyColor").colorValue = new Color(1f, 0.86f, 0.52f);
            so.FindProperty("claimedColor").colorValue = new Color(0.86f, 0.82f, 0.74f);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = row.gameObject;
            Debug.Log("[Challenges] Challenge list styled (journal ticket strips). Tweak RowTemplate in the Canvas as you like.");
        }

        private static RectTransform Child(Transform parent, string name)
        {
            var t = parent.Find(name) as RectTransform;
            if (t == null) { t = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); t.SetParent(parent, false); }
            return t;
        }

        private static T Comp<T>(Component c) where T : Component
        {
            var x = c.GetComponent<T>();
            return x != null ? x : c.gameObject.AddComponent<T>();
        }

        private static void Place(RectTransform t, Vector2 min, Vector2 max)
        {
            t.anchorMin = min; t.anchorMax = max; t.offsetMin = t.offsetMax = Vector2.zero; t.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
