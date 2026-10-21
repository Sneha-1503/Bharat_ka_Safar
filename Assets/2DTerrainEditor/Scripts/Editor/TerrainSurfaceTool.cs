//using UnityEngine;
//using UnityEditor;

//public class TerrainSurfaceTool : EditorWindow
//{
//    private enum SurfaceShape
//    {
//        Hill,
//        Dip
//    }

//    private float startX = 100f;
//    private float endX = 200f;
//    private float amount = 3f;
//    private SurfaceShape shape = SurfaceShape.Hill;

//    [MenuItem("Bharat Ka Safar/Terrain/Surface Tool")]
//    public static void ShowWindow()
//    {
//        GetWindow<TerrainSurfaceTool>("Terrain Surface Tool");
//    }

//    private void OnGUI()
//    {
//        GUILayout.Label("Driving Surface Editor", EditorStyles.boldLabel);

//        EditorGUILayout.Space();

//        startX = EditorGUILayout.FloatField("Start X", startX);
//        endX = EditorGUILayout.FloatField("End X", endX);
//        amount = EditorGUILayout.FloatField("Height / Depth", amount);

//        shape = (SurfaceShape)EditorGUILayout.EnumPopup("Shape", shape);

//        EditorGUILayout.Space();

//        EditorGUILayout.HelpBox(
//            "Only the selected X range will be modified. " +
//            "The EdgeCollider2D will update automatically.",
//            MessageType.Info
//        );

//        EditorGUILayout.Space();

//        if (GUILayout.Button("Apply Surface Section"))
//        {
//            ApplySurface();
//        }
//    }

//    private void ApplySurface()
//    {
//        GameObject obj = Selection.activeGameObject;

//        if (obj == null)
//        {
//            Debug.LogWarning("Select your Terrain Level object first.");
//            return;
//        }

//        TerrainEditor2D terrain = obj.GetComponent<TerrainEditor2D>();
//        MeshFilter meshFilter = obj.GetComponent<MeshFilter>();

//        if (terrain == null || meshFilter == null || meshFilter.sharedMesh == null)
//        {
//            Debug.LogWarning(
//                "Selected object must contain TerrainEditor2D and MeshFilter."
//            );
//            return;
//        }

//        if (endX <= startX)
//        {
//            Debug.LogWarning("End X must be greater than Start X.");
//            return;
//        }

//        // Create a unique mesh so other terrain objects aren't affected.
//        Mesh mesh = Instantiate(meshFilter.sharedMesh);
//        mesh.name = meshFilter.sharedMesh.name + "_SurfaceEdited";

//        Undo.RecordObject(meshFilter, "Edit Terrain Surface");
//        Undo.RecordObject(obj.GetComponent<EdgeCollider2D>(),
//            "Update Terrain Collider");

//        Vector3[] vertices = mesh.vertices;

//        float sectionLength = endX - startX;

//        for (int i = 0; i < vertices.Length; i += 2)
//        {
//            float x = vertices[i].x;

//            if (x < startX || x > endX)
//                continue;

//            // 0 at Start X
//            // 1 at End X
//            float t = (x - startX) / sectionLength;

//            // Smooth hill:
//            //
//            // Start        Middle        End
//            //   0     ->     1     ->    0
//            //
//            float curve = Mathf.Sin(t * Mathf.PI);

//            float change = curve * amount;

//            if (shape == SurfaceShape.Dip)
//                change = -change;

//            // Modify ONLY the upper/driving vertex.
//            vertices[i].y += change;
//        }

//        mesh.vertices = vertices;

//        mesh.RecalculateBounds();
//        mesh.RecalculateNormals();

//        meshFilter.sharedMesh = mesh;

//        // IMPORTANT:
//        // TerrainEditor2D reads the new upper vertices and
//        // rebuilds EdgeCollider2D from them.
//        terrain.UpdateCollider2D();

//        EditorUtility.SetDirty(meshFilter);
//        EditorUtility.SetDirty(terrain);

//        EdgeCollider2D edge = obj.GetComponent<EdgeCollider2D>();

//        if (edge != null)
//            EditorUtility.SetDirty(edge);

//        SceneView.RepaintAll();

//        Debug.Log(
//            $"Surface updated from X {startX} to {endX}. " +
//            "EdgeCollider2D updated automatically."
//        );
//    }
//}













