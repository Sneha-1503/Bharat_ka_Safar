// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;

// public class GS2DRopeEditorWindow : EditorWindow
// {
//     #region Declarations
//     // Add menu named "GS 2D Rope" to the Window menu
//     [MenuItem("Window/GS 2D Rope")]
//     static void Init()
//     {
//         // Get existing open window or if none, make a new one:
//         EditorWindow.GetWindow(typeof(GS2DRopeEditorWindow));
//     }
//     public SegmentSelectionMode segmentsMode;
//     public LineOverflowMode overflowMode;
//     public Transform ropeHolder;
//     bool withPhysics = true, hangFirstPoint = true, hangLastPoint = true, useBendLimit = true, breakableJoint,
//         createSeemlessRope, springy;
//     public Material ropeMaterial;
//     const int MAX_CHAIN_PREFAB_COUNT = 3;
//     public SpriteRenderer[] chainPrefabs = new SpriteRenderer[MAX_CHAIN_PREFAB_COUNT];
//     public int totalChainPrefabs = 1, bendLimit = 45;
//     public float breakForce = 100, ropeWidth = 0.25f;
//     public float overlapRopePartsFactor = 0f;
//     public string infoText;
//     public string gameObjectName = "GS2DRope";
//     //public Rope rope2D;
//     #endregion
//     void OnGUI()
//     {
//         //create input form for pointsHolder
//         GUILayout.Space(10);
//         gameObjectName = EditorGUILayout.TextField("Rope Instance Name", gameObjectName);
//         GUILayout.Space(10);
//         EditorGUILayout.BeginHorizontal();
//         ropeHolder = (Transform)EditorGUILayout.ObjectField("Rope Holder", ropeHolder, typeof(Transform), true);
//         if (GUILayout.Button("X", GUILayout.MaxWidth(20), GUILayout.MaxHeight(15)))
//             ropeHolder = null;
//         EditorGUILayout.EndHorizontal();

//         //create input Field for Chain Prefabs
//         EditorGUILayout.BeginHorizontal();
//         chainPrefabs[0] = ((SpriteRenderer)EditorGUILayout.ObjectField("Rope Prefab", chainPrefabs[0], typeof(SpriteRenderer), true));

//         if (totalChainPrefabs < MAX_CHAIN_PREFAB_COUNT && GUILayout.Button("+", GUILayout.MaxWidth(20), GUILayout.MaxHeight(15)))
//         {
//             totalChainPrefabs++;
//         }

//         if (GUILayout.Button("X", GUILayout.MaxWidth(20), GUILayout.MaxHeight(15)))
//         {
//             totalChainPrefabs = 1;
//         }
//         EditorGUILayout.EndHorizontal();
//         for (int i = 1; i < totalChainPrefabs; i++)
//         {
//             chainPrefabs[i] = ((SpriteRenderer)EditorGUILayout.ObjectField(
//                 "Chain Prefab " + i, chainPrefabs[i], typeof(SpriteRenderer), true));
//         }
//         GUILayout.Space(3);
//         overlapRopePartsFactor = EditorGUILayout.Slider("Segment Overlap Factor", overlapRopePartsFactor, -1.0f, 1.0f);
//         segmentsMode = (SegmentSelectionMode)EditorGUILayout.EnumPopup("Segment Selection Mode", segmentsMode);
//         overflowMode = (LineOverflowMode)EditorGUILayout.EnumPopup("Over Flow Mode", overflowMode);
//         GUILayout.Space(5);
//         withPhysics = EditorGUILayout.BeginToggleGroup("Enable 2D Physics", withPhysics);

//         //if (withPhysics)
//         {
//             hangFirstPoint = EditorGUILayout.Toggle("Hang First Part", hangFirstPoint);

//             hangLastPoint = EditorGUILayout.Toggle("Hang Last Part", hangLastPoint);
//             springy = EditorGUILayout.Toggle("Enable Spring Behaviour", springy);
//             useBendLimit = EditorGUILayout.BeginToggleGroup("Use Bend Limits", useBendLimit);
//             bendLimit = EditorGUILayout.IntSlider("Bend Limits", bendLimit, 0, 180);
//             EditorGUILayout.EndToggleGroup();
//             breakableJoint = EditorGUILayout.BeginToggleGroup("Breakable Joints", breakableJoint);
//             breakForce = EditorGUILayout.FloatField("Break Force", breakForce);
//             EditorGUILayout.EndToggleGroup();
//         }
//         EditorGUILayout.EndToggleGroup();
//         GUILayout.Space(10);

