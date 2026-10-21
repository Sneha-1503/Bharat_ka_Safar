using UnityEngine;
using BharatKaSafar.Core;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Store
{
    /// <summary>
    /// Added by MissionManager in every gameplay scene. Before the legacy StartPoint spawns its car,
    /// swaps in the vehicle selected in the Store (if any), then makes sure the spawned vehicle
    /// gets the Store upgrades (BKS_UpgradeLoader).
    /// </summary>
    public class BKS_SelectedVehicleSpawner : MonoBehaviour
    {
        private float searchUntil;

        private void Awake()
        {
            searchUntil = Time.time + 5f;
            var save = BKS_SaveManager.Instance;
            var catalog = BKS_StoreCatalog.Load();
            var start = FindFirstObjectByType<StartPoint>();
            if (save == null || catalog == null || start == null) return;

            // the equipped vehicle, or the free starter that every level shares
            var v = BKS_StoreService.Equipped(catalog);
            if (v == null || v.prefab == null) return;

            // StartPoint picks cars[PlayerPrefs "SelectedCar"]; fill every slot so any index gets this vehicle
            int slots = Mathf.Max(1, start.cars != null ? start.cars.Length : 1, PlayerPrefs.GetInt("SelectedCar") + 1);
            var cars = new GameObject[slots];
            for (int i = 0; i < slots; i++) cars[i] = v.prefab;
            start.cars = cars;
        }

        private void Update()
        {
            var car = FindFirstObjectByType<CarController>();
            if (car != null)
            {
                // every vehicle can take damage (most prefabs were built without a health component) — before the upgrades read it
                if (BKS_VehicleUtil.GetHealth(car.gameObject) == null) car.gameObject.AddComponent<BKS_VehicleHealth>();
                if (car.GetComponent<BKS_UpgradeLoader>() == null) car.gameObject.AddComponent<BKS_UpgradeLoader>();
                if (car.GetComponent<BKS_HillClimbFeel>() == null) car.gameObject.AddComponent<BKS_HillClimbFeel>();
                Destroy(this);
            }
            else if (Time.time > searchUntil) Destroy(this);
        }
    }
}
