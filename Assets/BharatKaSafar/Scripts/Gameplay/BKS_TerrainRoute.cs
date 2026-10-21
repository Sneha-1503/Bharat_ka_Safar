using UnityEngine;

namespace BharatKaSafar.Gameplay
{
    [ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(EdgeCollider2D))]
    public class BKS_TerrainRoute : MonoBehaviour
    {
        public BKS_TerrainPlan plan;
        [Min(10)] public float groundDepth = 40;
        [Range(0.25f, 2f)] public float sampleSpacing = 1;
        private Mesh generatedMesh;
        public Vector2 Finish => plan == null ? Vector2.zero : (Vector2)transform.TransformPoint(LastPoint());

        private Vector2 LastPoint() { var p = plan.BuildPoints(sampleSpacing); return p[p.Length - 1]; }
        private void OnEnable() { Rebuild(); }
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += RefreshAfterValidation;
#endif
        }
#if UNITY_EDITOR
        private void RefreshAfterValidation() { if (this != null && isActiveAndEnabled) Rebuild(); }
#endif
        [ContextMenu("Rebuild Terrain")]
        public void Rebuild()
        {
            if (plan == null) return;
            var points = plan.BuildPoints(sampleSpacing);
            var vertices = new Vector3[points.Length * 2];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(points.Length - 1) * 6];
            float bottom = -groundDepth;
            foreach (var p in points) bottom = Mathf.Min(bottom, p.y - groundDepth);
            for (int i = 0; i < points.Length; i++)
            {
                vertices[2*i] = points[i]; vertices[2*i+1] = new Vector3(points[i].x, bottom, 0);
                uv[2*i] = new Vector2(points[i].x / 12, points[i].y / 12);
                uv[2*i+1] = new Vector2(points[i].x / 12, bottom / 12);
                if (i == points.Length - 1) continue;
                int t = 6*i, v = 2*i;
                triangles[t]=v; triangles[t+1]=v+2; triangles[t+2]=v+1;
                triangles[t+3]=v+1; triangles[t+4]=v+2; triangles[t+5]=v+3;
            }
            ReleaseMesh();
            generatedMesh = new Mesh { name = "BKS Difficulty Terrain", hideFlags = HideFlags.DontSave };
            if (vertices.Length > 65535) generatedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            generatedMesh.vertices = vertices; generatedMesh.uv = uv; generatedMesh.triangles = triangles;
            generatedMesh.RecalculateNormals(); generatedMesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = generatedMesh;
            var renderer = GetComponent<MeshRenderer>(); renderer.sharedMaterial = plan.groundMaterial; renderer.sortingOrder = -5;
            GetComponent<EdgeCollider2D>().points = points;
        }
        private void ReleaseMesh()
        {
            if (generatedMesh == null) return;
            if (Application.isPlaying) Destroy(generatedMesh); else DestroyImmediate(generatedMesh);
            generatedMesh = null;
        }
        private void OnDestroy() { ReleaseMesh(); }
    }
}
