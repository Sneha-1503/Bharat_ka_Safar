using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;

namespace BharatKaSafar.Store
{
    /// <summary>
    /// Store screen (scene BKS_Store): tabs for Vehicles, Upgrades and 2X Boosts, coin balance,
    /// Back to the screen the Store was opened from. Prices come from Resources/BKS_StoreCatalog.
    /// </summary>
    public class BKS_StoreScreen : MonoBehaviour
    {
        [System.Serializable]
        public class Tab
        {
            public Button button;
            public GameObject panel;
            public RectTransform content;
            public BKS_StoreItemView template;
        }

        [SerializeField] private BKS_StoreCatalog catalog;
        [SerializeField] private Tab vehiclesTab = new Tab();
        [SerializeField] private Tab upgradesTab = new Tab();
        [SerializeField] private Tab boostsTab = new Tab();
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text boostsText;
        [SerializeField] private TMP_Text feedbackText;
        [SerializeField] private Button backButton;
        [SerializeField] private Color tabOn = new Color(0.95f, 0.55f, 0.12f);
        [SerializeField] private Color tabOff = new Color(0.05f, 0.36f, 0.38f);

        private readonly List<BKS_StoreItemView> spawned = new List<BKS_StoreItemView>();
        private Tab current;
        private float feedbackUntil;

        private void Start()
        {
            if (catalog == null) catalog = BKS_StoreCatalog.Load();
            foreach (var t in new[] { vehiclesTab, upgradesTab, boostsTab })
            {
                var tab = t;
                if (tab.template != null) tab.template.gameObject.SetActive(false);
                if (tab.button != null) tab.button.onClick.AddListener(() => Open(tab));
            }
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.ReturnFromStore();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_INDIA_MAP);
            });
            Open(vehiclesTab);
        }

        private void Update()
        {
            var save = BKS_SaveManager.Instance;
            if (coinsText != null) coinsText.text = save != null ? save.Coins.ToString("N0") : "0";
            if (boostsText != null) boostsText.text = "2X x" + (save != null ? save.Boost2XCount : 0);
            if (feedbackText != null && feedbackText.gameObject.activeSelf && Time.unscaledTime > feedbackUntil)
                feedbackText.gameObject.SetActive(false);
        }

        private void Open(Tab tab)
        {
            current = tab;
            foreach (var t in new[] { vehiclesTab, upgradesTab, boostsTab })
            {
                if (t.panel != null) t.panel.SetActive(t == tab);
                if (t.button != null && t.button.targetGraphic != null) t.button.targetGraphic.color = t == tab ? tabOn : tabOff;
            }
            Refresh();
        }

        private void Refresh()
        {
            foreach (var s in spawned) if (s != null) Destroy(s.gameObject);
            spawned.Clear();
            if (catalog == null || current == null || current.template == null) return;

            if (current == vehiclesTab)
                foreach (var v in catalog.vehicles)
                {
                    var veh = v;
                    bool owned = BKS_StoreService.IsOwned(veh), sel = BKS_StoreService.IsSelected(veh);
                    string btn = sel ? "EQUIPPED" : owned ? "EQUIP" : $"BUY  {veh.price:N0}";
                    Add().Show(veh.icon, veh.displayName, owned ? "Owned" : "Locked", btn, !sel,
                        () => { if (owned) BKS_StoreService.SelectVehicle(veh); else Buy(BKS_StoreService.BuyVehicle(veh), veh.price); Refresh(); },
                        selected: sel);
                }
            else if (current == upgradesTab)
                foreach (var u in catalog.upgrades)
                {
                    var line = u;
                    int level = BKS_StoreService.UpgradeLevel(line.kind);
                    bool max = level >= line.MaxLevel;
                    Add().Show(line.icon, $"{line.displayName}  (Lv {level}/{line.MaxLevel})", line.description,
                        max ? "MAX" : $"UPGRADE  {line.levelCosts[level]:N0}", !max,
                        () => { Buy(BKS_StoreService.BuyUpgrade(line), max ? 0 : line.levelCosts[level]); Refresh(); },
                        level, line.MaxLevel);
                }
            else
                foreach (var p in catalog.boostPacks)
                {
                    var pack = p;
                    Add().Show(catalog.boostIcon, $"2X Boost  x{pack.count}", "Double coins and score for 10 seconds. Use with the 2X button while driving.",
                        $"BUY  {pack.price:N0}", true,
                        () => { Buy(BKS_StoreService.BuyBoosts(pack), pack.price); Refresh(); });
                }
        }

        private BKS_StoreItemView Add()
        {
            var item = Instantiate(current.template, current.content);
            item.gameObject.SetActive(true);
            spawned.Add(item);
            return item;
        }

        private void Buy(bool ok, int price)
        {
            if (feedbackText == null) return;
            feedbackText.text = ok ? "Purchased!" : $"Not enough coins (need {price:N0})";
            feedbackText.color = ok ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.8f, 0.25f, 0.15f);
            feedbackText.gameObject.SetActive(true);
            feedbackUntil = Time.unscaledTime + 2f;
            if (ok && AudioManager.instance != null) AudioManager.instance.PlayCoinSound();
        }
    }
}
