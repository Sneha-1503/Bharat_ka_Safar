using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Home
{
    /// <summary>
    /// The ONE reusable chapter panel on the right side of the Home page.
    ///
    /// It only DISPLAYS data. It does not decide what happens on click —
    /// it raises OnActionClicked and the Home controller decides (enter / unlock).
    ///
    /// Static art (frames, decoration, button background) stays in the scene.
    /// Everything state-specific (texts, images, status, price) is set here.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChapterDetailPanel : MonoBehaviour
    {
        // ── Content ───────────────────────────────────────────────────────────────
        [Header("Content")]
        [Tooltip("State name, e.g. UTTAR PRADESH")]
        [SerializeField] private TMP_Text titleText;
        [Tooltip("Optional: chapter title, e.g. Heritage & Heartland")]
        [SerializeField] private TMP_Text chapterTitleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Image    mainStateImage;
        [SerializeField] private bool     titleUppercase = true;

        [Header("Chapter progress (optional)")]
        [Tooltip("e.g. CHAPTER 01")]
        [SerializeField] private TMP_Text chapterNumberText;
        [SerializeField] private string   chapterNumberFormat = "CHAPTER {0:00}";
        [Tooltip("Image Type = Filled (Horizontal): fill = completed missions / all missions")]
        [SerializeField] private Image    progressFill;
        [Tooltip("e.g. Missions 2 / 5")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private string   progressFormat = "Missions  {0} / {1}";
        [Tooltip("e.g. ★ 9 / 15 (3 stars per mission)")]
        [SerializeField] private TMP_Text starsText;
        [SerializeField] private string   starsFormat = "Stars  {0} / {1}";

        [Header("Famous Landmarks (up to 5)")]
        [SerializeField] private Image[]      landmarkImages = new Image[0];
        [Tooltip("Optional: frame/root object per slot. If set, the whole slot is hidden when empty.")]
        [SerializeField] private GameObject[] landmarkSlots  = new GameObject[0];

        // ── Status ────────────────────────────────────────────────────────────────
        [Header("Status Area")]
        [SerializeField] private GameObject lockIcon;
        [SerializeField] private TMP_Text   statusText;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private TMP_Text   unlockCostText;
        [SerializeField] private GameObject completedBadge;

        // ── Action button ─────────────────────────────────────────────────────────
        [Header("Action Button (one button, changes behaviour)")]
        [SerializeField] private Button     actionButton;
        [SerializeField] private TMP_Text   actionButtonText;
        [Tooltip("Optional: button art with ENTER painted on it (text hidden while it is shown)")]
        [SerializeField] private Sprite     enterSprite;
        [Tooltip("Optional: plain button art used for UNLOCK / UPCOMING (text shown on it)")]
        [SerializeField] private Sprite     plainButtonSprite;
        [Tooltip("Optional coin icon inside the button, shown only for UNLOCK")]
        [SerializeField] private GameObject actionCoinIcon;

        // ── Feedback ──────────────────────────────────────────────────────────────
        [Header("Feedback (optional)")]
        [Tooltip("Short message like NOT ENOUGH COINS. Hidden when not in use.")]
        [SerializeField] private TMP_Text feedbackText;

        // ── Labels (editable in Inspector) ────────────────────────────────────────
        [Header("Labels")]
        [SerializeField] private string labelEnter          = "ENTER CHAPTER";
        [Tooltip("{0} = cost")]
        [SerializeField] private string labelUnlockFormat   = "UNLOCK {0}";
        [SerializeField] private string labelUpcoming       = "UPCOMING";
        [SerializeField] private string statusUnlocked      = "UNLOCKED";
        [SerializeField] private string statusLocked        = "LOCKED CHAPTER";
        [SerializeField] private string statusCompleted     = "COMPLETED";
        [SerializeField] private string statusUpcoming      = "UPCOMING";
        [SerializeField][TextArea] private string upcomingFallbackDescription = "This chapter is coming soon. Stay tuned for the journey!";

        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Raised when the action button is pressed. The controller decides what it means.</summary>
        public event Action OnActionClicked;

        private Coroutine _feedbackRoutine;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(RaiseActionClicked);
                actionButton.onClick.AddListener(RaiseActionClicked);
            }

            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (actionButton != null) actionButton.onClick.RemoveListener(RaiseActionClicked);
        }

        private void RaiseActionClicked() => OnActionClicked?.Invoke();

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Fill the panel for one state.
        /// </summary>
        /// <param name="data">Chapter data. May be null for a map state that has no data yet.</param>
        /// <param name="progress">Progress snapshot from the save system.</param>
        /// <param name="status">Resolved status (Upcoming / Locked / Unlocked / Completed).</param>
        /// <param name="fallbackName">Name to show when data is null (from the map button).</param>
        public void SetData(ChapterData data, StateProgressData progress, ChapterStatus status, string fallbackName)
        {
            // Title(s)
            string title = data != null ? data.DisplayName : fallbackName;
            SetText(titleText, titleUppercase && title != null ? title.ToUpperInvariant() : title);
            SetText(chapterTitleText, data != null ? data.ChapterTitle : string.Empty);

            // Description
            string description = data != null ? data.Description : null;
            if (string.IsNullOrEmpty(description) && status == ChapterStatus.Upcoming)
                description = upcomingFallbackDescription;
            SetText(descriptionText, description);

            // Images
            SetImage(mainStateImage, data != null ? data.MainImage : null);
            SetLandmarks(data != null ? data.LandmarkImages : null);

            // Chapter number + progress
            RefreshProgress(data);

            // Status + button
            RefreshStatus(status, data != null ? data.UnlockCost : 0);
            HideFeedback();
        }

        /// <summary>Chapter number (order in the chapter list), missions done and stars earned in this chapter.</summary>
        public void RefreshProgress(ChapterData data)
        {
            if (data == null)
            {
                SetText(chapterNumberText, string.Empty); SetText(progressText, string.Empty); SetText(starsText, string.Empty);
                if (progressFill != null) progressFill.fillAmount = 0f;
                return;
            }
            var registry = BharatKaSafar.Diary.BKS_ChapterRegistry.Load();
            int number = registry != null ? registry.chapters.IndexOf(data) + 1 : 0;
            SetText(chapterNumberText, number > 0 ? string.Format(chapterNumberFormat, number) : string.Empty);

            var save = BKS_SaveManager.Instance;
            int total = data.MissionCount, done = 0, stars = 0;
            for (int i = 0; i < total; i++)
            {
                var m = data.GetMission(i);
                if (m == null || save == null) continue;
                int r = save.GetMissionBestRating(m.MissionID);
                if (r > 0) done++;
                stars += Mathf.Clamp(r, 0, 3);
            }
            SetText(progressText, string.Format(progressFormat, done, total));
            SetText(starsText, string.Format(starsFormat, stars, total * 3));
            if (progressFill != null) progressFill.fillAmount = total > 0 ? done / (float)total : 0f;
        }

        /// <summary>Update only the status area and action button (e.g. right after unlocking).</summary>
        public void RefreshStatus(ChapterStatus status, int unlockCost)
        {
            bool isLocked    = status == ChapterStatus.Locked;
            bool isUpcoming  = status == ChapterStatus.Upcoming;
            bool isCompleted = status == ChapterStatus.Completed;

            // Status area
            SetActive(lockIcon, isLocked);
            SetActive(coinIcon, isLocked);
            SetActive(unlockCostText != null ? unlockCostText.gameObject : null, isLocked);
            if (isLocked) SetText(unlockCostText, unlockCost.ToString("N0"));

            switch (status)
            {
                case ChapterStatus.Upcoming:  SetText(statusText, statusUpcoming);  break;
                case ChapterStatus.Locked:    SetText(statusText, statusLocked);    break;
                case ChapterStatus.Completed: SetText(statusText, statusCompleted); break;
                default:                      SetText(statusText, statusUnlocked);  break;
            }

            SetActive(completedBadge, isCompleted);

            // Action button
            if (isUpcoming)
                SetText(actionButtonText, labelUpcoming);
            else if (isLocked)
                SetText(actionButtonText, string.Format(labelUnlockFormat, unlockCost.ToString("N0")));
            else
                SetText(actionButtonText, labelEnter); // Unlocked AND Completed → can enter / replay

            SetActive(actionCoinIcon, isLocked);

            // button art: ENTER art when the chapter can be entered, plain art with text otherwise
            var img = actionButton != null ? actionButton.targetGraphic as Image : null;
            bool canEnter = !isLocked && !isUpcoming;
            if (img != null && enterSprite != null && canEnter) { img.sprite = enterSprite; img.preserveAspect = true; }
            else if (img != null && plainButtonSprite != null) { img.sprite = plainButtonSprite; img.preserveAspect = true; }
            if (actionButtonText != null) actionButtonText.gameObject.SetActive(!(canEnter && enterSprite != null));

            if (actionButton != null)
                actionButton.interactable = !isUpcoming;
        }

        /// <summary>Show a short message (e.g. NOT ENOUGH COINS). Does nothing if no feedback text is assigned.</summary>
        public void ShowFeedback(string message, float duration = 2f)
        {
            if (feedbackText == null) return;

            if (_feedbackRoutine != null) StopCoroutine(_feedbackRoutine);
            feedbackText.text = message;
            feedbackText.gameObject.SetActive(true);

            if (isActiveAndEnabled)
                _feedbackRoutine = StartCoroutine(HideFeedbackAfter(duration));
        }

        public void HideFeedback()
        {
            if (_feedbackRoutine != null)
            {
                StopCoroutine(_feedbackRoutine);
                _feedbackRoutine = null;
            }
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private IEnumerator HideFeedbackAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            _feedbackRoutine = null;
            if (feedbackText != null) feedbackText.gameObject.SetActive(false);
        }

        private void SetLandmarks(IReadOnlyList<Sprite> sprites)
        {
            if (landmarkImages == null) return;

            for (int i = 0; i < landmarkImages.Length; i++)
            {
                Sprite sprite = (sprites != null && i < sprites.Count) ? sprites[i] : null;
                bool hasSprite = sprite != null;

                Image img = landmarkImages[i];
                if (img != null)
                {
                    img.sprite = sprite;
                    img.preserveAspect = true;
                    img.gameObject.SetActive(hasSprite);
                }

                // Hide the whole slot (frame) too, if one is assigned
                if (landmarkSlots != null && i < landmarkSlots.Length && landmarkSlots[i] != null)
                    landmarkSlots[i].SetActive(hasSprite);
            }
        }

        /// <summary>Missing sprite → image is hidden instead of showing a white box.</summary>
        private static void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            image.sprite  = sprite;
            image.enabled = sprite != null;
            if (sprite != null) image.preserveAspect = true;
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null) text.text = value ?? string.Empty;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null && go.activeSelf != active) go.SetActive(active);
        }
    }
}
