using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// TealTravel look for the two gameplay popups (menus and the settings popup are not touched):
    ///  • Mission Result (Resources/BKS_MissionResultPopup and the BKS_MissionResult scene, so both stay the same):
    ///    parchment frame, India Map / Retry / Continue sprite buttons, Electronic Highway Sign font
    ///  • Revive: builds Resources/BKS_RevivePopup (parchment frame, REVIVE / NO THANKS buttons) — every mission uses it
    /// Menu: Bharat Ka Safar > UI > TealTravel: Result + Revive Popups
    /// </summary>
    public static class BKS_TealPopupsBuilder
    {
        private const string Art = "Assets/BharatKaSafar/Art/UI/TealTravelSprites/Engraved/";
        private const string ResultPrefab = "Assets/Resources/BKS_MissionResultPopup.prefab";
        private const string ResultScene = "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity";
        private const string RevivePrefab = "Assets/Resources/BKS_RevivePopup.prefab";
        private const string FontPath = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Electronic Highway Sign SDF.asset";

        [MenuItem("Bharat Ka Safar/UI/TealTravel: Result + Revive Popups")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            // result popup prefab
            var root = PrefabUtility.LoadPrefabContents(ResultPrefab);
            try { StyleResult(root.transform); PrefabUtility.SaveAsPrefabAsset(root, ResultPrefab); }
            finally { PrefabUtility.UnloadPrefabContents(root); }

            // result scene (same design, so "Copy Result Scene Design" keeps it)
            var scene = EditorSceneManager.OpenScene(ResultScene, OpenSceneMode.Additive);
            try
            {
                var canvas = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "MissionResultCanvas");
                if (canvas != null) { StyleResult(canvas.transform); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
            }
            finally { EditorSceneManager.CloseScene(scene, true); }

            BuildRevive();
            AssetDatabase.SaveAssets();
            Debug.Log("[TealTravel] Mission Result + Revive popups restyled. Every mission uses them.");
        }

        // ── result ────────────────────────────────────────────────────────────────
        private static void StyleResult(Transform root)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var panel = Find(root, "ResultPanel");
            if (panel != null)
            {
                var img = panel.GetComponent<Image>(); if (img == null) img = panel.gameObject.AddComponent<Image>();
                img.sprite = Sprite("Teal and Gold Adventure Parchment Frame.png"); img.color = Color.white;
                img.type = Image.Type.Simple; img.preserveAspect = true;
            }
            ButtonArt(root, "MapButton", "india-map.png");
            ButtonArt(root, "RetryButton", "retry.png");
            ButtonArt(root, "ContinueButton", "continue.png");
            foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (font != null) t.font = font;
                // text on the parchment: dark ink; the result line sits in the teal title band
                t.color = t.name == "ResultStatusText" ? Color.white : new Color(0.32f, 0.18f, 0.05f);
            }
        }

        private static void ButtonArt(Transform root, string name, string sprite)
        {
            var b = Find(root, name);
            if (b == null) return;
            var img = b.GetComponent<Image>(); if (img == null) img = b.gameObject.AddComponent<Image>();
            img.sprite = Sprite(sprite); img.color = Color.white; img.type = Image.Type.Simple; img.preserveAspect = true;
            // the button art has its word on it: hide the old label
            foreach (var t in b.GetComponentsInChildren<TMP_Text>(true)) t.gameObject.SetActive(false);
        }

        // ── revive ────────────────────────────────────────────────────────────────
        private static void BuildRevive()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            var root = new GameObject("BKS_RevivePopup", typeof(RectTransform), typeof(Image));
            var rrt = (RectTransform)root.transform; rrt.anchorMin = Vector2.zero; rrt.anchorMax = Vector2.one; rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

            var panel = Img("Panel", root.transform, Sprite("Teal and Gold Adventure Parchment Frame.png"));
            var prt = (RectTransform)panel.transform; prt.anchorMin = prt.anchorMax = new Vector2(0.5f, 0.5f); prt.sizeDelta = new Vector2(1140f, 760f);

            Text("Title", panel.transform, "CONTINUE JOURNEY?", new Vector2(0.30f, 0.72f), new Vector2(0.70f, 0.86f), Color.white, 54f, font);
            Text("Cost", panel.transform, "Revive for 100 Coins", new Vector2(0.14f, 0.52f), new Vector2(0.86f, 0.66f), new Color(0.32f, 0.18f, 0.05f), 58f, font);
            Text("Balance", panel.transform, "You have 0 coins", new Vector2(0.14f, 0.42f), new Vector2(0.86f, 0.52f), new Color(0.45f, 0.30f, 0.12f), 40f, font);
            Text("Countdown", panel.transform, "8", new Vector2(0.80f, 0.56f), new Vector2(0.92f, 0.70f), new Color(0.04f, 0.42f, 0.50f), 72f, font);

            Btn("ReviveButton", panel.transform, Sprite("revive.png"), new Vector2(0.34f, 0.23f));
            Btn("DeclineButton", panel.transform, Sprite("no-thanks.png"), new Vector2(0.66f, 0.23f));

            PrefabUtility.SaveAsPrefabAsset(root, RevivePrefab);
            Object.DestroyImmediate(root);
        }

        // ── helpers ───────────────────────────────────────────────────────────────
        private static Sprite Sprite(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + file);

        private static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        private static GameObject Img(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.sprite = sprite; img.preserveAspect = true;
            return go;
        }

        private static void Text(string name, Transform parent, string text, Vector2 min, Vector2 max, Color color, float size, TMP_FontAsset font)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform; rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            var t = go.GetComponent<TextMeshProUGUI>();
            if (font != null) t.font = font;
            t.text = text; t.color = color; t.alignment = TextAlignmentOptions.Center;
            t.enableAutoSizing = true; t.fontSizeMin = 18f; t.fontSizeMax = size; t.raycastTarget = false;
        }

        private static void Btn(string name, Transform parent, Sprite sprite, Vector2 centre)
        {
            var go = Img(name, parent, sprite);
            go.AddComponent<Button>().targetGraphic = go.GetComponent<Image>();
            var rt = (RectTransform)go.transform; rt.anchorMin = rt.anchorMax = centre; rt.sizeDelta = new Vector2(340f, 137f);
        }
    }
}
