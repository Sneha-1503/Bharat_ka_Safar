using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// One place that answers "did the player's vehicle touch me?".
    ///
    /// The legacy car prefabs are not tagged consistently: only "Body" has the tag "Car",
    /// colliders like VehicleSprite are Untagged, and the wheels are NOT children of Body.
    /// So every pickup/obstacle uses this helper instead of its own tag checks.
    /// </summary>
    public static class BKS_VehicleUtil
    {
        /// <summary>The GameObject that has CarController (the moving "Body"), or null.</summary>
        public static CarController GetCar(GameObject touched)
        {
            if (touched == null) return null;

            var car = touched.GetComponentInParent<CarController>();
            if (car != null) return car;

            // Wheels: sibling of Body under the same root
            Transform root = touched.transform.root;
            return root != null ? root.GetComponentInChildren<CarController>() : null;
        }

        public static bool IsVehicle(GameObject touched)
        {
            if (touched == null) return false;
            if (touched.CompareTag("Car") || touched.CompareTag("Player")) return true;
            return GetCar(touched) != null;
        }

        /// <summary>Health component of the touched vehicle, if any.</summary>
        public static BKS_VehicleHealth GetHealth(GameObject touched)
        {
            if (touched == null) return null;
            var health = touched.GetComponentInParent<BKS_VehicleHealth>();
            if (health != null) return health;

            Transform root = touched.transform.root;
            return root != null ? root.GetComponentInChildren<BKS_VehicleHealth>() : null;
        }
    }
}
