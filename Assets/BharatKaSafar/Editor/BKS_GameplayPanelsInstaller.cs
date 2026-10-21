using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BharatKaSafar.UI;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Rewards;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Adds the BKS Pause panel and Stage banner to gameplay scenes, as normal Canvas objects you can edit.
    /// Also routes the HUD pause button to the new panel and switches off the legacy Pause component
    /// in that scene (the legacy script file itself is not changed).
    /// </summary>
    public static class BKS_GameplayPanelsInstaller
    {
        private const string Art = "Assets/BharatKaSafar/Art/UI/IndiaPausePanel/";

        [MenuItem("Bharat Ka Safar/UI/Add Gameplay Panels (Open Scene)")]
        public static void InstallOpenScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (Install(scene)) { EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); }
        }

        [MenuItem("Bharat Ka Safar/UI/Add Gameplay Panels (All Mission Scenes)")]
        public static void InstallAllMissionScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var paths = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/BharatKaSafar/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(p), @"_M\d\d_"))
                .ToList();
            int done = 0;
            foreach (var path in paths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                if (Install(scene)) { EditorSceneManager.SaveScene(scene); done++; }
            }
            Debug.Log($"[BKS Panels] Installed in {done} of {paths.Count} mission scenes.");
        }

        private static bool Install(Scene scene)
        {
            var hud = Object.FindFirstObjectByType<BKS_GameplayHUD>(FindObjectsInactive.Include);
            Canvas canvas = hud != null ? hud.GetComponentInParent<Canvas>(true) : null;
            if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) { Debug.LogWarning($"[BKS Panels] {scene.name}: no Canvas, skipped."); return false; }
            canvas = canvas.rootCanvas;

            bool changed = false;
            if (Object.FindFirstObjectByType<BKS_StageBanner>(FindObjectsInactive.Include) == null)
            {
                BuildStageBanner(canvas.transform); changed = true;
            }
            if (Object.FindFirstObjectByType<BKS_ObjectiveHUD>(FindObjectsInactive.Include) == null)
            {
                BuildObjectiveHud(canvas.transform); changed = true;
            }
            if (Object.FindFirstObjectByType<BKS_DoubleRewardButton>(FindObjectsInactive.Include) == null)
            {
                BuildDoubleButton(canvas.transform); changed = true;
            }
            if (Object.FindFirstObjectByType<BKS_PausePanel>(FindObjectsInactive.Include) == null)
            {
                BuildPausePanel(canvas.transform); changed = true;
            }
            if (Object.FindFirstObjectByType<BKS_ReviveManager>(FindObjectsInactive.Include) == null)
            {
                BuildRevive(canvas.transform); changed = true;
            }
            changed |= RoutePauseButton(hud);
            Debug.Log($"[BKS Panels] {scene.name}: " + (changed ? "updated." : "already set up."));
            return changed;
        }

        // ── Pause button routing ─────────────────────────────────────────────
        private static bool RoutePauseButton(BKS_GameplayHUD hud)
        {
            bool changed = false;
            Button button = null;
            if (hud != null) button = new SerializedObject(hud).FindProperty("pauseButton").objectReferenceValue as Button;

            // Legacy HillClimb Pause adds its own listener in Start(); disable it in this scene only.
            foreach (var legacy in Object.FindObjectsByType<Pause>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button == null) button = legacy.pauseButton;
                if (legacy.enabled) { legacy.enabled = false; changed = true; EditorUtility.SetDirty(legacy); }
                if (legacy.PauseMen != null && legacy.PauseMen.activeSelf) { legacy.PauseMen.SetActive(false); changed = true; }
            }
            if (button == null || hud == null) return changed;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == hud && button.onClick.GetPersistentMethodName(i) == nameof(BKS_GameplayHUD.OnPausePressed))
                    return changed;
            UnityEventTools.AddPersistentListener(button.onClick, hud.OnPausePressed);
            EditorUtility.SetDirty(button);
            return true;
        }

        // ── Stage banner ─────────────────────────────────────────────────────
        private static void BuildStageBanner(Transform canvas)
        {
            var root = Rect("BKS_StageBanner", canvas, new Vector2(0.2f, 0.74f), new Vector2(0.8f, 0.88f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.12f, 0.14f, 0.7f);
            bg.raycastTarget = false;
            root.gameObject.AddComponent<CanvasGroup>().alpha = 0f; // hidden until a stage trigger fires
            var title = Text("Title", root, "STAGE 1", 30, new Color(1f, 0.72f, 0.2f), FontStyles.Bold,
                             new Vector2(0.03f, 0.48f), new Vector2(0.97f, 0.98f));
            var line = Text("Dialogue", root, "Stage message", 18, Color.white, FontStyles.Normal,
                            new Vector2(0.03f, 0.04f), new Vector2(0.97f, 0.5f));
            var banner = root.gameObject.AddComponent<BKS_StageBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("dialogueText").objectReferenceValue = line;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Objective HUD ("Diya 3/5", "Hits 1/3", timer) ────────────────────
        private static void BuildObjectiveHud(Transform canvas)
        {
            var root = Rect("BKS_ObjectiveHUD", canvas, new Vector2(0.36f, 0.66f), new Vector2(0.64f, 0.86f));
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.12f, 0.14f, 0.55f);
            bg.raycastTarget = false;

            var collect = Rect("CollectRow", root, new Vector2(0f, 0.66f), new Vector2(1f, 1f));
            var icon = collect.gameObject.AddComponent<Image>(); // placeholder so the row has a rect; icon is a child
            icon.enabled = false;
            var iconImg = Rect("Icon", collect, new Vector2(0.03f, 0.08f), new Vector2(0.2f, 0.92f)).gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true; iconImg.raycastTarget = false; iconImg.enabled = false;
            var collectText = Text("Text", collect, "Diya 0/5", 22, Color.white, FontStyles.Bold, new Vector2(0.22f, 0f), new Vector2(0.97f, 1f));
            collectText.alignment = TextAlignmentOptions.Left;

            var hits = Rect("HitsRow", root, new Vector2(0f, 0.33f), new Vector2(1f, 0.66f));
            var hitsText = Text("Text", hits, "Hits 0/3", 20, Color.white, FontStyles.Bold, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));

            var time = Rect("TimeRow", root, new Vector2(0f, 0f), new Vector2(1f, 0.33f));
            var timeText = Text("Text", time, "0:00 / 2:30", 20, Color.white, FontStyles.Bold, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));

            var hud = root.gameObject.AddComponent<BKS_ObjectiveHUD>();
            var so = new SerializedObject(hud);
            so.FindProperty("collectRow").objectReferenceValue = collect.gameObject;
            so.FindProperty("collectIcon").objectReferenceValue = iconImg;
            so.FindProperty("collectText").objectReferenceValue = collectText;
            so.FindProperty("hitsRow").objectReferenceValue = hits.gameObject;
            so.FindProperty("hitsText").objectReferenceValue = hitsText;
            so.FindProperty("timeRow").objectReferenceValue = time.gameObject;
            so.FindProperty("timeText").objectReferenceValue = timeText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── 2X button ────────────────────────────────────────────────────────
        private static void BuildDoubleButton(Transform canvas)
        {
            var b = ColorButton("BKS_2XButton", canvas, "2X", new Color(0.95f, 0.6f, 0.1f), new Vector2(0.84f, 0.38f), new Vector2(0.98f, 0.47f));
            var view = b.gameObject.AddComponent<BKS_DoubleRewardButton>();
            var so = new SerializedObject(view);
            so.FindProperty("button").objectReferenceValue = b;
            so.FindProperty("label").objectReferenceValue = b.GetComponentInChildren<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Revive ("Continue Journey?") ─────────────────────────────────────
        private static void BuildRevive(Transform canvas)
        {
            var holder = Rect("BKS_Revive", canvas, Vector2.zero, Vector2.one);
            holder.SetAsLastSibling();
            var root = Rect("ReviveRoot", holder, Vector2.zero, Vector2.one);
            root.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
            var box = Rect("Box", root, new Vector2(0.27f, 0.22f), new Vector2(0.73f, 0.8f));
            box.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.93f, 0.8f, 1f);
            var teal = new Color(0.05f, 0.3f, 0.33f);
            var title = Text("Title", box, "Continue Journey?", 34, teal, FontStyles.Bold, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.96f));
            var count = Text("Countdown", box, "8", 40, new Color(0.8f, 0.3f, 0.15f), FontStyles.Bold, new Vector2(0.4f, 0.6f), new Vector2(0.6f, 0.78f));
            var cost = Text("Cost", box, "Revive for 200 Coins", 26, teal, FontStyles.Bold, new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.6f));
            var bal = Text("Balance", box, "You have 0 coins", 18, new Color(0.3f, 0.25f, 0.2f), FontStyles.Normal, new Vector2(0.05f, 0.37f), new Vector2(0.95f, 0.47f));
            var yes = ColorButton("ReviveButton", box, "REVIVE", new Color(0.95f, 0.55f, 0.12f), new Vector2(0.08f, 0.08f), new Vector2(0.5f, 0.3f));
            var no = ColorButton("NoThanksButton", box, "NO THANKS", new Color(0.45f, 0.45f, 0.45f), new Vector2(0.55f, 0.08f), new Vector2(0.92f, 0.3f));

            var popup = holder.gameObject.AddComponent<BKS_RevivePopup>();
            var so = new SerializedObject(popup);
            so.FindProperty("root").objectReferenceValue = root.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("costText").objectReferenceValue = cost;
            so.FindProperty("balanceText").objectReferenceValue = bal;
            so.FindProperty("countdownText").objectReferenceValue = count;
            so.FindProperty("reviveButton").objectReferenceValue = yes;
            so.FindProperty("declineButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);

            var manager = new GameObject("BKS_ReviveManager").AddComponent<BKS_ReviveManager>();
            var mso = new SerializedObject(manager);
            mso.FindProperty("popup").objectReferenceValue = popup;
            mso.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── Pause panel ──────────────────────────────────────────────────────
        private static void BuildPausePanel(Transform canvas)
        {
            var holder = Rect("BKS_PauseMenu", canvas, Vector2.zero, Vector2.one);
            holder.SetAsLastSibling();

            var root = Rect("PauseRoot", holder, Vector2.zero, Vector2.one);
            var dim = root.gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.6f); // also blocks clicks on the HUD behind

            var popup = Rect("PausePopup", root, new Vector2(0.14f, 0.12f), new Vector2(0.86f, 0.9f));
            var panelImg = popup.gameObject.AddComponent<Image>();
            panelImg.sprite = Sprite(Art + "pause-panel.png");
            panelImg.preserveAspect = true;

            string[] names = { "Resume", "Restart", "Settings", "Home" };
            var buttons = new Button[4];
            for (int i = 0; i < 4; i++)
            {
                // two rows of two, inside the panel's empty centre
                float x0 = i % 2 == 0 ? 0.2f : 0.52f, y0 = i < 2 ? 0.47f : 0.27f;
                buttons[i] = ImageButton(names[i] + "Button", popup, Art + "MatchingButtons/" + names[i].ToLower() + "-button.png",
                                         new Vector2(x0, y0), new Vector2(x0 + 0.28f, y0 + 0.17f));
            }

            // Quit confirmation
            var quit = Rect("QuitConfirm", root, Vector2.zero, Vector2.one);
            quit.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
            var box = Rect("Box", quit, new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f));
            box.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.93f, 0.8f, 1f);
            Text("Message", box, "Mission chhodein?\n<size=70%>Is safar ki progress save nahi hogi.</size>", 26,
                 new Color(0.05f, 0.3f, 0.33f), FontStyles.Bold, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.95f));
            var yes = ColorButton("YesButton", box, "HAAN", new Color(0.8f, 0.3f, 0.15f), new Vector2(0.1f, 0.1f), new Vector2(0.45f, 0.35f));
            var no = ColorButton("NoButton", box, "NAHI", new Color(0.05f, 0.45f, 0.48f), new Vector2(0.55f, 0.1f), new Vector2(0.9f, 0.35f));

            var panel = holder.gameObject.AddComponent<BKS_PausePanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("pauseRoot").objectReferenceValue = root.gameObject;
            so.FindProperty("popupPanel").objectReferenceValue = popup;
            so.FindProperty("quitConfirmRoot").objectReferenceValue = quit.gameObject;
            so.FindProperty("resumeButton").objectReferenceValue = buttons[0];
            so.FindProperty("restartButton").objectReferenceValue = buttons[1];
            so.FindProperty("settingsButton").objectReferenceValue = buttons[2];
            so.FindProperty("homeButton").objectReferenceValue = buttons[3];
            so.FindProperty("quitYesButton").objectReferenceValue = yes;
            so.FindProperty("quitNoButton").objectReferenceValue = no;
            so.ApplyModifiedPropertiesWithoutUndo();

            quit.gameObject.SetActive(false);
            root.gameObject.SetActive(false);
        }

        // ── Helpers ──────────────────────────────────────────────────────────
        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min; rt.anchorMax = max; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static TMP_Text Text(string name, Transform parent, string text, float size, Color color, FontStyles style, Vector2 min, Vector2 max)
        {
            var t = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color; t.fontStyle = style;
            t.alignment = TextAlignmentOptions.Center; t.enableAutoSizing = true; t.fontSizeMax = size; t.fontSizeMin = 10;
            t.raycastTarget = false;
            return t;
        }

        private static Button ImageButton(string name, Transform parent, string spritePath, Vector2 min, Vector2 max)
        {
            var rt = Rect(name, parent, min, max);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = Sprite(spritePath);
            img.preserveAspect = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            return b;
        }

        private static Button ColorButton(string name, Transform parent, string label, Color color, Vector2 min, Vector2 max)
        {
            var rt = Rect(name, parent, min, max);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            Text("Label", rt, label, 24, Color.white, FontStyles.Bold, Vector2.zero, Vector2.one);
            return b;
        }

        private static Sprite Sprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) { Debug.LogWarning("[BKS Panels] Missing art " + path); return null; }
            if (imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
