using UnityEngine;
using UnityEditor;

public class BKSCoinPlacementTool : EditorWindow
{
    private enum CoinPattern
    {
        FollowTerrain,
        Straight,
        Arc,
        Wave,
        Cluster
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private GameObject coinTemplate;
    private Transform customParent;

    // =========================================================
    // PATTERN
    // =========================================================

    private CoinPattern pattern = CoinPattern.FollowTerrain;

    // =========================================================
    // PLACEMENT
    // =========================================================

    private float startX = 0f;
    private float endX = 100f;
    private float spacing = 3f;
    private float heightAboveGround = 2.5f;

    // Coin size
    private float coinSize = 0.33f;

    // =========================================================
    // GROUND DETECTION
    // =========================================================

    private float raycastStartY = 100f;
    private float raycastDistance = 300f;
    private LayerMask groundLayer = ~0;

    // =========================================================
    // PATTERN SETTINGS
    // =========================================================

    private float straightY = 5f;

    private float arcHeight = 5f;

    private float waveHeight = 2f;
    private float waveFrequency = 0.08f;

    private int clusterCount = 8;
    private float clusterWidth = 8f;
    private float clusterHeight = 3f;

    private bool alignToTerrain = false;

    private Vector2 scrollPosition;

    private const string ROOT_NAME = "BKS_GeneratedCoins";

    // =========================================================
    // WINDOW
    // =========================================================

    [MenuItem("Bharat Ka Safar/Items/Coin Placement Tool")]
    public static void OpenWindow()
    {
        BKSCoinPlacementTool window =
            GetWindow<BKSCoinPlacementTool>("BKS Coin Tool");

        window.minSize = new Vector2(420, 680);
    }

    private void OnGUI()
    {
        scrollPosition =
            EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Bharat Ka Safar - Coin Placement Tool",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Create and manage terrain-aware coin patterns. Existing ItemSpawner objects are not modified.",
            MessageType.Info
        );

        GUILayout.Space(10);

        DrawReferences();

        GUILayout.Space(12);

        DrawPattern();

        GUILayout.Space(12);

        DrawPlacement();

        GUILayout.Space(12);

        DrawPatternSettings();

        GUILayout.Space(12);

        DrawGroundSettings();

        GUILayout.Space(18);

        DrawButtons();

        GUILayout.Space(12);

        EditorGUILayout.HelpBox(
            "Each generated pattern is stored separately inside BKS_GeneratedCoins.",
            MessageType.None
        );

        EditorGUILayout.EndScrollView();
    }

    // =========================================================
    // REFERENCES
    // =========================================================

    private void DrawReferences()
    {
        EditorGUILayout.LabelField(
            "References",
            EditorStyles.boldLabel
        );

        coinTemplate =
            (GameObject)EditorGUILayout.ObjectField(
                "Coin Template",
                coinTemplate,
                typeof(GameObject),
                true
            );

        customParent =
            (Transform)EditorGUILayout.ObjectField(
                "Optional Parent",
                customParent,
                typeof(Transform),
                true
            );
    }

    // =========================================================
    // PATTERN
    // =========================================================

    private void DrawPattern()
    {
        EditorGUILayout.LabelField(
            "Pattern",
            EditorStyles.boldLabel
        );

        pattern =
            (CoinPattern)EditorGUILayout.EnumPopup(
                "Coin Pattern",
                pattern
            );
    }

    // =========================================================
    // PLACEMENT
    // =========================================================

    private void DrawPlacement()
    {
        EditorGUILayout.LabelField(
            "Placement",
            EditorStyles.boldLabel
        );

        startX =
            EditorGUILayout.FloatField(
                "Start X",
                startX
            );

        endX =
            EditorGUILayout.FloatField(
                "End X",
                endX
            );

        if (pattern != CoinPattern.Cluster)
        {
            spacing =
                EditorGUILayout.FloatField(
                    "Coin Spacing",
                    spacing
                );
        }

        // Draggable Coin Size Slider
        coinSize =
            EditorGUILayout.Slider(
                "Coin Size",
                coinSize,
                0.10f,
                1.00f
            );

        if (pattern != CoinPattern.Straight)
        {
            heightAboveGround =
                EditorGUILayout.FloatField(
                    "Height Above Ground",
                    heightAboveGround
                );
        }

        EditorGUILayout.HelpBox(
            "Coin Size controls X, Y and Z scale uniformly.",
            MessageType.None
        );
    }

