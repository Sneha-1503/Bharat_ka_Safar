using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Mission Result Screen UI (scene: BKS_MissionResult).
    /// Reads BKS_MissionResultStore, filled by BKS_MissionResult in the gameplay scene.
    /// Saving already happened there — this screen only displays and navigates.
    ///
    /// Buttons:
    ///   Continue  → completed: back to the chapter's Mission Select (next mission is now unlocked)
    ///               chapter completed: back to the India Map (state shows green)
    ///   Retry     → replay the same mission
    ///   India Map → Home / India Map
    /// </summary>
    public class BKS_MissionResultScreenUI : MonoBehaviour
    {
        [Header("Result Display")]
        [SerializeField] private TMP_Text missionNameText;
        [SerializeField] private TMP_Text resultStatusText;   // MISSION COMPLETE! / MISSION FAILED / CHAPTER COMPLETE!
        [SerializeField] private TMP_Text coinsEarnedText;
        [SerializeField] private TMP_Text safarStarsEarnedText;
        [SerializeField] private TMP_Text finalHealthText;
        [Tooltip("Optional: e.g. 'Uttar Pradesh  3 / 5 missions'")]
        [SerializeField] private TMP_Text chapterProgressText;

        [Header("Star Rating Display")]
        [SerializeField] private Image[] starImages;
        [SerializeField] private Sprite  starFilled;
        [Tooltip("Optional. If empty, unearned stars use the same sprite, tinted dark.")]
        [SerializeField] private Sprite  starEmpty;
        [SerializeField] private Color   starEmptyTint = new Color(0.25f, 0.25f, 0.25f, 0.8f);

        [Header("Buttons")]
        [Tooltip("Continue / Next")]
        [SerializeField] private Button   nextMissionButton;
        [SerializeField] private TMP_Text nextMissionButtonText;
        [SerializeField] private Button   retryButton;
        [SerializeField] private Button   garageButton;   // vehicle upgrades are out of scope → hidden
        [SerializeField] private Button   indiaMapButton;

        [Header("Panels")]
        [SerializeField] private GameObject successPanel;
        [SerializeField] private GameObject failurePanel;
        [SerializeField] private GameObject chapterCompletePanel;

        [Header("Labels")]
        [SerializeField] private string labelMissionComplete = "MISSION COMPLETE!";
        [SerializeField] private string labelMissionFailed   = "MISSION FAILED";
        [Tooltip("{0} = state name")]
        [SerializeField] private string labelChapterComplete = "{0} COMPLETED!";
        [SerializeField] private string labelContinue        = "CONTINUE";
        [SerializeField] private string labelBackToMap       = "BACK TO MAP";

        private bool _chapterCompleted;

        private void Start()
        {
            if (finalHealthText != null)
                finalHealthText.gameObject.SetActive(Vehicle.BKS_VehicleHealth.HealthSystemEnabled);
            if (BKS_MissionResultStore.HasResult) Time.timeScale = 0f;
            DisplayResult();
        }

        private void DisplayResult()
        {
            if (!BKS_MissionResultStore.HasResult)
            {
                Debug.LogWarning("[BKS_MissionResultScreenUI] No result stored. Did you come from a gameplay scene?");
                SetupButtons(completed: false);
                return;
            }

            var result = BKS_MissionResultStore.LastResult;
            _chapterCompleted = result.MissionCompleted && result.ChapterCompleted;

            if (missionNameText != null) missionNameText.text = result.MissionName;

            if (resultStatusText != null)
            {
                if (_chapterCompleted)
                    resultStatusText.text = string.Format(labelChapterComplete, (result.ChapterName ?? string.Empty).ToUpperInvariant());
                else if (result.MissionCompleted)
                    resultStatusText.text = labelMissionComplete;
                else
                    resultStatusText.text = string.IsNullOrEmpty(result.FailReason)
                        ? labelMissionFailed
                        : $"{labelMissionFailed}\n<size=60%>{result.FailReason}</size>";
            }

            if (chapterProgressText != null)
            {
                chapterProgressText.gameObject.SetActive(result.ChapterMissionCount > 0);
                chapterProgressText.text = $"{result.ChapterName}   {result.ChapterMissionsDone} / {result.ChapterMissionCount} missions";
            }

            if (successPanel != null)         successPanel.SetActive(result.MissionCompleted);
            if (failurePanel != null)         failurePanel.SetActive(!result.MissionCompleted);
            if (chapterCompletePanel != null) chapterCompletePanel.SetActive(_chapterCompleted);

            if (coinsEarnedText      != null) coinsEarnedText.text      = $"+{result.CoinsCollected}";
            if (safarStarsEarnedText != null) safarStarsEarnedText.text = $"+{result.SafarStarsEarned}";
            if (finalHealthText      != null) finalHealthText.text      = $"Health: {Mathf.RoundToInt(result.FinalHealthRatio * 100f)}%";

            UpdateStarDisplay(result.StarRating);
            SetupButtons(result.MissionCompleted);

            BKS_MissionResultStore.Clear();
        }

        private void UpdateStarDisplay(int rating)
        {
            if (starImages == null) return;
            for (int i = 0; i < starImages.Length; i++)
            {
                Image img = starImages[i];
                if (img == null) continue;

                bool earned = i < rating;
                if (earned)
                {
                    if (starFilled != null) img.sprite = starFilled;
                    img.color = Color.white;
                }
                else if (starEmpty != null)
                {
                    img.sprite = starEmpty;
                    img.color  = Color.white;
                }
                else
                {
                    img.color = starEmptyTint;
                }
            }
        }

        private void SetupButtons(bool completed)
        {
            if (nextMissionButton != null)
            {
                nextMissionButton.gameObject.SetActive(completed);
                nextMissionButton.onClick.RemoveAllListeners();
                nextMissionButton.onClick.AddListener(OnContinue);
                if (nextMissionButtonText != null)
                    nextMissionButtonText.text = _chapterCompleted ? labelBackToMap : labelContinue;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveAllListeners();
                retryButton.onClick.AddListener(OnRetry);
            }

            if (indiaMapButton != null)
            {
                indiaMapButton.onClick.RemoveAllListeners();
                indiaMapButton.onClick.AddListener(OnIndiaMap);
            }

            if (garageButton != null) garageButton.gameObject.SetActive(false);
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void OnContinue()
        {
            Time.timeScale = 1f;
            var loader = BKS_SceneLoader.Instance;
            if (loader == null) { UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP); return; }

            if (_chapterCompleted) loader.GoToIndiaMap();   // see the state turn green
            else                   loader.ReturnToChapter(); // Mission Select, next mission unlocked
        }

        private void OnRetry()
        {
            Time.timeScale = 1f;
            var loader = BKS_SceneLoader.Instance;
            if (loader != null) loader.RetryActiveMission();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }

        private void OnIndiaMap()
        {
            Time.timeScale = 1f;
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToIndiaMap();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP);
        }
    }
}
