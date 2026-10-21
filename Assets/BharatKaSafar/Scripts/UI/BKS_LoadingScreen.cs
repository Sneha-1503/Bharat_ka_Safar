using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Manages the loading screen UI for smooth transitions between scenes.
    /// Can be attached to a persistent canvas or instantiated by the SceneLoader.
    /// </summary>
    public class BKS_LoadingScreen : MonoBehaviour
    {
        public static BKS_LoadingScreen Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text loadingTipsText;

        [Header("Configuration")]
        [SerializeField] private string[] loadingTips = new string[] {
            "Use the horn to clear obstacles like cows!",
            "Upgrade your engine to climb steeper hills.",
            "Watch out for potholes that damage your vehicle.",
            "Collect Safar Stars to get a 3-star rating!"
        };
        [SerializeField] private float minLoadingTime = 1.0f; // Ensure loading screen is visible for at least 1s

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        public void ShowLoadingScreen()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (progressBar != null) progressBar.value = 0f;
                if (progressText != null) progressText.text = "0%";
                
                // Show random tip
                if (loadingTipsText != null && loadingTips.Length > 0)
                {
                    loadingTipsText.text = loadingTips[Random.Range(0, loadingTips.Length)];
                }
            }
        }

        public void HideLoadingScreen()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }

        public void UpdateProgress(float progress)
        {
            if (progressBar != null)
                progressBar.value = progress;
                
            if (progressText != null)
                progressText.text = Mathf.RoundToInt(progress * 100f) + "%";
        }

        /// <summary>
        /// Coroutine to handle the actual async loading with the UI.
        /// Expected to be called by BKS_SceneLoader.
        /// </summary>
        public IEnumerator LoadSceneWithUI(string sceneName, System.Action onComplete)
        {
            ShowLoadingScreen();
            
            float startTime = Time.unscaledTime;
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            
            if (op == null)
            {
                Debug.LogError($"[BKS_LoadingScreen] Failed to load scene '{sceneName}'.");
                HideLoadingScreen();
                yield break;
            }
            
            // Prevent auto-activation until minimum time has passed (for polish)
            op.allowSceneActivation = false;

            while (!op.isDone)
            {
                // Unity's progress stops at 0.9 when allowSceneActivation is false
                float progress = Mathf.Clamp01(op.progress / 0.9f);
                UpdateProgress(progress);

                if (op.progress >= 0.9f)
                {
                    // Check if minimum loading time has elapsed
                    if (Time.unscaledTime - startTime >= minLoadingTime)
                    {
                        op.allowSceneActivation = true;
                    }
                }
                
                yield return null;
            }
            
            HideLoadingScreen();
            onComplete?.Invoke();
        }
    }
}