    // =========================================================
    // PATTERN SETTINGS
    // =========================================================

    private void DrawPatternSettings()
    {
        EditorGUILayout.LabelField(
            "Pattern Settings",
            EditorStyles.boldLabel
        );

        switch (pattern)
        {
            case CoinPattern.FollowTerrain:

                EditorGUILayout.HelpBox(
                    "Coins follow the detected terrain surface.",
                    MessageType.None
                );

                break;

            case CoinPattern.Straight:

                straightY =
                    EditorGUILayout.FloatField(
                        "Straight Y",
                        straightY
                    );

                EditorGUILayout.HelpBox(
                    "Straight Y is the world-space Y position.",
                    MessageType.None
                );

                break;

            case CoinPattern.Arc:

                arcHeight =
                    EditorGUILayout.FloatField(
                        "Arc Height",
                        arcHeight
                    );

                EditorGUILayout.HelpBox(
                    "Creates a curved coin trail above the terrain.",
                    MessageType.None
                );

                break;

            case CoinPattern.Wave:

                waveHeight =
                    EditorGUILayout.FloatField(
                        "Wave Height",
                        waveHeight
                    );

                waveFrequency =
                    EditorGUILayout.FloatField(
                        "Wave Frequency",
                        waveFrequency
                    );

                EditorGUILayout.HelpBox(
                    "Creates a repeating wave-shaped coin trail.",
                    MessageType.None
                );

                break;

            case CoinPattern.Cluster:

                clusterCount =
                    EditorGUILayout.IntSlider(
                        "Coin Count",
                        clusterCount,
                        2,
                        50
                    );

                clusterWidth =
                    EditorGUILayout.FloatField(
                        "Cluster Width",
                        clusterWidth
                    );

                clusterHeight =
                    EditorGUILayout.FloatField(
                        "Cluster Height",
                        clusterHeight
                    );

                EditorGUILayout.HelpBox(
                    "Creates a random group of coins around the selected section.",
                    MessageType.None
                );

                break;
        }
    }

    // =========================================================
    // GROUND SETTINGS
    // =========================================================

    private void DrawGroundSettings()
    {
        if (pattern == CoinPattern.Straight)
            return;

        EditorGUILayout.LabelField(
            "Ground Detection",
            EditorStyles.boldLabel
        );

        groundLayer =
            LayerMaskField(
                "Ground Layer",
                groundLayer
            );

        raycastStartY =
            EditorGUILayout.FloatField(
                "Raycast Start Y",
                raycastStartY
            );

        raycastDistance =
            EditorGUILayout.FloatField(
                "Raycast Distance",
                raycastDistance
            );

        alignToTerrain =
            EditorGUILayout.Toggle(
                "Align Coins To Terrain",
                alignToTerrain
            );
    }

    // =========================================================
    // BUTTONS
    // =========================================================

    private void DrawButtons()
    {
        GUI.backgroundColor =
            new Color(0.45f, 1f, 0.45f);

        if (GUILayout.Button(
            "GENERATE NEW PATTERN",
            GUILayout.Height(42)))
        {
            GeneratePattern();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        if (GUILayout.Button(
            "SELECT LAST PATTERN",
            GUILayout.Height(30)))
        {
            SelectLastPattern();
        }

        GUILayout.Space(6);

        GUI.backgroundColor =
            new Color(0.45f, 0.75f, 1f);

        if (GUILayout.Button(
            "RESIZE LAST PATTERN",
            GUILayout.Height(34)))
        {
            ResizeLastPattern();
        }

        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);

        GUI.backgroundColor =
            new Color(1f, 0.75f, 0.35f);

        if (GUILayout.Button(
            "CLEAR LAST PATTERN",
            GUILayout.Height(34)))
        {
            ClearLastPattern();
        }

        GUILayout.Space(6);

        GUI.backgroundColor =
            new Color(1f, 0.45f, 0.45f);

        if (GUILayout.Button(
            "CLEAR ALL GENERATED COINS",
            GUILayout.Height(36)))
        {
            ClearAll();
        }

        GUI.backgroundColor = Color.white;
    }

