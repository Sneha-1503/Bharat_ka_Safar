using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Core
{
    /// <summary>
    /// ScriptableObject that describes one chapter = one Indian state.
    ///
    /// This holds STATIC configuration only (text, images, cost, which scene to open,
    /// ordered mission list). Runtime progress (unlocked / completed) is NOT stored here —
    /// it lives in BKS_SaveManager.
    ///
    /// The ChapterID is also the state ID used by the India Map (e.g. "UP", "RJ", "TN", "PB").
    /// </summary>
    [CreateAssetMenu(fileName = "ChapterData",
                     menuName = "Bharat Ka Safar/Chapter Data",
                     order = 0)]
    public class ChapterData : ScriptableObject
    {
        [Header("Chapter Identity")]
        [Tooltip("Unique state/chapter ID used by the map and the save system, e.g. UP, RJ, TN, PB")]
        [SerializeField] private string chapterID;
        [SerializeField] private string stateName;
        [SerializeField] private string chapterTitle;
        [SerializeField][TextArea] private string subtitle;

        [Header("Home Page — Chapter Panel")]
        [Tooltip("Shown in the chapter panel. Falls back to Subtitle if empty.")]
        [SerializeField][TextArea(3, 6)] private string description;
        [Tooltip("Big image of the state shown in the chapter panel (no baked text).")]
        [SerializeField] private Sprite mainImage;
        [Tooltip("Up to 5 famous landmark images. Empty slots are hidden automatically.")]
        [SerializeField] private Sprite[] landmarkImages = new Sprite[0];

        [Header("Availability & Unlock")]
        [Tooltip("FALSE = Upcoming (not built yet). Player can view it but cannot unlock or enter.")]
        [SerializeField] private bool chapterAvailable = true;
        [Tooltip("TRUE = unlocked automatically on a new save (e.g. Uttar Pradesh).")]
        [SerializeField] private bool unlockedByDefault = false;
        [Tooltip("Coins needed to unlock this chapter (ignored if Unlocked By Default).")]
        [SerializeField] private int unlockCost = 0;

        [Header("Navigation")]
        [Tooltip("Scene loaded by ENTER CHAPTER (usually this chapter's Mission Select scene). Must be in Build Settings.")]
        [SerializeField] private string chapterSceneName;

        [Header("Art")]
        [Tooltip("Full-screen background of the Mission Select page (a white veil is drawn over it).")]
        [SerializeField] private Sprite missionSelectBackground;
        [Tooltip("State name header art shown on the Mission Select page instead of the title text.")]
        [SerializeField] private Sprite headerImage;
        [SerializeField] private Sprite mapHighlightSprite;
        [SerializeField] private Color chapterColor = new Color(1f, 0.6f, 0.1f, 1f);

        [Header("Missions (ordered)")]
        [SerializeField] private List<MissionData> missions = new List<MissionData>();

        [Header("Monuments (optional)")]
        [SerializeField] private List<MonumentData> monuments = new List<MonumentData>();

        [Header("Unlock Requirements (legacy, not used by Home page)")]
        [SerializeField] private int safarStarsRequired = 0;
        [Tooltip("Leave empty if this chapter is always available")]
        [SerializeField] private string requiredPreviousChapterID;

        // ── Read-only Properties ──────────────────────────────────────────────────
        public string ChapterID              => chapterID != null ? chapterID.Trim() : string.Empty;
        public string StateName              => stateName;
        public string ChapterTitle           => chapterTitle;
        public string Subtitle               => subtitle;
        public string Description            => string.IsNullOrEmpty(description) ? subtitle : description;
        public Sprite MainImage              => mainImage;
        public IReadOnlyList<Sprite> LandmarkImages => landmarkImages ?? (landmarkImages = new Sprite[0]);
        public bool   ChapterAvailable       => chapterAvailable;
        public bool   UnlockedByDefault      => unlockedByDefault;
        public int    UnlockCost             => Mathf.Max(0, unlockCost);
        public string ChapterSceneName       => chapterSceneName != null ? chapterSceneName.Trim() : string.Empty;
        public Sprite MapHighlightSprite     => mapHighlightSprite;
        public Sprite MissionSelectBackground => missionSelectBackground;
        public Sprite HeaderImage => headerImage;
        public Color  ChapterColor           => chapterColor;
        public int    SafarStarsRequired     => safarStarsRequired;
        public string RequiredPreviousChapter => requiredPreviousChapterID;

        /// <summary>Best name to show on screen: state name, else chapter title, else ID.</summary>
        public string DisplayName =>
            !string.IsNullOrEmpty(stateName) ? stateName :
            !string.IsNullOrEmpty(chapterTitle) ? chapterTitle : ChapterID;

        public IReadOnlyList<MissionData> Missions => missions;
        public IReadOnlyList<MonumentData> Monuments => monuments;

        public int MissionCount => missions.Count;

        public MissionData GetMission(int index)
        {
            if (index < 0 || index >= missions.Count) return null;
            return missions[index];
        }

        public MissionData GetMissionByID(string missionID)
        {
            foreach (var m in missions)
                if (m != null && m.MissionID == missionID) return m;
            return null;
        }

#if UNITY_EDITOR
        public void InitForEditor(
            string cID, string sName, string title, string sub,
            List<MissionData> missionList, List<MonumentData> monumentList = null, int starsRequired = 0)
        {
            chapterID = cID;
            stateName = sName;
            chapterTitle = title;
            subtitle = sub;
            missions = missionList ?? new List<MissionData>();
            monuments = monumentList ?? new List<MonumentData>();
            safarStarsRequired = starsRequired;
        }

        /// <summary>Editor-only: set the ordered mission list and Mission Select art from the planner sheet.</summary>
        public void SetPlannerData(string cID, string sName, List<MissionData> missionList, Sprite selectBackground, string selectScene)
        {
            if (string.IsNullOrEmpty(chapterID)) chapterID = cID;
            if (string.IsNullOrEmpty(stateName)) stateName = sName;
            if (string.IsNullOrEmpty(chapterTitle)) chapterTitle = sName;
            missions = missionList ?? new List<MissionData>();
            missionSelectBackground = selectBackground;
            if (!string.IsNullOrEmpty(selectScene)) chapterSceneName = selectScene;
        }
#endif
    }
}
