using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.UI;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Menu: Bharat Ka Safar → Missions → Build Result Screen
    ///
    /// Opens BKS_MissionResult, builds a simple (placeholder-art) result UI under the
    /// MissionResultCanvas if it is empty, wires every BKS_MissionResultScreenUI field and saves the scene.
    /// Safe to run again: existing children are reused, nothing is duplicated.
    /// </summary>
    public static class BKS_MissionFlowSetupUtility
    {
        private const string ResultScenePath = "Assets/BharatKaSafar/Scenes/BKS_MissionResult.unity";
        private const string SpriteFolder    = "Assets/BharatKaSafar/BKS sprite/home/";

        [MenuItem("Bharat Ka Safar/Missions/Build Result Screen", priority = 20)]
        public static void BuildResultScreen()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var scene = EditorSceneManager.OpenScene(ResultScenePath, OpenSceneMode.Single);
            var ui = Object.FindFirstObjectByType<BKS_MissionResultScreenUI>(FindObjectsInactive.Include);
            if (ui == null)
            {
                EditorUtility.DisplayDialog("Bharat Ka Safar", "BKS_MissionResultScreenUI not found in " + ResultScenePath, "OK");
                return;
            }

            Transform canvas = ui.transform;
            EnsureScaler(canvas.gameObject);

            Sprite star    = LoadSprite("star.png");
            Sprite coin    = LoadSprite("coin.png");
            Sprite button  = LoadSprite("button_yellow_idle.png");
            Color  gold    = new Color(1f, 0.82f, 0.25f, 1f);

            // Dim background + panel
            var dim = GetOrCreate(canvas, "Dim", Vector2.zero, Vector2.one);
            AddImage(dim, null, new Color(0f, 0f, 0f, 0.6f), false);

            var panel = GetOrCreate(canvas, "ResultPanel", new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.9f));
            AddImage(panel, null, new Color(0.08f, 0.16f, 0.38f, 0.96f), false);

            var status  = AddText(GetOrCreate(panel, "ResultStatusText", new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f)), "MISSION COMPLETE!", 64, gold);
            var mission = AddText(GetOrCreate(panel, "MissionNameText",  new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.80f)), "Mission Name", 38, Color.white);

            var starsRow = GetOrCreate(panel, "Stars", new Vector2(0.25f, 0.47f), new Vector2(0.75f, 0.69f));
            var hlg = GetOrAdd<HorizontalLayoutGroup>(starsRow.gameObject);
            hlg.spacing = 20f; hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = hlg.childControlHeight = true;
            hlg.childForceExpandWidth = hlg.childForceExpandHeight = true;
            var stars = new List<Image>();
            for (int i = 1; i <= 3; i++)
                stars.Add(AddImage(GetOrCreate(starsRow, "Star" + i, Vector2.zero, Vector2.one), star, Color.white, true));

            var coinIcon = GetOrCreate(panel, "CoinIcon", new Vector2(0.18f, 0.35f), new Vector2(0.26f, 0.44f));
            AddImage(coinIcon, coin, Color.white, true);
            var coins = AddText(GetOrCreate(panel, "CoinsEarnedText", new Vector2(0.27f, 0.35f), new Vector2(0.48f, 0.44f)), "+0", 40, Color.white, TextAlignmentOptions.Left);

            var starIcon = GetOrCreate(panel, "SafarStarIcon", new Vector2(0.54f, 0.35f), new Vector2(0.62f, 0.44f));
            AddImage(starIcon, star, Color.white, true);
            var safar = AddText(GetOrCreate(panel, "SafarStarsEarnedText", new Vector2(0.63f, 0.35f), new Vector2(0.84f, 0.44f)), "+0", 40, Color.white, TextAlignmentOptions.Left);

            var progress = AddText(GetOrCreate(panel, "ChapterProgressText", new Vector2(0.05f, 0.25f), new Vector2(0.95f, 0.33f)), "Uttar Pradesh   1 / 5 missions", 30, new Color(0.85f, 0.9f, 1f, 1f));

            var buttonsRow = GetOrCreate(panel, "Buttons", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.20f));
            var bh = GetOrAdd<HorizontalLayoutGroup>(buttonsRow.gameObject);
            bh.spacing = 24f; bh.childAlignment = TextAnchor.MiddleCenter;
            bh.childControlWidth = bh.childControlHeight = true;
            bh.childForceExpandWidth = bh.childForceExpandHeight = true;

            var continueParts = MakeButton(buttonsRow, "ContinueButton", "CONTINUE",  button);
            var retryParts    = MakeButton(buttonsRow, "RetryButton",    "RETRY",     button);
            var mapParts      = MakeButton(buttonsRow, "MapButton",      "INDIA MAP", button);
            Button   continueBtn  = continueParts.Item1;
            TMP_Text continueText = continueParts.Item2;
            Button   retryBtn     = retryParts.Item1;
            Button   mapBtn       = mapParts.Item1;

            // Wire the screen script
            var so = new SerializedObject(ui);
            so.FindProperty("missionNameText").objectReferenceValue       = mission;
            so.FindProperty("resultStatusText").objectReferenceValue      = status;
            so.FindProperty("coinsEarnedText").objectReferenceValue       = coins;
            so.FindProperty("safarStarsEarnedText").objectReferenceValue  = safar;
            so.FindProperty("chapterProgressText").objectReferenceValue   = progress;
            so.FindProperty("starFilled").objectReferenceValue            = star;
            var starsProp = so.FindProperty("starImages");
            starsProp.arraySize = stars.Count;
            for (int i = 0; i < stars.Count; i++) starsProp.GetArrayElementAtIndex(i).objectReferenceValue = stars[i];
            so.FindProperty("nextMissionButton").objectReferenceValue     = continueBtn;
            so.FindProperty("nextMissionButtonText").objectReferenceValue = continueText;
            so.FindProperty("retryButton").objectReferenceValue           = retryBtn;
            so.FindProperty("indiaMapButton").objectReferenceValue        = mapBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[BKS Mission Setup] Result screen built and saved: " + ResultScenePath);
            EditorUtility.DisplayDialog("Bharat Ka Safar", "Result screen built and saved.\n\n" + ResultScenePath, "OK");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static (Button, TMP_Text) MakeButton(Transform parent, string name, string label, Sprite sprite)
        {
            var rt  = GetOrCreate(parent, name, Vector2.zero, Vector2.one);
            var img = AddImage(rt, sprite, Color.white, false);
            img.raycastTarget = true;
            var btn = GetOrAdd<Button>(rt.gameObject);
            btn.targetGraphic = img;
            var text = AddText(GetOrCreate(rt, "Label", Vector2.zero, Vector2.one), label, 34, new Color(0.35f, 0.15f, 0f, 1f));
            return (btn, text);
        }

        private static void EnsureScaler(GameObject canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.AddComponent<CanvasScaler>();
            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.AddComponent<GraphicRaycaster>();
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include) != null) return;
            var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            go.transform.SetAsLastSibling();
        }

        private static Sprite LoadSprite(string file)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFolder + file);
            if (s == null) Debug.LogWarning("[BKS Mission Setup] Sprite not found: " + SpriteFolder + file);
            return s;
        }

        private static T GetOrAdd<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            if (c == null) c = go.AddComponent<T>();
            return c;
        }

        private static RectTransform GetOrCreate(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var existing = parent.Find(name);
            if (existing != null) return (RectTransform)existing;

            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static Image AddImage(RectTransform rt, Sprite sprite, Color color, bool preserveAspect)
        {
            var img = rt.GetComponent<Image>();
            if (img == null)
            {
                img = rt.gameObject.AddComponent<Image>();
                img.sprite = sprite;
                img.color = color;
            }
            img.preserveAspect = preserveAspect;
            img.raycastTarget = false;
            return img;
        }

        private static TMP_Text AddText(RectTransform rt, string text, float size, Color color,
                                        TextAlignmentOptions align = TextAlignmentOptions.Center)
        {
            var tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.text = text;
                tmp.fontSize = size;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 14;
                tmp.fontSizeMax = size;
                tmp.alignment = align;
                tmp.color = color;
            }
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
