using UnityEngine;

namespace BharatKaSafar.Missions
{
    /// <summary>Mission style from the Mission Planner sheet.</summary>
    public enum BKS_MissionType { Destination, Collectible, ObstacleChallenge, Hybrid }

    /// <summary>3rd-star bonus. Auto picks one from the mission type.</summary>
    public enum BKS_BonusType { Auto, None, NoHits, TimeLimit, AllSafarStars, NoRevive }

    [CreateAssetMenu(fileName = "MissionData",
                    menuName = "Bharat Ka Safar/Mission Data",
                    order = 1)]
    public class MissionData : ScriptableObject
    {
        [Header("Terrain Difficulty")]
        [SerializeField] private Gameplay.BKS_TerrainPlan terrainPlan;
        public Gameplay.BKS_TerrainPlan TerrainPlan => terrainPlan;
        // ── Identity ──────────────────────────────────────────────────────────────
        [Header("Identity")]
        [SerializeField] private string missionID;
        [SerializeField] private string chapterID;
        [SerializeField] private int    missionNumber;
        [SerializeField] private string missionName;
        [SerializeField][TextArea] private string description;
        [SerializeField][TextArea] private string storyText;

        // ── Scene ─────────────────────────────────────────────────────────────────
        [Header("Scene")]
        [SerializeField] private string gameplaySceneName;

        // ── Objectives ────────────────────────────────────────────────────────────
        [Header("Objectives")]
        [SerializeField] private string primaryObjectiveText;
        [SerializeField] private bool   hasTimeLimit;
        [SerializeField] private float  timeLimitSeconds;

        // ── Rewards ───────────────────────────────────────────────────────────────
        [Header("Rewards")]
        [Tooltip("Base coins awarded on mission completion")]
        [SerializeField] private int coinReward = 500;
        [Tooltip("Maximum Safar Stars available in this mission")]
        [SerializeField] private int maxSafarStars = 10;

        // ── Rating Thresholds ─────────────────────────────────────────────────────
        [Header("Star Rating Thresholds")]
        [Tooltip("Fraction of Safar Stars needed for 2-star (e.g. 0.60 = 60%)")]
        [SerializeField][Range(0f, 1f)] private float twoStarStarRatio   = 0.60f;
        [Tooltip("Fraction of Safar Stars needed for 3-star (e.g. 0.80 = 80%)")]
        [SerializeField][Range(0f, 1f)] private float threeStarStarRatio = 0.80f;
        [Tooltip("Vehicle health fraction required for 3-star (e.g. 0.50 = above 50%)")]
        [SerializeField][Range(0f, 1f)] private float threeStarHealthRatio = 0.50f;

        // ── Progression ───────────────────────────────────────────────────────────
        [Header("Progression")]
        [Tooltip("Mission ID to unlock on completion (leave empty if last mission)")]
        [SerializeField] private string nextMissionID;

        // ── UI ────────────────────────────────────────────────────────────────────
        [Header("UI")]
        [SerializeField] private Sprite thumbnailSprite;
        [Tooltip("Recommended power level shown in mission select")]
        [SerializeField] private int recommendedPower = 80;

        // ── Mission Planner (imported from Data/Missions/BKS_Missions.csv) ──────
        [Header("Mission Planner (from Excel)")]
        [SerializeField] private string stateName;
        [SerializeField] private string placeName;
        [SerializeField] private BKS_MissionType missionType = BKS_MissionType.Destination;
        [Tooltip("Mission card artwork (place name, title and 3 empty stars are part of the image)")]
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private string collectibleName;
        [SerializeField, Min(0)] private int collectibleCount;
        [SerializeField] private Sprite collectibleSprite;
        [SerializeField][TextArea] private string obstaclesText;
        [SerializeField][TextArea(2, 5)] private string howToDealText;
        [SerializeField] private string memoryTokenName;
        [SerializeField] private Sprite memoryTokenSprite;
        [Tooltip("Short fact about the place, shown in the Safar Diary")]
        [SerializeField][TextArea(2, 4)] private string placeFact;
        [Tooltip("Gameplay background (behind the terrain)")]
        [SerializeField] private Sprite backgroundSprite;
        [Tooltip("Scene generated for this mission but not playable yet (no terrain). Link it when the level is ready.")]
        [SerializeField] private string draftSceneName;

