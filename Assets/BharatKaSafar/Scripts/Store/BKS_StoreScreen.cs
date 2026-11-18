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
        private readonly Dictionary<Button, Vector3> tabScale = new Dictionary<Button, Vector3>();

        // tab art keeps its own colours (no green/orange tint): the selected tab glows gold and grows a little
        private void SetTab(Button b, bool on)
        {
            if (b == null) return;
            if (b.targetGraphic != null) b.targetGraphic.color = Color.white;
            if (!tabScale.ContainsKey(b)) tabScale[b] = b.transform.localScale;
            var o = b.GetComponent<Outline>(); if (o == null) o = b.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(1f, 0.85f, 0.35f, 0.95f); o.effectDistance = new Vector2(7f, -7f); o.useGraphicAlpha = true;
            o.enabled = on;
            b.transform.localScale = on ? tabScale[b] * 1.08f : tabScale[b];
        }
        private float feedbackUntil;

        private void Start()
        {
            if (catalog == null) catalog = BKS_StoreCatalog.Load();
            foreach (var t in new[] { vehiclesTab, upgradesTab, boostsTab })
            {
                var tab = t;
                if (tab.template != null) tab.template.gameObject.SetActive(false);
                // the card panel sits inside its tab button in the scene: touching / scrolling the cards pressed the tab too
                // (press scale + selected scale moved the whole list). Lift it out, same place on screen, drawn under the tabs.
                if (tab.panel != null && tab.button != null && tab.panel.transform.IsChildOf(tab.button.transform))
                {
                    var bt = tab.button.transform;
                    tab.panel.transform.SetParent(bt.parent, true);
                    tab.panel.transform.SetSiblingIndex(bt.GetSiblingIndex());
                }
                // smooth card scrolling (glide + snap to the nearest card)
                var sr = tab.panel != null ? tab.panel.GetComponentInChildren<ScrollRect>(true) : null;
                if (sr != null && sr.GetComponent<UI.BKS_SmoothScroll>() == null) sr.gameObject.AddComponent<UI.BKS_SmoothScroll>();
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
            if (boostsText != null) boostsText.text = (save != null ? save.Boost2XCount : 0).ToString();
            if (feedbackText != null && feedbackText.gameObject.activeSelf && Time.unscaledTime > feedbackUntil)
                feedbackText.gameObject.SetActive(false);
        }

        private void Open(Tab tab)
        {
            current = tab;
            foreach (var t in new[] { vehiclesTab, upgradesTab, boostsTab })
            {
                if (t.panel != null) t.panel.SetActive(t == tab);
                SetTab(t.button, t == tab);
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
                    var art = sel ? StoreButtonArt.Equipped : owned ? StoreButtonArt.Equip : Affordable(veh.price) ? StoreButtonArt.Buy : StoreButtonArt.Locked;
                    Add().Show(veh.icon, veh.displayName, owned ? "Owned" : $"{veh.price:N0} Coins", btn, !sel,
                        () => { if (owned) BKS_StoreService.SelectVehicle(veh); else Buy(BKS_StoreService.BuyVehicle(veh), veh.price); Refresh(); },
                        selected: sel, art: art);
                }
            else if (current == upgradesTab)
                foreach (var u in catalog.upgrades)
                {
                    var line = u;
                    int level = BKS_StoreService.UpgradeLevel(line.kind);
                    bool max = level >= line.MaxLevel;
                    int cost = max ? 0 : line.levelCosts[level];
                    Add().Show(line.icon, $"{line.displayName}  Lv {level}/{line.MaxLevel}", max ? "Max level" : $"{cost:N0} Coins",
                        max ? "MAX" : $"UPGRADE  {cost:N0}", !max,
                        () => { Buy(BKS_StoreService.BuyUpgrade(line), cost); Refresh(); },
                        level, line.MaxLevel, art: max ? StoreButtonArt.Text : Affordable(cost) ? StoreButtonArt.Buy : StoreButtonArt.Locked);
                }
            else
                foreach (var p in catalog.boostPacks)
                {
                    var pack = p;
                    Add().Show(catalog.boostIcon, $"2X Boost  x{pack.count}", $"{pack.price:N0} Coins",
                        $"BUY  {pack.price:N0}", true,
                        () => { Buy(BKS_StoreService.BuyBoosts(pack), pack.price); Refresh(); },
                        art: Affordable(pack.price) ? StoreButtonArt.Buy : StoreButtonArt.Locked);
                }
        }

        private static bool Affordable(int price) => BKS_SaveManager.Instance == null || BKS_SaveManager.Instance.Coins >= price;

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
