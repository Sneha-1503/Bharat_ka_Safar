using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;
using BharatKaSafar.UI;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Editor
{
    /// <summary>Integration checks against a real mission/HUD. Restores tutorial preferences and exits Play Mode.</summary>
    public static class BKS_OnboardingChecks
    {
        private const string Key = "BKS_DrivingTutorial_v1";
        private static bool running;

        [MenuItem("Bharat Ka Safar/QA/Verify Tutorial (Play Mode)")]
        private static async void Run()
        {
            if (!Application.isPlaying || running || !BKS_DrivingTutorial.IsShowing)
            { Debug.LogWarning("[Onboarding checks] Start a mission with Practise Controls ON first."); return; }
            running = true;
            EditorApplication.isPaused = false;
            bool hadKey = PlayerPrefs.HasKey(Key);
            int saved = PlayerPrefs.GetInt(Key);
            try
            {
                var mission = UnityEngine.Object.FindFirstObjectByType<MissionManager>();
                var car = UnityEngine.Object.FindFirstObjectByType<CarController>();
                var horn = car.GetComponentInChildren<BKS_HornController>();
                var gm = UnityEngine.Object.FindFirstObjectByType<GameManager>();
                var objectives = BKS_MissionObjectiveTracker.Instance;
                float fuel = gm.TotalFuel, elapsed = objectives.Elapsed;
                int coins = PlayerPrefs.GetInt("Coins");
                int stars = UnityEngine.Object.FindFirstObjectByType<Core.BKS_SafarStarTracker>().CollectedCount;
                await Task.Delay(150);
                Check(Time.timeScale == 0f, "Practice must pause the game.");
                Check(!Button("Continue").interactable, "Continue must wait for practice.");
                car.Brake();
                await Task.Delay(800);
                Check(!Button("Continue").interactable, "The wrong pedal must not complete the gas lesson.");
                await Hold(car, true); Continue();
                await Hold(car, false); Continue();
                await Hold(car, true);
                Check(!Button("Continue").interactable, "Balance requires both pedals.");
                await Hold(car, false); Continue();
                Check(!Button("Continue").interactable, "Horn lesson waits for a honk.");
                horn.Honk();
                Check(Button("Continue").interactable, "A honk completes horn practice.");
                Continue();
                Check(Mathf.Approximately(fuel, gm.TotalFuel), "Practice must not burn fuel.");
                Check(Mathf.Approximately(elapsed, objectives.Elapsed), "Practice must not count as mission time.");
                Check(PlayerPrefs.GetInt("Coins") == coins, "Practice must not change the wallet.");
                Check(UnityEngine.Object.FindFirstObjectByType<Core.BKS_SafarStarTracker>().CollectedCount == stars,
                    "Practice horn must not award obstacle stars.");
                Continue();
                Check(!BKS_DrivingTutorial.IsShowing && Time.timeScale == 1f, "Completion must resume driving.");
                Check(PlayerPrefs.GetInt(Key) == 1 && !BKS_DrivingTutorial.NeedsIntroduction, "Completion must persist.");
                Check(Mathf.Abs(car.DriveInput) < .01f, "Completing practice must release the pedals.");

                BKS_DrivingTutorial.TryBegin(mission);
                Check(!BKS_DrivingTutorial.IsShowing, "Completed tutorial must not auto-repeat.");
                BKS_DrivingTutorial.RequestReplay(true);
                BKS_DrivingTutorial.TryBegin(mission);
                await Task.Delay(300);
                Check(BKS_DrivingTutorial.IsShowing, "Explicit replay must work after completion.");
                Button("Skip").onClick.Invoke();
                Check(Time.timeScale == 1f && !BKS_DrivingTutorial.IsShowing, "Skip must release the pause.");
                Check(PlayerPrefs.GetInt(Key) == 1, "Skipping replay must not erase completion.");

                PlayerPrefs.DeleteKey(Key);
                BKS_DrivingTutorial.RequestReplay(false);
                BKS_DrivingTutorial.TryBegin(mission);
                Check(!BKS_DrivingTutorial.IsShowing && BKS_DrivingTutorial.NeedsIntroduction,
                    "Turning practice off skips only this run, without marking it completed.");
                BKS_DrivingTutorial.TryBegin(mission);
                await Task.Delay(300);
                Button("Skip").onClick.Invoke();
                Check(PlayerPrefs.GetInt(Key) == 2 && !BKS_DrivingTutorial.NeedsIntroduction,
                    "Explicit skip must be remembered.");
                Debug.Log("[Onboarding checks] PASS: input gating, both balance inputs, horn, frozen fuel/time, reward isolation, completion, replay, skip, preferences and released pedals.");
            }
            catch (Exception error) { Debug.LogError("[Onboarding checks] FAIL: " + error); }
            finally
            {
                if (hadKey) PlayerPrefs.SetInt(Key, saved); else PlayerPrefs.DeleteKey(Key);
                PlayerPrefs.Save();
                BKS_DrivingTutorial.RequestReplay(false);
                running = false;
                EditorApplication.isPlaying = false;
            }
        }

        private static async Task Hold(CarController car, bool gas)
        {
            if (gas) car.Acceleration(); else car.Brake();
            await Task.Delay(850);
            car.GasBrakeRelease();
        }
        private static void Continue()
        {
            var button = Button("Continue");
            Check(button.interactable, "The current practice step did not complete.");
            button.onClick.Invoke();
        }
        private static Button Button(string name) => UnityEngine.Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
            .Single(b => b.name == name && b.GetComponentInParent<Canvas>().name == "Driving tutorial");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