        // ── 3-Star rules ──────────────────────────────────────────────────────────
        // 1 star  = reach the destination
        // 2 stars = + main objective (collect all items / stay within the hit limit)
        // 3 stars = + bonus (no hits / no revive / time limit)
        [Header("3-Star Rules")]
        [Tooltip("Max obstacle hits allowed for the 2nd star. -1 = automatic (Obstacle/Hybrid 0, Destination 3, Collectible no limit)")]
        [SerializeField, Min(-1)] private int objectiveHitLimit = -1;
        [SerializeField] private BKS_BonusType bonusType = BKS_BonusType.Auto;
        [Tooltip("Used when the bonus is Time Limit")]
        [SerializeField, Min(1f)] private float bonusTimeSeconds = 150f;

        // ── Read-only Properties ──────────────────────────────────────────────────
        public string MissionID             => missionID;
        public string ChapterID             => chapterID;
        public int    MissionNumber         => missionNumber;
        public string MissionName           => missionName;
        public string Description           => description;
        public string StoryText             => storyText;
        public string GameplaySceneName     => gameplaySceneName;
        public string PrimaryObjectiveText  => primaryObjectiveText;
        public bool   HasTimeLimit          => hasTimeLimit;
        public float  TimeLimitSeconds      => timeLimitSeconds;
        public int    CoinReward            => coinReward;
        public int    MaxSafarStars         => maxSafarStars;
        public float  TwoStarStarRatio      => twoStarStarRatio;
        public float  ThreeStarStarRatio    => threeStarStarRatio;
        public float  ThreeStarHealthRatio  => threeStarHealthRatio;
        public string NextMissionID         => nextMissionID;
        public Sprite ThumbnailSprite       => thumbnailSprite;
        public int    RecommendedPower      => recommendedPower;
        public string StateName             => stateName;
        public string PlaceName             => string.IsNullOrEmpty(placeName) ? missionName : placeName;
        public BKS_MissionType MissionType  => missionType;
        public Sprite CardSprite            => cardSprite != null ? cardSprite : thumbnailSprite;
        public string CollectibleName       => collectibleName;
        public int    CollectibleCount      => collectibleCount;
        public Sprite CollectibleSprite     => collectibleSprite;
        public string ObstaclesText         => obstaclesText;
        public string HowToDealText         => howToDealText;
        public string MemoryTokenName       => memoryTokenName;
        public Sprite MemoryTokenSprite     => memoryTokenSprite;
        public Sprite BackgroundSprite      => backgroundSprite;
        public string PlaceFact             => placeFact;
        public string DraftSceneName        => draftSceneName;
        /// <summary>True when a playable scene exists for this mission.</summary>
        public bool   IsPlayable            => !string.IsNullOrEmpty(gameplaySceneName);
        /// <summary>Missions imported from the planner use the 3-star rules below; older ones keep ComputeStarRating.</summary>
        public bool   UsesPlannerRating     => !string.IsNullOrEmpty(stateName);
        public float  BonusTimeSeconds      => bonusTimeSeconds;

        /// <summary>Hit limit for the 2nd star, or -1 when hits do not matter.</summary>
        public int HitLimit
        {
            get
            {
                if (objectiveHitLimit >= 0) return objectiveHitLimit;
                switch (missionType)
                {
                    case BKS_MissionType.ObstacleChallenge:
                    case BKS_MissionType.Hybrid: return 0;
                    case BKS_MissionType.Destination: return 3;
                    default: return -1;
                }
            }
        }

        public BKS_BonusType Bonus
        {
            get
            {
                if (bonusType != BKS_BonusType.Auto) return bonusType;
                // "bina takkar" missions already need 0 hits for the 2nd star, so their bonus is finishing without a revive
                return missionType == BKS_MissionType.ObstacleChallenge || missionType == BKS_MissionType.Hybrid
                    ? BKS_BonusType.NoRevive : BKS_BonusType.NoHits;
            }
        }

