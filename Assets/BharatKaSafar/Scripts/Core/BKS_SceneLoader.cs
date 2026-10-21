using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Core
{
    /// <summary>
    /// Centralized scene loader for all BKS scenes.
    /// Uses scene name strings, never hard-coded build indices.
    /// Holds the scene name for the currently selected mission so result/garage scenes know where to return.
    /// </summary>
    public class BKS_SceneLoader : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────────────────────────
        public static BKS_SceneLoader Instance { get; private set; }

        // ── Scene Name Constants ──────────────────────────────────────────────────
        // Home is the main menu; BKS_StateMap preserves the existing state map.
        public const string SCENE_HOME           = "Home";
        public const string SCENE_INDIA_MAP      = "BKS_StateMap";
        public const string SCENE_UP_MONUMENT_SEL = "BKS_UP_MonumentSelect";
        public const string SCENE_UP_MISSION_SEL = "BKS_UP_MissionSelect";
        public const string SCENE_MISSION_RESULT = "BKS_MissionResult";
        public const string SCENE_GARAGE         = "BKS_Garage";
        public const string SCENE_STORE          = "BKS_Store";
        public const string SCENE_CHALLENGES     = "BKS_Challenges";
        public const string SCENE_DIARY          = "BKS_SafarDiary";

        private string sceneBeforeMenu;

        /// <summary>Open the Store; Back returns to the screen it was opened from.</summary>
        public void GoToStore() => OpenMenuScene(SCENE_STORE);
        /// <summary>Open Challenges; Back returns to the screen it was opened from.</summary>
        public void GoToChallenges() => OpenMenuScene(SCENE_CHALLENGES);
        /// <summary>Open the Safar Diary; Back returns to the screen it was opened from.</summary>
        public void GoToDiary() => OpenMenuScene(SCENE_DIARY);

        private void OpenMenuScene(string scene)
        {
            string current = SceneManager.GetActiveScene().name;
            if (current != SCENE_STORE && current != SCENE_CHALLENGES && current != SCENE_DIARY) sceneBeforeMenu = current;
            LoadScene(scene);
        }

        public void ReturnFromStore() => ReturnFromMenu();

        public void ReturnFromMenu()
        {
            if (!string.IsNullOrEmpty(sceneBeforeMenu)) LoadScene(sceneBeforeMenu);
            else GoToIndiaMap();
        }
        public const string SCENE_RAJASTHAN_MISSION_SEL = "BKS_Rajasthan_MissionSelect";

        // ── Session State (survives scene transitions) ────────────────────────────
        /// <summary>The MissionData that was last launched. Read by MissionResult screen.</summary>
        public MissionData ActiveMission { get; private set; }

        /// <summary>The MonumentData currently selected by the player.</summary>
        public MonumentData ActiveMonument { get; set; }

        /// <summary>The chapter (state) the player entered from the India Map. Read by Mission Select.</summary>
        public ChapterData ActiveChapter { get; private set; }

        // ── Runtime ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Load a scene by name directly.</summary>
        public void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[BKS_SceneLoader] Scene name is null or empty.");
                return;
            }
            Time.timeScale = 1f; // Ensure time scale is reset (Fixes Revive bugs)
            SceneManager.LoadScene(sceneName);
        }

        /// <summary>Load a mission's gameplay scene. Stores the active mission for result screen.</summary>
        public void LoadMission(MissionData data)
        {
            if (data == null)
            {
                Debug.LogError("[BKS_SceneLoader] LoadMission called with null MissionData.");
                return;
            }

            if (string.IsNullOrEmpty(data.GameplaySceneName))
            {
                Debug.LogError($"[BKS_SceneLoader] MissionData '{data.MissionName}' has no GameplaySceneName set.");
                return;
            }

            ActiveMission = data;
            Debug.Log($"[BKS_SceneLoader] Loading mission scene: {data.GameplaySceneName}");
            Time.timeScale = 1f;
            SceneManager.LoadScene(data.GameplaySceneName);
        }

        /// <summary>
        /// Enter a chapter from the India Map: remembers it as ActiveChapter and loads its scene.
        /// Returns false (and loads nothing) if the scene name is empty or not in Build Settings.
        /// </summary>
        public bool LoadChapter(ChapterData chapter)
        {
            if (chapter == null)
            {
                Debug.LogError("[BKS_SceneLoader] LoadChapter called with null ChapterData.");
                return false;
            }

            string sceneName = chapter.ChapterSceneName;
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError($"[BKS_SceneLoader] Chapter '{chapter.ChapterID}' has no Chapter Scene Name set.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[BKS_SceneLoader] Scene '{sceneName}' for chapter '{chapter.ChapterID}' is not in Build Settings (File → Build Profiles / Build Settings).");
                return false;
            }

            ActiveChapter = chapter;
            Debug.Log($"[BKS_SceneLoader] Entering chapter '{chapter.ChapterID}' → {sceneName}");
            LoadScene(sceneName);
            return true;
        }

        /// <summary>Back to the active chapter's Mission Select (or the India Map if there is none).</summary>
        public void ReturnToChapter()
        {
            if (ActiveChapter != null && LoadChapter(ActiveChapter)) return;
            GoToIndiaMap();
        }

        /// <summary>Replay the last launched mission (or reload the current scene if none is known).</summary>
        public void RetryActiveMission()
        {
            if (ActiveMission != null) LoadMission(ActiveMission);
            else ReloadCurrentScene();
        }

        /// <summary>Load India Map (top-level navigation).</summary>
        public void GoToIndiaMap()  => LoadScene(SCENE_INDIA_MAP);

        /// <summary>Load UP Monument Select.</summary>
        public void GoToUPMonumentSelect() => LoadScene(SCENE_UP_MONUMENT_SEL);

        /// <summary>Load UP Mission Select.</summary>
        public void GoToUPMissionSelect() => LoadScene(SCENE_UP_MISSION_SEL);

        /// <summary>Load Rajasthan Mission Select.</summary>
        public void GoToRajasthanMissionSelect() => LoadScene(SCENE_RAJASTHAN_MISSION_SEL);

        /// <summary>Go to Mission Result. Call this after mission ends.</summary>
        public void GoToMissionResult() => LoadScene(SCENE_MISSION_RESULT);

        /// <summary>Go to Garage.</summary>
        public void GoToGarage() => LoadScene(SCENE_GARAGE);

        /// <summary>Reload the currently active scene (Retry).</summary>
        public void ReloadCurrentScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Async scene load with optional loading screen support.</summary>
        public void LoadSceneAsync(string sceneName, System.Action onComplete = null)
        {
            StartCoroutine(LoadAsync(sceneName, onComplete));
        }

        private IEnumerator LoadAsync(string sceneName, System.Action onComplete)
        {
            // If we have a UI loading screen, use it
            if (UI.BKS_LoadingScreen.Instance != null)
            {
                yield return UI.BKS_LoadingScreen.Instance.LoadSceneWithUI(sceneName, onComplete);
            }
            else
            {
                // Fallback to basic load without UI
                Time.timeScale = 1f;
                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
                if (op == null)
                {
                    Debug.LogError($"[BKS_SceneLoader] Failed to load scene '{sceneName}'. Make sure it is added to Build Settings.");
                    yield break;
                }

                while (!op.isDone)
                    yield return null;

                onComplete?.Invoke();
            }
        }
    }
}
