using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Home
{
    /// <summary>Navigation only. All visuals are saved in the Unity scenes.</summary>
    public class BKS_MainMenu : MonoBehaviour
    {
        [SerializeField] private int screen;
        [Header("Pages")]
        [SerializeField] private GameObject homePage;
        [SerializeField] private GameObject settingsPage;
        [SerializeField] private GameObject collectionPage;
        [SerializeField] private GameObject storePage;
        [SerializeField] private GameObject challengesPage;
        [SerializeField] private GameObject quitPage;
        [Header("Live labels")]
        [SerializeField] private TMP_Text coinsLabel;
        [SerializeField] private TMP_Text musicLabel;
        [SerializeField] private UI.BKS_AudioSettingsPopup settingsPopup;
        [Header("Splash")]
        [SerializeField] private TMP_Text loadingLabel;
        [SerializeField] private Image loadingFill;
        [SerializeField, Min(0f)] private float minimumSplashSeconds = 1.5f;
        private GameObject currentPage;

        private void Awake()
        {
            // the old scene-only settings popup is not used any more (it looked different from the shared one)
            if (settingsPopup != null) settingsPopup.gameObject.SetActive(false);
            Time.timeScale = 1f;
            HomeManage.isLoad = true;
            if (AudioManager.instance == null)
            {
                var audio = Resources.Load<GameObject>("BKS_HomeAudio");
                if (audio != null) Instantiate(audio);
            }
            if (screen != 2 && FindFirstObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
            if (BKS_SaveManager.Instance == null || BKS_SceneLoader.Instance == null)
                new GameObject("BKS Persistent Managers").AddComponent<BKS_PersistentManagers>();
        }

        private void Start()
        {
            if (screen == 1) StartCoroutine(LoadHome());
            else if (screen == 0) ShowHome();
        }

        private void Update()
        {
            if (UI.BKS_AudioSettingsPopup.BlocksNavigation) return;
            if (!Input.GetKeyDown(KeyCode.Escape) || screen == 1) return;
            if (screen == 2) GoHome();
            else if (currentPage == homePage) ShowQuit();
            else ShowHome();
        }

        private void ShowPage(GameObject selected)
        {
            foreach (var page in new[] { homePage, settingsPage, collectionPage, storePage, challengesPage, quitPage })
                if (page != null) page.SetActive(page == selected);
            currentPage = selected;
        }

        public void ShowHome()
        {
            ShowPage(homePage);
            if (coinsLabel != null) coinsLabel.text = "Coins: " + PlayerPrefs.GetInt("Coins", 0);
        }
        public void StartJourney() => Load(BKS_SceneLoader.SCENE_INDIA_MAP);
        public void GoHome() => Load(BKS_SceneLoader.SCENE_HOME);
        // every screen (home, map, pause) opens the same shared popup: Resources/BKS_AudioSettingsPopup
        public void ShowSettings() => UI.BKS_AudioSettingsPopup.Open();
        // COLLECTION = Safar Diary (its own scene)
        public void ShowCollection()
        {
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToDiary();
            else ShowPage(collectionPage);
        }
        // The real Store is its own scene (BKS_Store); Back there returns to Home
        public void ShowStore()
        {
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToStore();
            else ShowPage(storePage);
        }
        public void ShowChallenges()
        {
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToChallenges();
            else ShowPage(challengesPage);
        }
        public void ShowQuit() => ShowPage(quitPage);

        public void ToggleMusic()
        {
            bool enabled = PlayerPrefs.GetFloat("MusicVolume", 1f) > 0f;
            float volume = enabled ? 0f : PlayerPrefs.GetFloat("BKS_LastMusicVolume", 1f);
            if (enabled) PlayerPrefs.SetFloat("BKS_LastMusicVolume", PlayerPrefs.GetFloat("MusicVolume", 1f));
            if (!enabled && volume <= 0f) volume = 1f;
            PlayerPrefs.SetFloat("MusicVolume", volume);
            if (AudioManager.instance != null) AudioManager.instance.SetMusicVolume(volume);
            PlayerPrefs.Save();
            RefreshMusicLabel();
        }

        private void RefreshMusicLabel()
        {
            if (musicLabel != null) musicLabel.text = PlayerPrefs.GetFloat("MusicVolume", 1f) > 0f ? "MUSIC: ON" : "MUSIC: OFF";
        }

        public void QuitGame()
        {
            PlayerPrefs.Save();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadHome()
        {
            var load = SceneManager.LoadSceneAsync(BKS_SceneLoader.SCENE_HOME);
            if (load == null) yield break;
            load.allowSceneActivation = false;
            float started = Time.realtimeSinceStartup;
            while (load.progress < 0.9f || Time.realtimeSinceStartup - started < minimumSplashSeconds)
            {
                float elapsed = minimumSplashSeconds > 0f ? Mathf.Clamp01((Time.realtimeSinceStartup - started) / minimumSplashSeconds) : 1f;
                float progress = Mathf.Min(load.progress / 0.9f, elapsed);
                if (loadingFill != null) loadingFill.fillAmount = progress;
                if (loadingLabel != null) loadingLabel.text = "Loading... " + Mathf.RoundToInt(progress * 100) + "%";
                yield return null;
            }
            if (loadingFill != null) loadingFill.fillAmount = 1f;
            if (loadingLabel != null) loadingLabel.text = "Loading... 100%";
            load.allowSceneActivation = true;
        }

        private void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