    // =========================================================
    // VALIDATION
    // =========================================================

    private bool ValidateSettings()
    {
        if (coinTemplate == null)
        {
            EditorUtility.DisplayDialog(
                "Coin Template Missing",
                "Drag your working Coin GameObject or Coin Prefab into Coin Template.",
                "OK"
            );

            return false;
        }

        if (endX <= startX)
        {
            EditorUtility.DisplayDialog(
                "Invalid Range",
                "End X must be greater than Start X.",
                "OK"
            );

            return false;
        }

        if (spacing <= 0f &&
            pattern != CoinPattern.Cluster)
        {
            EditorUtility.DisplayDialog(
                "Invalid Spacing",
                "Coin Spacing must be greater than zero.",
                "OK"
            );

            return false;
        }

        return true;
    }

    // =========================================================
    // GENERATE
    // =========================================================

    private void GeneratePattern()
    {
        if (!ValidateSettings())
            return;

        Transform root =
            GetOrCreateRoot();

        Transform group =
            CreatePatternGroup(root);

        switch (pattern)
        {
            case CoinPattern.FollowTerrain:
                GenerateFollowTerrain(group);
                break;

            case CoinPattern.Straight:
                GenerateStraight(group);
                break;

            case CoinPattern.Arc:
                GenerateArc(group);
                break;

            case CoinPattern.Wave:
                GenerateWave(group);
                break;

            case CoinPattern.Cluster:
                GenerateCluster(group);
                break;
        }

        Selection.activeGameObject =
            group.gameObject;

        EditorGUIUtility.PingObject(
            group.gameObject
        );

        Debug.Log(
            "[BKS Coin Tool] Generated: " +
            group.name
        );
    }

    // =========================================================
    // ROOT
    // =========================================================

    private Transform GetOrCreateRoot()
    {
        if (customParent != null)
        {
            Transform existing =
                customParent.Find(ROOT_NAME);

            if (existing != null)
                return existing;

            GameObject root =
                new GameObject(ROOT_NAME);

            Undo.RegisterCreatedObjectUndo(
                root,
                "Create Coin Root"
            );

            root.transform.SetParent(
                customParent,
                false
            );

            return root.transform;
        }

        GameObject existingRoot =
            GameObject.Find(ROOT_NAME);

        if (existingRoot != null)
            return existingRoot.transform;

        GameObject newRoot =
            new GameObject(ROOT_NAME);

        Undo.RegisterCreatedObjectUndo(
            newRoot,
            "Create Coin Root"
        );

        return newRoot.transform;
    }

    private Transform FindRoot()
    {
        if (customParent != null)
        {
            return customParent.Find(
                ROOT_NAME
            );
        }

        GameObject root =
            GameObject.Find(ROOT_NAME);

        if (root == null)
            return null;

        return root.transform;
    }

    // =========================================================
    // PATTERN GROUP
    // =========================================================

    private Transform CreatePatternGroup(
        Transform root)
    {
        int number =
            GetNextPatternNumber(root);

        string groupName =
            pattern.ToString() +
            "_" +
            number.ToString("00");

        GameObject group =
            new GameObject(groupName);

        Undo.RegisterCreatedObjectUndo(
            group,
            "Create Coin Pattern"
        );

        group.transform.SetParent(
            root,
            false
        );

        return group.transform;
    }

    private int GetNextPatternNumber(
        Transform root)
    {
        int highest = 0;

        for (int i = 0;
             i < root.childCount;
             i++)
        {
            string[] parts =
                root.GetChild(i)
                    .name
                    .Split('_');

            if (parts.Length == 0)
                continue;

            int value;

            if (int.TryParse(
                parts[parts.Length - 1],
                out value))
            {
                if (value > highest)
                    highest = value;
            }
        }

        return highest + 1;
    }

    // =========================================================
    // FOLLOW TERRAIN
    // =========================================================

    private void GenerateFollowTerrain(
        Transform group)
    {
        int id = 1;

        for (float x = startX;
             x <= endX + 0.001f;
             x += spacing)
        {
            RaycastHit2D hit;

            if (!TryGetGround(x, out hit))
                continue;

            Vector3 position =
                new Vector3(
                    x,
                    hit.point.y +
                    heightAboveGround,
                    coinTemplate.transform.position.z
                );

            CreateCoin(
                position,
                group,
                id++,
                hit
            );
        }
    }

