using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Challenges
{
    /// <summary>
    /// Today's/this week's challenges, their progress and claiming.
    /// Daily resets at local midnight, weekly on Monday. The challenges chosen for a period come from
    /// the pool with a seed from the date, so they stay the same all day/week.
    /// </summary>
    public static class BKS_ChallengeService
    {
        public struct Entry
        {
            public BKS_ChallengeDef def;
            public BKS_ChallengePeriod period;
            public int progress;
            public bool claimed;
            public bool Complete => progress >= def.target;
        }

        public static string PeriodKey(BKS_ChallengePeriod p)
        {
            var now = DateTime.Now;
            switch (p)
            {
                case BKS_ChallengePeriod.Daily: return "D" + now.ToString("yyyyMMdd");
                case BKS_ChallengePeriod.Weekly: return "W" + WeekStart(now).ToString("yyyyMMdd");
                default: return "O";
            }
        }

        public static TimeSpan TimeUntilReset(BKS_ChallengePeriod p)
        {
            var now = DateTime.Now;
            if (p == BKS_ChallengePeriod.Daily) return now.Date.AddDays(1) - now;
            if (p == BKS_ChallengePeriod.Weekly) return WeekStart(now).AddDays(7) - now;
            return TimeSpan.Zero;
        }

        private static DateTime WeekStart(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7)); // Monday

        public static List<Entry> Get(BKS_ChallengePeriod period)
        {
            var cat = BKS_ChallengeCatalog.Load();
            string key = PeriodKey(period);
            List<BKS_ChallengeDef> defs;
            if (period == BKS_ChallengePeriod.Overall) defs = cat.overall;
            else defs = Pick(period == BKS_ChallengePeriod.Daily ? cat.dailyPool : cat.weeklyPool,
                             period == BKS_ChallengePeriod.Daily ? cat.dailyCount : cat.weeklyCount, key);

            var list = new List<Entry>();
            foreach (var d in defs)
            {
                int total = BKS_ChallengeStats.Get(d.stat);
                int progress = period == BKS_ChallengePeriod.Overall ? total : total - Baseline(key, d.stat, total);
                list.Add(new Entry { def = d, period = period, progress = Mathf.Max(0, progress), claimed = PlayerPrefs.GetInt(ClaimKey(key, d), 0) == 1 });
            }
            return list;
        }

        /// <summary>Number of completed, unclaimed challenges (for a badge on the Home button).</summary>
        public static int ReadyToClaim()
        {
            int n = 0;
            foreach (BKS_ChallengePeriod p in Enum.GetValues(typeof(BKS_ChallengePeriod)))
                foreach (var e in Get(p)) if (e.Complete && !e.claimed) n++;
            return n;
        }

        public static bool Claim(Entry e)
        {
            string key = PeriodKey(e.period);
            if (!e.Complete || PlayerPrefs.GetInt(ClaimKey(key, e.def), 0) == 1) return false;
            PlayerPrefs.SetInt(ClaimKey(key, e.def), 1);
            var save = BKS_SaveManager.Instance;
            if (save != null)
            {
                if (e.def.rewardCoins > 0) save.AddCoins(e.def.rewardCoins);
                if (e.def.rewardBoosts > 0) save.AddBoost2X(e.def.rewardBoosts);
            }
            PlayerPrefs.Save();
            return true;
        }

        public static string RewardText(BKS_ChallengeDef d)
        {
            var parts = new List<string>();
            if (d.rewardCoins > 0) parts.Add(d.rewardCoins.ToString("N0", CultureInfo.InvariantCulture) + " Coins");
            if (d.rewardBoosts > 0) parts.Add(d.rewardBoosts + " x 2X Boost");
            return string.Join(" + ", parts);
        }

        // Progress for a period starts from the lifetime value seen when the period was first opened
        private static int Baseline(string periodKey, BKS_Stat stat, int currentTotal)
        {
            string k = $"BKS_ChBase_{periodKey}_{stat}";
            if (!PlayerPrefs.HasKey(k)) PlayerPrefs.SetInt(k, currentTotal);
            return PlayerPrefs.GetInt(k);
        }

        /// <summary>Snapshot today's/this week's baselines (called at game start so early progress counts).</summary>
        public static void EnsureBaselines()
        {
            foreach (var p in new[] { BKS_ChallengePeriod.Daily, BKS_ChallengePeriod.Weekly }) Get(p);
        }

        private static int StableHash(string s)
        {
            unchecked { int h = (int)2166136261; foreach (char c in s) h = (h ^ c) * 16777619; return h; }
        }

        private static string ClaimKey(string periodKey, BKS_ChallengeDef d) => $"BKS_ChClaim_{periodKey}_{d.id}";

        private static List<BKS_ChallengeDef> Pick(List<BKS_ChallengeDef> pool, int count, string seedKey)
        {
            var copy = new List<BKS_ChallengeDef>(pool);
            var rng = new System.Random(StableHash(seedKey)); // same picks on every device/run for a given day
            for (int i = copy.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); (copy[i], copy[j]) = (copy[j], copy[i]); }
            // one challenge per stat so a day doesn't get "300 coins" and "500 coins" together
            var picked = new List<BKS_ChallengeDef>();
            foreach (var d in copy)
            {
                if (picked.Count >= count) break;
                if (picked.Exists(x => x.stat == d.stat)) continue;
                picked.Add(d);
            }
            return picked;
        }
    }
}
