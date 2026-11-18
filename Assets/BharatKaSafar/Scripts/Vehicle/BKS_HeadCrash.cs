using UnityEngine;
using BharatKaSafar.Integration;

namespace BharatKaSafar.Vehicle
{
    /// <summary>
    /// Hill Climb style crash: the driver's head touching the ground ends the run at once.
    /// Sits on a trigger circle around the driver's head (added by the New Vehicle Builder).
    /// </summary>
    public class BKS_HeadCrash : MonoBehaviour
    {
        [Tooltip("Seconds after spawn before a head hit counts (the car settles onto the road)")]
        public float graceTime = 1f;
        private float startTime;
        private bool crashed;

        private HingeJoint2D neck;
        private HingeJoint2D seat;
        private Rigidbody2D head;
        private float seatedAngle;
        private bool hadSeatLimits;
        private bool hadNeckLimits;
        private float restingAngle;

        private void Awake()
        {
            neck = GetComponentInParent<HingeJoint2D>();
            if (neck == null || neck.connectedBody == null) return;
            head = neck.attachedRigidbody;
            seat = neck.connectedBody.GetComponent<HingeJoint2D>();
            if (seat != null && seat.connectedBody != null)
                seatedAngle = Mathf.DeltaAngle(seat.connectedBody.rotation, neck.connectedBody.rotation);
            restingAngle = Mathf.DeltaAngle(neck.connectedBody.rotation, head.rotation);
        }

        private void Start() { startTime = Time.time; }

        private void FixedUpdate()
        {
            if (crashed || head == null || neck == null || !neck.enabled || !neck.useLimits || neck.connectedBody == null) return;
            var torso = neck.connectedBody;
            float error = Mathf.DeltaAngle(head.rotation, torso.rotation + restingAngle);
            float relativeSpeed = head.angularVelocity - torso.angularVelocity;
            // A damped spring about the painted pose yields on impact and releases on a crash.
            float torque = Mathf.Clamp((18f * error - 1.6f * relativeSpeed) * Mathf.Deg2Rad, -2f, 2f);
            head.AddTorque(torque);
            torso.AddTorque(-torque);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (crashed || Time.time - startTime < graceTime) return;
            if (!col.CompareTag("Ground") && !col.CompareTag("rope2D")) return;
            if (BKS_GameplayBridge.Instance == null) return;
            BKS_GameplayBridge.Instance.ReportFailure("Driver crashed");
        }

        public bool ReleaseOnDeath()
        {
            if (crashed || head == null || neck == null || neck.connectedBody == null) return false;
            crashed = true;
            hadNeckLimits = neck.useLimits;
            neck.useLimits = false;
            if (seat != null)
            {
                hadSeatLimits = seat.useLimits;
                seat.useLimits = false;
            }

            // At low speed the two bodies can be balanced and nearly motionless.
            // Start a small nod there, leaving the impact's own rotation intact on faster crashes.
            var torso = neck.connectedBody;
            float relativeSpeed = head.angularVelocity - torso.angularVelocity;
            if (Mathf.Abs(relativeSpeed) < 35f)
            {
                float direction = Mathf.Abs(relativeSpeed) > 1f ? Mathf.Sign(relativeSpeed) : -1f;
                float impulse = direction * 65f * Mathf.Deg2Rad * head.inertia;
                head.AddTorque(impulse, ForceMode2D.Impulse);
                torso.AddTorque(-impulse, ForceMode2D.Impulse);
            }
            return true;
        }

        public void RestoreAfterRevive()
        {
            startTime = Time.time;
            if (!crashed) return;
            if (seat != null) RestoreJoint(seat, seatedAngle, hadSeatLimits);
            RestoreJoint(neck, restingAngle, hadNeckLimits);
            crashed = false;
        }

        private static void RestoreJoint(HingeJoint2D joint, float restAngle, bool limits)
        {
            if (joint == null || joint.connectedBody == null) return;
            var body = joint.attachedRigidbody;
            var parent = joint.connectedBody;
            var pose = Quaternion.Euler(0f, 0f, parent.rotation + restAngle);
            Vector3 anchor = parent.transform.TransformPoint(joint.connectedAnchor);
            Vector3 offset = pose * Vector3.Scale(joint.anchor, body.transform.lossyScale);
            body.transform.SetPositionAndRotation(anchor - offset, pose);
            body.position = body.transform.position;
            body.rotation = parent.rotation + restAngle;
            body.linearVelocity = parent.linearVelocity;
            body.angularVelocity = parent.angularVelocity;
            joint.useLimits = limits;
        }
    }
}
