using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Store
{
    /// <summary>Store rules in one place: owning, buying and selecting vehicles, upgrades and 2X boosts.</summary>
    public static class BKS_StoreService
    {
        public static bool IsOwned(BKS_StoreVehicle v)
        {
            var save = BKS_SaveManager.Instance;
            return v != null && (v.freeByDefault || v.price <= 0 || (save != null && save.IsVehicleOwned(v.id)));
        }

        public static bool IsSelected(BKS_StoreVehicle v)
        {
            var save = BKS_SaveManager.Instance;
            if (save == null || v == null) return false;
            string sel = save.SelectedVehicle;
            // nothing chosen yet = the free starter vehicle
            return sel == v.id || (string.IsNullOrEmpty(sel) && v.freeByDefault);
        }

        public static bool BuyVehicle(BKS_StoreVehicle v)
        {
            var save = BKS_SaveManager.Instance;
            if (save == null || v == null || IsOwned(v) || !save.SpendCoins(v.price)) return false;
            save.SetVehicleOwned(v.id);
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.VehiclesBought);
            save.SelectVehicle(v.id);
            return true;
        }

        public static void SelectVehicle(BKS_StoreVehicle v)
        {
            var save = BKS_SaveManager.Instance;
            if (save != null && IsOwned(v)) save.SelectVehicle(v.id);
        }

        /// <summary>The vehicle that will be used in the next mission (selected one, else the free starter).</summary>
        public static BKS_StoreVehicle Equipped(BKS_StoreCatalog catalog)
        {
            if (catalog == null) return null;
            var save = BKS_SaveManager.Instance;
            var v = save != null ? catalog.GetVehicle(save.SelectedVehicle) : null;
            if (v != null && IsOwned(v)) return v;
            return catalog.vehicles.Find(x => x.freeByDefault);
        }

        /// <summary>All vehicles the player can drive (free + bought), in catalog order.</summary>
        public static System.Collections.Generic.List<BKS_StoreVehicle> OwnedVehicles(BKS_StoreCatalog catalog) =>
            catalog == null ? new System.Collections.Generic.List<BKS_StoreVehicle>() : catalog.vehicles.FindAll(IsOwned);

        public static bool BuyBoosts(BKS_BoostPack pack)
        {
            var save = BKS_SaveManager.Instance;
            if (save == null || pack == null || !save.SpendCoins(pack.price)) return false;
            save.AddBoost2X(pack.count);
            return true;
        }

        public static int UpgradeLevel(BKS_UpgradeKind kind)
        {
            var save = BKS_SaveManager.Instance;
            if (save == null) return 0;
            switch (kind)
            {
                case BKS_UpgradeKind.Engine: return save.EngineLevel;
                case BKS_UpgradeKind.Tyres: return save.TyreLevel;
                case BKS_UpgradeKind.Suspension: return save.SuspensionLevel;
                default: return save.HornLevel;
            }
        }

        public static bool BuyUpgrade(BKS_UpgradeLine line)
        {
            var save = BKS_SaveManager.Instance;
            int level = UpgradeLevel(line.kind);
            if (save == null || level >= line.MaxLevel || !save.SpendCoins(line.levelCosts[level])) return false;
            switch (line.kind)
            {
                case BKS_UpgradeKind.Engine: save.SetEngineLevel(level + 1); break;
                case BKS_UpgradeKind.Tyres: save.SetTyreLevel(level + 1); break;
                case BKS_UpgradeKind.Suspension: save.SetSuspensionLevel(level + 1); break;
                default: save.SetHornLevel(level + 1); break;
            }
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.UpgradesBought);
            return true;
        }
    }
}
