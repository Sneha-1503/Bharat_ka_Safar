using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BharatKaSafar.Core;

namespace BharatKaSafar.UI
{
    /// <summary>
    /// Garage screen for vehicle upgrades.
    ///
    /// Upgrade categories (per spec):
    ///   Engine    — affects acceleration / torque (motorPower)
    ///   Tyres     — affects grip / max speed
    ///   Suspension — improves stability / reduces impact damage
    ///   Horn      — increases horn range and effectiveness
    ///
    /// Uses BKS_SaveManager for coin balance.
    /// Applies BaseStat + UpgradeModifier pattern — never modifies base prefab values.
    ///
    /// Upgrade data is defined directly in Inspector arrays (upgrade costs per level).
    /// Each category is an UpgradeCategory struct.
    /// </summary>
    public class BKS_GarageUI : MonoBehaviour
    {
        // ── Data ──────────────────────────────────────────────────────────────────
        [System.Serializable]
        public class UpgradeCategory
        {
            public string   categoryName;
            [TextArea] public string description;
            public int[]    levelCosts;    // cost to go from level N to N+1
            public int      maxLevel => levelCosts != null ? levelCosts.Length : 0;
        }

        // ── Inspector: Upgrade Categories ────────────────────────────────────────
        [Header("Upgrade Categories")]
        [SerializeField] private UpgradeCategory engineUpgrade = new UpgradeCategory
        {
            categoryName = "Engine",
            description  = "Increases torque and acceleration power.",
            levelCosts   = new[] { 300, 600, 1000, 1500, 2000 }
        };
        [SerializeField] private UpgradeCategory tyreUpgrade = new UpgradeCategory
        {
            categoryName = "Tyres",
            description  = "Improves grip and increases top speed.",
            levelCosts   = new[] { 300, 600, 1000, 1500, 2000 }
        };
        [SerializeField] private UpgradeCategory suspensionUpgrade = new UpgradeCategory
        {
            categoryName = "Suspension",
            description  = "Reduces damage from obstacles and rough terrain.",
            levelCosts   = new[] { 400, 800, 1200, 1800, 2500 }
        };
        [SerializeField] private UpgradeCategory hornUpgrade = new UpgradeCategory
        {
            categoryName = "Horn",
            description  = "Increases horn detection range and effectiveness.",
            levelCosts   = new[] { 200, 400, 700, 1000, 1500 }
        };

        // ── Inspector: UI ─────────────────────────────────────────────────────────
        [Header("Coin Display")]
        [SerializeField] private TMP_Text coinsText;

        [Header("Selected Upgrade Info Panel")]
        [SerializeField] private TMP_Text selectedNameText;
        [SerializeField] private TMP_Text selectedDescText;
        [SerializeField] private TMP_Text selectedLevelText;
        [SerializeField] private TMP_Text selectedCostText;
        [SerializeField] private Button   upgradeButton;

        [Header("Category Tab Buttons")]
        [SerializeField] private Button engineTabBtn;
        [SerializeField] private Button tyreTabBtn;
        [SerializeField] private Button suspensionTabBtn;
        [SerializeField] private Button hornTabBtn;

        [Header("Level Bar Fills (one Image per upgrade category, fill amount = level/max)")]
        [SerializeField] private Image engineLevelFill;
        [SerializeField] private Image tyreLevelFill;
        [SerializeField] private Image suspensionLevelFill;
        [SerializeField] private Image hornLevelFill;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        // ── Runtime ───────────────────────────────────────────────────────────────
        private int _selectedCategory = 0; // 0=Engine, 1=Tyre, 2=Suspension, 3=Horn

        private void Start()
        {
            SetupTabButtons();
            RefreshCoins();
            SelectCategory(0);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackPressed);

            if (BKS_SaveManager.Instance != null)
                BKS_SaveManager.Instance.OnCoinsChanged += _ => RefreshCoins();
        }

        private void SetupTabButtons()
        {
            if (engineTabBtn    != null) engineTabBtn.onClick.AddListener(()    => SelectCategory(0));
            if (tyreTabBtn      != null) tyreTabBtn.onClick.AddListener(()      => SelectCategory(1));
            if (suspensionTabBtn!= null) suspensionTabBtn.onClick.AddListener(()=> SelectCategory(2));
            if (hornTabBtn      != null) hornTabBtn.onClick.AddListener(()      => SelectCategory(3));
            if (upgradeButton   != null) upgradeButton.onClick.AddListener(OnUpgradePressed);
        }