//         //create checkbox for decision to use linerenderer
//         createSeemlessRope = EditorGUILayout.BeginToggleGroup("Create Seamless Rope", createSeemlessRope);

//         //create input form for linerenderer's material
//         ropeMaterial = (Material)EditorGUILayout.ObjectField("Rope Material", ropeMaterial, typeof(Material), true);
//         //create input form for linerenderer's width
//         EditorGUILayout.BeginHorizontal();
//         ropeWidth = EditorGUILayout.FloatField("Rope Width", ropeWidth);
//         if (ropeWidth < 0)
//             ropeWidth = 0.0f;
//         EditorGUILayout.EndHorizontal();
//         EditorGUILayout.EndToggleGroup();
//         GUILayout.Space(10);
//         //create font style for Information Text
//         GUIStyle fontStyle = new GUIStyle();
//         fontStyle.fontStyle = FontStyle.Bold;
//         fontStyle.fontSize = 12;
//         fontStyle.alignment = TextAnchor.MiddleCenter;
//         //create label for helpText
//         GUILayout.Label(infoText, fontStyle);

//         GUILayout.Space(10);
//         if (GUILayout.Button("Create Rope", GUILayout.MinHeight(30)))
//         {
//             int errorCount = 0;
//             if (ropeHolder == null)
//             {
//                 infoText = "Rope Holder is Not Assigned!";
//                 errorCount++;
//             }
//             else if (chainPrefabs[0] == null)
//             {
//                 infoText = "Atleast One Chain Prefab Needs to be Assign!";
//                 errorCount++;
//             }
//             else if (totalChainPrefabs > 1)
//             {
//                 for (int i = 0; i < totalChainPrefabs; i++)
//                 {
//                     if (chainPrefabs[i] == null)
//                     {
//                         infoText = "More than One Chain Prefab chosen but One of them is Not Assigned!";
//                         errorCount++;
//                         break;
//                     }
//                 }
//             }
//             else if (createSeemlessRope)
//             {
//                 if (ropeMaterial == null)
//                 {
//                     infoText = "Assign a Rope Material or Uncheck Seemless Rope";
//                     errorCount++;
//                 }
//             }
//             if (errorCount == 0)
//             {
//                 Create2DRope();
//                 infoText = "Created";
//                 //this.Close();
//             }
//         }
//     }
//     public void Create2DRope()
//     {
//         ropeHolder.name = gameObjectName;
//         if (ropeHolder.GetComponent<GS2DRope>() == null)
//         {
//             ropeHolder.gameObject.AddComponent<GS2DRope>();
//         }
//         //Vector3 position = SceneView.lastActiveSceneView.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1.0f)).origin;
//         //ropeHolder.transform.position = new Vector3(position.x, position.y, 0);
//         Selection.activeGameObject = ropeHolder.gameObject;
//         GS2DRope r = ropeHolder.GetComponent<GS2DRope>();

//         /////////////Assigning Editor Window Values to GameObject in Scene

//         SpriteRenderer[] temp = new SpriteRenderer[totalChainPrefabs];

//         for (int i = 0; i < totalChainPrefabs; i++)
//         {
//             temp[i] = chainPrefabs[i];
//         }
//         r.SegmentsPrefabs = temp;
//         r.SegmentsMode = segmentsMode;
//         r.OverflowMode = overflowMode;
//         r.WithPhysics = withPhysics;

//         r.HangFirstSegment = hangFirstPoint;
//         r.HangLastSegment = hangLastPoint;
//         r.springy = springy;
//         if (hangFirstPoint)
//             r.FirstSegmentConnectionAnchor = r.nodes[0];
//         if (hangLastPoint)
//             r.LastSegmentConnectionAnchor = r.nodes[r.nodes.Count - 1];

//         if (useBendLimit)
//         {
//             r.bendLimit = bendLimit;
//         }
//         if (breakableJoint)
//         {
//             r.BreakForce = breakForce;
//         }
//         r.overlapFactor = overlapRopePartsFactor / 2;
//         r.ropeMaterial = ropeMaterial;
//         r.ropeWidth = ropeWidth;
//         r.createSeemlessRope = createSeemlessRope;
//         //////////End Here ///////////////////////
//         GS2DRopeHelper.UpdateRope(r, true);
//     }
// }