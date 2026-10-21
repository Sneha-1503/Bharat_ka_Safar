using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    [RequireComponent(typeof(CarController), typeof(Rigidbody2D))]
    public class BKS_BuiltVehicleDrive : MonoBehaviour
    {
        public WheelJoint2D rearJoint;
        public WheelJoint2D frontJoint;
        public CircleCollider2D rearTyre;
        public CircleCollider2D frontTyre;
        public bool allWheelDrive;
        public float reverseSpeed = 5f;
        public float motorTorque = 1500f;
        public float brakingTorque = 3000f;
        public float reverseEngageSpeed = 0.25f;
        private CarController controller;
        private Rigidbody2D body;
        private readonly ContactPoint2D[] contacts = new ContactPoint2D[16];

        private void Awake()
        {
            controller = GetComponent<CarController>();
            body = GetComponent<Rigidbody2D>();
        }

        public static bool ShouldBrake(float input, float signedSpeed, float threshold)
        {
            return (input < 0 && signedSpeed > threshold) || (input > 0 && signedSpeed < -threshold);
        }

        private void FixedUpdate()
        {
            if (rearJoint == null || rearTyre == null) return;
            float input = controller.DriveInput;
            float signedSpeed = body.linearVelocity.x;
            bool braking = ShouldBrake(input, signedSpeed, Mathf.Max(0.01f, reverseEngageSpeed));
            Drive(rearJoint, rearTyre, input, signedSpeed, braking, true);
            if (frontJoint != null && frontTyre != null)
                Drive(frontJoint, frontTyre, input, signedSpeed, braking, allWheelDrive);
            if (!TouchesWorld(rearTyre) && !TouchesWorld(frontTyre))
                body.AddTorque(input * controller.RotateForce);
        }

        private void Drive(WheelJoint2D joint, CircleCollider2D tyre, float input, float speed, bool braking, bool powered)
        {
            float limit = input < 0 ? reverseSpeed : controller.maxSpeed;
            bool atLimit = input > 0 ? speed >= limit : speed <= -limit;
            joint.useMotor = braking || (powered && Mathf.Abs(input) > 0.01f && !atLimit);
            float radius = Mathf.Max(0.01f, tyre.radius * Mathf.Abs(tyre.transform.lossyScale.x));
            // Match the existing chassis-mounted CarController joint convention.
            joint.motor = new JointMotor2D
            {
                motorSpeed = braking ? 0f : -input * limit / radius * Mathf.Rad2Deg,
                // Existing engine upgrades/fuel shutdown modify CarController.motorPower.
                maxMotorTorque = braking ? brakingTorque : motorTorque * Mathf.Max(0f, controller.motorPower) / 1400f
            };
        }

        private bool TouchesWorld(Collider2D tyre)
        {
            if (tyre == null) return false;
            int count = tyre.GetContacts(contacts);
            for (int i = 0; i < count; i++)
            {
                var other = contacts[i].collider == tyre ? contacts[i].otherCollider : contacts[i].collider;
                if (other != null && !other.isTrigger && other.transform.root != transform.root) return true;
            }
            return false;
        }
    }
}
