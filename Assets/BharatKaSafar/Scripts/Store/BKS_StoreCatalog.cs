using System;
using System.Collections.Generic;
using UnityEngine;

namespace BharatKaSafar.Store
{
    /// <summary>One vehicle sold in the Store.</summary>
    [Serializable]
    public class BKS_StoreVehicle
    {
        public string id;
        public string displayName;
        [Tooltip("Empty = the level's own default car")]
        public GameObject prefab;
        public Sprite icon;
        [Min(0)] public int price;
        public bool freeByDefault;
    }

    /// <summary>A pack of 2X boosts.</summary>
    [Serializable]
    public class BKS_BoostPack
    {
        [Min(1)] public int count = 1;
        [Min(0)] public int price = 150;
    }

    /// <summary>An upgrade line (Engine, Tyres …) with the coin cost of each level.</summary>
    [Serializable]
    public class BKS_UpgradeLine
    {
        public BKS_UpgradeKind kind;
        public string displayName;
        [TextArea] public string description;
        public Sprite icon;
        public int[] levelCosts = { 300, 600, 1000, 1500, 2000 };
        public int MaxLevel => levelCosts != null ? levelCosts.Length : 0;
    }

    public enum BKS_UpgradeKind { Engine, Tyres, Suspension, Horn }

    /// <summary>
    /// Everything the Store sells, with prices. Lives in Resources/BKS_StoreCatalog so gameplay can find
    /// the selected vehicle. Edit prices in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Bharat Ka Safar/Store Catalog", fileName = "BKS_StoreCatalog")]
    public class BKS_StoreCatalog : ScriptableObject
    {
        public List<BKS_StoreVehicle> vehicles = new List<BKS_StoreVehicle>();
        public List<BKS_BoostPack> boostPacks = new List<BKS_BoostPack>
        {
            new BKS_BoostPack { count = 1, price = 150 },
            new BKS_BoostPack { count = 5, price = 600 },
            new BKS_BoostPack { count = 10, price = 1000 },
        };
        public Sprite boostIcon;
        public List<BKS_UpgradeLine> upgrades = new List<BKS_UpgradeLine>
        {
            new BKS_UpgradeLine { kind = BKS_UpgradeKind.Engine, displayName = "Engine", description = "More power and faster acceleration on every vehicle.", levelCosts = new[] { 300, 600, 1000, 1500, 2000 } },
            new BKS_UpgradeLine { kind = BKS_UpgradeKind.Tyres, displayName = "Tyres / Speed", description = "Higher top speed and better grip.", levelCosts = new[] { 300, 600, 1000, 1500, 2000 } },
            new BKS_UpgradeLine { kind = BKS_UpgradeKind.Suspension, displayName = "Suspension", description = "Less damage from obstacles and rough roads.", levelCosts = new[] { 400, 800, 1200, 1800, 2500 } },
            new BKS_UpgradeLine { kind = BKS_UpgradeKind.Horn, displayName = "Horn", description = "Louder horn that clears animals and carts from farther away.", levelCosts = new[] { 200, 400, 700, 1000, 1500 } },
        };

        private static BKS_StoreCatalog cached;
        public static BKS_StoreCatalog Load()
        {
            if (cached == null) cached = Resources.Load<BKS_StoreCatalog>("BKS_StoreCatalog");
            return cached;
        }

        public BKS_StoreVehicle GetVehicle(string id) => vehicles.Find(v => v.id == id);
    }
}
