using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.UI;

namespace BharatKaSafar.EditorTools
{
    /// <summary>
    /// Menu: Bharat Ka Safar → HUD → Setup Speedometer (Tamil Nadu + Uttar Pradesh)
    ///
    /// Tamil Nadu (5 scenes): the dial "speedometer" already exists. This turns its needle on,
    ///   puts the needle pivot on the hub, and wires the needle + "Speed Text" to BKS_GameplayHUD.
    /// After TN Mission 1 is set up, its dial is saved as a prefab:
    ///   Assets/BharatKaSafar/Prefabs/UI/BKS_Speedometer.prefab
    /// Uttar Pradesh (5 scenes): that prefab is added to the HUD canvas (same place as in TN),
    ///   the old plain "Speedometer" text is hidden, and the HUD is wired.
    ///
    /// Every scene is saved. Safe to run again (nothing is duplicated).
    /// </summary>
    public static class BKS_SpeedometerSetupUtility
    {
        private const string PrefabPath = "Assets/BharatKaSafar/Prefabs/UI/BKS_Speedometer.prefab";

        private static readonly string[] TamilNaduScenes =
        {
            "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M01_Madurai.unity",
            "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M02_Chennai.unity",
            "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M03_Mahabalipuram.unity",
            "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M04_Ooty.unity",
            "Assets/BharatKaSafar/Scenes/Tamil nadu/BKS_TN_M05_Kanyakumari.unity",
        };

        private static readonly string[] UttarPradeshScenes =
        {
            "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M01_Agra.unity",
            "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M02_Varanasi.unity",
            "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M03_Sarnath.unity",
            "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M04_Prayagraj.unity",
            "Assets/BharatKaSafar/Scenes/UP/BKS_UP_M05_Bhulbhulaiya.unity",
        };

        // Object names used in the TN scenes
        private const string DialName   = "speedometer";          // lower-case: the dial image
        private const string NeedleName = "Speedometer Needle";
        private const string TextName   = "Speed Text";
        private const string OldUPText  = "Speedometer";          // plain text used in UP before

        // Measured from the art:
        //   needle sprite: hub centre at (0.498, 0.209) of the image, tip at 72% of image height above it
        //   dial sprite:   hub at the dial centre, 0 = left, 60 = top, 120 = right
        private static readonly Vector2 NeedlePivot = new Vector2(0.498f, 0.209f);
        private const float NeedleHeightOfDial = 0.475f;     // needle image height relative to dial size
        private const float NeedleAspect       = 1272f / 1236f;

        // Same place as the TN dial (both chapters use an 800×600 reference canvas)
        private static readonly Vector2 DialPosition = new Vector2(-332f, 67f);
        private static readonly Vector2 DialSize     = new Vector2(120f, 112f);

        [MenuItem("Bharat Ka Safar/HUD/Setup Speedometer (Tamil Nadu + Uttar Pradesh)", priority = 30)]
        public static void SetupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string startScene = EditorSceneManager.GetActiveScene().path;

            var report = new List<string>();

            // ── Tamil Nadu ──
            foreach (var path in TamilNaduScenes)
                report.Add(SetupTamilNaduScene(path, savePrefab: path == TamilNaduScenes[0]));

            // ── Uttar Pradesh ──
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                report.Add("⚠ Speedometer prefab missing — UP skipped (TN Mission 1 must have a 'speedometer' dial).");
            }
            else
            {
                foreach (var path in UttarPradeshScenes)
                    report.Add(SetupUttarPradeshScene(path, prefab));
            }

            if (!string.IsNullOrEmpty(startScene) && File.Exists(startScene))
                EditorSceneManager.OpenScene(startScene, OpenSceneMode.Single);

