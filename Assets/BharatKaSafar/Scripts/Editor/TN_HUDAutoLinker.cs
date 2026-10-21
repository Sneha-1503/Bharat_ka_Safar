using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.UI;
using BharatKaSafar.Vehicle;
using BharatKaSafar.Core;

public class TN_HUDAutoLinker
{
    [MenuItem("Bharat Ka Safar/Auto-Link HUD (Current Scene)")]
    public static void AutoLinkHUD()
    {
        BKS_GameplayHUD hud = Object.FindObjectOfType<BKS_GameplayHUD>();
        if (hud == null)
        {
            Debug.LogError("[HUD Linker] BKS_GameplayHUD not found in scene!");
            return;
        }

        SerializedObject so = new SerializedObject(hud);

        // ── Health ────────────────────────────────────────────────────────────
        TryLinkSlider(so, "healthBar", "HealthSlider");
        TryLinkTMP(so, "healthText", "HPText");

        // ── Stars ─────────────────────────────────────────────────────────────
        TryLinkTMP(so, "safarStarsText", "StarsText", "StarText", "SafarStarsText");

        // ── Coins ─────────────────────────────────────────────────────────────
        TryLinkTMP(so, "coinsText", "CoinsText", "CoinText", "Coins");

        // ── Score ─────────────────────────────────────────────────────────────
        TryLinkTMP(so, "scoreText", "ScoreText");

        // ── Multiplier Badge ──────────────────────────────────────────────────
        TryLinkGameObject(so, "multiplierBadge", "MultiplierBadge", "2X", "Double", "DoubleBadge");
        TryLinkTMP(so, "multiplierText", "MultiplierText", "2XText");

        // ── Speed Text ────────────────────────────────────────────────────────
        TryLinkTMP(so, "speedText", "SpeedText", "Speed Text", "SpeedDisplay");

        // ── Buttons ───────────────────────────────────────────────────────────
        TryLinkButton(so, "pauseButton", "PauseButton", "Pause", "BtnPause");
        TryLinkButton(so, "brakeButton", "BrakeButton", "Brake", "BRAKE");
        TryLinkButton(so, "accelerateButton", "AccelerateButton", "GasButton", "GAS", "Gas");
        TryLinkButton(so, "hornButton", "BKS_HornButton", "HornButton", "Horn");

        // ── System References ─────────────────────────────────────────────────
        // Vehicle Health
        var vHealth = Object.FindObjectOfType<BKS_VehicleHealth>();
        if (vHealth != null)
        {
            var prop = so.FindProperty("vehicleHealth");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = vHealth;
                Debug.Log("  Linked: vehicleHealth");
            }
        }

        // Star Tracker
        var starTracker = Object.FindObjectOfType<BKS_SafarStarTracker>();
        if (starTracker != null)
        {
            var prop = so.FindProperty("starTracker");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = starTracker;
                Debug.Log("  Linked: starTracker");
            }
        }

        // Car Controller
        var carController = Object.FindObjectOfType<CarController>();
        if (carController != null)
        {
            var prop = so.FindProperty("carController");
            if (prop != null && prop.objectReferenceValue == null)
            {
                prop.objectReferenceValue = carController;
                Debug.Log("  Linked: carController");
            }
        }

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(hud);

        // Also auto-link TN_HUDExtension if it exists
        TN_HUDExtension tnHud = Object.FindObjectOfType<TN_HUDExtension>();
        if (tnHud != null)
        {
            SerializedObject tnSo = new SerializedObject(tnHud);

            // Speedometer needle
            TryLinkRectTransform(tnSo, "speedometerNeedle", "Needle", "SpeedometerNeedle");
            TryLinkTMP(tnSo, "speedText", "SpeedText", "Speed Text");
            TryLinkTMP(tnSo, "starCountText", "StarsText", "StarCountText", "StarText");
            TryLinkGameObject(tnSo, "starPanel", "StarPanel", "Stars Panel", "StarCounter");
            TryLinkGameObject(tnSo, "doubleBadge", "MultiplierBadge", "2X", "Double", "DoubleBadge");
            TryLinkTMP(tnSo, "doubleTimerText", "MultiplierText", "TimerText", "2XText");

            tnSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(tnHud);
        }

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        Debug.Log("[HUD Linker] ✅ HUD auto-linked and saved! Press Play to test.");
    }

    // ── Helper Methods ────────────────────────────────────────────────────────────

    private static void TryLinkTMP(SerializedObject so, string propName, params string[] objNames)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (string n in objNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                var tmp = go.GetComponent<TMP_Text>();
                if (tmp != null) { prop.objectReferenceValue = tmp; Debug.Log($"  Linked TMP: {propName} → {n}"); return; }
            }
        }

        // Fallback: search all TMP_Texts in scene by name match
        TMP_Text[] all = Object.FindObjectsOfType<TMP_Text>();
        foreach (var t in all)
        {
            foreach (string n in objNames)
            {
                if (t.gameObject.name.ToLower().Contains(n.ToLower()))
                {
                    prop.objectReferenceValue = t;
                    Debug.Log($"  Linked TMP (search): {propName} → {t.gameObject.name}");
                    return;
                }
            }
        }
    }

    private static void TryLinkSlider(SerializedObject so, string propName, params string[] objNames)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (string n in objNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                var slider = go.GetComponent<Slider>();
                if (slider != null) { prop.objectReferenceValue = slider; Debug.Log($"  Linked Slider: {propName} → {n}"); return; }
            }
        }
    }

    private static void TryLinkButton(SerializedObject so, string propName, params string[] objNames)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (string n in objNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                var btn = go.GetComponent<Button>();
                if (btn != null) { prop.objectReferenceValue = btn; Debug.Log($"  Linked Button: {propName} → {n}"); return; }
                btn = go.GetComponentInChildren<Button>();
                if (btn != null) { prop.objectReferenceValue = btn; Debug.Log($"  Linked Button (child): {propName} → {n}"); return; }
            }
        }
    }

    private static void TryLinkGameObject(SerializedObject so, string propName, params string[] objNames)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (string n in objNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) { prop.objectReferenceValue = go; Debug.Log($"  Linked GO: {propName} → {n}"); return; }
        }
    }

    private static void TryLinkRectTransform(SerializedObject so, string propName, params string[] objNames)
    {
        var prop = so.FindProperty(propName);
        if (prop == null || prop.objectReferenceValue != null) return;

        foreach (string n in objNames)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
            {
                var rt = go.GetComponent<RectTransform>();
                if (rt != null) { prop.objectReferenceValue = rt; Debug.Log($"  Linked RT: {propName} → {n}"); return; }
            }
        }
    }
}
