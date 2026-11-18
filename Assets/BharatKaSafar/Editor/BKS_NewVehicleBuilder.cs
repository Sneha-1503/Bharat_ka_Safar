using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Store;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds the new cute vehicles (Art/Vehicles/New/&lt;name&gt;/&lt;name&gt;_rig.json + part PNGs) into Hill Climb style prefabs:
    /// body + two sprung wheels (WheelJoint2D), driver torso + head, head-hits-ground crash (BKS_HeadCrash),
    /// rear-wheel drive, nose lifts on full gas, gas/brake spin in the air. Bikes and cars get their own tuning.
    /// Menus:
    ///  • Bharat Ka Safar > Vehicles > Build New Vehicles      → Prefabs/Vehicles/New/*.prefab (old prefabs untouched)
    ///  • Bharat Ka Safar > Vehicles > Use New Vehicles In Store → Store sells only the new ones (jeep free);
    ///    the old list is kept in Resources/BKS_StoreCatalog_OldVehicles.asset
    /// </summary>
    public static class BKS_NewVehicleBuilder
    {
        private const string ArtRoot = "Assets/BharatKaSafar/Art/Vehicles/New";
        private const string OutFolder = "Assets/BharatKaSafar/Prefabs/Vehicles/New";
        private const float CarWidth = 4.6f, BikeWidth = 3.6f;   // body width in world units

        // store order + price (first = free starter)
        private static readonly (string name, string title, int price)[] Lineup =
        {
            ("01-jeep", "Jeep", 0), ("02-scooter", "Scooter", 1500), ("01-dirt-bike", "Dirt Bike", 3000),
            ("02-dune-buggy", "Dune Buggy", 5000), ("04-tractor", "Tractor", 7500), ("03-sport-bike", "Sport Bike", 10000),
            ("03-roadster", "Roadster", 14000), ("04-scrambler", "Scrambler", 18000), ("05-cruiser", "Cruiser", 24000),
            ("05-monster-truck", "Monster Truck", 32000),
        };

        [System.Serializable] private class Part { public float cx, cy, scale, angle; public float[] px; }
        [System.Serializable] private class Parts { public Part body, wheel_rear, wheel_front, head, torso; }
        [System.Serializable] private class Rig { public string name, kind; public float[] body_px; public Parts parts; }

        [MenuItem("Bharat Ka Safar/Vehicles/Build New Vehicles")]
        public static void BuildAll()
        {
            int built = 0;
            foreach (var l in Lineup) if (Build(l.name, l.title) != null) built++;
            AssetDatabase.SaveAssets();
            Debug.Log($"[NewVehicles] Built {built} prefabs in {OutFolder}. Drag one into a scene to test, or run 'Use New Vehicles In Store'.");
        }

        [MenuItem("Bharat Ka Safar/Vehicles/Build Jeep Only (test)")]
        public static void BuildJeep() { Build("01-jeep", "Jeep"); AssetDatabase.SaveAssets(); }

        public static GameObject Build(string name, string title)
        {
            string dir = $"{ArtRoot}/{name}";
            string rigPath = $"{dir}/{name}_rig.json";
            if (!File.Exists(rigPath)) { Debug.LogWarning("[NewVehicles] Missing " + rigPath); return null; }
            var rig = JsonUtility.FromJson<Rig>(File.ReadAllText(rigPath));
            bool bike = rig.kind == "bike";
            float ppu = rig.body_px[0] / (bike ? BikeWidth : CarWidth);
            float bw = rig.body_px[0], bh = rig.body_px[1];

            // one pixels-per-unit for every part, so the rig's relative scales apply directly
            Sprite S(string part)
            {
                string p = $"{dir}/{name}_{part}.png";
                var imp = (TextureImporter)AssetImporter.GetAtPath(p);
                if (imp == null) return null;
                if (imp.textureType != TextureImporterType.Sprite || imp.spriteImportMode != SpriteImportMode.Single || Mathf.Abs(imp.spritePixelsPerUnit - ppu) > 0.01f)
                {
                    imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = ppu; imp.mipmapEnabled = false; imp.alphaIsTransparency = true;
                    imp.SaveAndReimport();
                }
                return AssetDatabase.LoadAssetAtPath<Sprite>(p);
            }
            // rig centre (body pixels, y down) → chassis local units
            Vector2 Pos(Part p) => new Vector2((p.cx - bw / 2f) / ppu, -(p.cy - bh / 2f) / ppu);
            float Radius(Part p) => p.px[0] * p.scale / 2f / ppu;

            var r = ScriptableObject.CreateInstance<BKS_VehicleRecipe>();
            r.vehicleName = "BKS_New_" + title.Replace(" ", "");
            r.bodySprite = S("body"); r.rearTyreSprite = S("wheel_rear"); r.frontTyreSprite = S("wheel_front");
            r.playerBodySprite = S("torso"); r.playerHeadSprite = S("head");
            if (r.bodySprite == null || r.rearTyreSprite == null || r.frontTyreSprite == null) { Debug.LogWarning("[NewVehicles] Missing sprites for " + name); return null; }

            r.bodyCollider = BKS_BodyCollider.Polygon;
            r.rearWheelPosition = Pos(rig.parts.wheel_rear); r.frontWheelPosition = Pos(rig.parts.wheel_front);
            r.rearWheelRadius = Radius(rig.parts.wheel_rear); r.frontWheelRadius = Radius(rig.parts.wheel_front);

            // ragdoll driver: torso hinged at the seat, head hinged at the neck — sways with bumps and landings,
            // limits keep him sitting; the head is the crash point
            r.driverMode = BKS_DriverMode.Ragdoll; r.autoFitDriver = false;
            r.playerBodyPosition = Pos(rig.parts.torso); r.playerBodyScale = rig.parts.torso.scale;
            r.playerHeadPosition = Pos(rig.parts.head); r.playerHeadScale = rig.parts.head.scale;
            float torsoH = rig.parts.torso.px[1] * rig.parts.torso.scale / ppu, headH = rig.parts.head.px[1] * rig.parts.head.scale / ppu;
            r.playerBodyAngle = rig.parts.torso.angle;
            float torsoW = rig.parts.torso.px[0] * rig.parts.torso.scale / ppu;
            // On the bike sprites the hips are behind the centre, above the boots.
            Vector2 hips = bike ? new Vector2(-0.29f * torsoW, -0.10f * torsoH) : Vector2.down * torsoH * 0.38f;
            r.seatAnchor = r.playerBodyPosition + (Vector2)(Quaternion.Euler(0f, 0f, r.playerBodyAngle) * hips);
            r.neckAnchor = r.playerHeadPosition + Vector2.down * headH * 0.36f;    // chin
            r.torsoLimits = bike ? new Vector2(-8f, 8f) : new Vector2(-12f, 12f);
            r.neckLimits = new Vector2(-10f, 10f);
            r.torsoMass = bike ? 0.35f : 0.3f; r.headMass = 0.12f;
            r.headColliderRadius = Mathf.Max(0.05f, rig.parts.head.px[0] * rig.parts.head.scale / 2f / ppu * 0.75f);
            r.addHeadCrashSensor = true; r.addHorn = true;

            // ── Hill Climb tuning ─────────────────────────────────────────────────
            // heavy body, light wheels, centre of mass a little low so gas lifts the nose but it doesn't flip on every bump
            float axle = (r.rearWheelPosition.y + r.frontWheelPosition.y) / 2f;
            if (bike)
            {
                r.bodyMass = 5f; r.wheelMass = 0.5f; r.forwardSpeed = 27f; r.reverseSpeed = 6f;
                r.motorTorque = 2000f; r.brakingTorque = 3000f; r.suspensionFrequency = 5f; r.suspensionDamping = 0.45f;
                r.centreOfMass = new Vector2(0f, axle + 0.55f);
            }
            else
            {
                r.bodyMass = 8f; r.wheelMass = 0.8f; r.forwardSpeed = 25f; r.reverseSpeed = 7f;
                r.motorTorque = 2600f; r.brakingTorque = 4000f; r.suspensionFrequency = 4.5f; r.suspensionDamping = 0.4f;
                r.centreOfMass = new Vector2(0f, axle + 0.45f);
            }
            r.allWheelDrive = false;
            r.cameraDistance = 15f;
            r.outputFolder = OutFolder;

            var root = BKS_VehiclePrefabMaker.Build(r);
            try
            {
                var chassis = root.transform.Find("Body");
                // taller coin pickup box: grabs coins floating above the road even when the car is low or in a dip
                var pick = chassis.Find("Coin Pickup Trigger")?.GetComponent<BoxCollider2D>();
                if (pick != null) { pick.size = new Vector2(pick.size.x, 5f); pick.offset = new Vector2(0f, 1f); }
                // bike rider sits on top of the bike; in a car the door hides the driver's legs
                // ragdoll parts sit on the root (static ones on the chassis)
                var torso = root.transform.Find("Player Body") ?? chassis.Find("Player Body");
                var head = root.transform.Find("Player Head") ?? chassis.Find("Player Head");
                if (torso != null) torso.GetComponent<SpriteRenderer>().sortingOrder = bike ? 11 : 9;
                if (head != null)
                {
                    head.GetComponent<SpriteRenderer>().sortingOrder = 12;
                    var sensor = head.Find("Head Crash Sensor");
                    if (sensor != null) sensor.gameObject.AddComponent<BKS_HeadCrash>();  // instant crash (DeadTrigger only flags it)
                }

                // the runtime feel component, pre-set per kind (the spawner keeps it instead of adding the default one)
                var feel = chassis.gameObject.AddComponent<BKS_HillClimbFeel>();
                feel.torqueMultiplier = 1f; feel.topSpeedMultiplier = 1f; feel.rearWheelDriveOnly = true;
                feel.tyreFriction = bike ? 1.1f : 1.0f; feel.tyreBounciness = 0.1f;
                feel.suspensionFrequency = r.suspensionFrequency; feel.suspensionDamping = r.suspensionDamping;
                feel.airSpin = bike ? 480f : 360f;

                EnsureFolder(OutFolder);
                string path = $"{OutFolder}/{r.vehicleName}.prefab";
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[NewVehicles] {title} ({rig.kind}) → {path}");
                return prefab;
            }
            finally { Object.DestroyImmediate(root); Object.DestroyImmediate(r); }
        }

        [MenuItem("Bharat Ka Safar/Vehicles/Use New Vehicles In Store")]
        public static void UseInStore()
        {
            var catalog = BKS_StoreCatalog.Load();
            if (catalog == null) { Debug.LogError("[NewVehicles] Resources/BKS_StoreCatalog not found."); return; }

            // keep the old list once, so it can be brought back
            const string backup = "Assets/Resources/BKS_StoreCatalog_OldVehicles.asset";
            if (AssetDatabase.LoadAssetAtPath<BKS_StoreCatalog>(backup) == null)
                AssetDatabase.CreateAsset(Object.Instantiate(catalog), backup);

            var list = new List<BKS_StoreVehicle>();
            foreach (var l in Lineup)
            {
                string title = l.title.Replace(" ", "");
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{OutFolder}/BKS_New_{title}.prefab");
                if (prefab == null) { Debug.LogWarning($"[NewVehicles] {l.title} prefab missing — run 'Build New Vehicles' first."); continue; }
                list.Add(new BKS_StoreVehicle
                {
                    id = "new_" + l.name, displayName = l.title, prefab = prefab, price = l.price, freeByDefault = l.price == 0,
                    icon = Icon($"{ArtRoot}/{l.name}/{l.name}_preview.png"),
                });
            }
            if (list.Count == 0) return;
            Undo.RecordObject(catalog, "Use New Vehicles");
            catalog.vehicles = list;
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[NewVehicles] Store now has {list.Count} new vehicles (Jeep free). Old list saved in {backup}.");
        }

        private static Sprite Icon(string path)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            if (imp == null) return null;
            if (imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false; imp.alphaIsTransparency = true; imp.maxTextureSize = 512;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