            string text = string.Join("\n", report);
            Debug.Log("[BKS Speedometer] \n" + text);
            EditorUtility.DisplayDialog("Bharat Ka Safar — Speedometer", text, "OK");
        }

        // ── Tamil Nadu ────────────────────────────────────────────────────────────

        private static string SetupTamilNaduScene(string path, bool savePrefab)
        {
            string label = Path.GetFileNameWithoutExtension(path);
            if (!File.Exists(path)) return $"⚠ {label}: scene not found";

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var hud = Object.FindFirstObjectByType<BKS_GameplayHUD>(FindObjectsInactive.Include);
            if (hud == null) return $"⚠ {label}: no BKS_GameplayHUD";

            var dial = FindExact(hud.transform, DialName) as RectTransform;
            if (dial == null) return $"⚠ {label}: dial '{DialName}' not found";

            if (!ConfigureDial(dial, out var needle, out var text))
                return $"⚠ {label}: needle or speed text missing inside the dial";

            WireHud(hud, needle, text);

            if (savePrefab)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath));
                PrefabUtility.SaveAsPrefabAsset(dial.gameObject, PrefabPath);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"✓ {label}: needle on, HUD wired" + (savePrefab ? " (prefab saved)" : "");
        }

        // ── Uttar Pradesh ─────────────────────────────────────────────────────────

        private static string SetupUttarPradeshScene(string path, GameObject prefab)
        {
            string label = Path.GetFileNameWithoutExtension(path);
            if (!File.Exists(path)) return $"⚠ {label}: scene not found";

            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var hud = Object.FindFirstObjectByType<BKS_GameplayHUD>(FindObjectsInactive.Include);
            if (hud == null) return $"⚠ {label}: no BKS_GameplayHUD";

            // Reuse a dial from an earlier run, otherwise add the prefab
            var dial = FindExact(hud.transform, DialName) as RectTransform;
            bool added = false;
            if (dial == null)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, hud.transform);
                go.name = DialName;
                dial = (RectTransform)go.transform;
                dial.anchorMin = dial.anchorMax = new Vector2(0.5f, 0.5f);
                dial.pivot = new Vector2(0.5f, 0.5f);
                dial.anchoredPosition = DialPosition;
                dial.sizeDelta = DialSize;
                dial.localScale = Vector3.one;
                added = true;
            }

            if (!ConfigureDial(dial, out var needle, out var text))
                return $"⚠ {label}: speedometer prefab is missing its needle or text";

            // Hide the old plain text so there is only one speed display
            var oldText = FindExact(hud.transform, OldUPText);
            if (oldText != null && oldText != dial.transform && oldText.GetComponent<TMP_Text>() != null)
                oldText.gameObject.SetActive(false);

            WireHud(hud, needle, text);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            return $"✓ {label}: " + (added ? "speedometer added, " : "") + "HUD wired";
        }

        // ── Shared ────────────────────────────────────────────────────────────────

        /// <summary>Turns the needle on and places its pivot on the dial hub.</summary>
        private static bool ConfigureDial(RectTransform dial, out RectTransform needle, out TMP_Text text)
        {
            needle = FindExact(dial, NeedleName) as RectTransform;
            var textTr = FindExact(dial, TextName);
            text = textTr != null ? textTr.GetComponent<TMP_Text>() : null;
            if (needle == null || text == null) return false;

            float s = Mathf.Min(dial.rect.width, dial.rect.height);
            if (s <= 0f) s = Mathf.Min(DialSize.x, DialSize.y);
            float h = s * NeedleHeightOfDial;

            needle.gameObject.SetActive(true);
            needle.anchorMin = needle.anchorMax = new Vector2(0.5f, 0.5f);
            needle.pivot = NeedlePivot;
            needle.sizeDelta = new Vector2(h * NeedleAspect, h);
            needle.anchoredPosition = Vector2.zero;           // hub = dial centre
            needle.localRotation = Quaternion.Euler(0f, 0f, 90f); // 0 km/h
            needle.localScale = Vector3.one;

            var needleImage = needle.GetComponent<Image>();
            if (needleImage != null) { needleImage.preserveAspect = true; needleImage.raycastTarget = false; }

            text.raycastTarget = false;
            text.transform.SetAsLastSibling();                // text drawn above the needle
            return true;
        }

        private static void WireHud(BKS_GameplayHUD hud, RectTransform needle, TMP_Text text)
        {
            var so = new SerializedObject(hud);
            so.FindProperty("speedText").objectReferenceValue         = text;
            so.FindProperty("speedometerNeedle").objectReferenceValue = needle;
            so.FindProperty("speedTextFormat").stringValue            = "{0} KM/H";
            so.FindProperty("needleAngleAtZero").floatValue           = 90f;
            so.FindProperty("needleAngleAtMax").floatValue            = -90f;
            so.FindProperty("maxDialSpeedKmh").floatValue             = 120f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Case-sensitive, recursive, includes inactive children ("speedometer" ≠ "Speedometer").</summary>
        private static Transform FindExact(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var found = FindExact(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
