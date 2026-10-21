using System;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Editor
{
    public static class BKS_VehicleMakerChecks
    {
        [MenuItem("Bharat Ka Safar/Vehicle Maker/Run V1 Validation Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Run these checks outside Play Mode.");
            var texture = new Texture2D(64, 32);
            var sprite = Sprite.Create(texture, new Rect(0, 0, 64, 32), new Vector2(0.5f, 0.5f), 16);
            var recipe = ScriptableObject.CreateInstance<BKS_VehicleRecipe>();
            try
            {
                recipe.bodySprite = recipe.rearTyreSprite = recipe.frontTyreSprite = sprite;
                recipe.playerBodySprite = recipe.playerHeadSprite = sprite;
                Check(Mathf.Abs(BKS_VehiclePrefabMaker.DriverScale(sprite, 1f, 4f, 0.14f, true) * sprite.bounds.size.y - 0.56f) < 0.001f, "Driver image normalized to vehicle size");
                foreach (BKS_DriverMode mode in Enum.GetValues(typeof(BKS_DriverMode)))
                {
                    recipe.driverMode = mode;
                    var root = BKS_VehiclePrefabMaker.Build(recipe);
                    try
                    {
                        BKS_VehiclePrefabMaker.ValidateVehicle(root);
                        var drive = root.GetComponentInChildren<BKS_BuiltVehicleDrive>();
                        Check(drive.rearJoint.connectedBody == drive.rearTyre.attachedRigidbody, "Rear wheel connection");
                        Check(drive.frontJoint.connectedBody == drive.frontTyre.attachedRigidbody, "Front wheel connection");
                        Check(drive.rearJoint.connectedAnchor == Vector2.zero, "Wheel anchor");
                        Check(root.GetComponentInChildren<CarController>().CompareTag("Car"), "Gameplay vehicle tag");
                        Check(root.GetComponentsInChildren<HingeJoint2D>().Length == (mode == BKS_DriverMode.Ragdoll ? 2 : 0), "Driver joints");
                        Check(root.GetComponentsInChildren<DeadTrigger>().Length == (mode == BKS_DriverMode.None ? 0 : 1), "Driver crash sensor");
                        if (mode == BKS_DriverMode.Ragdoll)
                        {
                            Check(root.GetComponent<BKS_DriverCollisionFilter>() != null, "Driver collision filtering");
                            foreach (var hinge in root.GetComponentsInChildren<HingeJoint2D>())
                            {
                                Vector3 anchor = hinge.transform.TransformPoint(hinge.anchor);
                                Vector3 connected = hinge.connectedBody.transform.TransformPoint(hinge.connectedAnchor);
                                Check(Vector3.Distance(anchor, connected) < 0.001f, "Aligned ragdoll anchors");
                            }
                        }
                    }
                    finally { UnityEngine.Object.DestroyImmediate(root); }
                }
                Check(BKS_BuiltVehicleDrive.ShouldBrake(-1, 8, 0.25f), "Brake forward before reverse");
                Check(!BKS_BuiltVehicleDrive.ShouldBrake(-1, 0.1f, 0.25f), "Reverse engages near stop");
                Check(!BKS_BuiltVehicleDrive.ShouldBrake(-1, -2, 0.25f), "Continue reversing");
                Check(BKS_BuiltVehicleDrive.ShouldBrake(1, -3, 0.25f), "Brake reverse before forward");
                Check(!BKS_BuiltVehicleDrive.ShouldBrake(0, 8, 0.25f), "Coast with pedals released");
                recipe.bodySprite = null;
                bool rejected = false;
                try { BKS_VehiclePrefabMaker.Build(recipe); } catch (InvalidOperationException) { rejected = true; }
                Check(rejected, "Reject missing artwork");
                Debug.Log("[BKS Vehicle Maker] PASS: three driver modes, connected wheels, joint anchors, crash sensors, input transitions and missing-art validation. Play Mode physics testing is still required.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(recipe);
                UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        private static void Check(bool passed, string label)
        {
            if (!passed) throw new InvalidOperationException("Vehicle Maker check failed: " + label);
        }
    }
}
