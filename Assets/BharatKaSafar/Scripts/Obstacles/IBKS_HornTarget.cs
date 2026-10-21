using UnityEngine;

namespace BharatKaSafar.Obstacles
{
    /// <summary>Anything that reacts to the horn (barricade, crate, rock pile, log, crossing animal/cart).</summary>
    public interface IBKS_HornTarget
    {
        /// <summary>False once it has been cleared, hit, or is not on the road.</summary>
        bool CanRespondToHorn { get; }
        /// <summary>Returns true when the horn really handled it (used for the Safar Star).</summary>
        bool OnHorn(float hornPower);
        Transform transform { get; }
    }

    /// <summary>Obstacles that can award one Safar Star when handled correctly (counted for the HUD total).</summary>
    public interface IBKS_SafarStarSource { }

    /// <summary>Shared helpers for obstacle scripts.</summary>
    public static class BKS_ObstacleUtil
    {
        /// <summary>Hit by the player: counts a hit, slows the car and deals a little damage.</summary>
        public static void PenaliseVehicle(GameObject vehiclePart, float speedKeep, float damage)
        {
            Missions.BKS_MissionObjectiveTracker.ReportHit();
            var car = Vehicle.BKS_VehicleUtil.GetCar(vehiclePart);
            var rb = car != null ? car.GetComponent<Rigidbody2D>() : vehiclePart.GetComponentInParent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity *= speedKeep;
            var health = Vehicle.BKS_VehicleUtil.GetHealth(vehiclePart);
            if (health != null && damage > 0f) health.TakeDamage(damage);
        }

        public static void AwardSafarStar()
        {
            var tracker = Object.FindFirstObjectByType<Core.BKS_SafarStarTracker>();
            if (tracker != null) tracker.CollectStar();
        }

        /// <summary>Player vehicle body (CarController), or null.</summary>
        public static Transform FindVehicle()
        {
            var car = Object.FindFirstObjectByType<CarController>();
            return car != null ? car.transform : null;
        }
    }
}
