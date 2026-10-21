using System;
using BharatKaSafar.Core;

namespace BharatKaSafar.Home
{
    /// <summary>
    /// Runtime snapshot of one state's progress, built from BKS_SaveManager.
    /// This is NOT a second save system — it is just a convenient read-only bundle
    /// passed to the UI. The real values live in BKS_SaveManager (PlayerPrefs).
    /// </summary>
    [Serializable]
    public class StateProgressData
    {
        public string stateId;
        public bool   isUnlocked;
        public bool   isCompleted;

        public StateProgressData(string stateId, bool isUnlocked, bool isCompleted)
        {
            this.stateId     = stateId;
            this.isUnlocked  = isUnlocked;
            this.isCompleted = isCompleted;
        }
    }

    /// <summary>What the player can do with a chapter right now.</summary>
    public enum ChapterStatus
    {
        /// <summary>Not built yet. Viewable, cannot unlock or enter.</summary>
        Upcoming,
        /// <summary>Built, but the player has not unlocked it. Can unlock with coins.</summary>
        Locked,
        /// <summary>Unlocked. Can enter.</summary>
        Unlocked,
        /// <summary>Finished. Can still enter / replay.</summary>
        Completed
    }

    public static class ChapterStatusResolver
    {
        /// <summary>
        /// Priority: Upcoming → Completed → Unlocked → Locked.
        /// A state with no ChapterData (map button only) is treated as Upcoming.
        /// </summary>
        public static ChapterStatus Resolve(ChapterData data, StateProgressData progress)
        {
            if (data == null || !data.ChapterAvailable) return ChapterStatus.Upcoming;
            if (progress != null && progress.isCompleted) return ChapterStatus.Completed;
            if (progress != null && progress.isUnlocked)  return ChapterStatus.Unlocked;
            return ChapterStatus.Locked;
        }
    }
}
