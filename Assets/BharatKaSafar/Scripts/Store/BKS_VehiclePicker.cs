using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.Store
{
    /// <summary>
    /// "‹ [vehicle] ›" chooser used in the Mission Briefing: cycles through the vehicles the player owns
    /// (free starter + bought) and equips the one shown, so the next mission uses it.
    /// </summary>
    public class BKS_VehiclePicker : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private string labelFormat = "{0}";

        private BKS_StoreCatalog catalog;

        private void Awake()
        {
            if (prevButton != null) prevButton.onClick.AddListener(() => Step(-1));
            if (nextButton != null) nextButton.onClick.AddListener(() => Step(1));
        }

        private void OnEnable() => Refresh();

        private void Step(int dir)
        {
            if (catalog == null) catalog = BKS_StoreCatalog.Load();
            var owned = BKS_StoreService.OwnedVehicles(catalog);
            if (owned.Count == 0) return;
            int i = owned.IndexOf(BKS_StoreService.Equipped(catalog));
            var next = owned[((i < 0 ? 0 : i) + dir + owned.Count) % owned.Count];
            BKS_StoreService.SelectVehicle(next);
            Refresh();
        }

        public void Refresh()
        {
            if (catalog == null) catalog = BKS_StoreCatalog.Load();
            var v = BKS_StoreService.Equipped(catalog);
            int count = BKS_StoreService.OwnedVehicles(catalog).Count;
            if (icon != null) { icon.sprite = v != null ? v.icon : null; icon.enabled = icon.sprite != null; }
            if (nameText != null) nameText.text = v != null ? string.Format(labelFormat, v.displayName) : "";
            // arrows only matter when there is something to switch to
            if (prevButton != null) prevButton.gameObject.SetActive(count > 1);
            if (nextButton != null) nextButton.gameObject.SetActive(count > 1);
        }
    }
}