    // =========================================================
    // STRAIGHT
    // =========================================================

    private void GenerateStraight(
        Transform group)
    {
        int id = 1;

        for (float x = startX;
             x <= endX + 0.001f;
             x += spacing)
        {
            Vector3 position =
                new Vector3(
                    x,
                    straightY,
                    coinTemplate.transform.position.z
                );

            CreateCoin(
                position,
                group,
                id++,
                null
            );
        }
    }

    // =========================================================
    // ARC
    // =========================================================

    private void GenerateArc(
        Transform group)
    {
        int id = 1;

        float length =
            endX - startX;

        for (float x = startX;
             x <= endX + 0.001f;
             x += spacing)
        {
            RaycastHit2D hit;

            if (!TryGetGround(x, out hit))
                continue;

            float t =
                Mathf.Clamp01(
                    (x - startX) /
                    length
                );

            float arc =
                Mathf.Sin(
                    t * Mathf.PI
                ) * arcHeight;

            Vector3 position =
                new Vector3(
                    x,
                    hit.point.y +
                    heightAboveGround +
                    arc,
                    coinTemplate.transform.position.z
                );

            CreateCoin(
                position,
                group,
                id++,
                hit
            );
        }
    }

    // =========================================================
    // WAVE
    // =========================================================

    private void GenerateWave(
        Transform group)
    {
        int id = 1;

        for (float x = startX;
             x <= endX + 0.001f;
             x += spacing)
        {
            RaycastHit2D hit;

            if (!TryGetGround(x, out hit))
                continue;

            float wave =
                Mathf.Sin(
                    (x - startX) *
                    waveFrequency *
                    Mathf.PI *
                    2f
                ) * waveHeight;

            Vector3 position =
                new Vector3(
                    x,
                    hit.point.y +
                    heightAboveGround +
                    wave,
                    coinTemplate.transform.position.z
                );

            CreateCoin(
                position,
                group,
                id++,
                hit
            );
        }
    }

    // =========================================================
    // CLUSTER
    // =========================================================

    private void GenerateCluster(
        Transform group)
    {
        float centerX =
            (startX + endX) * 0.5f;

        for (int i = 0;
             i < clusterCount;
             i++)
        {
            float x =
                centerX +
                Random.Range(
                    -clusterWidth * 0.5f,
                    clusterWidth * 0.5f
                );

            RaycastHit2D hit;

            if (!TryGetGround(x, out hit))
                continue;

            float randomHeight =
                Random.Range(
                    0f,
                    clusterHeight
                );

            Vector3 position =
                new Vector3(
                    x,
                    hit.point.y +
                    heightAboveGround +
                    randomHeight,
                    coinTemplate.transform.position.z
                );

            CreateCoin(
                position,
                group,
                i + 1,
                hit
            );
        }
    }

    // =========================================================
    // GROUND RAYCAST
    // =========================================================

    private bool TryGetGround(
        float x,
        out RaycastHit2D hit)
    {
        Vector2 origin =
            new Vector2(
                x,
                raycastStartY
            );

        hit =
            Physics2D.Raycast(
                origin,
                Vector2.down,
                raycastDistance,
                groundLayer
            );

        return hit.collider != null;
    }

    // =========================================================
    // CREATE COIN
    // =========================================================

