using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// In-game pause menu: Resume, Restart, Settings, Home.
    /// Home asks for confirmation first because leaving the mission loses this run's progress.
    /// Lives in the gameplay Canvas (added by Bharat Ka Safar > UI > Add Pause + Stage Panels) so it stays editable.
    /// </summary>
    public class BKS_PausePanel : MonoBehaviour
    {
        public static BKS_PausePanel Instance { get; private set; }

        [Header("Panels")]
        [Tooltip("Root shown while paused (dim background + panel). Starts hidden.")]
        [SerializeField] private GameObject pauseRoot;
        [Tooltip("The panel that pops in (scaled from 0).")]
        [SerializeField] private RectTransform popupPanel;
        [Tooltip("'Mission chhodein?' confirmation shown before going Home.")]
        [SerializeField] private GameObject quitConfirmRoot;

        [Header("Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button homeButton;
        [SerializeField] private Button quitYesButton;
        [SerializeField] private Button quitNoButton;

        [Header("Designed pause prefab")]
        [Tooltip("If the scene has this object (the 'Pause Menu' prefab), it is used as the pause screen instead of the panels above. Its buttons are found by name: Play Button, Restart Button, Home Button, Close Button.")]
        [SerializeField] private string designedPauseName = "Pause Menu";
        [Tooltip("Height of the pause frame as a part of the screen height (0 = keep the prefab's size)")]
        [SerializeField, Range(0f, 1f)] private float frameScreenHeight = 0.7f;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float popTime = 0.25f;

        public bool IsPaused { get; private set; }
        private float baseScale = 1f;
        private bool designedInUse;

        private void Awake()
        {
            Instance = this;
            UseDesignedPrefab();
            if (pauseRoot != null) pauseRoot.SetActive(false);
            if (quitConfirmRoot != null) quitConfirmRoot.SetActive(false);
            Bind(resumeButton, Resume);
            Bind(restartButton, Restart);
            Bind(settingsButton, () => BKS_AudioSettingsPopup.Open());
            Bind(homeButton, AskQuit);
            Bind(quitYesButton, GoHome);
            Bind(quitNoButton, () => quitConfirmRoot.SetActive(false));
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsPaused) Time.timeScale = 1f;
        }

        // the 'Pause Menu' prefab designed in the editor replaces the built-in panels
        private void UseDesignedPrefab()
        {
            if (string.IsNullOrEmpty(designedPauseName)) return;
            var designed = FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(t => t.name == designedPauseName && t.gameObject.scene == gameObject.scene);
            if (designed == null) return;

            designedInUse = true;
            var oldRoot = pauseRoot;
            pauseRoot = designed.gameObject;
            var frame = Find(designed, "Pause Frame");
            popupPanel = frame != null ? frame : designed;
            resumeButton  = Btn(designed, "Play Button");
            restartButton = Btn(designed, "Restart Button");
            homeButton    = Btn(designed, "Home Button");
            var close = Btn(designed, "Close Button");
            if (close != null) { close.onClick.RemoveAllListeners(); close.onClick.AddListener(Resume); }
            // the prefab's Settings button keeps its own OnClick (OpenSettings)

            // the 'leave this mission?' confirmation: the shared TealTravel popup (Resources/BKS_QuitConfirmPopup),
            // or else the old built-in one moved under the designed screen
            var confirmPrefab = Resources.Load<GameObject>("BKS_QuitConfirmPopup");
            var canvas = designed.GetComponentInParent<Canvas>(true);
            if (confirmPrefab != null && canvas != null)
            {
                var confirm = Instantiate(confirmPrefab, canvas.rootCanvas.transform, false);
                confirm.name = "BKS_QuitConfirmPopup";
                quitConfirmRoot = confirm;
                quitYesButton = Find(confirm.transform, "YesButton")?.GetComponent<Button>();
                quitNoButton = Find(confirm.transform, "NoButton")?.GetComponent<Button>();
            }
            else if (quitConfirmRoot != null && oldRoot != null && quitConfirmRoot.transform.IsChildOf(oldRoot.transform))
            {
                quitConfirmRoot.transform.SetParent(designed, false);
                quitConfirmRoot.transform.SetAsLastSibling();
            }
            // the prefab's saved OnClick calls go to the old legacy Pause script (its panel is empty → NullReference).
            // Resume / Restart / Home / Close are handled here, so switch all their saved calls off;
            // other buttons only lose calls whose target is gone (Settings keeps OpenSettings).
            var ours = new[] { resumeButton, restartButton, homeButton, close };
            foreach (var b in designed.GetComponentsInChildren<Button>(true))
                for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
                    if (ours.Contains(b) || b.onClick.GetPersistentTarget(i) == null)
                        b.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);

            if (oldRoot != null && oldRoot != pauseRoot) oldRoot.SetActive(false);
            designed.SetAsLastSibling(); // drawn on top of the HUD
        }

        // the designed frame fills frameScreenHeight of the screen (measured when pausing, the layout is ready then)
        private void FitFrame()
        {
            if (frameScreenHeight <= 0f || popupPanel == null || pauseRoot == null) return;
            var canvas = popupPanel.GetComponentInParent<Canvas>(true);
            var canvasRt = canvas != null ? (RectTransform)canvas.rootCanvas.transform : null;
            float h = popupPanel.rect.height * Mathf.Abs(popupPanel.parent != null ? popupPanel.parent.lossyScale.y : 1f);
            float screenH = canvasRt != null ? canvasRt.rect.height * canvasRt.lossyScale.y : Screen.height;
            if (h > 1f) baseScale = Mathf.Clamp(screenH * frameScreenHeight / h, 0.3f, 4f);
        }

        private static RectTransform Find(Transform root, string name) =>
            root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t => t.name == name);

        private static Button Btn(Transform root, string name)
        {
            var t = Find(root, name);
            var b = t != null ? t.GetComponent<Button>() : null;
            if (b != null) b.onClick.RemoveAllListeners(); // drop the prefab's empty / legacy calls; ours are added in Awake
            return b;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null) button.onClick.AddListener(action);
        }

        public void Toggle()
        {
            if (IsPaused) Resume(); else Pause();
        }

        public void Pause()
        {
            if (IsPaused || pauseRoot == null) return;
            IsPaused = true;
            Time.timeScale = 0f;
            pauseRoot.SetActive(true);
            if (designedInUse) FitFrame();
            if (quitConfirmRoot != null) quitConfirmRoot.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(Pop(0f, 1f, null));
        }

        public void Resume()
        {
            if (!IsPaused) return;
            StopAllCoroutines();
            StartCoroutine(Pop(1f, 0f, () =>
            {
                pauseRoot.SetActive(false);
                IsPaused = false;
                Time.timeScale = 1f;
            }));
        }

        private void Restart()
        {
            Unpause();
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.RetryActiveMission();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void AskQuit()
        {
            if (quitConfirmRoot != null) { quitConfirmRoot.SetActive(true); quitConfirmRoot.transform.SetAsLastSibling(); }
            else GoHome();
        }

        private void GoHome()
        {
            Unpause();
            // Back to this chapter's Mission Select (India Map if no chapter is known)
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.ReturnToChapter();
        }

        private void Unpause()
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }

        private IEnumerator Pop(float from, float to, System.Action done)
        {
            if (popupPanel != null)
            {
                for (float t = 0f; t < popTime; t += Time.unscaledDeltaTime)
                {
                    float k = t / popTime;
                    // ease-out-back (small overshoot) when opening, ease-in when closing
                    float e = to > from
                        ? 1f + 2.70158f * Mathf.Pow(k - 1f, 3) + 1.70158f * Mathf.Pow(k - 1f, 2)
                        : k * k;
                    popupPanel.localScale = Vector3.one * (baseScale * Mathf.LerpUnclamped(from, to, e));
                    yield return null;
                }
                popupPanel.localScale = Vector3.one * (baseScale * to);
            }
            done?.Invoke();
        }
    }
}
