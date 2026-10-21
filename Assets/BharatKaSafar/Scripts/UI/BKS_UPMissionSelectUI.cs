using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Mission Select screen — used by EVERY chapter (the name is historical).
    ///
    /// Which chapter? BKS_SceneLoader.ActiveChapter (set by ENTER CHAPTER on the India Map).
    /// The Inspector "Chapter Data" is only a fallback for testing the scene directly.
    ///
    /// Locking is data-driven (BKS_SaveManager.IsMissionUnlocked(chapter, index)):
    ///   mission 1 always playable, mission N unlocks when mission N-1 is completed.
    /// Features card list, lock states, 3-star rating display, and briefing popup.
    /// </summary>
    public class BKS_UPMissionSelectUI : MonoBehaviour
    {
        [System.Serializable]
        public class MissionCard
        {
            public GameObject   root;
            public Button       playButton;
            public TMP_Text     missionNumberText;
            public TMP_Text     missionNameText;
            public TMP_Text     objectiveText;
            public Image        thumbnailImage;
            public GameObject   lockOverlay;
            public TMP_Text     lockReasonText;

            // 3-star rating display
            public Image[]      starImages;
            public Sprite       starFilled;
            public Sprite       starEmpty;

            [HideInInspector] public MissionData boundMission;
        }

        [System.Serializable]
        public class MissionBriefingPopup
        {
            public GameObject   panel;
            public TMP_Text     titleText;
            public TMP_Text     storyText;
            public TMP_Text     objectiveText;
            public TMP_Text     difficultyText;
            public Button       startButton;
            public Button       closeButton;
        }

        [Header("Chapter & Monument Data")]
        [SerializeField] private ChapterData chapterData;
        [SerializeField] private MonumentData defaultMonument;

        [Header("Header Elements")]
        [SerializeField] private TMP_Text monumentTitleText;
        [SerializeField] private TMP_Text monumentSubtitleText;
        [SerializeField] private TMP_Text totalCoinsText;
        [SerializeField] private TMP_Text totalStarsText;

        [Header("Mission Cards")]
        [SerializeField] private List<MissionCard> missionCards = new List<MissionCard>();

        [Header("Briefing Popup")]
        [SerializeField] private MissionBriefingPopup briefingPopup;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        [Header("Card Labels")]
        [Tooltip("Appended to the mission number when completed, e.g. 'MISSION 01  •  COMPLETED'")]
        [SerializeField] private string completedSuffix = "  •  COMPLETED";
        [SerializeField] private string lockedReason    = "Complete the previous mission";

        private MissionData _selectedMission;
        private BKS_MissionCarousel carousel;

        /// <summary>Chapter shown by this screen: the one entered from the India Map, else the Inspector one.</summary>
        private ChapterData ActiveChapter
        {
            get
            {
                var loader = BKS_SceneLoader.Instance;
                if (loader != null && loader.ActiveChapter != null) return loader.ActiveChapter;
                return chapterData;
            }
        }

        private void Start()
        {
            if (BKS_SaveManager.Instance == null)
                new GameObject("BKS Save Manager").AddComponent<BKS_SaveManager>();
            // All chapters use the edited shared prefab as the briefing source.
            var sharedPopup = BKS_MissionBriefingView.Create(briefingPopup.panel, transform);
            if (sharedPopup != null)
            {
                briefingPopup.panel = sharedPopup.gameObject;
                briefingPopup.titleText = sharedPopup.titleText;
                briefingPopup.storyText = sharedPopup.storyText;
                briefingPopup.objectiveText = sharedPopup.objectiveText;
                briefingPopup.difficultyText = sharedPopup.difficultyText;
                briefingPopup.startButton = sharedPopup.startButton;
                briefingPopup.closeButton = sharedPopup.closeButton;
            }
            SetupHeader();
            PopulateCards();
            carousel = BKS_MissionCarousel.Setup(missionCards.ConvertAll(card => card.root));

            if (briefingPopup.panel != null) briefingPopup.panel.SetActive(false);
            if (briefingPopup.closeButton != null) briefingPopup.closeButton.onClick.AddListener(OnCloseBriefing);
            if (briefingPopup.startButton != null) briefingPopup.startButton.onClick.AddListener(OnStartMission);
            if (backButton != null) backButton.onClick.AddListener(OnBackPressed);
        }

        private void SetupHeader()
        {
            bool fromMap = BKS_SceneLoader.Instance != null && BKS_SceneLoader.Instance.ActiveChapter != null;
            var activeMon = BKS_SceneLoader.Instance != null ? BKS_SceneLoader.Instance.ActiveMonument : null;
            if (activeMon == null) activeMon = defaultMonument;
            if (fromMap) activeMon = null; // chapter flow: always show the chapter, not a monument
            var chapterData = ActiveChapter;

            if (activeMon != null)
            {
                if (monumentTitleText != null) monumentTitleText.text = activeMon.monumentName.ToUpper();
                if (monumentSubtitleText != null) monumentSubtitleText.text = activeMon.shortDescription;
            }
            else if (chapterData != null)
            {
                if (monumentTitleText != null) monumentTitleText.text = chapterData.StateName.ToUpper();
                if (monumentSubtitleText != null) monumentSubtitleText.text = chapterData.Subtitle;
            }

            if (BKS_SaveManager.Instance != null)
            {
                if (totalCoinsText != null) totalCoinsText.text = BKS_SaveManager.Instance.Coins.ToString("N0");
                if (totalStarsText != null) totalStarsText.text = $"Stars {BKS_SaveManager.Instance.SafarStars}";
            }
        }

        private void PopulateCards()
        {
            bool fromMap = BKS_SceneLoader.Instance != null && BKS_SceneLoader.Instance.ActiveChapter != null;
            var activeMon = BKS_SceneLoader.Instance != null ? BKS_SceneLoader.Instance.ActiveMonument : null;
            if (activeMon == null) activeMon = defaultMonument;
            if (fromMap) activeMon = null; // chapter flow: always the chapter's ordered missions
            var chapterData = ActiveChapter;

            IReadOnlyList<MissionData> missions = null;
            if (activeMon != null && activeMon.availableMissions != null && activeMon.availableMissions.Count > 0)
            {
                missions = activeMon.availableMissions;
            }
            else if (chapterData != null)
            {
                missions = chapterData.Missions;
            }

            int count = missions != null ? missions.Count : 0;

            for (int i = 0; i < missionCards.Count; i++)
            {
                var card = missionCards[i];
                if (i < count)
                {
                    var m = missions[i];
                    card.boundMission = m;

                    if (card.root != null) card.root.SetActive(true);
                    var save = BKS_SaveManager.Instance;
                    bool completed = save != null && save.IsMissionCompleted(m.MissionID);
                    if (card.missionNumberText != null)
                        card.missionNumberText.text = $"MISSION {m.MissionNumber:D2}" + (completed ? completedSuffix : string.Empty);
                    if (card.missionNameText != null) card.missionNameText.text = m.MissionName;
                    if (card.objectiveText != null) card.objectiveText.text = m.PrimaryObjectiveText;

                    // Lock state
                    // Chapter order decides: mission 1 always open, next opens when the previous is completed.
                    bool unlocked;
                    if (save == null) unlocked = i == 0;
                    else if (chapterData != null && chapterData.GetMissionByID(m.MissionID) != null)
                        unlocked = save.IsMissionUnlocked(chapterData, IndexInChapter(chapterData, m));
                    else
                        unlocked = i == 0 || save.IsMissionUnlocked(m.MissionID);

                    if (card.lockOverlay != null)
                    {
                        card.lockOverlay.SetActive(!unlocked);
                        card.lockOverlay.transform.SetAsLastSibling();
                        var lockImage = card.lockOverlay.GetComponent<Image>();
                        if (lockImage != null)
                        {
                            var tint = lockImage.color;
                            tint.a = Mathf.Max(0.72f, tint.a);
                            lockImage.color = tint;
                            lockImage.raycastTarget = true;
                        }
                    }
                    if (card.lockReasonText != null && !unlocked) card.lockReasonText.text = lockedReason;
                    if (card.playButton != null) card.playButton.interactable = unlocked;

                    // Stars
                    int bestRating = BKS_SaveManager.Instance != null ? BKS_SaveManager.Instance.GetMissionBestRating(m.MissionID) : 0;
                    if (card.starImages != null)
                    {
                        for (int s = 0; s < card.starImages.Length; s++)
                        {
                            if (card.starImages[s] != null)
                            {
                                card.starImages[s].gameObject.SetActive(unlocked);
                                if (unlocked)
                                {
                                    card.starImages[s].sprite = (s < bestRating && card.starFilled != null)
                                        ? card.starFilled
                                        : card.starEmpty;
                                }
                            }
                        }
                    }

                    if (card.playButton != null)
                    {
                        card.playButton.onClick.RemoveAllListeners();
                        card.playButton.onClick.AddListener(() => OnCardClicked(m));
                    }
                }
                else
                {
                    if (card.root != null) card.root.SetActive(false);
                }
            }
        }

        private static int IndexInChapter(ChapterData chapter, MissionData mission)
        {
            for (int i = 0; i < chapter.MissionCount; i++)
            {
                var m = chapter.GetMission(i);
                if (m == mission || (m != null && m.MissionID == mission.MissionID)) return i;
            }
            return -1;
        }

        private void OnCardClicked(MissionData mission)
        {
            var selectedCard = missionCards.Find(card => card.boundMission == mission);
            if (carousel != null && selectedCard != null && !carousel.Focus(selectedCard.root)) return;
            _selectedMission = mission;
            ShowBriefingPopup(mission);
        }

        private void ShowBriefingPopup(MissionData mission)
        {
            if (briefingPopup.panel == null)
            {
                OnStartMission();
                return;
            }

            if (briefingPopup.titleText != null) briefingPopup.titleText.text = mission.MissionName;
            if (briefingPopup.storyText != null) briefingPopup.storyText.text = mission.StoryText;
            if (briefingPopup.objectiveText != null) briefingPopup.objectiveText.text = mission.PrimaryObjectiveText;
            if (briefingPopup.difficultyText != null) briefingPopup.difficultyText.text = $"Difficulty: Mission {mission.MissionNumber}";

            briefingPopup.panel.transform.SetAsLastSibling();
            briefingPopup.panel.SetActive(true);
        }

        private void OnCloseBriefing()
        {
            if (briefingPopup.panel != null) briefingPopup.panel.SetActive(false);
            _selectedMission = null;
        }

        private void OnStartMission()
        {
            if (_selectedMission == null) return;

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
