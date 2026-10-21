using UnityEngine;
using UnityEditor;

public class TerrainDepthTool : EditorWindow
{
    private float terrainDepth = 5f;

    [MenuItem("Bharat Ka Safar/Terrain/Set Terrain Depth")]
    public static void ShowWindow()
    {
        GetWindow<TerrainDepthTool>("Terrain Depth");
    }

    private void OnGUI()
    {
        GUILayout.Label("Terrain Visual Depth", EditorStyles.boldLabel);

        terrainDepth = EditorGUILayout.FloatField("Depth", terrainDepth);

        GUILayout.Space(10);

        if (GUILayout.Button("Apply To Selected Terrain"))
        {
            ApplyDepth();
        }
    }

    private void ApplyDepth()
    {
        GameObject obj = Selection.activeGameObject;

        if (obj == null)
        {
            Debug.LogWarning("Select a Terrain GameObject first.");
            return;
        }

        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Selected object has no terrain mesh.");
            return;
        }

        if (terrainDepth <= 0f)
        {
            Debug.LogWarning("Terrain Depth must be greater than 0.");
            return;
        }

        // Make a unique copy so another terrain using the same mesh
        // is not modified accidentally.
        Mesh mesh = Instantiate(meshFilter.sharedMesh);
        mesh.name = meshFilter.sharedMesh.name + "_DepthAdjusted";

        Vector3[] vertices = mesh.vertices;

        // TerrainEditor2D stores:
        // even index = driving/top surface
        // odd index  = bottom vertex
        for (int i = 0; i < vertices.Length - 1; i += 2)
        {
            float topY = vertices[i].y;

            // ONLY move the bottom.
            vertices[i + 1].y = topY - terrainDepth;
        }

        Undo.RecordObject(meshFilter, "Set Terrain Depth");

        mesh.vertices = vertices;
        mesh.RecalculateBounds();

        meshFilter.sharedMesh = mesh;

        EditorUtility.SetDirty(meshFilter);

        Debug.Log(
            $"Terrain depth changed to {terrainDepth}. Driving surface was not changed."
        );
    }
}