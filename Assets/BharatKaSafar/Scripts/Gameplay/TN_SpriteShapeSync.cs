using UnityEngine;
using UnityEngine.U2D;

namespace BharatKaSafar.Gameplay
{
    [ExecuteAlways]
    public class TN_SpriteShapeSync : MonoBehaviour
    {
        [SerializeField] private EdgeCollider2D sourceCollider;
        [SerializeField] private SpriteShapeController spriteShape;
        [SerializeField] private float groundDepth = 2f;

        [ContextMenu("Sync With Edge Collider")]
        public void SyncWithCollider()
        {
            if (sourceCollider == null || spriteShape == null)
            {
                Debug.LogWarning("[TN_SpriteShapeSync] Assign Source Collider and Sprite Shape.");
                return;
            }

            Vector2[] points = sourceCollider.points;

            if (points.Length < 2)
                return;

            Spline spline = spriteShape.spline;
            spline.Clear();

            // Top surface - same as EdgeCollider2D
            for (int i = 0; i < points.Length; i++)
            {
                spline.InsertPointAt(
                    spline.GetPointCount(),
                    new Vector3(points[i].x, points[i].y, 0f)
                );

                int index = spline.GetPointCount() - 1;
                spline.SetTangentMode(index, ShapeTangentMode.Linear);
            }

            // Bottom-right
            Vector2 last = points[points.Length - 1];

            spline.InsertPointAt(
                spline.GetPointCount(),
                new Vector3(last.x, last.y - groundDepth, 0f)
            );

            spline.SetTangentMode(
                spline.GetPointCount() - 1,
                ShapeTangentMode.Linear
            );

            // Bottom-left
            Vector2 first = points[0];

            spline.InsertPointAt(
                spline.GetPointCount(),
                new Vector3(first.x, first.y - groundDepth, 0f)
            );

            spline.SetTangentMode(
                spline.GetPointCount() - 1,
                ShapeTangentMode.Linear
            );

            spriteShape.BakeMesh();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(spriteShape);
#endif
        }
    }
}
