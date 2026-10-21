using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Mission Select v2 — one scene for every chapter (scene: BKS_MissionSelect).
    /// Chapter background with a white veil, "Chapter : State" title, Back button top-right,
    /// a horizontally scrollable row of mission cards (selected card grows), and ENTER
    /// which opens the Mission Briefing popup.
    /// </summary>
    public class BKS_MissionSelectScreen : MonoBehaviour
    {
        [Header("Chapter")]
        [Tooltip("Used when the scene is played directly (no chapter chosen on the India Map).")]
        [SerializeField] private ChapterData testChapter;

        [Header("Page")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image whiteVeil;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private string titleFormat = "Chapter : {0}";
        [Tooltip("Shows the chapter's header art (ChapterData > Header Image); the title text is hidden while it is shown")]
        [SerializeField] private Image headerImage;
        [SerializeField] private Button backButton;

        [Header("Cards")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform cardContainer;
        [Tooltip("Inactive template card inside the container; it is cloned for each mission.")]
        [SerializeField] private BKS_MissionCardView cardTemplate;
        [SerializeField, Min(1f)] private float centreSpeed = 10f;

        [Header("Enter")]
        [SerializeField] private Button enterButton;
        [SerializeField] private BKS_MissionBriefingPopup briefing;

        private readonly List<BKS_MissionCardView> cards = new List<BKS_MissionCardView>();
        private BKS_MissionCardView selected;
        private float? scrollTarget;

        private ChapterData Chapter
        {
            get
            {
                var loader = BKS_SceneLoader.Instance;
                return loader != null && loader.ActiveChapter != null ? loader.ActiveChapter : testChapter;
            }
        }

        private void Start()
        {
            if (backButton != null) backButton.onClick.AddListener(GoBack);
            if (enterButton != null) enterButton.onClick.AddListener(OpenBriefing);
            if (cardTemplate != null) cardTemplate.gameObject.SetActive(false);
            // stop auto-centering as soon as the player drags the list
            if (scrollRect != null) scrollRect.gameObject.AddComponent<DragListener>().onBeginDrag = () => scrollTarget = null;
            Build();
        }

        private void Build()
        {
            var chapter = Chapter;
            if (chapter == null)
            {
                Debug.LogError("[BKS_MissionSelectScreen] No chapter. Enter from the India Map or assign Test Chapter.", this);
                return;
            }

            if (titleText != null) titleText.text = string.Format(titleFormat, chapter.DisplayName);
            bool hasHeader = headerImage != null && chapter.HeaderImage != null;
            if (headerImage != null) { headerImage.sprite = chapter.HeaderImage; headerImage.preserveAspect = true; headerImage.gameObject.SetActive(hasHeader); }
            if (titleText != null) titleText.gameObject.SetActive(!hasHeader);
            if (backgroundImage != null && chapter.MissionSelectBackground != null) backgroundImage.sprite = chapter.MissionSelectBackground;

            var save = BKS_SaveManager.Instance;
            BKS_MissionCardView firstOpen = null, lastUnlocked = null;
            for (int i = 0; i < chapter.MissionCount; i++)
            {
                var mission = chapter.GetMission(i);
                if (mission == null) continue;
                bool unlocked = save == null ? i == 0 : save.IsMissionUnlocked(chapter, i);
                int stars = save == null ? 0 : save.GetMissionBestRating(mission.MissionID);

                var card = Instantiate(cardTemplate, cardContainer);
                card.gameObject.SetActive(true);
                card.Setup(mission, unlocked, stars, Select);
                cards.Add(card);
                if (unlocked) lastUnlocked = card;
                if (unlocked && stars == 0 && firstOpen == null) firstOpen = card;
            }
            // Start on the next mission to play
            var start = firstOpen ?? lastUnlocked ?? (cards.Count > 0 ? cards[0] : null);
            if (start != null) Select(start);
        }

        private void Select(BKS_MissionCardView card)
        {
            if (selected == card && card != null && card.Unlocked) { OpenBriefing(); return; } // second tap = enter
            if (selected != null) selected.SetSelected(false);
            selected = card;
            selected.SetSelected(true);
            if (enterButton != null) enterButton.interactable = card.Unlocked;
            CentreOn(cards.IndexOf(card));
        }

        private void CentreOn(int index)
        {
            if (scrollRect == null || cards.Count <= 1) return;
            scrollTarget = Mathf.Clamp01(index / (float)(cards.Count - 1));
        }

        private void Update()
        {
            if (scrollTarget.HasValue && scrollRect != null)
            {
                float v = Mathf.Lerp(scrollRect.horizontalNormalizedPosition, scrollTarget.Value,
                                     1f - Mathf.Exp(-centreSpeed * Time.unscaledDeltaTime));
                scrollRect.horizontalNormalizedPosition = v;
                if (Mathf.Abs(v - scrollTarget.Value) < 0.001f) scrollTarget = null;
            }
        }

        private void OpenBriefing()
        {
            if (selected == null || !selected.Unlocked) return;
            if (briefing != null) briefing.Show(selected.Mission, StartMission);
            else StartMission(selected.Mission);
        }

        private void StartMission(MissionData mission)
        {
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.LoadMission(mission);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(mission.GameplaySceneName);
        }

        private void GoBack()
        {
            if (BKS_AudioSettingsPopup.BlocksNavigation) return;
            if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.GoToIndiaMap();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP);
        }

        private class DragListener : MonoBehaviour, IBeginDragHandler
        {
            public System.Action onBeginDrag;
            public void OnBeginDrag(PointerEventData eventData) => onBeginDrag?.Invoke();
        }
    }
}