        /// <summary>Short text for the 2nd star, e.g. "Collect 5 Diya" or "No hits".</summary>
        public string ObjectiveRuleText
        {
            get
            {
                var parts = new System.Collections.Generic.List<string>();
                if (collectibleCount > 0) parts.Add($"Collect {collectibleCount} {collectibleName}");
                int limit = HitLimit;
                if (limit == 0) parts.Add("No hits");
                else if (limit > 0) parts.Add($"Max {limit} hits");
                return parts.Count > 0 ? string.Join(" + ", parts) : "Reach the destination";
            }
        }

        public string BonusRuleText
        {
            get
            {
                switch (Bonus)
                {
                    case BKS_BonusType.NoHits: return "No hits";
                    case BKS_BonusType.TimeLimit: return $"Finish in {Mathf.FloorToInt(bonusTimeSeconds / 60f)}:{Mathf.FloorToInt(bonusTimeSeconds % 60f):00}";
                    case BKS_BonusType.AllSafarStars: return "Collect every Safar Star";
                    case BKS_BonusType.NoRevive: return "No revive";
                    default: return "";
                }
            }
        }

        /// <summary>
        /// Compute star rating (1–3) given session results.
        /// Always returns at least 1 if called (mission was completed).
        /// </summary>
        public int ComputeStarRating(float collectedStarRatio, float finalHealthRatio)
        {
            // 3 stars: enough Safar Stars AND enough health remaining
            if (collectedStarRatio >= threeStarStarRatio &&
                (!Vehicle.BKS_VehicleHealth.HealthSystemEnabled || finalHealthRatio >= threeStarHealthRatio))
                return 3;

            // 2 stars: enough Safar Stars collected
            if (collectedStarRatio >= twoStarStarRatio)
                return 2;

            // 1 star: mission completed, base award
            return 1;
        }

#if UNITY_EDITOR
        public void InitForEditor(
            string mID, string cID, int mNum, string mName, string story, string sceneName,
            string objText, int coins, int maxStars, float twoStarRatio, float threeStarRatio,
            float threeHealthRatio, string nextID, int power)
        {
            missionID = mID;
            chapterID = cID;
            missionNumber = mNum;
            missionName = mName;
            storyText = story;
            gameplaySceneName = sceneName;
            primaryObjectiveText = objText;
            coinReward = coins;
            maxSafarStars = maxStars;
            twoStarStarRatio = twoStarRatio;
            threeStarStarRatio = threeStarRatio;
            threeStarHealthRatio = threeHealthRatio;
            nextMissionID = nextID;
            recommendedPower = power;
        }

        public void SetBackgroundForEditor(Sprite background) => backgroundSprite = background;
        public void SetFactForEditor(string fact) { if (!string.IsNullOrEmpty(fact)) placeFact = fact; }
        public void SetDraftSceneForEditor(string sceneName) => draftSceneName = sceneName;
        /// <summary>Editor-only: make the draft scene the playable gameplay scene.</summary>
        public void LinkDraftSceneForEditor() { if (!string.IsNullOrEmpty(draftSceneName)) gameplaySceneName = draftSceneName; }

        /// <summary>Editor-only: fill the planner fields from one row of the mission sheet.</summary>
        public void InitFromPlanner(
            string mID, string cID, int mNum, string mName, string state, string place, BKS_MissionType type,
            string objective, Sprite card, string cName, int cCount, Sprite cSprite,
            string obstacles, string howTo, string tokenName, Sprite tokenSprite, string sceneName, string nextID)
        {
            missionID = mID;
            if (string.IsNullOrEmpty(chapterID)) chapterID = cID; // existing missions keep theirs
            missionNumber = mNum;
            missionName = mName;
            stateName = state;
            placeName = place;
            missionType = type;
            primaryObjectiveText = objective;
            if (string.IsNullOrEmpty(storyText)) storyText = objective;
            cardSprite = card;
            collectibleName = cName;
            collectibleCount = cCount;
            collectibleSprite = cSprite;
            obstaclesText = obstacles;
            howToDealText = howTo;
            memoryTokenName = tokenName;
            memoryTokenSprite = tokenSprite;
            // keep a scene that was already wired up by hand
            if (!string.IsNullOrEmpty(sceneName)) gameplaySceneName = sceneName;
            nextMissionID = nextID;
        }
#endif
    }
}
