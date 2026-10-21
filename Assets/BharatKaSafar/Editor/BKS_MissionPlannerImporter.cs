using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Imports the 140-mission planner (Data/Missions/BKS_Missions.csv, exported from
    /// Bharat_Ka_Safar_140_Missions.xlsx) into MissionData + ChapterData assets.
    ///  • Existing missions are reused by place (keeps their IDs, scenes and saves).
    ///  • Every chapter's mission order follows the sheet; nextMissionID is chained.
    ///  • Every chapter opens the shared Mission Select scene (BKS_MissionSelect).
    /// Menu: Bharat Ka Safar > Data > Import 140 Missions (CSV)
    /// </summary>
    public static class BKS_MissionPlannerImporter
    {
        public const string CsvPath = "Assets/BharatKaSafar/Data/Missions/BKS_Missions.csv";
        public const string MissionSelectScene = "BKS_MissionSelect";
        private const string Art = "Assets/BharatKaSafar/Art/Missions/";
        private const string SO = "Assets/BharatKaSafar/ScriptableObjects/";

        // Chapter IDs: existing assets keep theirs (UttarPradesh, RJ, TN, PB)
        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            {"Andhra Pradesh","AP"},{"Arunachal Pradesh","AR"},{"Assam","AS"},{"Bihar","BR"},{"Chhattisgarh","CG"},
            {"Goa","GA"},{"Gujarat","GJ"},{"Haryana","HR"},{"Himachal Pradesh","HP"},{"Jharkhand","JH"},
            {"Karnataka","KA"},{"Kerala","KL"},{"Madhya Pradesh","MP"},{"Maharashtra","MH"},{"Manipur","MN"},
            {"Meghalaya","ML"},{"Mizoram","MZ"},{"Nagaland","NL"},{"Odisha","OD"},{"Punjab","PB"},
            {"Rajasthan","RJ"},{"Sikkim","SK"},{"Tamil Nadu","TN"},{"Telangana","TG"},{"Tripura","TR"},
            {"Uttar Pradesh","UttarPradesh"},{"Uttarakhand","UK"},{"West Bengal","WB"},
        };
        // Place in the sheet -> word found in an existing mission scene name
        private static readonly Dictionary<string, string> PlaceAlias = new Dictionary<string, string> { { "lucknow", "bhulbhulaiya" } };

        [MenuItem("Bharat Ka Safar/Data/Import 140 Missions (CSV)")]
        public static void Import()
        {
            if (!File.Exists(CsvPath)) { Debug.LogError("[Mission Import] Missing " + CsvPath); return; }
            var rows = ReadCsv(File.ReadAllText(CsvPath, Encoding.UTF8));
            if (rows.Count == 0) { Debug.LogError("[Mission Import] CSV is empty."); return; }

            PrepareSprites();
            var chapters = AssetDatabase.FindAssets("t:ChapterData").Select(g => AssetDatabase.LoadAssetAtPath<ChapterData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            var missions = AssetDatabase.FindAssets("t:MissionData").Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g))).ToList();
            int created = 0, reused = 0;

            foreach (var group in rows.GroupBy(r => r["State"]))
            {
                string state = group.Key;
                string code = Codes.TryGetValue(state, out var c) ? c : Slug(state).ToUpper();
                var chapter = chapters.FirstOrDefault(ch => ch.ChapterID == code || ch.StateName == state);
                bool newChapter = chapter == null;
                if (newChapter)
                {
                    chapter = ScriptableObject.CreateInstance<ChapterData>();
                    AssetDatabase.CreateAsset(chapter, $"{SO}chapters/{Slug(state)}_ChapterData.asset");
                    chapters.Add(chapter);
                    // New states start as "Upcoming" until their gameplay scenes exist
                    var cso = new SerializedObject(chapter);
                    cso.FindProperty("chapterAvailable").boolValue = false;
                    cso.ApplyModifiedPropertiesWithoutUndo();
                }

                var ordered = group.OrderBy(r => int.Parse(r["MissionNo"])).ToList();
                var list = new List<MissionData>();
                foreach (var r in ordered)
                {
                    int num = int.Parse(r["MissionNo"]);
                    string place = r["Place"];
                    var m = FindExisting(missions, code, place);
                    if (m == null)
                    {
                        string folder = $"{SO}missions/{Slug(state)}";
                        EnsureFolder(folder);
                        m = ScriptableObject.CreateInstance<MissionData>();
                        AssetDatabase.CreateAsset(m, $"{folder}/{code}_M{num:00}_{Slug(place)}.asset");
                        missions.Add(m);
                        created++;
                    }
                    else reused++;
                    list.Add(m);
                }

                for (int i = 0; i < ordered.Count; i++)
                {
                    var r = ordered[i];
                    var m = list[i];
                    int num = int.Parse(r["MissionNo"]);
                    string id = string.IsNullOrEmpty(m.MissionID) ? $"{code}_M{num:00}" : m.MissionID;
                    string next = i + 1 < list.Count ? (string.IsNullOrEmpty(list[i + 1].MissionID) ? $"{code}_M{num + 1:00}" : list[i + 1].MissionID) : "";
                    System.Enum.TryParse(r["Type"], out BKS_MissionType type);
                    string title = string.IsNullOrEmpty(r["MissionName"]) ? r["Place"] : r["MissionName"];
                    m.InitFromPlanner(id, code, num, title, state, r["Place"], type, r["Objective"],
                        Sprite(Art + "Cards/" + r["CardSprite"]),
                        r["CollectibleName"], int.TryParse(r["CollectibleCount"], out int cc) ? cc : 0,
                        string.IsNullOrEmpty(r["CollectibleSprite"]) ? null : Sprite(Art + "Collectibles/" + r["CollectibleSprite"]),
                        r["Obstacles"], r["HowToDeal"], r["MemoryToken"],
                        Sprite(Art + "MemoryTokens/" + r["MemoryTokenSprite"]),
                        null, next);
                    if (r.TryGetValue("Background", out var bgFile) && !string.IsNullOrEmpty(bgFile))
                        m.SetBackgroundForEditor(Sprite(Art + "Backgrounds/" + bgFile));
                    if (r.TryGetValue("Fact", out var fact)) m.SetFactForEditor(fact);
                    EditorUtility.SetDirty(m);
                }

                chapter.SetPlannerData(code, state, list, Sprite(Art + "StateBackgrounds/" + ordered[0]["StateBackground"]), MissionSelectScene);
                EditorUtility.SetDirty(chapter);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Mission Import] {rows.Count} missions: {reused} existing reused, {created} created. All chapters now open '{MissionSelectScene}'.");
        }

        private static MissionData FindExisting(List<MissionData> missions, string chapterID, string place)
        {
            string key = Slug(place).Replace("-", "");
            string alias = PlaceAlias.TryGetValue(place.ToLower(), out var a) ? a : null;
            foreach (var m in missions)
            {
                if (m == null) continue;
                bool sameChapter = m.ChapterID == chapterID || (chapterID == "RJ" && m.MissionID.StartsWith("Raj_"));
                if (!sameChapter) continue;
                if (!string.IsNullOrEmpty(m.StateName) && Slug(m.PlaceName).Replace("-", "") == key) return m;
                string scene = (m.GameplaySceneName ?? "").ToLower().Replace("_", "");
                if (scene.Contains(key) || (alias != null && scene.Contains(alias))) return m;
            }
            return null;
        }

        private static void PrepareSprites()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                SetSprites(Art + "Cards", 512);
                SetSprites(Art + "StateBackgrounds", 2048);
                SetSprites(Art + "Backgrounds", 2048);
                SetSprites(Art + "MemoryTokens", 256);
                SetSprites(Art + "Collectibles", 256);
                SetSprites(Art + "Obstacles", 512);
                SetSprites(Art + "UI", 2048);
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        public static void SetSprites(string folder, int maxSize)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { folder }))
            {
                var imp = AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid)) as TextureImporter;
                if (imp == null || (imp.textureType == TextureImporterType.Sprite && imp.maxTextureSize == maxSize)) continue;
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.maxTextureSize = maxSize;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
        }

        public static Sprite Sprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static string Slug(string s)
        {
            var sb = new StringBuilder();
            foreach (char ch in (s ?? "").ToLowerInvariant()) sb.Append(char.IsLetterOrDigit(ch) ? ch : '-');
            return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }

        // Minimal RFC-4180 CSV reader (quoted fields may contain commas and new lines)
        private static List<Dictionary<string, string>> ReadCsv(string text)
        {
            var records = new List<List<string>>();
            var field = new StringBuilder();
            var record = new List<string>();
            bool quoted = false;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (quoted)
                {
                    if (ch == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                    else if (ch == '"') quoted = false;
                    else field.Append(ch);
                }
                else if (ch == '"') quoted = true;
                else if (ch == ',') { record.Add(field.ToString()); field.Clear(); }
                else if (ch == '\n' || ch == '\r')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    record.Add(field.ToString()); field.Clear();
                    if (record.Count > 1 || record[0].Length > 0) records.Add(record);
                    record = new List<string>();
                }
                else field.Append(ch);
            }
            if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }

            var header = records[0].Select(h => h.Trim('﻿', ' ')).ToList();
            var result = new List<Dictionary<string, string>>();
            foreach (var rec in records.Skip(1))
            {
                var row = new Dictionary<string, string>();
                for (int k = 0; k < header.Count; k++) row[header[k]] = k < rec.Count ? rec[k] : "";
                result.Add(row);
            }
            return result;
        }
    }
}
