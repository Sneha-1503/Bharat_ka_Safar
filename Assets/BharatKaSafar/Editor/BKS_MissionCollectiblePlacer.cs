using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using BharatKaSafar.Gameplay;
using BharatKaSafar.Missions;
using BharatKaSafar.Rewards;

namespace BharatKaSafar.Editor
{
    /// <summary>
    /// Places this scene's mission collectibles (count from MissionData, e.g. "Diya x5") evenly along
    /// the road between the start point and the destination, just above the ground.
    /// Re-running replaces the previous set. Move individual items by hand afterwards if needed.
    /// Menu: Bharat Ka Safar > Missions > Place Mission Collectibles (Open Scene)
    /// </summary>
    public static class BKS_MissionCollectiblePlacer
    {
        private const string GroupName = "BKS_MissionCollectibles";

        [MenuItem("Bharat Ka Safar/Missions/Place Mission Collectibles (Open Scene)")]
        public static void Place()
        {
            var scene = SceneManager.GetActiveScene();
            var mission = AssetDatabase.FindAssets("t:MissionData")
                .Select(g => AssetDatabase.LoadAssetAtPath<MissionData>(AssetDatabase.GUIDToAssetPath(g)))
                .FirstOrDefault(m => m != null && m.GameplaySceneName == scene.name);
            if (mission == null) { Debug.LogWarning($"[Collectibles] No MissionData uses scene '{scene.name}'."); return; }

            var old = GameObject.Find(GroupName);
            if (old != null) Undo.DestroyObjectImmediate(old);
            if (mission.CollectibleCount <= 0) { Debug.Log($"[Collectibles] {mission.MissionName} has no collectibles."); return; }

            if (!FindRoute(out float startX, out float endX)) { Debug.LogWarning("[Collectibles] Could not find start point / destination."); return; }

            Physics2D.SyncTransforms();
            var group = new GameObject(GroupName);
            Undo.RegisterCreatedObjectUndo(group, "Place Mission Collectibles");
            int n = mission.CollectibleCount;
            float from = Mathf.Lerp(startX, endX, 0.12f), to = Mathf.Lerp(startX, endX, 0.92f);
            int placed = 0;
            for (int i = 0; i < n; i++)
            {
                float x = n == 1 ? (from + to) / 2f : Mathf.Lerp(from, to, i / (float)(n - 1));
                if (!GroundY(x, out float y)) continue;
                var item = new GameObject($"Collectible_{i + 1:00}");
                item.transform.SetParent(group.transform);
                item.transform.position = new Vector3(x, y + 2.4f, 0f); // above the coin line so it is not lost among coins
                var sr = item.AddComponent<SpriteRenderer>();
                sr.sprite = mission.CollectibleSprite;
                sr.sortingOrder = 20;
                if (sr.sprite != null) item.transform.localScale = Vector3.one * (1.8f / Mathf.Max(0.01f, sr.sprite.bounds.size.y));
                var col = item.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = sr.sprite != null ? sr.sprite.bounds.extents.y : 0.6f;
                item.AddComponent<BKS_MissionCollectible>();
                placed++;
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"[Collectibles] Placed {placed}/{n} '{mission.CollectibleName}' in {scene.name}. Save the scene to keep them.");
        }

        private static bool FindRoute(out float startX, out float endX)
        {
            startX = endX = 0f;
            var start = Object.FindFirstObjectByType<StartPoint>(FindObjectsInactive.Include);
            var dest = Object.FindFirstObjectByType<BKS_Destination>(FindObjectsInactive.Include);
            var gate = Object.FindFirstObjectByType<BharatKaSafar.Rajasthan.BKS_DestinationGate>(FindObjectsInactive.Include);
            var gm = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
            startX = start != null ? start.transform.position.x : 0f;
            if (dest != null) endX = dest.transform.position.x;
            else if (gate != null) endX = gate.transform.position.x;
            else if (gm != null && gm.targetDistance > 0) endX = gm.targetDistance;
            return endX > startX + 10f;
        }

        private static bool GroundY(float x, out float y)
        {
            y = 0f;
            var hits = Physics2D.RaycastAll(new Vector2(x, 1000f), Vector2.down, 3000f);
            foreach (var h in hits)
            {
                if (h.collider.isTrigger || BharatKaSafar.Vehicle.BKS_VehicleUtil.IsVehicle(h.collider.gameObject)) continue;
                y = h.point.y;
                return true;
            }
            return false;
        }
    }
}
