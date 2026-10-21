using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Mission Select screen for the Rajasthan chapter.
    /// Reads ChapterData to populate 5 mission cards dynamically.
    /// Follows the same card-based pattern as BKS_MissionSelectUI (UP chapter).
    /// 
    /// Clicking a card opens the Mission Briefing popup. Confirmed start loads
    /// the level via BKS_SceneLoader. Progression unlocks are driven by BKS_SaveManager.
    /// </summary>
    public class BKS_RajasthanMissionSelectUI : MonoBehaviour
    {
        // ── Mission Card Definition ────────────────────────────────────────────────
        [System.Serializable]
        public class MissionCard
        {
            public GameObject   root;
            public Button       playButton;
            public TMP_Text     missionNumberText;
            public TMP_Text     missionNameText;
            public TMP_Text     locationText;
            public TMP_Text     storyText;
            public Image        thumbnailImage;
            public GameObject   lockOverlay;
            public TMP_Text     lockReasonText;

            // 3-star rating display
            public Image[] starImages;
            public Sprite  starFilled;
            public Sprite  starEmpty;

            [HideInInspector] public MissionData boundMission;
        }

        // ── Mission Briefing Popup ────────────────────────────────────────────────
        [System.Serializable]
        public class MissionBriefingPopup
        {
            public GameObject   panel;
            public TMP_Text     titleText;
            public TMP_Text     locationText;
            public TMP_Text     storyText;
            public TMP_Text     objectiveText;
            public TMP_Text     difficultyText;
            public Button       startButton;
            public Button       closeButton;
        }

        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Chapter Data")]
        [SerializeField] private ChapterData chapterData;

        [Header("Chapter Header")]
        [SerializeField] private TMP_Text chapterTitleText;
        [SerializeField] private TMP_Text chapterSubtitleText;

        [Header("Mission Cards")]
        [SerializeField] private List<MissionCard> missionCards = new List<MissionCard>();

        [Header("Mission Briefing Popup")]
        [SerializeField] private MissionBriefingPopup briefingPopup;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private MissionData _selectedMission;
        private BKS_MissionCarousel carousel;

        private static readonly string[] DifficultyLabels =
            { "Easy", "Easy-Medium", "Medium", "Hard", "Hardest — Boss Level" };

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Start()
        {
            if (BKS_SaveManager.Instance == null)
                new GameObject("BKS Save Manager").AddComponent<BKS_SaveManager>();
            var sharedPopup = BKS_MissionBriefingView.Create(briefingPopup.panel, transform);
            if (sharedPopup != null)
            {
                briefingPopup.panel = sharedPopup.gameObject;
                briefingPopup.titleText = sharedPopup.titleText;
                briefingPopup.storyText = sharedPopup.storyText;
                briefingPopup.objectiveText = sharedPopup.objectiveText;
                briefingPopup.difficultyText = sharedPopup.difficultyText;
                briefingPopup.locationText = null;
                briefingPopup.startButton = sharedPopup.startButton;
                briefingPopup.closeButton = sharedPopup.closeButton;
            }
            if (chapterData == null)
            {
                Debug.LogError("[BKS_RajasthanMissionSelectUI] No ChapterData assigned! Drag RajasthanChapter.asset into the Inspector.", this);
                return;
            }

            SetupHeader();
            PopulateCards();
            carousel = BKS_MissionCarousel.Setup(missionCards.ConvertAll(card => card.root));

            if (briefingPopup.panel != null) briefingPopup.panel.SetActive(false);
            if (briefingPopup.closeButton != null)  briefingPopup.closeButton.onClick.AddListener(OnCloseBriefing);
            if (briefingPopup.startButton != null)  briefingPopup.startButton.onClick.AddListener(OnStartMission);
            if (backButton != null)                 backButton.onClick.AddListener(OnBackPressed);
        }

        // ── Setup ─────────────────────────────────────────────────────────────────

        private void SetupHeader()
        {
            if (chapterTitleText    != null) chapterTitleText.text    = chapterData.ChapterTitle.ToUpper();
            if (chapterSubtitleText != null) chapterSubtitleText.text = chapterData.Subtitle;
        }

        private void PopulateCards()
        {
            for (int i = 0; i < missionCards.Count; i++)
            {
                var card = missionCards[i];
                MissionData data = chapterData.GetMission(i);

                if (data == null)
                {
                    if (card.root != null) card.root.SetActive(false);
                    continue;
                }

                if (card.root != null) card.root.SetActive(true);
                card.boundMission = data;

                bool unlocked = BKS_SaveManager.Instance != null &&
                                BKS_SaveManager.Instance.IsMissionUnlocked(chapterData, i);

                // Text
                if (card.missionNumberText != null) card.missionNumberText.text = $"MISSION {data.MissionNumber}";
                if (card.missionNameText   != null) card.missionNameText.text   = data.MissionName;
                if (card.storyText         != null) card.storyText.text         = data.Description;
                if (card.thumbnailImage    != null && data.ThumbnailSprite != null)
                    card.thumbnailImage.sprite = data.ThumbnailSprite;

                // Lock state
                if (card.lockOverlay != null) card.lockOverlay.SetActive(!unlocked);
                if (card.playButton  != null) card.playButton.interactable = unlocked;

                // Star rating
                int best = BKS_SaveManager.Instance != null
                    ? BKS_SaveManager.Instance.GetMissionBestRating(data.MissionID) : 0;
                ApplyStars(card, best);

                // Click — open briefing popup
                if (card.playButton != null)
                {
                    int captured = i;
                    card.playButton.onClick.RemoveAllListeners();
                    card.playButton.onClick.AddListener(() => OnCardClicked(captured));
                }
            }
        }

        private void ApplyStars(MissionCard card, int rating)
        {
            if (card.starImages == null) return;
            for (int s = 0; s < card.starImages.Length; s++)
            {
                if (card.starImages[s] == null) continue;
                card.starImages[s].sprite = (s < rating) ? card.starFilled : card.starEmpty;
            }
        }

        // ── Card Click → Briefing Popup ──────────────────────────────────────────

        private void OnCardClicked(int index)
        {
            var card = missionCards[index];
            if (card.boundMission == null) return;
            if (carousel != null && !carousel.Focus(card.root)) return;

            _selectedMission = card.boundMission;

            if (briefingPopup.panel == null) { LaunchMission(); return; }

            briefingPopup.panel.SetActive(true);

            int mNum = _selectedMission.MissionNumber;
            string diffLabel = (mNum >= 1 && mNum <= DifficultyLabels.Length)
                ? DifficultyLabels[mNum - 1] : "Unknown";

            if (briefingPopup.titleText    != null) briefingPopup.titleText.text    = _selectedMission.MissionName;
            if (briefingPopup.storyText    != null) briefingPopup.storyText.text    = _selectedMission.StoryText;
            if (briefingPopup.objectiveText!= null) briefingPopup.objectiveText.text= $"Objective: {_selectedMission.PrimaryObjectiveText}";
            if (briefingPopup.difficultyText!= null)briefingPopup.difficultyText.text = $"Difficulty: {diffLabel}";
        }

        // ── Briefing Popup Actions ────────────────────────────────────────────────

        private void OnStartMission()
        {
            briefingPopup.panel?.SetActive(false);
            LaunchMission();
        }

        private void OnCloseBriefing()
        {
            briefingPopup.panel?.SetActive(false);
            _selectedMission = null;
        }

        private void LaunchMission()
        {
            if (_selectedMission == null) return;

            Debug.Log($"[BKS_RajasthanMissionSelectUI] Launching: {_selectedMission.MissionName}");

            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.LoadMission(_selectedMission);
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(_selectedMission.GameplaySceneName);
        }

        private void OnBackPressed()
        {
            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.GoToIndiaMap();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP);
        }
    }
}