        // ── Selection ─────────────────────────────────────────────────────────────

        private void SelectCategory(int index)
        {
            _selectedCategory = index;
            RefreshInfoPanel();
            RefreshLevelBars();
        }

        private UpgradeCategory GetCategory(int index)
        {
            return index switch
            {
                0 => engineUpgrade,
                1 => tyreUpgrade,
                2 => suspensionUpgrade,
                3 => hornUpgrade,
                _ => engineUpgrade
            };
        }

        private int GetCurrentLevel(int index)
        {
            if (BKS_SaveManager.Instance == null) return 0;
            return index switch
            {
                0 => BKS_SaveManager.Instance.EngineLevel,
                1 => BKS_SaveManager.Instance.TyreLevel,
                2 => BKS_SaveManager.Instance.SuspensionLevel,
                3 => BKS_SaveManager.Instance.HornLevel,
                _ => 0
            };
        }

        // ── UI Refresh ────────────────────────────────────────────────────────────

        private void RefreshCoins()
        {
            if (coinsText != null && BKS_SaveManager.Instance != null)
                coinsText.text = BKS_SaveManager.Instance.Coins.ToString("N0");
        }

        private void RefreshInfoPanel()
        {
            var cat   = GetCategory(_selectedCategory);
            int level = GetCurrentLevel(_selectedCategory);

            if (selectedNameText  != null) selectedNameText.text  = cat.categoryName.ToUpper();
            if (selectedDescText  != null) selectedDescText.text  = cat.description;
            if (selectedLevelText != null) selectedLevelText.text = $"Level {level} / {cat.maxLevel}";

            bool maxed = level >= cat.maxLevel;

            if (selectedCostText != null)
                selectedCostText.text = maxed ? "MAXED OUT" : $"Cost: {cat.levelCosts[level]} coins";

            if (upgradeButton != null)
                upgradeButton.interactable = !maxed;
        }

        private void RefreshLevelBars()
        {
            SetFill(engineLevelFill,     engineUpgrade,     BKS_SaveManager.Instance?.EngineLevel     ?? 0);
            SetFill(tyreLevelFill,       tyreUpgrade,       BKS_SaveManager.Instance?.TyreLevel       ?? 0);
            SetFill(suspensionLevelFill, suspensionUpgrade, BKS_SaveManager.Instance?.SuspensionLevel ?? 0);
            SetFill(hornLevelFill,       hornUpgrade,       BKS_SaveManager.Instance?.HornLevel       ?? 0);
        }

        private void SetFill(Image bar, UpgradeCategory cat, int level)
        {
            if (bar == null || cat.maxLevel <= 0) return;
            bar.fillAmount = (float)level / cat.maxLevel;
        }

        // ── Upgrade ───────────────────────────────────────────────────────────────

        private void OnUpgradePressed()
        {
            if (BKS_SaveManager.Instance == null) return;

            var cat   = GetCategory(_selectedCategory);
            int level = GetCurrentLevel(_selectedCategory);

            if (level >= cat.maxLevel)
            {
                Debug.Log("[BKS_GarageUI] Already maxed out.");
                return;
            }

            int cost = cat.levelCosts[level];

            if (!BKS_SaveManager.Instance.SpendCoins(cost))
            {
                Debug.Log("[BKS_GarageUI] Not enough coins.");
                // TODO: show "not enough coins" feedback animation
                return;
            }

            // Apply upgrade
            int newLevel = level + 1;
            switch (_selectedCategory)
            {
                case 0: BKS_SaveManager.Instance.SetEngineLevel(newLevel);     break;
                case 1: BKS_SaveManager.Instance.SetTyreLevel(newLevel);       break;
                case 2: BKS_SaveManager.Instance.SetSuspensionLevel(newLevel); break;
                case 3: BKS_SaveManager.Instance.SetHornLevel(newLevel);       break;
            }

            BKS_SaveManager.Instance.Save();
            Debug.Log($"[BKS_GarageUI] Upgraded {cat.categoryName} to Level {newLevel}");

            RefreshInfoPanel();
            RefreshLevelBars();
            RefreshCoins();
        }

        // ── Navigation ────────────────────────────────────────────────────────────

        private void OnBackPressed()
        {
            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.GoToUPMissionSelect();
            else
                UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_UP_MISSION_SEL);
        }
    }
}