//==================================================================================================================================

//________-----________________________________NEW CODE FOR TOOL FOR TERRAIN MODIFICATION________________________--------------------

//====================================================================================================================================





using UnityEngine;
using UnityEditor;

public class TerrainSurfaceTool : EditorWindow
{
    private enum SurfaceOperation
    {
        Hill,
        Dip,
        RampUp,
        RampDown,
        Smooth,
        Flatten
    }

    private SurfaceOperation operation = SurfaceOperation.Hill;

    private float startX = 100f;
    private float endX = 150f;

    private float amount = 3f;

    private int smoothIterations = 3;
    private float smoothStrength = 0.5f;

    private bool blendEdges = true;

    [MenuItem("Bharat Ka Safar/Terrain/Surface Editor")]
    public static void ShowWindow()
    {
        TerrainSurfaceTool window =
            GetWindow<TerrainSurfaceTool>("BKS Terrain Editor");

        window.minSize = new Vector2(380, 420);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Bharat Ka Safar - Terrain Surface Editor",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Select a TerrainEditor2D object in the Hierarchy. " +
            "Only the selected terrain will be modified.",
            MessageType.Info
        );

        GUILayout.Space(10);

        operation = (SurfaceOperation)EditorGUILayout.EnumPopup(
            "Operation",
            operation
        );

        GUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Terrain Section",
            EditorStyles.boldLabel
        );

        startX = EditorGUILayout.FloatField("Start X", startX);
        endX = EditorGUILayout.FloatField("End X", endX);

        GUILayout.Space(8);

        DrawOperationSettings();

        GUILayout.Space(12);

        blendEdges = EditorGUILayout.Toggle(
            "Smooth Section Edges",
            blendEdges
        );

        GUILayout.Space(15);

        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);

        if (GUILayout.Button("APPLY TO SELECTED TERRAIN", GUILayout.Height(40)))
        {
            ApplyTerrainOperation();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(8);

        EditorGUILayout.HelpBox(
            "Terrain Mesh + Cap + EdgeCollider2D will update together.",
            MessageType.None
        );
    }

    private void DrawOperationSettings()
    {
        switch (operation)
        {
            case SurfaceOperation.Hill:

                amount = EditorGUILayout.FloatField(
                    "Hill Height",
                    amount
                );

                break;

            case SurfaceOperation.Dip:

                amount = EditorGUILayout.FloatField(
                    "Dip Depth",
                    amount
                );

                break;

            case SurfaceOperation.RampUp:

                amount = EditorGUILayout.FloatField(
                    "Ramp Height",
                    amount
                );

                break;

            case SurfaceOperation.RampDown:

                amount = EditorGUILayout.FloatField(
                    "Ramp Depth",
                    amount
                );

                break;

            case SurfaceOperation.Smooth:

                smoothIterations = EditorGUILayout.IntSlider(
                    "Iterations",
                    smoothIterations,
                    1,
                    20
                );

                smoothStrength = EditorGUILayout.Slider(
                    "Strength",
                    smoothStrength,
                    0.05f,
                    1f
                );

                break;

            case SurfaceOperation.Flatten:

                EditorGUILayout.HelpBox(
                    "The selected section will be flattened while " +
                    "blending into the surrounding terrain.",
                    MessageType.Info
                );

                break;
        }
    }

    private void ApplyTerrainOperation()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            EditorUtility.DisplayDialog(
                "No Terrain Selected",
                "Select your Terrain Level object first.",
                "OK"
            );

            return;
        }

        TerrainEditor2D terrain =
            selected.GetComponent<TerrainEditor2D>();

        if (terrain == null)
        {
            EditorUtility.DisplayDialog(
                "TerrainEditor2D Missing",
                "The selected object does not contain TerrainEditor2D.",
                "OK"
            );

            return;
        }

        MeshFilter meshFilter =
            selected.GetComponent<MeshFilter>();

        if (meshFilter == null ||
            meshFilter.sharedMesh == null)
        {
            EditorUtility.DisplayDialog(
                "Mesh Missing",
                "Selected terrain does not have a valid mesh.",
                "OK"
            );

            return;
        }

        if (endX <= startX)
        {
            EditorUtility.DisplayDialog(
                "Invalid Range",
                "End X must be greater than Start X.",
                "OK"
            );

            return;
        }

        // ---------------------------------------------------------
        // Make terrain mesh unique
        // ---------------------------------------------------------

        Mesh originalMesh = meshFilter.sharedMesh;

        Mesh workingMesh = Instantiate(originalMesh);

        workingMesh.name =
            originalMesh.name + "_BKS_" + selected.GetInstanceID();

        Undo.RegisterCreatedObjectUndo(
            workingMesh,
            "Create Terrain Mesh Copy"
        );

        Undo.RecordObject(
            meshFilter,
            "Assign Terrain Mesh"
        );

        meshFilter.sharedMesh = workingMesh;

        // ---------------------------------------------------------
        // Get vertices
        // ---------------------------------------------------------

        Vector3[] vertices = workingMesh.vertices;

        if (vertices == null || vertices.Length < 4)
        {
            Debug.LogError(
                "[BKS Terrain Editor] Terrain mesh has no valid vertices."
            );

            return;
        }

        Undo.RecordObject(
            terrain,
            "Edit Terrain Surface"
        );

        EdgeCollider2D edgeCollider =
            selected.GetComponent<EdgeCollider2D>();

        if (edgeCollider != null)
        {
            Undo.RecordObject(
                edgeCollider,
                "Update Terrain Collider"
            );
        }

        if (terrain.CapObj != null)
        {
            MeshFilter capMeshFilter =
                terrain.CapObj.GetComponent<MeshFilter>();

            if (capMeshFilter != null)
            {
                Undo.RecordObject(
                    capMeshFilter,
                    "Update Terrain Cap"
                );

                if (capMeshFilter.sharedMesh != null)
                {
                    Undo.RecordObject(
                        capMeshFilter.sharedMesh,
                        "Update Terrain Cap Mesh"
                    );
                }
            }
        }

        // ---------------------------------------------------------
        // Perform operation
        // ---------------------------------------------------------

        switch (operation)
        {
            case SurfaceOperation.Hill:

                ApplyHill(
                    vertices,
                    startX,
                    endX,
                    Mathf.Abs(amount)
                );

                break;

            case SurfaceOperation.Dip:

                ApplyHill(
                    vertices,
                    startX,
                    endX,
                    -Mathf.Abs(amount)
                );

                break;

            case SurfaceOperation.RampUp:

                ApplyRamp(
                    vertices,
                    startX,
                    endX,
                    Mathf.Abs(amount)
                );

                break;

            case SurfaceOperation.RampDown:

                ApplyRamp(
                    vertices,
                    startX,
                    endX,
                    -Mathf.Abs(amount)
                );

                break;

            case SurfaceOperation.Smooth:

                ApplySmooth(
                    vertices,
                    startX,
                    endX,
                    smoothIterations,
                    smoothStrength
                );

                break;

            case SurfaceOperation.Flatten:

                ApplyFlatten(
                    vertices,
                    startX,
                    endX
                );

                break;
        }

        // ---------------------------------------------------------
        // Prevent invalid terrain heights
        // ---------------------------------------------------------

        ClampSurface(vertices, terrain);

        // ---------------------------------------------------------
        // Let TerrainEditor2D rebuild its systems
        // EditTerrain updates:
        // Main mesh
        // UV
        // Cap / Smart Cap
        // 3D collider
        // ---------------------------------------------------------

        terrain.EditTerrain(
            vertices,
            false
        );

        // TerrainEditor2D EditTerrain does not update EdgeCollider2D,
        // therefore explicitly update it.
        terrain.UpdateCollider2D();

        EditorUtility.SetDirty(terrain);
        EditorUtility.SetDirty(meshFilter);
        EditorUtility.SetDirty(workingMesh);

        if (terrain.CapObj != null)
        {
            EditorUtility.SetDirty(
                terrain.CapObj
            );
        }

        SceneView.RepaintAll();

        Debug.Log(
            "[BKS Terrain Editor] " +
            operation +
            " applied to " +
            selected.name +
            ". Terrain, Cap and Collider updated."
        );
    }

    // =============================================================
    // HILL / DIP
    // =============================================================

    private void ApplyHill(
        Vector3[] vertices,
        float fromX,
        float toX,
        float height)
    {
        float length = toX - fromX;

        for (int i = 0; i < vertices.Length; i += 2)
        {
            float x = vertices[i].x;

            if (x < fromX || x > toX)
                continue;

            float t =
                Mathf.Clamp01(
                    (x - fromX) / length
                );

            // Smooth sine hill.
            // Start and end remain unchanged.
            float curve =
                Mathf.Sin(t * Mathf.PI);

            vertices[i].y +=
                curve * height;
        }
    }

    // =============================================================
    // RAMP UP / DOWN
    // =============================================================

    private void ApplyRamp(
        Vector3[] vertices,
        float fromX,
        float toX,
        float height)
    {
        float length = toX - fromX;

        for (int i = 0; i < vertices.Length; i += 2)
        {
            float x = vertices[i].x;

            if (x < fromX || x > toX)
                continue;

            float t =
                Mathf.Clamp01(
                    (x - fromX) / length
                );

            if (blendEdges)
            {
                t = SmoothStep01(t);
            }

            vertices[i].y +=
                height * t;
        }
    }

    // =============================================================
    // SMOOTH
    // =============================================================

    private void ApplySmooth(
        Vector3[] vertices,
        float fromX,
        float toX,
        int iterations,
        float strength)
    {
        int pointCount =
            vertices.Length / 2;

        for (int iteration = 0;
             iteration < iterations;
             iteration++)
        {
            float[] originalY =
                new float[pointCount];

            for (int p = 0;
                 p < pointCount;
                 p++)
            {
                originalY[p] =
                    vertices[p * 2].y;
            }

            for (int p = 1;
                 p < pointCount - 1;
                 p++)
            {
                int vertexIndex =
                    p * 2;

                float x =
                    vertices[vertexIndex].x;

                if (x < fromX ||
                    x > toX)
                    continue;

                float average =
                    (
                        originalY[p - 1] +
                        originalY[p] +
                        originalY[p + 1]
                    ) / 3f;

                float weight = strength;

                if (blendEdges)
                {
                    weight *=
                        GetSectionBlend(
                            x,
                            fromX,
                            toX
                        );
                }

                vertices[vertexIndex].y =
                    Mathf.Lerp(
                        originalY[p],
                        average,
                        weight
                    );
            }
        }
    }

    // =============================================================
    // FLATTEN
    // =============================================================

    private void ApplyFlatten(
        Vector3[] vertices,
        float fromX,
        float toX)
    {
        float totalHeight = 0f;
        int count = 0;

        for (int i = 0;
             i < vertices.Length;
             i += 2)
        {
            float x =
                vertices[i].x;

            if (x >= fromX &&
                x <= toX)
            {
                totalHeight +=
                    vertices[i].y;

                count++;
            }
        }

        if (count == 0)
            return;

        float targetHeight =
            totalHeight / count;

        for (int i = 0;
             i < vertices.Length;
             i += 2)
        {
            float x =
                vertices[i].x;

            if (x < fromX ||
                x > toX)
                continue;

            float blend = 1f;

            if (blendEdges)
            {
                blend =
                    GetSectionBlend(
                        x,
                        fromX,
                        toX
                    );
            }

            vertices[i].y =
                Mathf.Lerp(
                    vertices[i].y,
                    targetHeight,
                    blend
                );
        }
    }

    // =============================================================
    // SECTION EDGE BLENDING
    // =============================================================

    private float GetSectionBlend(
        float x,
        float fromX,
        float toX)
    {
        float length =
            toX - fromX;

        if (length <= 0f)
            return 1f;

        float t =
            Mathf.Clamp01(
                (x - fromX) /
                length
            );

        // 20% blending zone on both sides.
        const float edgeSize = 0.2f;

        float leftBlend =
            Mathf.Clamp01(
                t / edgeSize
            );

        float rightBlend =
            Mathf.Clamp01(
                (1f - t) /
                edgeSize
            );

        return SmoothStep01(
            Mathf.Min(
                leftBlend,
                rightBlend
            )
        );
    }

    private float SmoothStep01(float t)
    {
        t = Mathf.Clamp01(t);

        return
            t * t *
            (3f - (2f * t));
    }

    // =============================================================
    // SAFETY
    // =============================================================

    private void ClampSurface(
        Vector3[] vertices,
        TerrainEditor2D terrain)
    {
        for (int i = 0;
             i < vertices.Length;
             i += 2)
        {
            vertices[i].y =
                Mathf.Clamp(
                    vertices[i].y,
                    0.05f,
                    terrain.Height
                );
        }
    }
}