using System;
using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Core
{
    /// <summary>
    /// Centralized save/load manager for all Bharat Ka Safar progression data.
    ///
    /// COINS (single wallet):
    ///   Coins are stored in the PlayerPrefs key "Coins". This is the SAME key the
    ///   legacy gameplay (GameManager) writes to while driving, so there is only one
    ///   wallet in the whole game. The old separate "BKS_Coins" balance is merged into
    ///   it once (see MigrateLegacyCoins) and then removed.
    ///   Coins are always read fresh from PlayerPrefs because legacy gameplay code can
    ///   change the value without going through this manager.
    ///
    /// STATE / CHAPTER PROGRESS:
    ///   Unlocked  → PlayerPrefs "BKS_SU_" + stateId
    ///   Completed → PlayerPrefs "BKS_CC_" + stateId
    ///   Which states start unlocked on a NEW save comes from ChapterData.UnlockedByDefault
    ///   (see EnsureDefaultUnlockedStates). Existing progress is never reset or re-locked.
    ///
    /// All other BKS keys are namespaced with "BKS_".
    /// </summary>
    public class BKS_SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────
        public static BKS_SaveManager Instance { get; private set; }

        // ── PlayerPrefs Keys ─────────────────────────────────────────────────────
        /// <summary>Shared wallet key — also used by legacy gameplay (GameManager).</summary>
        private const string KEY_COINS              = "Coins";
        private const string KEY_LEGACY_BKS_COINS   = "BKS_Coins";        // old separate wallet, merged once
        private const string KEY_COINS_MIGRATED     = "BKS_CoinsMigrated";

        private const string KEY_SAFAR_STARS    = "BKS_SafarStars";
        private const string KEY_ENGINE_LEVEL   = "BKS_EngineLevel";
        private const string KEY_TYRE_LEVEL     = "BKS_TyreLevel";
        private const string KEY_SUSPENSION_LVL = "BKS_SuspensionLevel";
        private const string KEY_HORN_LEVEL     = "BKS_HornLevel";
        private const string PREFIX_MISSION_UNLOCKED = "BKS_MU_";   // + missionID
        private const string PREFIX_MISSION_RATING   = "BKS_MR_";   // + missionID → 0,1,2,3
        private const string PREFIX_STATE_UNLOCKED   = "BKS_SU_";   // + stateID
        private const string PREFIX_CHAPTER_COMPLETED= "BKS_CC_";   // + chapterID / stateID

        // ── Events ────────────────────────────────────────────────────────────────
        public event Action<int> OnCoinsChanged;
        public event Action<int> OnSafarStarsChanged;

        /// <summary>Fires when a state/chapter is unlocked or completed. Arg: stateId.</summary>
        public event Action<string> OnStateProgressChanged;

        // ── Cached Values ─────────────────────────────────────────────────────────
        private int _safarStars;
        private int _engineLevel;
        private int _tyreLevel;
        private int _suspensionLevel;
        private int _hornLevel;

        // ── Public Read Properties ────────────────────────────────────────────────
        /// <summary>Current coin balance (always read fresh from the shared wallet).</summary>
        public int Coins           => PlayerPrefs.GetInt(KEY_COINS, 0);
        public int SafarStars      => _safarStars;
        public int EngineLevel     => _engineLevel;
        public int TyreLevel       => _tyreLevel;
        public int SuspensionLevel => _suspensionLevel;
        public int HornLevel       => _hornLevel;

        // ─────────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }

        // ── Load / Save ───────────────────────────────────────────────────────────

        public void Load()
        {
            MigrateLegacyCoins();

            _safarStars      = PlayerPrefs.GetInt(KEY_SAFAR_STARS, 0);
            _engineLevel     = PlayerPrefs.GetInt(KEY_ENGINE_LEVEL, 0);
            _tyreLevel       = PlayerPrefs.GetInt(KEY_TYRE_LEVEL, 0);
            _suspensionLevel = PlayerPrefs.GetInt(KEY_SUSPENSION_LVL, 0);
            _hornLevel       = PlayerPrefs.GetInt(KEY_HORN_LEVEL, 0);

            // NOTE: Missions are no longer unlocked by hardcoded IDs here.
            // The first mission of every chapter is always playable and the next one
            // unlocks when the previous one is completed — see IsMissionUnlocked(chapter, index).

            // NOTE: States are no longer force-unlocked here.
            // Default unlocked states come from ChapterData.UnlockedByDefault
            // via EnsureDefaultUnlockedStates(), called by the Home page.
        }

        /// <summary>Flush everything to PlayerPrefs storage.</summary>
        public void Save()
        {
            // Coins are written immediately whenever they change, so they are not written here
            // (writing a cached value could overwrite coins earned by legacy gameplay).
            PlayerPrefs.SetInt(KEY_SAFAR_STARS, _safarStars);
            PlayerPrefs.SetInt(KEY_ENGINE_LEVEL, _engineLevel);
            PlayerPrefs.SetInt(KEY_TYRE_LEVEL, _tyreLevel);
            PlayerPrefs.SetInt(KEY_SUSPENSION_LVL, _suspensionLevel);
            PlayerPrefs.SetInt(KEY_HORN_LEVEL, _hornLevel);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// One-time merge of the old separate "BKS_Coins" balance into the shared "Coins" wallet.
        /// </summary>
        private void MigrateLegacyCoins()
        {
            if (PlayerPrefs.GetInt(KEY_COINS_MIGRATED, 0) == 1) return;

            int oldBksCoins = PlayerPrefs.GetInt(KEY_LEGACY_BKS_COINS, 0);
            if (oldBksCoins > 0)
            {
                PlayerPrefs.SetInt(KEY_COINS, Coins + oldBksCoins);
                Debug.Log($"[BKS_SaveManager] Merged {oldBksCoins} old BKS coins into the shared wallet.");
            }

            PlayerPrefs.DeleteKey(KEY_LEGACY_BKS_COINS);
            PlayerPrefs.SetInt(KEY_COINS_MIGRATED, 1);
            PlayerPrefs.Save();
        }

        // ── Coins ─────────────────────────────────────────────────────────────────

        public int GetCoins() => Coins;

        public bool CanSpendCoins(int amount) => amount <= 0 || Coins >= amount;

        // ── Vehicles (Store) ──────────────────────────────────────────────────────
        private const string KEY_VEHICLE_OWNED = "BKS_VehOwned_";
        private const string KEY_VEHICLE_SELECTED = "BKS_SelectedVehicle";
        public event Action<string> OnVehicleChanged;
        public bool IsVehicleOwned(string vehicleID) => PlayerPrefs.GetInt(KEY_VEHICLE_OWNED + vehicleID, 0) == 1;
        public void SetVehicleOwned(string vehicleID)
        {
            PlayerPrefs.SetInt(KEY_VEHICLE_OWNED + vehicleID, 1);
            PlayerPrefs.Save();
            OnVehicleChanged?.Invoke(vehicleID);
        }
        /// <summary>Selected vehicle ID ("" = the level's own default car).</summary>
        public string SelectedVehicle => PlayerPrefs.GetString(KEY_VEHICLE_SELECTED, "");
        public void SelectVehicle(string vehicleID)
        {
            PlayerPrefs.SetString(KEY_VEHICLE_SELECTED, vehicleID ?? "");
            PlayerPrefs.Save();
            OnVehicleChanged?.Invoke(vehicleID);
        }

        // ── 2X Boosts (bought in the Store, used with the in-game 2X button) ─────
        private const string KEY_BOOST_2X = "BKS_Boost2X";
        public event Action<int> OnBoostsChanged;
        public int Boost2XCount => PlayerPrefs.GetInt(KEY_BOOST_2X, 0);
        public void AddBoost2X(int amount)
        {
            PlayerPrefs.SetInt(KEY_BOOST_2X, Mathf.Max(0, Boost2XCount + amount));
            PlayerPrefs.Save();
            OnBoostsChanged?.Invoke(Boost2XCount);
        }
        // Testing until the Store exists: right-click the BKS_SaveManager component in Play Mode
        [ContextMenu("Debug/Add 5 2X Boosts")]
        private void DebugAddBoosts() => AddBoost2X(5);

        public bool UseBoost2X()
        {
            if (Boost2XCount <= 0) return false;
            AddBoost2X(-1);
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.CoinsEarned, amount);
            int newBalance = Coins + amount;
            PlayerPrefs.SetInt(KEY_COINS, newBalance);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(newBalance);
        }

        /// <returns>True if deduction succeeded (enough coins), false otherwise.</returns>
        public bool SpendCoins(int amount)
        {
            if (amount <= 0) return true;
            int balance = Coins;
            if (balance < amount) return false;

            balance -= amount;
            PlayerPrefs.SetInt(KEY_COINS, balance);
            PlayerPrefs.Save();
            OnCoinsChanged?.Invoke(balance);
            return true;
        }

        // ── Safar Stars ───────────────────────────────────────────────────────────

        public void AddSafarStars(int amount)
        {
            if (amount <= 0) return;
            _safarStars += amount;
            PlayerPrefs.SetInt(KEY_SAFAR_STARS, _safarStars);
            OnSafarStarsChanged?.Invoke(_safarStars);
        }

        // ── Mission Progress ──────────────────────────────────────────────────────

        public bool IsMissionUnlocked(string missionID)
        {
            return PlayerPrefs.GetInt(PREFIX_MISSION_UNLOCKED + missionID, 0) == 1;
        }

        public void SetMissionUnlocked(string missionID, bool unlocked)
        {
            PlayerPrefs.SetInt(PREFIX_MISSION_UNLOCKED + missionID, unlocked ? 1 : 0);
        }

        /// <returns>Best star rating (0–3). 0 means never completed.</returns>
        public int GetMissionBestRating(string missionID)
        {
            return PlayerPrefs.GetInt(PREFIX_MISSION_RATING + missionID, 0);
        }

        /// <summary>Saves mission result. Only updates if new rating is better.</summary>
        public void SaveMissionResult(string missionID, int starRating, int coinsEarned, int safarStarsEarned, string nextMissionID = null)
        {
            int previous = GetMissionBestRating(missionID);
            if (starRating > previous)
                PlayerPrefs.SetInt(PREFIX_MISSION_RATING + missionID, starRating);

            AddCoins(coinsEarned);
            AddSafarStars(safarStarsEarned);

            // Unlock next mission if provided
            if (!string.IsNullOrEmpty(nextMissionID))
                SetMissionUnlocked(nextMissionID, true);

            PlayerPrefs.Save();
        }

        // ── Chapter-based mission progression ────────────────────────────────────

        /// <summary>A mission counts as completed once it has a best rating of 1+ stars.</summary>
        public bool IsMissionCompleted(string missionID) => GetMissionBestRating(missionID) > 0;

        /// <summary>
        /// Data-driven unlock rule, works for every chapter without hardcoded IDs:
        ///   • mission #1 of a chapter is always unlocked
        ///   • mission #N is unlocked when mission #N-1 is completed
        ///     (completed missions remain replayable; old debug unlock flags are ignored)
        /// </summary>
        public bool IsMissionUnlocked(ChapterData chapter, int missionIndex)
        {
            if (chapter == null) return false;
            MissionData mission = chapter.GetMission(missionIndex);
            if (mission == null) return false;
            if (missionIndex == 0) return true;
            if (IsMissionCompleted(mission.MissionID)) return true;

            MissionData previous = chapter.GetMission(missionIndex - 1);
            return previous != null && IsMissionCompleted(previous.MissionID);
        }

        public int GetCompletedMissionCount(ChapterData chapter)
        {
            if (chapter == null) return 0;
            int count = 0;
            for (int i = 0; i < chapter.MissionCount; i++)
            {
                MissionData m = chapter.GetMission(i);
                if (m != null && IsMissionCompleted(m.MissionID)) count++;
            }
            return count;
        }

        public bool AreAllMissionsCompleted(ChapterData chapter)
        {
            return chapter != null && chapter.MissionCount > 0
                && GetCompletedMissionCount(chapter) >= chapter.MissionCount;
        }

        /// <summary>
        /// Save a completed mission inside its chapter:
        ///   best star rating, coins, Safar Stars, unlock the next mission (by chapter order),
        ///   and mark the chapter/state completed when all of its missions are done.
        /// Returns true only when THIS mission finished the chapter for the first time
        /// (replaying a mission of an already-completed chapter returns false).
        /// </summary>
        public bool CompleteMission(ChapterData chapter, MissionData mission, int starRating, int coinsEarned, int safarStarsEarned)
        {
            if (chapter == null || mission == null) return false;

            string id = mission.MissionID;
            bool wasChapterCompleted = IsChapterCompleted(chapter.ChapterID);
            int rating = Mathf.Clamp(starRating, 1, 3); // completing always gives at least 1 star
            if (rating > GetMissionBestRating(id))
                PlayerPrefs.SetInt(PREFIX_MISSION_RATING + id, rating);

            AddCoins(coinsEarned);
            AddSafarStars(safarStarsEarned);

            // Unlock the next mission in this chapter's order
            int index = IndexOfMission(chapter, mission);
            MissionData next = index >= 0 ? chapter.GetMission(index + 1) : null;
            if (next != null) SetMissionUnlocked(next.MissionID, true);

            PlayerPrefs.Save();

            bool chapterDone = AreAllMissionsCompleted(chapter);
            if (chapterDone) CompleteState(chapter.ChapterID); // state turns green on the India Map

            Debug.Log($"[BKS_SaveManager] Mission '{id}' completed ({rating}★). " +
                      $"Chapter '{chapter.ChapterID}': {GetCompletedMissionCount(chapter)}/{chapter.MissionCount}" +
                      (chapterDone ? " — CHAPTER COMPLETE" : string.Empty));
            return chapterDone && !wasChapterCompleted;
        }

        private static int IndexOfMission(ChapterData chapter, MissionData mission)
        {
            for (int i = 0; i < chapter.MissionCount; i++)
            {
                MissionData m = chapter.GetMission(i);
                if (m == mission || (m != null && m.MissionID == mission.MissionID)) return i;
            }
            return -1;
        }

        // ── State / Chapter Unlock ────────────────────────────────────────────────

        public bool IsStateUnlocked(string stateID)
        {
            if (string.IsNullOrEmpty(stateID)) return false;
            return PlayerPrefs.GetInt(PREFIX_STATE_UNLOCKED + stateID, 0) == 1;
        }

        public void SetStateUnlocked(string stateID, bool unlocked)
        {
            if (string.IsNullOrEmpty(stateID)) return;
            PlayerPrefs.SetInt(PREFIX_STATE_UNLOCKED + stateID, unlocked ? 1 : 0);
        }

        public bool IsChapterCompleted(string chapterID)
        {
            if (string.IsNullOrEmpty(chapterID)) return false;
            return PlayerPrefs.GetInt(PREFIX_CHAPTER_COMPLETED + chapterID, 0) == 1;
        }

        public void SetChapterCompleted(string chapterID, bool completed)
        {
            if (string.IsNullOrEmpty(chapterID)) return;
            PlayerPrefs.SetInt(PREFIX_CHAPTER_COMPLETED + chapterID, completed ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Unlocks every available chapter marked "Unlocked By Default" (e.g. Tamil Nadu) if it is not
        /// unlocked yet. Safe to call every time the Home page opens: it only ever unlocks,
        /// it never re-locks or resets anything.
        /// </summary>
        public void EnsureDefaultUnlockedStates(IEnumerable<ChapterData> chapters)
        {
            if (chapters == null) return;

            bool changed = false;
            foreach (var chapter in chapters)
            {
                if (chapter == null || !chapter.ChapterAvailable || !chapter.UnlockedByDefault) continue;
                if (IsStateUnlocked(chapter.ChapterID)) continue;

                SetStateUnlocked(chapter.ChapterID, true);
                changed = true;
            }

            if (changed) PlayerPrefs.Save();
        }

        /// <summary>
        /// Spends coins and unlocks the state in one step.
        /// Returns true if the state is unlocked afterwards (already unlocked counts as success).
        /// Returns false — and changes nothing — when there are not enough coins.
        /// </summary>
        public bool TryUnlockState(string stateID, int cost)
        {
            if (string.IsNullOrEmpty(stateID)) return false;
            if (IsStateUnlocked(stateID)) return true;
            if (!SpendCoins(cost)) return false;

            SetStateUnlocked(stateID, true);
            PlayerPrefs.Save();
            OnStateProgressChanged?.Invoke(stateID);
            return true;
        }

        /// <summary>
        /// Marks a state/chapter as completed (and unlocked). Never re-locks.
        /// Call this when the last mission of a chapter is finished.
        /// </summary>
        public void CompleteState(string stateID)
        {
            if (string.IsNullOrEmpty(stateID)) return;
            SetStateUnlocked(stateID, true);
            SetChapterCompleted(stateID, true); // also saves
            OnStateProgressChanged?.Invoke(stateID);
        }

        /// <summary>Debug helper: forget unlock + completion for one state.</summary>
        public void ResetStateProgress(string stateID)
        {
            if (string.IsNullOrEmpty(stateID)) return;
            PlayerPrefs.DeleteKey(PREFIX_STATE_UNLOCKED + stateID);
            PlayerPrefs.DeleteKey(PREFIX_CHAPTER_COMPLETED + stateID);
            PlayerPrefs.Save();
            OnStateProgressChanged?.Invoke(stateID);
        }

        // ── Upgrade Levels ────────────────────────────────────────────────────────

        public void SetEngineLevel(int level)
        {
            _engineLevel = level;
            PlayerPrefs.SetInt(KEY_ENGINE_LEVEL, level);
        }

        public void SetTyreLevel(int level)
        {
            _tyreLevel = level;
            PlayerPrefs.SetInt(KEY_TYRE_LEVEL, level);
        }

        public void SetSuspensionLevel(int level)
        {
            _suspensionLevel = level;
            PlayerPrefs.SetInt(KEY_SUSPENSION_LVL, level);
        }

        public void SetHornLevel(int level)
        {
            _hornLevel = level;
            PlayerPrefs.SetInt(KEY_HORN_LEVEL, level);
        }

        // ── Debug Utility ─────────────────────────────────────────────────────────

        [ContextMenu("BKS / Reset All Save Data")]
        public void ResetAllData()
        {
            string[] keys =
            {
                KEY_COINS, KEY_SAFAR_STARS,
                KEY_ENGINE_LEVEL, KEY_TYRE_LEVEL, KEY_SUSPENSION_LVL, KEY_HORN_LEVEL
            };
            foreach (var key in keys)
                PlayerPrefs.DeleteKey(key);

            Load();
            OnCoinsChanged?.Invoke(Coins);
            Debug.Log("[BKS_SaveManager] All BKS save data reset.");
        }

        [ContextMenu("BKS / Add 1000 Coins (Debug)")]
        private void DebugAddCoins() => AddCoins(1000);
    }
}
