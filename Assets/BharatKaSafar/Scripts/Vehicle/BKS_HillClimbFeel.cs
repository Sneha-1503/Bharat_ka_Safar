using System.Collections;
using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Hill Climb Racing–style handling for every vehicle (added to the spawned car by BKS_SelectedVehicleSpawner).
    /// The prefabs are not changed; these values are applied at runtime:
    ///  • normal tyre grip (not glue) — steep slopes need momentum
    ///  • softer, bouncier suspension
    ///  • centre of mass in the middle of the body — full gas lifts the nose, the car can flip over
    ///  • less engine torque, rear-wheel drive (4x4 only where the vehicle is set to keep it)
    ///  • strong air control: gas = lean back, brake = lean forward while flying
    /// Tweak the numbers here (or on the car in Play mode) to tune the feel.
    /// </summary>
    [RequireComponent(typeof(CarController))]
    public class BKS_HillClimbFeel : MonoBehaviour
    {
        [Header("Tyres")]
        [Tooltip("Tyre grip. Old value was 3 (glue). Hill Climb feel ~1")]
        public float tyreFriction = 1.0f;
        public float tyreBounciness = 0.12f;

        [Header("Suspension")]
        [Tooltip("Lower = softer and bouncier (old 8-11)")]
        public float suspensionFrequency = 4.5f;
        [Range(0f, 1f)] public float suspensionDamping = 0.35f;

        [Header("Balance")]
        [Tooltip("Centre of mass height inside the body box (0 = middle, 0.5 = top, -0.5 = bottom). Higher = flips more easily")]
        [Range(-0.5f, 0.5f)] public float centerOfMassHeight = 0.05f;

        [Header("Engine")]
        [Tooltip("Multiplier on the vehicle's motor torque (1 = as built)")]
        public float torqueMultiplier = 0.55f;
        [Tooltip("Only the rear wheels drive (Hill Climb style). Off = keep each vehicle's 4x4 setting")]
        public bool rearWheelDriveOnly = true;

        [Tooltip("Top speed multiplier (1 = as built: most vehicles stop at ~50 km/h). 1.7 ≈ 85 km/h")]
        public float topSpeedMultiplier = 1.7f;

        [Header("Air control")]
        [Tooltip("Spin speed added per second while flying (degrees/s²)")]
        public float airSpin = 420f;

        private IEnumerator Start()
        {
            yield return null; // after CarController.Start (it sets its own centre of mass)
            Apply();
        }

        private float baseTorque = -1f;
        private float baseMaxSpeed = -1f;
        private bool baseAwd;

        // change the numbers in Play mode, then right-click the component > "Apply Now" to feel them at once
        [ContextMenu("Apply Now")]
        public void Apply()
        {
            var body = GetComponent<Rigidbody2D>();
            var car = GetComponent<CarController>();
            var drive = GetComponent<BKS_BuiltVehicleDrive>();
            if (body == null || car == null) return;

            // tyres
            var mat = new PhysicsMaterial2D("BKS_HillClimbTyre") { friction = tyreFriction, bounciness = tyreBounciness };
            if (drive != null)
            {
                if (drive.rearTyre != null) drive.rearTyre.sharedMaterial = mat;
                if (drive.frontTyre != null) drive.frontTyre.sharedMaterial = mat;
                foreach (var j in new[] { drive.rearJoint, drive.frontJoint })
                {
                    if (j == null) continue;
                    var s = j.suspension; s.frequency = suspensionFrequency; s.dampingRatio = suspensionDamping; j.suspension = s;
                }
                if (baseTorque < 0f) { baseTorque = drive.motorTorque; baseAwd = drive.allWheelDrive; } // remember the vehicle's own values once
                drive.motorTorque = baseTorque * torqueMultiplier;
                drive.allWheelDrive = !rearWheelDriveOnly && baseAwd;
            }
            else if (car.motorWheel != null)
            {
                var s = car.motorWheel.suspension; s.frequency = suspensionFrequency; s.dampingRatio = suspensionDamping; car.motorWheel.suspension = s;
            }

            // top speed (store upgrades are added on top of this by BKS_UpgradeLoader)
            if (baseMaxSpeed < 0f) baseMaxSpeed = car.maxSpeed;
            car.maxSpeed = baseMaxSpeed * topSpeedMultiplier;

            // centre of mass: middle of the body box (the body's own biggest BoxCollider2D)
            BoxCollider2D box = null;
            foreach (var b in GetComponents<BoxCollider2D>()) if (!b.isTrigger && (box == null || b.size.x > box.size.x)) box = b;
            if (box != null) body.centerOfMass = box.offset + Vector2.up * (box.size.y * centerOfMassHeight);

            // air control scales with the body's inertia so every vehicle spins alike
            car.RotateForce = body.inertia * airSpin * Mathf.Deg2Rad;
        }
    }
}
