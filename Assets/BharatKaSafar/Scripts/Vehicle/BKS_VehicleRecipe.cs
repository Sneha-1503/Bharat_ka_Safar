using UnityEngine;

namespace BharatKaSafar.Vehicle
{
    public enum BKS_DriverMode { None, Static, Ragdoll }
    public enum BKS_BodyCollider { Polygon, Box }

    [CreateAssetMenu(menuName = "Bharat Ka Safar/Vehicle Recipe", fileName = "VehicleRecipe")]
    public class BKS_VehicleRecipe : ScriptableObject
    {
        public string vehicleName = "BKS Vehicle";
        [Header("Separate artwork (vehicle faces right)")]
        public Sprite bodySprite;
        public Sprite rearTyreSprite;
        public Sprite frontTyreSprite;
        public Vector2 bodyScale = Vector2.one;
        [Header("Body physics")]
        [Min(0.1f)] public float bodyMass = 8f;
        public BKS_BodyCollider bodyCollider = BKS_BodyCollider.Polygon;
        public Vector2 boxSize = new Vector2(4f, 1.2f);
        public Vector2 boxOffset;
        public Vector2 centreOfMass = new Vector2(0, -0.35f);
        [Header("Tyres and suspension (positions in chassis local units)")]
        public Vector2 rearWheelPosition = new Vector2(-1.4f, -0.65f);
        public Vector2 frontWheelPosition = new Vector2(1.4f, -0.65f);
        [Min(0.05f)] public float rearWheelRadius = 0.55f;
        [Min(0.05f)] public float frontWheelRadius = 0.55f;
        [Min(0.05f)] public float wheelMass = 0.8f;
        [Min(0.01f)] public float suspensionFrequency = 5f;
        [Range(0, 1)] public float suspensionDamping = 0.7f;
        public PhysicsMaterial2D tyreMaterial;
        public bool allWheelDrive;
        [Header("Driving")]
        [Min(0.1f)] public float forwardSpeed = 14f;
        [Min(0.1f)] public float reverseSpeed = 5f;
        [Min(1)] public float motorTorque = 1500f;
        [Min(1)] public float brakingTorque = 3000f;
        [Min(0.01f)] public float reverseEngageSpeed = 0.25f;
        public float airRotationTorque = 140f;
        public bool mobileOnlyInput;
        public AudioClip engineClip;
        [Header("Player / driver")]
        public BKS_DriverMode driverMode = BKS_DriverMode.Static;
        public Sprite playerBodySprite;
        public Sprite playerHeadSprite;
        [Tooltip("Normalize source-image sizes to the vehicle width. The scales below remain manual multipliers.")]
        public bool autoFitDriver = true;
        [Range(0.05f, 0.6f)] public float torsoHeightToVehicleWidth = 0.24f;
        [Range(0.03f, 0.4f)] public float headHeightToVehicleWidth = 0.14f;
        public Vector2 playerBodyPosition = new Vector2(0, 0.6f);
        public Vector2 playerHeadPosition = new Vector2(0, 1.35f);
        [Min(0.01f)] public float playerBodyScale = 1f;
        [Min(0.01f)] public float playerHeadScale = 1f;
        public Vector2 seatAnchor = new Vector2(0, 0.35f);
        public Vector2 neckAnchor = new Vector2(0, 1.1f);
        public Vector2 torsoLimits = new Vector2(-15, 15);
        public Vector2 neckLimits = new Vector2(-25, 25);
        [Min(0.01f)] public float torsoMass = 0.3f;
        [Min(0.01f)] public float headMass = 0.15f;
        [Min(0.05f)] public float headColliderRadius = 0.25f;
        [Header("Integration")]
        public bool addHorn = true;
        public bool addHeadCrashSensor = true;
        [Min(0.1f)] public float cameraDistance = 15f;
        public string outputFolder = "Assets/BharatKaSafar/Prefabs/Vehicles";
    }
}
