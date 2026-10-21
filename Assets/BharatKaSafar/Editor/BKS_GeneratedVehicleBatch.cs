using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Builds vehicle prefabs from the parts in Art/Vehicles/Generated (body, tyre, driver body/head)
    /// and the measured layout in Art/Vehicles/Generated/vehicles.json, using BKS_VehiclePrefabMaker.
    /// Menu: Bharat Ka Safar > Vehicle Maker > Build Generated Vehicles
    /// </summary>
    public static class BKS_GeneratedVehicleBatch
    {
        private const string Root = "Assets/BharatKaSafar/Art/Vehicles/Generated";
        private const string PrefabFolder = "Assets/BharatKaSafar/Prefabs/Vehicles";
        private const float GravityScale = 2.1f; // same as the legacy car body

        [Serializable] private class Files { public string body, rear, front, dbody, dhead; }
        [Serializable] private class Entry
        {
            public string name, kind, note, error;
            public bool driverBehind;
            public Files files;
            public float bodyScale, rearR, frontR, torsoRatio, headRatio;
            public float[] rearPos, frontPos, torsoPos, headPos, seat, neck;
        }

        // Physics feel per vehicle: mass, forward speed, motor torque, all wheel drive, suspension frequency
        private static readonly Dictionary<string, (float mass, float speed, float torque, bool awd, float susp)> Feel =
            new Dictionary<string, (float, float, float, bool, float)>
        {
            { "AutoRickshaw",    (6f,  11f, 1200f, false, 4.5f) },
            { "BlueHatchback",   (7f,  15f, 1400f, false, 5f) },
            { "RedMini",         (6.5f,15f, 1400f, false, 5.5f) },
            { "BeetleClassic",   (7f,  13f, 1300f, false, 4.5f) },
            { "BlueSedan",       (8f,  14f, 1500f, false, 4.5f) },
            { "VintageRoadster", (7f,  14f, 1400f, false, 4f) },
            { "VintageTourer",   (8f,  13f, 1400f, false, 4f) },
            { "RedMuscleCar",    (8f,  18f, 1800f, false, 5.5f) },
            { "BlueConvertible", (7.5f,17f, 1700f, false, 5.5f) },
            { "PoliceSUV",       (9f,  16f, 1800f, true,  5f) },
            { "DeliveryVan",     (10f, 13f, 1800f, false, 4.5f) },
            { "OrangePickup",    (9f,  15f, 1900f, true,  4.5f) },
            { "AdventureJeep",   (9f,  14f, 2000f, true,  4.5f) },
            { "TNJeep",          (9f,  14f, 2000f, true,  4.5f) },
            { "RedJeep",         (9f,  14f, 2000f, true,  4.5f) },
            { "IndianArmyJeep",  (10f, 14f, 2200f, true,  4.5f) },
            { "DuneBuggy",       (6f,  17f, 1800f, true,  3.5f) },
            { "GreenTractor",    (12f, 9f,  2600f, false, 4f) },
        };

        [MenuItem("Bharat Ka Safar/Vehicle Maker/Build Generated Vehicles")]
        public static void BuildAll()
        {
            string jsonPath = Root + "/vehicles.json";
            if (!File.Exists(jsonPath)) { Debug.LogError("[Vehicle Batch] Missing " + jsonPath); return; }

            var entries = JsonUtility.FromJson<Wrapper>(WrapJson(File.ReadAllText(jsonPath))).items;
            var tyreMat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/HillClimb_Core/Materials/Tire.physicsMaterial2D");
            var engine = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/HillClimb_Core/Sounds/Engine Sound.wav");
            EnsureFolder(PrefabFolder);

            int ok = 0;
            var failed = new List<string>();
            foreach (var e in entries)
            {
                if (!string.IsNullOrEmpty(e.error)) { failed.Add(e.name + ": " + e.error); continue; }
                try
                {
                    string dir = Root + "/" + e.name + "/";
                    var r = ScriptableObject.CreateInstance<BKS_VehicleRecipe>();
                    r.vehicleName = "BKS_" + e.name;
                    r.bodySprite = Sprite(dir + e.files.body);
                    r.rearTyreSprite = Sprite(dir + e.files.rear);
                    r.frontTyreSprite = string.IsNullOrEmpty(e.files.front) ? r.rearTyreSprite : Sprite(dir + e.files.front);
                    r.bodyScale = Vector2.one * e.bodyScale;
                    r.bodyCollider = BKS_BodyCollider.Polygon;
                    r.rearWheelPosition = V(e.rearPos);
                    r.frontWheelPosition = V(e.frontPos);
                    r.rearWheelRadius = e.rearR;
                    r.frontWheelRadius = e.frontR;
                    r.centreOfMass = new Vector2((r.rearWheelPosition.x + r.frontWheelPosition.x) * 0.5f,
                                                 Mathf.Max(r.rearWheelPosition.y, r.frontWheelPosition.y) + 0.25f);
                    r.tyreMaterial = tyreMat;
                    r.engineClip = engine;
                    r.outputFolder = PrefabFolder;
                    r.cameraDistance = 18f;

                    if (Feel.TryGetValue(e.name, out var f))
                    {
                        // Match the legacy Hill Climb cars (body mass 12, gravity x2.1, motor force 10000)
                        // so generated vehicles feel just as heavy instead of floating.
                        r.bodyMass = f.mass * 1.3f; r.forwardSpeed = f.speed;
                        r.motorTorque = f.torque * 4f; r.brakingTorque = 8000f;
                        r.allWheelDrive = f.awd; r.suspensionFrequency = f.susp;
                    }
                    // Stiff springs: with gravity x2.1 a soft spring let the body sink until it scraped the ground.
                    // Sag is roughly g / (2*pi*f)^2, so f = 9 keeps it to a few centimetres.
                    r.wheelMass = 2f;
                    r.suspensionFrequency = Mathf.Max(r.suspensionFrequency * 2f, 8f);
                    r.suspensionDamping = 0.6f;

                    if (!string.IsNullOrEmpty(e.files.dbody))
                    {
                        r.driverMode = BKS_DriverMode.Ragdoll;
                        r.playerBodySprite = Sprite(dir + e.files.dbody);
                        r.playerHeadSprite = Sprite(dir + e.files.dhead);
                        r.autoFitDriver = true;
                        r.torsoHeightToVehicleWidth = e.torsoRatio;
                        r.headHeightToVehicleWidth = e.headRatio;
                        r.playerBodyPosition = V(e.torsoPos);
                        r.playerHeadPosition = V(e.headPos);
                        r.seatAnchor = V(e.seat);
                        r.neckAnchor = V(e.neck);
                        r.headColliderRadius = 6.5f * e.headRatio * 0.35f;
                        // Tight joints: heavier gravity made the ragdoll slump back to its old +-15 degree limit
                        r.torsoLimits = new Vector2(-6f, 6f);
                        r.neckLimits = new Vector2(-12f, 12f);
                        r.torsoMass = 0.5f; r.headMass = 0.2f;
                    }
                    else r.driverMode = BKS_DriverMode.None; // driver is painted into the body art

                    // Keep the recipe so the vehicle can be re-tuned in Vehicle Prefab Maker V1
                    string recipePath = dir + e.name + "_Recipe.asset";
                    AssetDatabase.DeleteAsset(recipePath);
                    AssetDatabase.CreateAsset(r, recipePath);

                    var go = BKS_VehiclePrefabMaker.Build(r);
                    try
                    {
                        // Closed cars: driver sits behind the body and shows through the windows
                        if (e.driverBehind)
                            foreach (var sr in go.GetComponentsInChildren<SpriteRenderer>())
                                if (sr.name == "Player Body") sr.sortingOrder = 8;
                                else if (sr.name == "Player Head") sr.sortingOrder = 9;
                        foreach (var rb in go.GetComponentsInChildren<Rigidbody2D>())
                            rb.gravityScale = GravityScale;
                        string path = PrefabFolder + "/" + r.vehicleName + ".prefab";
                        PrefabUtility.SaveAsPrefabAsset(go, path, out bool saved);
                        if (!saved) throw new InvalidOperationException("Could not save " + path);
                    }
                    finally { UnityEngine.Object.DestroyImmediate(go); }
                    ok++;
                }
                catch (Exception ex) { failed.Add(e.name + ": " + ex.Message); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vehicle Batch] Built {ok} vehicle prefabs in {PrefabFolder}." +
                      (failed.Count > 0 ? "\nFailed:\n" + string.Join("\n", failed) : ""));
        }

        private static Sprite Sprite(string path)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) throw new FileNotFoundException("Missing image " + path);
            if (imp.textureType != TextureImporterType.Sprite || imp.spritePixelsPerUnit != 100f || !imp.alphaIsTransparency)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 100f;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
                s.spriteAlignment = (int)SpriteAlignment.Center; imp.SetTextureSettings(s);
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Vector2 V(float[] a) => a != null && a.Length == 2 ? new Vector2(a[0], a[1]) : Vector2.zero;

        // vehicles.json is a dictionary keyed by name; turn it into an array JsonUtility can read
        [Serializable] private class Wrapper { public Entry[] items; }
        private static string WrapJson(string dict)
        {
            var sb = new System.Text.StringBuilder("{\"items\":[");
            int depth = 0; bool inStr = false, first = true; int objStart = -1;
            for (int i = 0; i < dict.Length; i++)
            {
                char c = dict[i];
                if (c == '"' && dict[i - 1] != '\\') inStr = !inStr;
                if (inStr) continue;
                if (c == '{') { depth++; if (depth == 2) objStart = i; }
                else if (c == '}') { if (depth == 2) { if (!first) sb.Append(','); sb.Append(dict, objStart, i - objStart + 1); first = false; } depth--; }
            }
            return sb.Append("]}").ToString();
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
