using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    public class BKS_DriverCollisionFilter : MonoBehaviour
    {
        public Collider2D[] driverColliders;
        public Collider2D[] vehicleColliders;
        private void Awake()
        {
            foreach (var driver in driverColliders)
                foreach (var vehicle in vehicleColliders)
                    if (driver != null && vehicle != null) Physics2D.IgnoreCollision(driver, vehicle);
        }
    }
}