    private void CreateCoin(
        Vector3 position,
        Transform group,
        int id,
        RaycastHit2D? groundHit)
    {
        GameObject coin;

        if (PrefabUtility.IsPartOfPrefabAsset(
            coinTemplate))
        {
            coin =
                (GameObject)
                PrefabUtility.InstantiatePrefab(
                    coinTemplate
                );
        }
        else
        {
            coin =
                Instantiate(
                    coinTemplate
                );
        }

        Undo.RegisterCreatedObjectUndo(
            coin,
            "Generate BKS Coin"
        );

        coin.name =
            "Coin_" +
            id.ToString("00");

        coin.transform.SetParent(
            group,
            true
        );

        coin.transform.position =
            position;

        // Apply selected uniform size
        coin.transform.localScale =
            new Vector3(
                coinSize,
                coinSize,
                coinSize
            );

        if (alignToTerrain &&
            groundHit.HasValue)
        {
            Vector2 normal =
                groundHit.Value.normal;

            float angle =
                Mathf.Atan2(
                    normal.y,
                    normal.x
                ) *
                Mathf.Rad2Deg -
                90f;

            coin.transform.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle
                );
        }
        else
        {
            coin.transform.rotation =
                coinTemplate.transform.rotation;
        }
    }

    // =========================================================
    // SELECT LAST
    // =========================================================

    private void SelectLastPattern()
    {
        Transform root =
            FindRoot();

        if (root == null ||
            root.childCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Nothing Found",
                "There are no generated coin patterns.",
                "OK"
            );

            return;
        }

        Transform last =
            root.GetChild(
                root.childCount - 1
            );

        Selection.activeGameObject =
            last.gameObject;

        EditorGUIUtility.PingObject(
            last.gameObject
        );
    }

    // =========================================================
    // RESIZE LAST PATTERN
    // =========================================================

    private void ResizeLastPattern()
    {
        Transform root =
            FindRoot();

        if (root == null ||
            root.childCount == 0)
        {
            EditorUtility.DisplayDialog(
                "Nothing Found",
                "Generate a coin pattern first.",
                "OK"
            );

            return;
        }

        Transform lastPattern =
            root.GetChild(
                root.childCount - 1
            );

        Undo.RecordObject(
            lastPattern,
            "Resize Coin Pattern"
        );

        for (int i = 0;
             i < lastPattern.childCount;
             i++)
        {
            Transform coin =
                lastPattern.GetChild(i);

            Undo.RecordObject(
                coin,
                "Resize Coin"
            );

            coin.localScale =
                new Vector3(
                    coinSize,
                    coinSize,
                    coinSize
                );

            EditorUtility.SetDirty(coin);
        }

        Debug.Log(
            "[BKS Coin Tool] Resized " +
            lastPattern.name +
            " to " +
            coinSize
        );

        SceneView.RepaintAll();
    }

    // =========================================================
    // CLEAR LAST
    // =========================================================

    private void ClearLastPattern()
    {
        Transform root =
            FindRoot();

        if (root == null ||
            root.childCount == 0)
        {
            Debug.Log(
                "[BKS Coin Tool] No pattern to clear."
            );

            return;
        }

        Transform last =
            root.GetChild(
                root.childCount - 1
            );

        string deletedName =
            last.name;

        Undo.DestroyObjectImmediate(
            last.gameObject
        );

        Debug.Log(
            "[BKS Coin Tool] Cleared: " +
            deletedName
        );
    }

    // =========================================================
    // CLEAR ALL
    // =========================================================

    private void ClearAll()
    {
        Transform root =
            FindRoot();

        if (root == null)
        {
            Debug.Log(
                "[BKS Coin Tool] No generated coins found."
            );

            return;
        }

        bool confirm =
            EditorUtility.DisplayDialog(
                "Clear All Generated Coins?",
                "This will remove every generated pattern inside " +
                ROOT_NAME +
                ". Existing ItemSpawner objects will not be affected.",
                "Clear All",
                "Cancel"
            );

        if (!confirm)
            return;

        Undo.DestroyObjectImmediate(
            root.gameObject
        );

        Debug.Log(
            "[BKS Coin Tool] All generated coin patterns cleared."
        );
    }

    // =========================================================
    // LAYER MASK
    // =========================================================

    private LayerMask LayerMaskField(
        string label,
        LayerMask selected)
    {
        string[] layers =
            UnityEditorInternal
            .InternalEditorUtility
            .layers;

        int displayedMask = 0;

        for (int i = 0;
             i < layers.Length;
             i++)
        {
            int layer =
                LayerMask.NameToLayer(
                    layers[i]
                );

            if ((selected.value &
                 (1 << layer)) != 0)
            {
                displayedMask |=
                    1 << i;
            }
        }

        displayedMask =
            EditorGUILayout.MaskField(
                label,
                displayedMask,
                layers
            );

        int actualMask = 0;

        for (int i = 0;
             i < layers.Length;
             i++)
        {
            if ((displayedMask &
                 (1 << i)) != 0)
            {
                int layer =
                    LayerMask.NameToLayer(
                        layers[i]
                    );

                actualMask |=
                    1 << layer;
            }
        }

        selected.value =
            actualMask;

        return selected;
    }
}