using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    [ExecuteAlways]
    [RequireComponent(typeof(EdgeCollider2D))]
    public class TN_TerrainVisual : MonoBehaviour
    {
        [Header("Visual")]
        [SerializeField] private Material terrainMaterial;

        [Tooltip("How far the ground extends below the driving surface.")]
        [SerializeField] private float groundDepth = 2f;

        [SerializeField] private float textureScale = 1f;

        private EdgeCollider2D edgeCollider;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh mesh;

        private void OnEnable()
        {
            BuildVisual();
        }

        private void OnValidate()
        {
            BuildVisual();
        }

        [ContextMenu("Rebuild Terrain Visual")]
        public void BuildVisual()
        {
            edgeCollider = GetComponent<EdgeCollider2D>();

            if (edgeCollider == null || edgeCollider.pointCount < 2)
                return;

            Transform visual = transform.Find("GeneratedVisual");

            if (visual == null)
            {
                GameObject obj = new GameObject("GeneratedVisual");
                obj.transform.SetParent(transform);
                obj.transform.localPosition = Vector3.zero;
                obj.transform.localRotation = Quaternion.identity;
                obj.transform.localScale = Vector3.one;

                visual = obj.transform;
            }

            meshFilter = visual.GetComponent<MeshFilter>();
            if (meshFilter == null)
                meshFilter = visual.gameObject.AddComponent<MeshFilter>();

            meshRenderer = visual.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
                meshRenderer = visual.gameObject.AddComponent<MeshRenderer>();

            if (terrainMaterial != null)
                meshRenderer.sharedMaterial = terrainMaterial;

            Vector2[] points = edgeCollider.points;

            Vector3[] vertices = new Vector3[points.Length * 2];
            Vector2[] uv = new Vector2[vertices.Length];

            for (int i = 0; i < points.Length; i++)
            {
                vertices[i * 2] =
                    new Vector3(points[i].x, points[i].y, 0);

                vertices[i * 2 + 1] =
                    new Vector3(points[i].x, points[i].y - groundDepth, 0);

                uv[i * 2] =
                    new Vector2(points[i].x * textureScale, 1);

                uv[i * 2 + 1] =
                    new Vector2(points[i].x * textureScale, 0);
            }

            int[] triangles = new int[(points.Length - 1) * 6];

            int t = 0;

            for (int i = 0; i < points.Length - 1; i++)
            {
                int topLeft = i * 2;
                int bottomLeft = topLeft + 1;
                int topRight = topLeft + 2;
                int bottomRight = topLeft + 3;

                triangles[t++] = topLeft;
                triangles[t++] = topRight;
                triangles[t++] = bottomLeft;

                triangles[t++] = topRight;
                triangles[t++] = bottomRight;
                triangles[t++] = bottomLeft;
            }

            if (mesh == null)
            {
                mesh = new Mesh();
                mesh.name = "TN Terrain Visual Mesh";
            }
            else
            {
                mesh.Clear();
            }

            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            meshFilter.sharedMesh = mesh;
        }
    }
}
