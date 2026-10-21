using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Editor
{
    public class BKS_VehiclePrefabMaker : EditorWindow
    {
        private BKS_VehicleRecipe recipe;
        private UnityEditor.Editor recipeEditor;
        private GameObject preview;
        private Vector2 scroll;
        private string status;

        [MenuItem("Bharat Ka Safar/Vehicle Prefab Maker V1")]
        public static void Open() => GetWindow<BKS_VehiclePrefabMaker>("Vehicle Maker V1");

        private void OnEnable()
        {
            if (recipe == null) recipe = CreateInstance<BKS_VehicleRecipe>();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Bharat Ka Safar — Vehicle Prefab Maker V1", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Assign separate sprites. Coordinates are local world units; artwork faces right. Generate a preview to edit colliders and joints in Scene View, or generate a prefab directly.", MessageType.Info);
            var chosen = (BKS_VehicleRecipe)EditorGUILayout.ObjectField("Saved recipe", EditorUtility.IsPersistent(recipe) ? recipe : null, typeof(BKS_VehicleRecipe), false);
            if (chosen != null && chosen != recipe)
            {
                ReleaseEditor();
                if (!EditorUtility.IsPersistent(recipe)) DestroyImmediate(recipe);
                recipe = chosen;
            }
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Recipe"))
                {
                    ReleaseEditor();
                    if (recipe != null && !EditorUtility.IsPersistent(recipe)) DestroyImmediate(recipe);
                    recipe = CreateInstance<BKS_VehicleRecipe>();
                }
                if (GUILayout.Button("Save Recipe")) SaveRecipe();
            }
            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (recipeEditor == null) recipeEditor = UnityEditor.Editor.CreateEditor(recipe);
            recipeEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Build Scene Preview", GUILayout.Height(30))) Execute(() =>
                {
                    if (preview != null) Undo.DestroyObjectImmediate(preview);
                    preview = Build(recipe);
                    Undo.RegisterCreatedObjectUndo(preview, "Build BKS Vehicle Preview");
                    Selection.activeGameObject = preview;
                    SceneView.lastActiveSceneView?.FrameSelected();
                    status = "Preview created. Edit wheel transforms and WheelJoint anchors together, then Save Preview as Prefab.";
                });
                if (GUILayout.Button("Generate Reusable Prefab", GUILayout.Height(34))) Execute(() =>
                {
                    var generated = Build(recipe);
                    try { SavePrefab(generated); }
                    finally { DestroyImmediate(generated); }
                });
                using (new EditorGUI.DisabledScope(preview == null))
                    if (GUILayout.Button("Save Edited Preview as Prefab")) Execute(() => SavePrefab(preview));
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.Info);
        }

        private void Execute(Action action)
        {
            try { action(); }
            catch (Exception error) { status = error.Message; Debug.LogException(error); }
        }

        private void SaveRecipe()
        {
            string path = EditorUtility.SaveFilePanelInProject("Save Vehicle Recipe", "VehicleRecipe", "asset", "Save this setup for reuse.");
            if (string.IsNullOrEmpty(path)) return;
            var copy = Instantiate(recipe);
            AssetDatabase.CreateAsset(copy, AssetDatabase.GenerateUniqueAssetPath(path));
            ReleaseEditor();
            if (!EditorUtility.IsPersistent(recipe)) DestroyImmediate(recipe);
            recipe = copy;
            AssetDatabase.SaveAssets();
        }

        private void SavePrefab(GameObject root)
        {
            ValidateVehicle(root);
            string folder = recipe.outputFolder.Replace('\\', '/').TrimEnd('/');
            if (folder != "Assets" && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                throw new InvalidOperationException("Output folder must be inside Assets.");
            foreach (string segment in folder.Split('/'))
                if (segment == ".." || segment == "." || string.IsNullOrWhiteSpace(segment))
                    throw new InvalidOperationException("Use a normal Assets folder path without dot segments.");
            EnsureFolder(folder);
            string name = recipe.vehicleName;
            foreach (char character in Path.GetInvalidFileNameChars()) name = name.Replace(character, '_');
            name = name.Replace('/', '_').Replace('\\', '_').Trim();
            if (string.IsNullOrEmpty(name)) name = "BKS Vehicle";
            string path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + name + ".prefab");
            var asset = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || asset == null) throw new InvalidOperationException("Unity could not save the vehicle prefab.");
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            status = "Generated " + path + ". Existing prefabs were preserved.";
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        public static GameObject Build(BKS_VehicleRecipe r)
        {
            if (r == null || r.bodySprite == null || r.rearTyreSprite == null || r.frontTyreSprite == null)
                throw new InvalidOperationException("Assign car body, rear tyre and front tyre sprites first.");
            if (r.bodyScale.x <= 0 || r.bodyScale.y <= 0 || r.bodyMass <= 0 || r.wheelMass <= 0 ||
                r.rearWheelRadius <= 0 || r.frontWheelRadius <= 0 || r.forwardSpeed <= 0 || r.reverseSpeed <= 0 ||
                r.motorTorque <= 0 || r.brakingTorque <= 0 || r.suspensionFrequency <= 0)
                throw new InvalidOperationException("Scales, masses, radii, speeds, torques and suspension frequency must be positive.");
            if (r.bodyCollider == BKS_BodyCollider.Box && (r.boxSize.x <= 0 || r.boxSize.y <= 0))
                throw new InvalidOperationException("Box collider dimensions must be positive.");
            if (r.driverMode != BKS_DriverMode.None && (r.playerBodySprite == null || r.playerHeadSprite == null))
                throw new InvalidOperationException("Assign both player body and head sprites, or choose Driver Mode: None.");
            if (r.driverMode != BKS_DriverMode.None && (r.playerBodyScale <= 0 || r.playerHeadScale <= 0 || r.headColliderRadius <= 0))
                throw new InvalidOperationException("Driver scales and head radius must be positive.");
            if (r.driverMode == BKS_DriverMode.Ragdoll && (r.torsoMass <= 0 || r.headMass <= 0 ||
                r.torsoLimits.x > r.torsoLimits.y || r.neckLimits.x > r.neckLimits.y))
                throw new InvalidOperationException("Ragdoll masses must be positive and joint limit minimum must not exceed maximum.");

            var root = new GameObject(r.vehicleName);
            try
            {
                var chassis = Child("Body", root.transform, Vector2.zero);
                chassis.tag = "Car";
                var body = PhysicsBody(chassis, r.bodyMass);
                var visual = SpriteObject("Body Sprite", chassis.transform, r.bodySprite, Vector2.zero, 10);
                visual.tag = "Player";
                visual.transform.localScale = new Vector3(r.bodyScale.x, r.bodyScale.y, 1);
                Collider2D bodyCollider;
                if (r.bodyCollider == BKS_BodyCollider.Box)
                {
                    var box = visual.AddComponent<BoxCollider2D>(); box.size = r.boxSize; box.offset = r.boxOffset; bodyCollider = box;
                }
                else bodyCollider = visual.AddComponent<PolygonCollider2D>();
                var com = Child("Centre of Mass", chassis.transform, r.centreOfMass);
                body.centerOfMass = r.centreOfMass;
                var rear = Wheel("Rear Tyre", root.transform, r.rearTyreSprite, r.rearWheelPosition, r.rearWheelRadius, r);
                var front = Wheel("Front Tyre", root.transform, r.frontTyreSprite, r.frontWheelPosition, r.frontWheelRadius, r);
                var rearJoint = Suspension(chassis, rear.GetComponent<Rigidbody2D>(), r.rearWheelPosition, r);
                var frontJoint = Suspension(chassis, front.GetComponent<Rigidbody2D>(), r.frontWheelPosition, r);
                var controller = chassis.AddComponent<CarController>();
                controller.useBuilderPhysics = true;
                controller.centerOfMass = com.transform;
                controller.motorWheel = rearJoint;
                controller.maxSpeed = r.forwardSpeed;
                controller.RotateForce = r.airRotationTorque;
                controller.isMobile = r.mobileOnlyInput;
                controller.cameraDistance = r.cameraDistance;
                if (r.engineClip != null) { var audio = chassis.AddComponent<AudioSource>(); audio.clip = r.engineClip; audio.playOnAwake = false; audio.loop = true; }
                var drive = chassis.AddComponent<BKS_BuiltVehicleDrive>();
                drive.rearJoint = rearJoint; drive.frontJoint = frontJoint;
                drive.rearTyre = rear.GetComponent<CircleCollider2D>(); drive.frontTyre = front.GetComponent<CircleCollider2D>();
                drive.allWheelDrive = r.allWheelDrive; drive.reverseSpeed = r.reverseSpeed;
                drive.motorTorque = r.motorTorque; drive.brakingTorque = r.brakingTorque; drive.reverseEngageSpeed = r.reverseEngageSpeed;
                if (r.addHorn) chassis.AddComponent<BKS_HornController>();
                var pickup = Child("Coin Pickup Trigger", chassis.transform, Vector2.zero);
                pickup.tag = "CoinTrigger";
                var pickupBox = pickup.AddComponent<BoxCollider2D>(); pickupBox.isTrigger = true;
                pickupBox.size = new Vector2(Mathf.Abs(r.frontWheelPosition.x - r.rearWheelPosition.x) + 2 * Mathf.Max(r.rearWheelRadius, r.frontWheelRadius), 2.5f);
                Driver(r, root, chassis, body, bodyCollider, drive.rearTyre, drive.frontTyre);
                ValidateVehicle(root);
                return root;
            }
            catch { DestroyImmediate(root); throw; }
        }

        private static void Driver(BKS_VehicleRecipe r, GameObject root, GameObject chassis, Rigidbody2D body, params Collider2D[] vehicleColliders)
        {
            if (r.driverMode == BKS_DriverMode.None) return;
            bool ragdoll = r.driverMode == BKS_DriverMode.Ragdoll;
            var parent = ragdoll ? root.transform : chassis.transform;
            float vehicleWidth = chassis.transform.Find("Body Sprite").GetComponent<SpriteRenderer>().bounds.size.x;
            float torsoScale = DriverScale(r.playerBodySprite, r.playerBodyScale, vehicleWidth, r.torsoHeightToVehicleWidth, r.autoFitDriver);
            float headScale = DriverScale(r.playerHeadSprite, r.playerHeadScale, vehicleWidth, r.headHeightToVehicleWidth, r.autoFitDriver);
            var torso = SpriteObject("Player Body", parent, r.playerBodySprite, r.playerBodyPosition, 11);
            torso.transform.localScale = new Vector3(torsoScale, torsoScale, 1);
            var head = SpriteObject("Player Head", parent, r.playerHeadSprite, r.playerHeadPosition, 12);
            head.transform.localScale = new Vector3(headScale, headScale, 1);
            if (ragdoll)
            {
                var torsoBody = PhysicsBody(torso, r.torsoMass);
                var headBody = PhysicsBody(head, r.headMass);
                var torsoCollider = torso.AddComponent<PolygonCollider2D>();
                var headCollider = head.AddComponent<CircleCollider2D>();
                headCollider.radius = r.headColliderRadius / headScale;
                Hinge(torso, body, root.transform.TransformPoint(r.seatAnchor), r.torsoLimits);
                Hinge(head, torsoBody, root.transform.TransformPoint(r.neckAnchor), r.neckLimits);
                var filter = root.AddComponent<BKS_DriverCollisionFilter>();
                filter.driverColliders = new Collider2D[] { torsoCollider, headCollider };
                filter.vehicleColliders = vehicleColliders;
                // Head/torso collision is disabled by the neck joint; ground remains collidable.
            }
            if (r.addHeadCrashSensor)
            {
                var sensor = Child("Head Crash Sensor", head.transform, Vector2.zero);
                var trigger = sensor.AddComponent<CircleCollider2D>();
                trigger.radius = r.headColliderRadius / headScale * 1.1f;
                trigger.isTrigger = true;
                sensor.AddComponent<DeadTrigger>();
            }
        }

        public static float DriverScale(Sprite sprite, float multiplier, float vehicleWidth, float heightRatio, bool autoFit)
        {
            if (!autoFit) return multiplier;
            return Mathf.Max(0.001f, vehicleWidth * heightRatio / Mathf.Max(0.001f, sprite.bounds.size.y) * multiplier);
        }

        private static GameObject Child(string name, Transform parent, Vector2 position)
        {
            var node = new GameObject(name); node.transform.SetParent(parent, false); node.transform.localPosition = position; return node;
        }
        private static GameObject SpriteObject(string name, Transform parent, Sprite sprite, Vector2 position, int order)
        {
            var node = Child(name, parent, position); var renderer = node.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sortingOrder = order; return node;
        }
        private static Rigidbody2D PhysicsBody(GameObject node, float mass)
        {
            var body = node.AddComponent<Rigidbody2D>(); body.mass = mass; body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; return body;
        }
        private static GameObject Wheel(string name, Transform parent, Sprite sprite, Vector2 position, float radius, BKS_VehicleRecipe r)
        {
            var node = SpriteObject(name, parent, sprite, position, 13);
            float artworkRadius = Mathf.Max(0.001f, Mathf.Max(sprite.bounds.extents.x, sprite.bounds.extents.y));
            node.transform.localScale = Vector3.one * radius / artworkRadius;
            PhysicsBody(node, r.wheelMass);
            var circle = node.AddComponent<CircleCollider2D>(); circle.radius = artworkRadius; circle.sharedMaterial = r.tyreMaterial;
            return node;
        }
        private static WheelJoint2D Suspension(GameObject chassis, Rigidbody2D wheel, Vector2 anchor, BKS_VehicleRecipe r)
        {
            var joint = chassis.AddComponent<WheelJoint2D>(); joint.connectedBody = wheel; joint.autoConfigureConnectedAnchor = false;
            joint.anchor = anchor; joint.connectedAnchor = Vector2.zero; joint.enableCollision = false;
            joint.suspension = new JointSuspension2D { dampingRatio = r.suspensionDamping, frequency = r.suspensionFrequency, angle = 90f };
            joint.useMotor = false; joint.motor = new JointMotor2D { motorSpeed = 0, maxMotorTorque = r.motorTorque }; return joint;
        }
        private static void Hinge(GameObject node, Rigidbody2D connected, Vector3 worldAnchor, Vector2 limits)
        {
            var joint = node.AddComponent<HingeJoint2D>(); joint.connectedBody = connected; joint.autoConfigureConnectedAnchor = false;
            joint.anchor = node.transform.InverseTransformPoint(worldAnchor);
            joint.connectedAnchor = connected.transform.InverseTransformPoint(worldAnchor);
            joint.enableCollision = false; joint.useLimits = true;
            joint.limits = new JointAngleLimits2D { min = limits.x, max = limits.y };
        }

        public static void ValidateVehicle(GameObject root)
        {
            var controller = root.GetComponentInChildren<CarController>();
            var drive = root.GetComponentInChildren<BKS_BuiltVehicleDrive>();
            if (controller == null || drive == null || controller.centerOfMass == null || controller.motorWheel == null)
                throw new InvalidOperationException("Vehicle is missing its controller, drive, centre of mass or motor wheel.");
            foreach (var joint in controller.GetComponents<WheelJoint2D>())
                if (joint.connectedBody == null) throw new InvalidOperationException("A wheel joint has no connected wheel rigidbody.");
            if (drive.rearTyre == null || drive.frontTyre == null)
                throw new InvalidOperationException("Both tyre colliders must be assigned.");
        }

        private void ReleaseEditor() { if (recipeEditor != null) DestroyImmediate(recipeEditor); }
        private void OnDisable()
        {
            ReleaseEditor();
            if (recipe != null && !EditorUtility.IsPersistent(recipe)) DestroyImmediate(recipe);
        }
    }
}
