// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Reflection;
// using UnityEditorInternal;
// using UnityEngine;
// using UnityEditor;
// using Random = UnityEngine.Random;

// [CustomEditor(typeof(TerrainEditor2D))]
// public class TerrainEditor2DIns: Editor
// {
//     #region Hotkeys
//     private static Dictionary<string, KeyCode> _hotKeys = new Dictionary<string, KeyCode>
//     {
//         {"SwitchEditMode", KeyCode.E},
//         {"DigMode", KeyCode.D},
//         {"EditModeUndo", KeyCode.Z},
//         {"EditModeRedo", KeyCode.Y},
//         {"ShowEditTab", KeyCode.F1},
//         {"ShowCapTab", KeyCode.F2},
//         {"ShowDeformTab", KeyCode.F3},
//         {"ShowMainTab", KeyCode.F4},
//     };
//     #endregion

//     #region Editor settings fields
//     //Brush settings
//     public float BrushSize = 5;
//     public float BrushHardness = 0.25f;
//     public float BrushNoise = 0;
//     public float BrushHeightLimit = 50;
//     public bool BrushHeightLimitEnabled = false;

//     private bool _showEditTab;
//     private bool _showCapTab;
//     private bool _showDeformTab;
//     private bool _showMainTab;

//     #endregion

//     #region Operational fields
//     private TerrainEditor2D _myTerrainEditor2D;

//     private bool _editMode;
//     private bool _playMode;
//     private bool _startEdit;
//     private bool _digMode;
//     private bool _brushSizeMode;
//     private bool _brushLockMode;
//     private Vector2 _brushHandleLockPos;

//     private bool _hotkeyEnteredInEditMode;
//     private Vector2 _mousePos;
//     private Vector2 _handleLocalPos;

//     private List<RecordedTerrainVerts> _recordedVerts = new List<RecordedTerrainVerts>();
//     private RecordedTerrainVerts _lastRecordedVerts;
//     private int _curUndoRecordedVertsId;

//     private Tool _previousUsingTool;

//     private Dictionary<string, Texture> _inspectorTextures = new Dictionary<string, Texture>();
//     private GUIStyle _guiButtonStyle;
//     private Texture2D _guiDefaultButton;

//     #endregion

//     [MenuItem("GameObject/2D Object/Terrain 2D")]
//     private static void CreateTerrain2D()
//     {
//         GameObject terrain2D = new GameObject("New Terrain 2D");

//         terrain2D.AddComponent<TerrainEditor2D>();
//         terrain2D.GetComponent<TerrainEditor2D>().SetInstanceId(terrain2D.GetInstanceID());
//         terrain2D.GetComponent<TerrainEditor2D>().Create();

//         Selection.activeGameObject = terrain2D.gameObject;

//         Undo.RegisterCompleteObjectUndo(terrain2D, "Terrain 2D object");
//     }

//     void OnEnable()
//     {
//         LoadInspector();

//         _myTerrainEditor2D = target as TerrainEditor2D;

//         if (_myTerrainEditor2D == null)
//             return;

//         if (PrefabUtility.GetPrefabType(_myTerrainEditor2D.gameObject) == PrefabType.Prefab)
//             return;

//         if (_myTerrainEditor2D.InstanceId != _myTerrainEditor2D.GetInstanceID())
//         {
//             CopySharedMesh(_myTerrainEditor2D.GetComponent<MeshFilter>());
//             if (_myTerrainEditor2D.CapObj != null)
//                 CopySharedMesh(_myTerrainEditor2D.CapObj.GetComponent<MeshFilter>());
//             if (_myTerrainEditor2D.Collider3DObj != null)
//                 CopySharedMesh(_myTerrainEditor2D.Collider3DObj.GetComponent<MeshFilter>());

//             _myTerrainEditor2D.SetInstanceId(_myTerrainEditor2D.GetInstanceID());
//         }

//         _recordedVerts.Add(new RecordedTerrainVerts(_myTerrainEditor2D.GetVertsPos()));
//     }

//     public override void OnInspectorGUI()
//     {
//         #region Check inspector
//         Undo.RecordObject(_myTerrainEditor2D, "2D Terrain Editor");

//         if (_myTerrainEditor2D == null)
//         {
//             EditorGUILayout.HelpBox("Missing object reference", MessageType.Error);
//             return;
//         }

//         if (PrefabUtility.GetPrefabType(_myTerrainEditor2D.gameObject) == PrefabType.Prefab)
//         {
//             EditorGUILayout.HelpBox("Terrain settings is not avaliable in project view. Please, place this prefab on the scene.", MessageType.Warning);
//             return;
//         }

//         _guiButtonStyle = new GUIStyle(GUI.skin.button);
//         _guiDefaultButton = _guiButtonStyle.normal.background;

//         string[] sortingLayers = GetSortingLayerNames();
//         #endregion

//         #region Tab controls

//         GUILayout.BeginHorizontal();
//         GUILayout.FlexibleSpace();

//         _guiButtonStyle.overflow.right = 4;
//         _guiButtonStyle.overflow.left = 4;

//         _guiButtonStyle.normal.background = _guiDefaultButton;
//         if (_showEditTab)
//             _guiButtonStyle.normal.background = _guiButtonStyle.onActive.background;
//         if (GUILayout.Button(new GUIContent(_inspectorTextures["Edit settings"], "Edit mode"), _guiButtonStyle, GUILayout.Height(25), GUILayout.Width(35)) 
//             || (Event.current.keyCode == _hotKeys["ShowEditTab"]))
//             SwitchTab("Edit settings");

//         _guiButtonStyle.normal.background = _guiDefaultButton;
//         if (_showCapTab)
//             _guiButtonStyle.normal.background = _guiButtonStyle.onActive.background;
//         if (GUILayout.Button(new GUIContent(_inspectorTextures["Cap settings"], "Cap settings"), _guiButtonStyle, GUILayout.Height(25), GUILayout.Width(35))
//             || (Event.current.keyCode == _hotKeys["ShowCapTab"]))
//             SwitchTab("Cap settings");

//         _guiButtonStyle.normal.background = _guiDefaultButton;
//         if (_showDeformTab)
//             _guiButtonStyle.normal.background = _guiButtonStyle.onActive.background;
//         if (GUILayout.Button(new GUIContent(_inspectorTextures["Deform settings"], "Realtime deformation settings"), _guiButtonStyle, GUILayout.Height(25), GUILayout.Width(35))
//             || (Event.current.keyCode == _hotKeys["ShowDeformTab"]))
//             SwitchTab("Deform settings");

//         _guiButtonStyle.normal.background = _guiDefaultButton;
//         if (_showMainTab)
//             _guiButtonStyle.normal.background = _guiButtonStyle.onActive.background;
//         if (GUILayout.Button(new GUIContent(_inspectorTextures["Main settings"], "Main settings"), _guiButtonStyle, GUILayout.Height(25), GUILayout.Width(35))
//             || (Event.current.keyCode == _hotKeys["ShowMainTab"]))
//             SwitchTab("Main settings");

//         GUILayout.FlexibleSpace();
//         GUILayout.EndHorizontal();

//         _guiButtonStyle = new GUIStyle(GUI.skin.button);

//         #endregion

//         #region Edit tab

//         if (_showEditTab)
//         {
//             //Brush settings
//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Brush", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             BrushSize = EditorGUILayout.Slider("Size", BrushSize, 0.1f, 50);
//             BrushHardness = EditorGUILayout.Slider("Hardness", BrushHardness, 0.01f, 1);
//             BrushNoise = EditorGUILayout.Slider("Noise", BrushNoise, 0, 1);

//             EditorGUILayout.BeginHorizontal();

//             BrushHeightLimitEnabled = EditorGUILayout.Toggle(BrushHeightLimitEnabled, GUILayout.MaxWidth(15));

//             EditorGUI.BeginDisabledGroup(!BrushHeightLimitEnabled);

//             BrushHeightLimit = EditorGUILayout.Slider("Limit height", BrushHeightLimit, 0, _myTerrainEditor2D.Height);

//             EditorGUI.EndDisabledGroup();
//             EditorGUILayout.EndHorizontal();

//             EditorGUILayout.Space();

//             if (_editMode)
//                 _guiButtonStyle.normal.background = _guiButtonStyle.onActive.background;

//             if (GUILayout.Button("EDIT MODE", _guiButtonStyle, GUILayout.Height(40)))
//             {
//                 SwitchEditMode();
//             }

//             if (_editMode)
//                 EditorGUILayout.HelpBox(
//                     "Hold [LMB] - raise terrain\n" +
//                     "Hold [" + _hotKeys["DigMode"] + " + LMB] - dig mode\n" +
//                     "Hold [ALT+Mouse Wheel] - change brush size\n" +
//                     "Hold [SHIFT] - horizontal lock\n" +
//                     "[SHIFT+" + _hotKeys["EditModeUndo"] + " / " + _hotKeys["EditModeRedo"] + "] - undo / redo",
//                     MessageType.None);
//             else
//             {
//                 if (GetSceneViewCamera() != null)
//                 {
//                     if (!GetSceneViewCamera().orthographic)
//                         EditorGUILayout.HelpBox("Set scene view camera to orthographic or 2D mode to enter edit mode.", MessageType.Warning);
//                     else EditorGUILayout.HelpBox("Use [Shift+" + _hotKeys["SwitchEditMode"] + "] to enter Edit Mode", MessageType.None);
//                 }
//             }

//             EditorGUILayout.Space();

//             //Randomizer
//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Random generation", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.RndHeight = EditorGUILayout.Slider("Average height", _myTerrainEditor2D.RndHeight, 0.1f, _myTerrainEditor2D.Height);
//             _myTerrainEditor2D.RndHillsCount = EditorGUILayout.IntSlider("Hills count", _myTerrainEditor2D.RndHillsCount, 1, _myTerrainEditor2D.Width / 2);
//             _myTerrainEditor2D.RndAmplitude = EditorGUILayout.Slider("Amplitude", _myTerrainEditor2D.RndAmplitude, 0.1f, _myTerrainEditor2D.Height / 2f);

//             if (GUILayout.Button("Generate"))
//             {
//                 _myTerrainEditor2D.RandomizeTerrain(true);
//             }

//             //Check transform
//             if (_myTerrainEditor2D.transform.rotation.eulerAngles != Vector3.zero || _myTerrainEditor2D.transform.localScale != Vector3.one)
//             {
//                 EditorGUILayout.HelpBox("Object rotation or scale is different from default. Edit mode may not work properly.", MessageType.Warning);
//             }
//         }

//         #endregion

//         #region Cap tab
//         if (_showCapTab)
//         {
//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Cap", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.GenerateCap = EditorGUILayout.Toggle("Enabled", _myTerrainEditor2D.GenerateCap);

//             EditorGUI.BeginDisabledGroup(!_myTerrainEditor2D.GenerateCap);

//             _myTerrainEditor2D.CapMaterial = (Material)EditorGUILayout.ObjectField("Material", _myTerrainEditor2D.CapMaterial, typeof(Material), false);
//             CheckTexture(_myTerrainEditor2D.CapMaterial);
//             _myTerrainEditor2D.CapColor = EditorGUILayout.ColorField(new GUIContent("Color", "Color of cap mesh. Works only if shader supports vertex colors."), _myTerrainEditor2D.CapColor);

//             _myTerrainEditor2D.CapHeight = EditorGUILayout.FloatField(new GUIContent("Height", "Cap height in units"), _myTerrainEditor2D.CapHeight);
//             _myTerrainEditor2D.CapOffset = EditorGUILayout.FloatField(new GUIContent("Offset", "Offset relative to base terrain path"), _myTerrainEditor2D.CapOffset);

//             EditorGUI.BeginDisabledGroup(_myTerrainEditor2D.SmartCap);

//             _myTerrainEditor2D.CapTextureTiling = EditorGUILayout.IntField(new GUIContent("Texture tiling", "Number of cap texture tiles for whole 2D terrain (does not affect to cap if smart cap enabled)"), _myTerrainEditor2D.CapTextureTiling);
//             EditorGUI.EndDisabledGroup();

//             _myTerrainEditor2D.CapSortingLayerId = EditorGUILayout.Popup("Sorting layer", _myTerrainEditor2D.CapSortingLayerId, sortingLayers);
//             _myTerrainEditor2D.CapSortingLayerName = sortingLayers[_myTerrainEditor2D.CapSortingLayerId];

//             _myTerrainEditor2D.CapOrderInLayer = EditorGUILayout.IntField("Order in layer", _myTerrainEditor2D.CapOrderInLayer);

//             EditorGUILayout.Space();

//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Smart cap", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.SmartCap = EditorGUILayout.Toggle("Enabled", _myTerrainEditor2D.SmartCap);

//             EditorGUI.BeginDisabledGroup(!_myTerrainEditor2D.SmartCap);
//             _myTerrainEditor2D.SmartCapCutHeight = EditorGUILayout.FloatField(new GUIContent("Cut height", "Min height in units for splitting cap into different paths"), _myTerrainEditor2D.SmartCapCutHeight);
//             _myTerrainEditor2D.SmartCapSegmentsUv = EditorGUILayout.Vector2Field(new GUIContent("Segments UV", "Horizontal UV for smart cap texture. It's like separator which divides cap texture into 3 parts."), _myTerrainEditor2D.SmartCapSegmentsUv);
//             _myTerrainEditor2D.SmartCapSideSegmentsWidth = EditorGUILayout.FloatField("Side segments width", _myTerrainEditor2D.SmartCapSideSegmentsWidth);

//             EditorGUI.EndDisabledGroup();

//             EditorGUI.EndDisabledGroup();
//         }
//         #endregion

//         #region Deform tab

//         if (_showDeformTab)
//         {
//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Realtime deformation", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.RealtimeDeformEnabled = EditorGUILayout.Toggle("Enabled", _myTerrainEditor2D.RealtimeDeformEnabled);

//             EditorGUI.BeginDisabledGroup(!_myTerrainEditor2D.RealtimeDeformEnabled);

//             _myTerrainEditor2D.RealtimeDeformUpdateColliders = EditorGUILayout.Toggle(new GUIContent("Update colliders", 
//                 "Is colliders should be updated on each iteration of deformation?"), _myTerrainEditor2D.RealtimeDeformUpdateColliders);
//             _myTerrainEditor2D.RealtimeDeformUpdateUv = EditorGUILayout.Toggle(new GUIContent("Update UV",
//                 "Is UV texture coordinates should be updated on each iteration of deformation?"), _myTerrainEditor2D.RealtimeDeformUpdateUv);

//             EditorGUI.EndDisabledGroup();

//             EditorGUILayout.HelpBox("Read the documentation to leran how realtime deformation works", MessageType.None);
//             EditorGUILayout.HelpBox("Note that updating 2D collider is very expensive operation due to 2D physics specificity", MessageType.None);
//         }
//         #endregion

//         #region Main tab

//         if (_showMainTab)
//         {
//             EditorGUI.BeginChangeCheck();

//             //Main
//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Main", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             EditorGUI.BeginDisabledGroup(true);
//             EditorGUILayout.IntField("Mesh ID", _myTerrainEditor2D.InstanceId);
//             EditorGUI.EndDisabledGroup();

//             _myTerrainEditor2D.MainMaterial = (Material)EditorGUILayout.ObjectField("Material", _myTerrainEditor2D.MainMaterial, typeof(Material), false);
//             CheckTexture(_myTerrainEditor2D.MainMaterial);
//             _myTerrainEditor2D.MainColor = EditorGUILayout.ColorField(new GUIContent("Color", "Color of 2D terrain mesh. Works only if shader supports vertex colors."), _myTerrainEditor2D.MainColor);

//             _myTerrainEditor2D.MainTextureSize = EditorGUILayout.IntField("Texture size", _myTerrainEditor2D.MainTextureSize);

//             _myTerrainEditor2D.SortingLayerId = EditorGUILayout.Popup("Sorting layer", _myTerrainEditor2D.SortingLayerId, sortingLayers);
//             _myTerrainEditor2D.SortingLayerName = sortingLayers[_myTerrainEditor2D.SortingLayerId];

//             _myTerrainEditor2D.OrderInLayer = EditorGUILayout.IntField("Order in layer", _myTerrainEditor2D.OrderInLayer);

//             //Additionally
//             EditorGUILayout.Space();

//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Additionally", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.FixSides = EditorGUILayout.Toggle(new GUIContent("Fix sides", 
//                 "Is side end points of terrain 2D path needs to be fixed and protected from changes? "), _myTerrainEditor2D.FixSides);
//             EditorGUI.BeginDisabledGroup(!_myTerrainEditor2D.FixSides);
//             _myTerrainEditor2D.LeftFixedPoint = EditorGUILayout.FloatField("Left fixed point", _myTerrainEditor2D.LeftFixedPoint);
//             _myTerrainEditor2D.RightFixedPoint = EditorGUILayout.FloatField("Right fixed point", _myTerrainEditor2D.RightFixedPoint);
//             EditorGUI.EndDisabledGroup();

//             _myTerrainEditor2D.Generate3DCollider = EditorGUILayout.Toggle("3D collider", _myTerrainEditor2D.Generate3DCollider);
//             EditorGUI.BeginDisabledGroup(!_myTerrainEditor2D.Generate3DCollider);
//             _myTerrainEditor2D.Collider3DWidth = EditorGUILayout.FloatField("3D collider width", _myTerrainEditor2D.Collider3DWidth);
//             EditorGUI.EndDisabledGroup();

//             //Resolution
//             EditorGUILayout.Space();

//             GUI.skin.label.fontStyle = FontStyle.Bold;
//             GUILayout.Label("Size and resolution", GUI.skin.label);
//             GUI.skin.label.fontStyle = FontStyle.Normal;

//             _myTerrainEditor2D.Width = EditorGUILayout.IntField("Width", _myTerrainEditor2D.Width);
//             _myTerrainEditor2D.Height = EditorGUILayout.IntField("Height", _myTerrainEditor2D.Height);
//             _myTerrainEditor2D.Resolution = EditorGUILayout.IntField("Resolution", _myTerrainEditor2D.Resolution);

//             if (_myTerrainEditor2D.Resolution > 8)
//                 EditorGUILayout.HelpBox("Resolution is responsible for the number of polygons. High resolution value may affect performance.", MessageType.Warning);

//             if (GUILayout.Button("Apply"))
//             {
//                 _recordedVerts.Clear();
//                 _curUndoRecordedVertsId = 0;
//                 _myTerrainEditor2D.Create();
//                 _recordedVerts.Add(new RecordedTerrainVerts(_myTerrainEditor2D.GetVertsPos()));
//             }
//             EditorGUILayout.HelpBox("Applying will clear all changes in geometry", MessageType.None);

//         }
//         #endregion

//         #region Check colliders
//         if (EditorApplication.isPlaying && !_playMode)
//         {
//             _myTerrainEditor2D.UpdateCollider2D();

//             if (_myTerrainEditor2D.Generate3DCollider)
//             {
//                 _myTerrainEditor2D.UpdateCollider3D(false);
//             }

//             _playMode = true;
//         }
//         else _playMode = false;
//         #endregion

//         if (GUI.changed)
//         {
//             CheckValues();

//             if (!EditorApplication.isPlaying)
//                 _myTerrainEditor2D.EditTerrain(_myTerrainEditor2D.GetVertsPos(), true);
//             else _myTerrainEditor2D.EditTerrain(_myTerrainEditor2D.GetVertsPos(), false);

//             EditorUtility.SetDirty(target);
//         }
//     }

//     void OnSceneGUI()
//     {
//         if (PrefabUtility.GetPrefabType(_myTerrainEditor2D.gameObject) == PrefabType.Prefab || _myTerrainEditor2D == null)
//             return;

//         #region Scene view events
//         //Switch tab
//         if (Event.current.type == EventType.KeyUp)
//         {
//             if (Event.current.keyCode == _hotKeys["ShowEditTab"])
//                 SwitchTab("Edit settings");
//             if (Event.current.keyCode == _hotKeys["ShowCapTab"])
//                 SwitchTab("Cap settings");
//             if (Event.current.keyCode == _hotKeys["ShowMainTab"])
//                 SwitchTab("Main settings");
//         }


//         if (Event.current.Equals(Event.KeyboardEvent("#" + _hotKeys["SwitchEditMode"])))
//             SwitchEditMode();

//         //Undo & redo (values)
//         if (Event.current.commandName == "UndoRedoPerformed")
//         {
//             if (!EditorApplication.isPlaying)
//                 _myTerrainEditor2D.EditTerrain(_myTerrainEditor2D.GetVertsPos(), true);
//             else _myTerrainEditor2D.EditTerrain(_myTerrainEditor2D.GetVertsPos(), false);
//         }

//         #endregion

//         if (!_editMode)
//             return;

//         #region Draw handles
//         if (GetSceneViewCamera() != null)
//         {
//             _mousePos = Camera.current.ScreenToWorldPoint(new Vector2(Event.current.mousePosition.x, (Camera.current.pixelHeight - Event.current.mousePosition.y)));
//             _handleLocalPos = _mousePos - new Vector2(_myTerrainEditor2D.transform.position.x, _myTerrainEditor2D.transform.position.y);
//             if (_brushLockMode)
//                 _handleLocalPos.y = _brushHandleLockPos.y;

//             Vector2 finalHandlePos = _handleLocalPos + (Vector2)_myTerrainEditor2D.transform.position;
//             Handles.color = Color.green;
//             Handles.CircleCap(0, finalHandlePos, Quaternion.identity, BrushSize);

//             if (_brushLockMode)
//             {
//                 Handles.DrawLine(finalHandlePos + new Vector2(-BrushSize, BrushSize), finalHandlePos + new Vector2(BrushSize, BrushSize));
//                 Handles.DrawLine(finalHandlePos + new Vector2(-BrushSize, -BrushSize), finalHandlePos + new Vector2(BrushSize, -BrushSize));
//             }

//             //Draw cap tile errors
//             if (_myTerrainEditor2D.GenerateCap && _myTerrainEditor2D.SmartCap)
//             {
//                 if (_myTerrainEditor2D.SmartCapAreas != null)
//                 {
//                     foreach (var smartCapArea in _myTerrainEditor2D.SmartCapAreas)
//                     {
//                         Handles.color = Color.red;
//                         if (smartCapArea.CorruptedTilesVertsPoints.Count >= 2)
//                         {
//                             Handles.DrawLine(smartCapArea.CorruptedTilesVertsPoints[0] + _myTerrainEditor2D.transform.position, smartCapArea.CorruptedTilesVertsPoints[1] + _myTerrainEditor2D.transform.position);
//                         }

//                         Handles.color = Color.green;
//                     }
//                 }
//             }

//         }
//         #endregion

//         #region Draw terrain borders
//         Vector3 terrainPos = _myTerrainEditor2D.transform.position;
//         Handles.color = new Color(1, 1, 1, 0.5f);
//         Handles.DrawLine(terrainPos, terrainPos + new Vector3(0, _myTerrainEditor2D.Height));
//         Handles.DrawLine(terrainPos, terrainPos + new Vector3(_myTerrainEditor2D.Width, 0));
//         Handles.DrawLine(terrainPos + new Vector3(0, _myTerrainEditor2D.Height), terrainPos + new Vector3(0, _myTerrainEditor2D.Height) + new Vector3(_myTerrainEditor2D.Width, 0));
//         Handles.DrawLine(terrainPos + new Vector3(_myTerrainEditor2D.Width, 0), terrainPos + new Vector3(0, _myTerrainEditor2D.Height) + new Vector3(_myTerrainEditor2D.Width, 0));

//         if (_myTerrainEditor2D.FixSides)
//         {
//             Vector3 leftFixedPoint = terrainPos + new Vector3(0, _myTerrainEditor2D.LeftFixedPoint);
//             Vector3 rightFixedPoint = terrainPos + new Vector3(_myTerrainEditor2D.Width, _myTerrainEditor2D.RightFixedPoint);
//             Handles.DrawLine(leftFixedPoint, leftFixedPoint - new Vector3(1, 0));
//             Handles.DrawLine(rightFixedPoint, rightFixedPoint + new Vector3(1, 0));
//         }

//         #endregion

//         #region Edit mode events
//         //Start edit
//         if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
//             _startEdit = true;

//         //End edit
//         if (Event.current.type == EventType.MouseUp)
//         {
//             _startEdit = false;

//             //Register custom undo
//             _lastRecordedVerts = new RecordedTerrainVerts(_myTerrainEditor2D.GetVertsPos());
//             if (_recordedVerts.Count > 0)
//             {
//                 if (!IsRecordedTerrainVertsEquals(_recordedVerts[_recordedVerts.Count - 1], _lastRecordedVerts))
//                 {
//                     if (_curUndoRecordedVertsId < _recordedVerts.Count - 1)
//                         _recordedVerts.RemoveRange(_curUndoRecordedVertsId + 1, (_recordedVerts.Count - _curUndoRecordedVertsId - 1));

//                     if (_recordedVerts.Count > 75)
//                         _recordedVerts.RemoveAt(0);

//                     _recordedVerts.Add(_lastRecordedVerts);
//                     _curUndoRecordedVertsId = _recordedVerts.Count - 1;
//                 }
//             }
//             else _recordedVerts.Add(_lastRecordedVerts);
//         }

//         //Dig mode
//         if (Event.current.keyCode == _hotKeys["DigMode"])
//         {
//             if (Event.current.type == EventType.KeyDown)
//                 _digMode = true;
//             if (Event.current.type == EventType.KeyUp)
//                 _digMode = false;
//         }

//         //Brush lock
//         if (Event.current.shift)
//         {
//             if (!_brushLockMode)
//             {
//                 _brushHandleLockPos = _handleLocalPos;
//                 _brushLockMode = true;
//             }
//         }
//         else _brushLockMode = false;

//         //Brush size
//         if (Event.current.alt)
//         {
//             if (Event.current.type == EventType.ScrollWheel)
//             {
//                 float sizeDelta = 0.2f;
//                 if (Event.current.control)
//                     sizeDelta += 0.8f;

//                 if (Event.current.delta.y > 0)
//                     BrushSize -= sizeDelta;
//                 if (Event.current.delta.y < 0)
//                     BrushSize += sizeDelta;

//                 Event.current.Use();
//             }
//             if (BrushSize <= 0.1f)
//                 BrushSize = 0.1f;
//         }

//         //Undo & redo (edit Mode)
//         if (Event.current.shift)
//         {
//             if (Event.current.type == EventType.KeyUp)
//             {
//                 //Undo
//                 if (Event.current.keyCode == _hotKeys["EditModeUndo"])
//                 {
//                     if (_recordedVerts.Count > 0 && _curUndoRecordedVertsId > 0)
//                     {
//                         _curUndoRecordedVertsId--;
//                         _myTerrainEditor2D.EditTerrain(_recordedVerts[_curUndoRecordedVertsId].TerrainVerts, true);
//                     }
//                 }

//                 //Redo
//                 if (Event.current.keyCode == _hotKeys["EditModeRedo"])
//                 {
//                     if (_curUndoRecordedVertsId < _recordedVerts.Count - 1)
//                     {
//                         _curUndoRecordedVertsId++;
//                         _myTerrainEditor2D.EditTerrain(_recordedVerts[_curUndoRecordedVertsId].TerrainVerts, true);
//                     }
//                 }
//             }
//         }

//         #endregion

//         #region Start edit mesh
//         if (_startEdit)
//         {
//             Vector3[] vertsPos = _myTerrainEditor2D.GetVertsPos();

//             for (int i = 0; i < vertsPos.Length; i += 2)
//             {
//                 if (Vector2.Distance(vertsPos[i], _handleLocalPos) <= BrushSize)
//                 {
//                     float vertOffset = BrushSize - Vector2.Distance(vertsPos[i], _handleLocalPos);

//                     if (_digMode)
//                         vertsPos[i] -= new Vector3(0, vertOffset * (BrushHardness * 0.1f));
//                     else vertsPos[i] += new Vector3(0, vertOffset * (BrushHardness * 0.1f));

//                     if (BrushNoise > 0f)
//                         vertsPos[i] += new Vector3(0, Random.Range(-BrushNoise * 0.25f, BrushNoise * 0.25f));

//                     if (BrushHeightLimitEnabled)
//                         if (vertsPos[i].y > BrushHeightLimit)
//                             vertsPos[i].y = BrushHeightLimit;

//                     if (vertsPos[i].y < 0)
//                         vertsPos[i].y = 0;
//                     if (vertsPos[i].y > _myTerrainEditor2D.Height)
//                         vertsPos[i].y = _myTerrainEditor2D.Height;

//                 }
//             }

//             if (!EditorApplication.isPlaying)
//                 _myTerrainEditor2D.EditTerrain(vertsPos, true);
//             else _myTerrainEditor2D.EditTerrain(vertsPos, false);

//             Selection.activeGameObject = _myTerrainEditor2D.gameObject;
//         }
//         #endregion

//         #region Configure handles

//         HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
//         if (_myTerrainEditor2D.GetComponent<Renderer>() != null)
//             EditorUtility.SetSelectedWireframeHidden(_myTerrainEditor2D.GetComponent<Renderer>(), true);
//         if (_myTerrainEditor2D.CapObj != null)
//         {
//             if (_myTerrainEditor2D.CapObj.GetComponent<Renderer>() != null)
//                 EditorUtility.SetSelectedWireframeHidden(_myTerrainEditor2D.CapObj.GetComponent<Renderer>(), true);
//         }

//         #endregion

//         SceneView.RepaintAll();
//         EditorUtility.SetDirty(_myTerrainEditor2D);
//     }

//     void OnDisable()
//     {
//         #region Save prefs
//         EditorPrefs.SetString("BrushSize", BrushSize.ToString());
//         EditorPrefs.SetString("BrushHardness", BrushHardness.ToString());
//         EditorPrefs.SetString("BrushNoise", BrushNoise.ToString());
//         EditorPrefs.SetString("BrushHeightLimit", BrushHeightLimit.ToString());
//         EditorPrefs.SetBool("BrushHeightLimitEnabled", BrushHeightLimitEnabled);

//         EditorPrefs.SetBool("ShowEditSettings", _showEditTab);
//         EditorPrefs.SetBool("ShowCapSettings", _showCapTab);
//         EditorPrefs.SetBool("ShowDeformSettings", _showDeformTab);
//         EditorPrefs.SetBool("ShowMainSettings", _showMainTab);
//         #endregion

//         if (_myTerrainEditor2D == null)
//             return;

//         if (PrefabUtility.GetPrefabType(_myTerrainEditor2D.gameObject) == PrefabType.Prefab)
//             return;

//         if (_editMode)
//             SwitchEditMode();

//         _myTerrainEditor2D.UpdateCollider2D();

//         if (_myTerrainEditor2D.Collider3DObj != null)
//             InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.Collider3DObj.GetComponent<MeshCollider>(), true);
//         if (_myTerrainEditor2D.GetComponent<EdgeCollider2D>())
//             InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.GetComponent<EdgeCollider2D>(), true);
//         if (_myTerrainEditor2D.GetComponent<PolygonCollider2D>())
//             InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.GetComponent<PolygonCollider2D>(), true);

//         #region Save meshes
//         if (!EditorApplication.isPlaying)
//         {
//             SaveMesh(_myTerrainEditor2D.GetComponent<MeshFilter>().sharedMesh);
//             if (_myTerrainEditor2D.GenerateCap)
//             {
//                 if (_myTerrainEditor2D.CapObj != null)
//                     SaveMesh(_myTerrainEditor2D.CapObj.GetComponent<MeshFilter>().sharedMesh);
//                 else _myTerrainEditor2D.GenerateCap = false;
//             }

//             if (_myTerrainEditor2D.Generate3DCollider)
//             {
//                 if (_myTerrainEditor2D.Collider3DObj != null)
//                 {
//                     SaveMesh(_myTerrainEditor2D.Collider3DObj.GetComponent<MeshFilter>().sharedMesh);
//                     _myTerrainEditor2D.Collider3DObj.GetComponent<MeshCollider>().enabled = false;
//                     _myTerrainEditor2D.Collider3DObj.GetComponent<MeshCollider>().enabled = true;
//                 }
//                 else _myTerrainEditor2D.Generate3DCollider = false;
//             }
//         }
//         #endregion


//     }

//     void SwitchTab(string tabName)
//     {
//         _showEditTab = false;
//         _showCapTab = false;
//         _showDeformTab = false;
//         _showMainTab = false;

//         switch (tabName)
//         {
//             case "Edit settings":
//                 _showEditTab = true;
//                 break;

//             case "Cap settings":
//                 _showCapTab = true;
//                 break;

//             case "Deform settings":
//                 _showDeformTab = true;
//                 break;

//             case "Main settings":
//                 _showMainTab = true;
//                 break;
//         }

//         Repaint();
//     }

//     void SwitchEditMode()
//     {
//         if (GetSceneViewCamera() != null)
//         {
//             if (!_editMode && !GetSceneViewCamera().orthographic)
//                 return;
//         }

//         _editMode = !_editMode;

//         if (_editMode)
//         {
//             if (_myTerrainEditor2D.Collider3DObj != null)
//                 InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.Collider3DObj.GetComponent<MeshCollider>(), false);
//             if (_myTerrainEditor2D.GetComponent<EdgeCollider2D>())
//                 InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.GetComponent<EdgeCollider2D>(), false);
//             if (_myTerrainEditor2D.GetComponent<PolygonCollider2D>())
//                 InternalEditorUtility.SetIsInspectorExpanded(_myTerrainEditor2D.GetComponent<PolygonCollider2D>(), false);
//             _previousUsingTool = Tools.current;
//             Tools.current = Tool.None;
//         }
//         else
//         {
//             Tools.current = _previousUsingTool;
//         }
//     }

//     void CheckTexture(Material mat)
//     {
//         if (mat == null)
//             return;

//         mat.mainTexture = (Texture)EditorGUILayout.ObjectField("Material texture", mat.mainTexture, typeof(Texture), false, GUILayout.MaxHeight(15));

//         if (mat.mainTexture != null)
//             if (mat.mainTexture.wrapMode != TextureWrapMode.Repeat)
//                 EditorGUILayout.HelpBox("Material texture wrap mode must be set to Repeat for correct tiling", MessageType.Warning);

//     }

//     void CheckValues()
//     {
//         if (_myTerrainEditor2D.Width < 10)
//             _myTerrainEditor2D.Width = 10;
//         if (_myTerrainEditor2D.Height < 10)
//             _myTerrainEditor2D.Height = 10;
//         if (_myTerrainEditor2D.Resolution < 1)
//             _myTerrainEditor2D.Resolution = 1;
//         if (_myTerrainEditor2D.CapHeight < 0.1f)
//             _myTerrainEditor2D.CapHeight = 0.1f;
//         if (_myTerrainEditor2D.LeftFixedPoint < 0)
//             _myTerrainEditor2D.LeftFixedPoint = 0;
//         if (_myTerrainEditor2D.LeftFixedPoint > _myTerrainEditor2D.Height)
//             _myTerrainEditor2D.LeftFixedPoint = _myTerrainEditor2D.Height;
//         if (_myTerrainEditor2D.RightFixedPoint < 0)
//             _myTerrainEditor2D.RightFixedPoint = 0;
//         if (_myTerrainEditor2D.RightFixedPoint > _myTerrainEditor2D.Height)
//             _myTerrainEditor2D.RightFixedPoint = _myTerrainEditor2D.Height;
//         if (_myTerrainEditor2D.SmartCapCutHeight < 0.01f)
//             _myTerrainEditor2D.SmartCapCutHeight = 0.01f;
//         if (_myTerrainEditor2D.SmartCapSideSegmentsWidth < 0)
//             _myTerrainEditor2D.SmartCapSideSegmentsWidth = 0;

//         _myTerrainEditor2D.GetComponent<Renderer>().material = _myTerrainEditor2D.MainMaterial;
//         if (_myTerrainEditor2D.GenerateCap)
//             if (_myTerrainEditor2D.CapObj != null)
//                 _myTerrainEditor2D.CapObj.GetComponent<Renderer>().material = _myTerrainEditor2D.CapMaterial;
//     }

//     void LoadInspector()
//     {
//         #region Load prefs
//         if (EditorPrefs.HasKey("BrushSize"))
//             BrushSize = Single.Parse(EditorPrefs.GetString("BrushSize"));
//         if (EditorPrefs.HasKey("BrushHardness"))
//             BrushHardness = Single.Parse(EditorPrefs.GetString("BrushHardness"));
//         if (EditorPrefs.HasKey("BrushNoise"))
//             BrushNoise = Single.Parse(EditorPrefs.GetString("BrushNoise"));
//         if (EditorPrefs.HasKey("BrushHeightLimit"))
//             BrushHeightLimit = Single.Parse(EditorPrefs.GetString("BrushHeightLimit"));
//         if (EditorPrefs.HasKey("BrushHeightLimitEnabled"))
//             BrushHeightLimitEnabled = EditorPrefs.GetBool("BrushHeightLimitEnabled");

//         if (EditorPrefs.HasKey("ShowEditSettings"))
//             _showEditTab = EditorPrefs.GetBool("ShowEditSettings");
//         if (EditorPrefs.HasKey("ShowCapSettings"))
//             _showCapTab = EditorPrefs.GetBool("ShowCapSettings");
//         if (EditorPrefs.HasKey("ShowDeformSettings"))
//             _showDeformTab = EditorPrefs.GetBool("ShowDeformSettings");
//         if (EditorPrefs.HasKey("ShowMainSettings"))
//             _showMainTab = EditorPrefs.GetBool("ShowMainSettings");
//         #endregion

//         string path = GetFolderPath("2DTerrainInspectorRes");
//         path = path.Replace('\\', '/');

//         _inspectorTextures["Edit settings"] = (Texture)AssetDatabase.LoadAssetAtPath(path + "2dterrain_inspector_edit_settings_button.png", typeof(Texture));
//         _inspectorTextures["Cap settings"] = (Texture)AssetDatabase.LoadAssetAtPath(path + "2dterrain_inspector_cap_settings_button.png", typeof(Texture));
//         _inspectorTextures["Deform settings"] = (Texture)AssetDatabase.LoadAssetAtPath(path + "2dterrain_inspector_deform_settings_button.png", typeof(Texture));
//         _inspectorTextures["Main settings"] = (Texture)AssetDatabase.LoadAssetAtPath(path + "2dterrain_inspector_main_settings_button.png", typeof(Texture));
//     }

//     void CopySharedMesh(MeshFilter meshFilter)
//     {
//         Mesh sharedMesh = meshFilter.sharedMesh;
//         Mesh newMeshInstance = new Mesh();

//         newMeshInstance.vertices = sharedMesh.vertices;
//         newMeshInstance.triangles = sharedMesh.triangles;
//         newMeshInstance.normals = sharedMesh.normals;
//         newMeshInstance.uv = sharedMesh.uv;
//         newMeshInstance.RecalculateBounds();

//         newMeshInstance.name = sharedMesh.name.Remove(sharedMesh.name.LastIndexOf('_') + 1, (sharedMesh.name.Length - sharedMesh.name.LastIndexOf('_') - 1));
//         newMeshInstance.name += _myTerrainEditor2D.GetInstanceID();

//         meshFilter.sharedMesh = newMeshInstance;
//     }

//     void SaveMesh(Mesh mesh)
//     {
//         if (PrefabUtility.GetPrefabType(_myTerrainEditor2D.gameObject) == PrefabType.Prefab)
//             return;

//         string path = GetFolderPath("2DTerrainSavedMeshes");

//         if (String.IsNullOrEmpty(path))
//         {
//             Debug.LogError("'SavedTerrain2DMeshes' folder is missing. Please, create folder with this name to save 2D terrain mesh.");
//             return;
//         }

//         AssetDatabase.Refresh();

//         if (!AssetDatabase.Contains(mesh))
//             AssetDatabase.CreateAsset(mesh, path + mesh.name + ".asset");


//         AssetDatabase.SaveAssets();
//     }

//     string[] GetSortingLayerNames()
//     {
//         Type internalEditorUtilityType = typeof(InternalEditorUtility);

//         PropertyInfo sortingLayersProperty = internalEditorUtilityType.GetProperty("sortingLayerNames", BindingFlags.Static | BindingFlags.NonPublic);
//         return (string[])sortingLayersProperty.GetValue(null, new object[0]);
//     }

//     string GetFolderPath(string folderName)
//     {
//         string[] targetDirs = Directory.GetDirectories(Application.dataPath, folderName, SearchOption.AllDirectories);
//         foreach (var savedMeshesPath in targetDirs)
//         {
//             return savedMeshesPath.Remove(0, savedMeshesPath.IndexOf("/Assets", StringComparison.Ordinal) + 1) + "\\";
//         }

//         return null;
//     }

//     Camera GetSceneViewCamera()
//     {
//         if (SceneView.lastActiveSceneView != null)
//             if (SceneView.lastActiveSceneView.camera != null)
//                 return SceneView.lastActiveSceneView.camera;

//         return null;
//     }

//     bool IsRecordedTerrainVertsEquals(RecordedTerrainVerts rtv1, RecordedTerrainVerts rtv2)
//     {
//         if (rtv1.TerrainVerts.Length != rtv2.TerrainVerts.Length)
//             return false;

//         for (int i = 0; i < rtv1.TerrainVerts.Length; i++)
//         {
//             if (rtv1.TerrainVerts[i] != rtv2.TerrainVerts[i])
//                 return false;
//         }
//         return true;
//     }

//     class RecordedTerrainVerts
//     {
//         public Vector3[] TerrainVerts;

//         public RecordedTerrainVerts(Vector3[] terrainVerts)
//         {
//             TerrainVerts = terrainVerts;
//         }
//     }

// }



















/*


using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Custom inspector for TerrainEditor2D.
/// Every setting updates the terrain live, and the path can be sculpted directly in the Scene view.
/// Place this file inside any folder named "Editor".
/// </summary>
[CustomEditor(typeof(TerrainEditor2D))]
public class TerrainEditor2DInspector : Editor
{
    TerrainEditor2D t;

    // Serialized properties
    SerializedProperty width, height, resolution;
    SerializedProperty mainMaterial, mainTextureSize, mainColor, sortingLayerName, sortingLayerId, orderInLayer;
    SerializedProperty rndHeight, rndHillsCount, rndAmplitude, useRandomSeed, randomSeed;
    SerializedProperty generateCap, capMaterial, capTextureTiling, capColor, capSortingLayerName, capSortingLayerId, capOrderInLayer;
    SerializedProperty capHeight, capOffset, smartCap, smartCapCutHeight, smartCapSideSegmentsWidth, smartCapSegmentsUv;
    SerializedProperty realtimeDeformEnabled, realtimeDeformUpdateUv, realtimeDeformUpdateColliders;
    SerializedProperty fixSides, leftFixedPoint, rightFixedPoint, generate3DCollider, collider3DWidth;

    // Scene editing state (kept for the editor session)
    static bool editPath;
    static float handleSize = 0.12f;
    static float brushRadius = 3f;
    static float smoothStrength = 0.5f;
    static float flattenHeight = 25f;

    int ExpectedVertexCount => t.Width * 2 * t.Resolution + 2;

    // ------------------------------------------------------------------ lifecycle

    void OnEnable()
    {
        t = (TerrainEditor2D)target;
        FindProperties();

        Undo.undoRedoPerformed += OnUndoRedo;

        if (EditorUtility.IsPersistent(t))
            return; // prefab asset in Project window: don't touch meshes

        t.SetInstanceId(t.GetInstanceID());
        EnsureUniqueMeshes();

        if (t.GetComponent<MeshFilter>().sharedMesh == null)
        {
            t.Create();
            MarkDirty();
        }
    }

    void OnDisable()
    {
        Undo.undoRedoPerformed -= OnUndoRedo;
    }

    void OnUndoRedo()
    {
        if (t == null || EditorUtility.IsPersistent(t))
            return;

        Refresh();
        Repaint();
    }

    void FindProperties()
    {
        width = serializedObject.FindProperty("Width");
        height = serializedObject.FindProperty("Height");
        resolution = serializedObject.FindProperty("Resolution");

        mainMaterial = serializedObject.FindProperty("MainMaterial");
        mainTextureSize = serializedObject.FindProperty("MainTextureSize");
        mainColor = serializedObject.FindProperty("MainColor");
        sortingLayerName = serializedObject.FindProperty("SortingLayerName");
        sortingLayerId = serializedObject.FindProperty("SortingLayerId");
        orderInLayer = serializedObject.FindProperty("OrderInLayer");

        rndHeight = serializedObject.FindProperty("RndHeight");
        rndHillsCount = serializedObject.FindProperty("RndHillsCount");
        rndAmplitude = serializedObject.FindProperty("RndAmplitude");
        useRandomSeed = serializedObject.FindProperty("UseRandomSeed");
        randomSeed = serializedObject.FindProperty("RandomSeed");

        generateCap = serializedObject.FindProperty("GenerateCap");
        capMaterial = serializedObject.FindProperty("CapMaterial");
        capTextureTiling = serializedObject.FindProperty("CapTextureTiling");
        capColor = serializedObject.FindProperty("CapColor");
        capSortingLayerName = serializedObject.FindProperty("CapSortingLayerName");
        capSortingLayerId = serializedObject.FindProperty("CapSortingLayerId");
        capOrderInLayer = serializedObject.FindProperty("CapOrderInLayer");
        capHeight = serializedObject.FindProperty("CapHeight");
        capOffset = serializedObject.FindProperty("CapOffset");
        smartCap = serializedObject.FindProperty("SmartCap");
        smartCapCutHeight = serializedObject.FindProperty("SmartCapCutHeight");
        smartCapSideSegmentsWidth = serializedObject.FindProperty("SmartCapSideSegmentsWidth");
        smartCapSegmentsUv = serializedObject.FindProperty("SmartCapSegmentsUv");

        realtimeDeformEnabled = serializedObject.FindProperty("RealtimeDeformEnabled");
        realtimeDeformUpdateUv = serializedObject.FindProperty("RealtimeDeformUpdateUv");
        realtimeDeformUpdateColliders = serializedObject.FindProperty("RealtimeDeformUpdateColliders");

        fixSides = serializedObject.FindProperty("FixSides");
        leftFixedPoint = serializedObject.FindProperty("LeftFixedPoint");
        rightFixedPoint = serializedObject.FindProperty("RightFixedPoint");
        generate3DCollider = serializedObject.FindProperty("Generate3DCollider");
        collider3DWidth = serializedObject.FindProperty("Collider3DWidth");
    }

    // ------------------------------------------------------------------ inspector

    public override void OnInspectorGUI()
    {
        if (EditorUtility.IsPersistent(t))
        {
            EditorGUILayout.HelpBox("Open the prefab or place it in a scene to edit the terrain.", MessageType.Info);
            DrawDefaultInspector();
            return;
        }

        serializedObject.Update();

        bool sizeChanged = false;
        bool settingsChanged = false;

        // ---------- Size
        if (Section("Terrain Size", "size"))
        {
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.DelayedIntField(width, new GUIContent("Width", width.tooltip));
            EditorGUILayout.DelayedIntField(height, new GUIContent("Height", height.tooltip));
            EditorGUILayout.PropertyField(resolution);
            if (EditorGUI.EndChangeCheck())
                sizeChanged = true;

            EditorGUILayout.LabelField("Path points", (t.Width * t.Resolution + 1).ToString(), EditorStyles.miniLabel);
            EndSection();
        }

        EditorGUI.BeginChangeCheck();

        // ---------- Appearance
        if (Section("Appearance", "appearance"))
        {
            EditorGUILayout.PropertyField(mainMaterial);
            EditorGUILayout.PropertyField(mainTextureSize, new GUIContent("Texture Tiling", mainTextureSize.tooltip));
            EditorGUILayout.PropertyField(mainColor, new GUIContent("Vertex Color", mainColor.tooltip));
            SortingLayerField("Sorting Layer", sortingLayerName, sortingLayerId);
            EditorGUILayout.PropertyField(orderInLayer);

            if (t.MainMaterial == null)
                EditorGUILayout.HelpBox("Assign a material to see the terrain texture.", MessageType.None);
            EndSection();
        }

        // ---------- Cap
        if (Section("Cap (Top Layer)", "cap"))
        {
            EditorGUILayout.PropertyField(generateCap);
            if (generateCap.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(capMaterial);
                EditorGUILayout.PropertyField(capColor, new GUIContent("Vertex Color", capColor.tooltip));
                SortingLayerField("Sorting Layer", capSortingLayerName, capSortingLayerId);
                EditorGUILayout.PropertyField(capOrderInLayer, new GUIContent("Order In Layer"));
                EditorGUILayout.PropertyField(capHeight);
                EditorGUILayout.PropertyField(capOffset);

                EditorGUILayout.Space(4);
                EditorGUILayout.PropertyField(smartCap);
                if (smartCap.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(smartCapCutHeight, new GUIContent("Cut Height", smartCapCutHeight.tooltip));
                    EditorGUILayout.PropertyField(smartCapSideSegmentsWidth, new GUIContent("End Segment Width", smartCapSideSegmentsWidth.tooltip));

                    Vector2 uv = smartCapSegmentsUv.vector2Value;
                    float min = uv.x, max = uv.y;
                    EditorGUILayout.MinMaxSlider(new GUIContent("Texture Split UV", smartCapSegmentsUv.tooltip), ref min, ref max, 0f, 1f);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUIUtility.labelWidth);
                    min = EditorGUILayout.FloatField(min);
                    max = EditorGUILayout.FloatField(max);
                    EditorGUILayout.EndHorizontal();
                    min = Mathf.Clamp01(min);
                    max = Mathf.Clamp(max, min, 1f);
                    smartCapSegmentsUv.vector2Value = new Vector2(min, max);
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUILayout.PropertyField(capTextureTiling);
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        // ---------- Sides & colliders
        if (Section("Sides & Colliders", "sides"))
        {
            EditorGUILayout.PropertyField(fixSides);
            using (new EditorGUI.DisabledScope(!fixSides.boolValue))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Slider(leftFixedPoint, 0f, height.intValue, new GUIContent("Left Height", leftFixedPoint.tooltip));
                EditorGUILayout.Slider(rightFixedPoint, 0f, height.intValue, new GUIContent("Right Height", rightFixedPoint.tooltip));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(generate3DCollider);
            if (generate3DCollider.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(collider3DWidth, new GUIContent("Depth (Z)", collider3DWidth.tooltip));
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        if (EditorGUI.EndChangeCheck())
            settingsChanged = true;

        // ---------- Random generation (only applied when pressing Randomize)
        bool randomize = false;
        if (Section("Random Generation", "random"))
        {
            EditorGUILayout.Slider(rndHeight, 0f, height.intValue, new GUIContent("Average Height", rndHeight.tooltip));
            EditorGUILayout.Slider(rndAmplitude, 0f, height.intValue * 0.5f, new GUIContent("Amplitude", rndAmplitude.tooltip));
            int maxHills = Mathf.Max(1, (width.intValue * resolution.intValue) / 2);
            EditorGUILayout.IntSlider(rndHillsCount, 1, maxHills, new GUIContent("Hills Count", rndHillsCount.tooltip));

            EditorGUILayout.PropertyField(useRandomSeed, new GUIContent("Use Fixed Seed", useRandomSeed.tooltip));
            if (useRandomSeed.boolValue)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(randomSeed, new GUIContent("Seed"));
                if (GUILayout.Button("New", GUILayout.Width(45)))
                    randomSeed.intValue = Random.Range(int.MinValue, int.MaxValue);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(2);
            if (GUILayout.Button("Randomize Terrain", GUILayout.Height(26)))
                randomize = true;
            EndSection();
        }

        // ---------- Sculpt tools
        bool smooth = false, flatten = false, reset = false, saveAsset = false;
        if (Section("Sculpt Tools", "tools"))
        {
            Color prev = GUI.backgroundColor;
            if (editPath) GUI.backgroundColor = new Color(0.55f, 0.85f, 1f);
            if (GUILayout.Button(editPath ? "Editing Path in Scene View (click to stop)" : "Edit Path in Scene View", GUILayout.Height(26)))
            {
                editPath = !editPath;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = prev;

            if (editPath)
            {
                EditorGUI.indentLevel++;
                brushRadius = EditorGUILayout.Slider(new GUIContent("Brush Radius", "Neighbour points within this distance (units) move with the dragged point. 0 = single point"), brushRadius, 0f, Mathf.Max(1f, t.Width * 0.5f));
                handleSize = EditorGUILayout.Slider("Handle Size", handleSize, 0.03f, 0.4f);
                EditorGUI.indentLevel--;
                EditorGUILayout.HelpBox("Drag the dots up or down in the Scene view to sculpt the terrain.", MessageType.None);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            smoothStrength = EditorGUILayout.Slider("Smooth Strength", smoothStrength, 0.05f, 1f);
            if (GUILayout.Button("Smooth", GUILayout.Width(70))) smooth = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            flattenHeight = EditorGUILayout.Slider("Flatten Height", Mathf.Min(flattenHeight, t.Height), 0f, t.Height);
            if (GUILayout.Button("Flatten", GUILayout.Width(70))) flatten = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Flat")) reset = true;
            if (GUILayout.Button("Save Meshes as Asset")) saveAsset = true;
            EditorGUILayout.EndHorizontal();
            EndSection();
        }

        // ---------- Runtime deform
        if (Section("Runtime Deformation", "deform", false))
        {
            EditorGUILayout.PropertyField(realtimeDeformEnabled, new GUIContent("Enabled", realtimeDeformEnabled.tooltip));
            using (new EditorGUI.DisabledScope(!realtimeDeformEnabled.boolValue))
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(realtimeDeformUpdateUv, new GUIContent("Update UV"));
                EditorGUILayout.PropertyField(realtimeDeformUpdateColliders, new GUIContent("Update Colliders"));
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        serializedObject.ApplyModifiedProperties();

        // ---------- Apply changes to the mesh (after properties are written to the component)
        if (sizeChanged)
        {
            RecordAll("Change Terrain Size");
            RebuildKeepingShape();
        }
        else if (settingsChanged)
        {
            RecordAll("Change Terrain Settings");
            Refresh();
        }

        if (randomize)
        {
            RecordAll("Randomize Terrain");
            if (t.GetVertsPos() == null || t.GetVertsPos().Length != ExpectedVertexCount) t.Create();
            t.RandomizeTerrain(true);
            MarkDirty();
        }
        if (smooth) { RecordAll("Smooth Terrain"); Smooth(); }
        if (flatten) { RecordAll("Flatten Terrain"); Flatten(); }
        if (reset) { RecordAll("Reset Terrain"); t.Create(); MarkDirty(); }
        if (saveAsset) SaveMeshesAsAsset();
    }

    // ------------------------------------------------------------------ scene view sculpting

    void OnSceneGUI()
    {
        if (!editPath || t == null || EditorUtility.IsPersistent(t))
            return;

        Vector3[] verts = t.GetVertsPos();
        if (verts == null || verts.Length < 4)
            return;

        Transform tr = t.transform;

        // Outline of the path
        Vector3[] line = new Vector3[verts.Length / 2];
        for (int i = 0; i < verts.Length; i += 2)
            line[i / 2] = tr.TransformPoint(verts[i]);

        Handles.color = new Color(0.3f, 0.8f, 1f, 0.9f);
        Handles.DrawAAPolyLine(3f, line);

        // Point handles (vertical only)
        int movedIndex = -1;
        Vector3 movedTo = Vector3.zero;

        for (int i = 0; i < verts.Length; i += 2)
        {
            Vector3 world = line[i / 2];
            bool locked = t.FixSides && (i == 0 || i == verts.Length - 2);
            Handles.color = locked ? new Color(1f, 0.6f, 0.2f) : Color.white;

            float size = HandleUtility.GetHandleSize(world) * handleSize;
            EditorGUI.BeginChangeCheck();
            Vector3 newWorld = Handles.Slider(world, tr.up, size, Handles.DotHandleCap, 0f);
            if (EditorGUI.EndChangeCheck())
            {
                movedIndex = i;
                movedTo = newWorld;
            }
        }

        if (movedIndex < 0)
            return;

        RecordAll("Sculpt Terrain");

        float newY = Mathf.Clamp(tr.InverseTransformPoint(movedTo).y, 0f, t.Height);
        float delta = newY - verts[movedIndex].y;
        float centerX = verts[movedIndex].x;

        for (int i = 0; i < verts.Length; i += 2)
        {
            float weight;
            if (i == movedIndex)
                weight = 1f;
            else if (brushRadius > 0f)
            {
                float d = Mathf.Abs(verts[i].x - centerX) / brushRadius;
                if (d >= 1f) continue;
                weight = Mathf.SmoothStep(1f, 0f, d);
            }
            else continue;

            verts[i].y = Mathf.Clamp(verts[i].y + delta * weight, 0f, t.Height);
        }

        // Dragging a locked end moves its fixed height
        if (t.FixSides)
        {
            t.LeftFixedPoint = verts[0].y;
            t.RightFixedPoint = verts[verts.Length - 2].y;
        }

        t.EditTerrain(verts, true);
        MarkDirty();
    }

    // ------------------------------------------------------------------ operations

    /// <summary>Re-applies all settings to the current terrain shape.</summary>
    void Refresh()
    {
        Vector3[] verts = t.GetVertsPos();
        if (verts == null || verts.Length != ExpectedVertexCount)
        {
            RebuildKeepingShape();
            return;
        }

        t.EditTerrain(verts, true);
        MarkDirty();
    }

    /// <summary>Rebuilds the mesh for a new Width/Height/Resolution while keeping the current hill shape.</summary>
    void RebuildKeepingShape()
    {
        Vector3[] oldPath = t.GetPath(Space.Self);
        t.Create();

        if (oldPath != null && oldPath.Length > 1)
        {
            Vector3[] verts = t.GetVertsPos();
            for (int i = 0; i < verts.Length; i += 2)
                verts[i].y = Mathf.Clamp(SampleHeight(oldPath, verts[i].x), 0f, t.Height);

            t.EditTerrain(verts, true);
        }

        MarkDirty();
    }

    static float SampleHeight(Vector3[] path, float x)
    {
        if (x <= path[0].x)
            return path[0].y;

        for (int i = 1; i < path.Length; i++)
        {
            if (x <= path[i].x)
                return Mathf.Lerp(path[i - 1].y, path[i].y, Mathf.InverseLerp(path[i - 1].x, path[i].x, x));
        }

        return path[path.Length - 1].y;
    }

    void Smooth()
    {
        Vector3[] verts = t.GetVertsPos();
        if (verts == null) return;

        float[] ys = new float[verts.Length];
        for (int i = 0; i < verts.Length; i++) ys[i] = verts[i].y;

        for (int i = 2; i < verts.Length - 2; i += 2)
        {
            float avg = (ys[i - 2] + ys[i] + ys[i + 2]) / 3f;
            verts[i].y = Mathf.Lerp(ys[i], avg, smoothStrength);
        }

        t.EditTerrain(verts, true);
        MarkDirty();
    }

    void Flatten()
    {
        Vector3[] verts = t.GetVertsPos();
        if (verts == null) return;

        for (int i = 0; i < verts.Length; i += 2)
            verts[i].y = flattenHeight;

        t.EditTerrain(verts, true);
        MarkDirty();
    }

    /// <summary>
    /// Saves terrain, cap and 3D collider meshes into one .asset file so the terrain can be used in prefabs.
    /// </summary>
    void SaveMeshesAsAsset()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Terrain Meshes", t.name + "_Terrain2D", "asset",
            "Choose where to save the terrain meshes");
        if (string.IsNullOrEmpty(path))
            return;

        MeshFilter mainFilter = t.GetComponent<MeshFilter>();
        MeshFilter capFilter = t.CapObj != null ? t.CapObj.GetComponent<MeshFilter>() : null;
        MeshFilter colFilter = t.Collider3DObj != null ? t.Collider3DObj.GetComponent<MeshFilter>() : null;

        Mesh mainCopy = Instantiate(mainFilter.sharedMesh);
        mainCopy.name = t.name + "_mesh";
        AssetDatabase.CreateAsset(mainCopy, path);
        Undo.RecordObject(mainFilter, "Save Terrain Meshes");
        mainFilter.sharedMesh = mainCopy;

        if (capFilter != null && capFilter.sharedMesh != null)
        {
            Mesh capCopy = Instantiate(capFilter.sharedMesh);
            capCopy.name = t.name + "_cap";
            AssetDatabase.AddObjectToAsset(capCopy, path);
            Undo.RecordObject(capFilter, "Save Terrain Meshes");
            capFilter.sharedMesh = capCopy;
        }

        if (colFilter != null && colFilter.sharedMesh != null)
        {
            Mesh colCopy = Instantiate(colFilter.sharedMesh);
            colCopy.name = t.name + "_collider3D";
            AssetDatabase.AddObjectToAsset(colCopy, path);
            Undo.RecordObject(colFilter, "Save Terrain Meshes");
            colFilter.sharedMesh = colCopy;
            MeshCollider mc = t.Collider3DObj.GetComponent<MeshCollider>();
            Undo.RecordObject(mc, "Save Terrain Meshes");
            mc.sharedMesh = colCopy;
        }

        AssetDatabase.SaveAssets();
        MarkDirty();
        EditorGUIUtility.PingObject(mainCopy);
    }

    // ------------------------------------------------------------------ helpers

    /// <summary>
    /// When a terrain is duplicated (Ctrl+D) both copies share the same meshes, so editing one changes the other.
    /// This gives the selected terrain its own copies.
    /// </summary>
    void EnsureUniqueMeshes()
    {
#if UNITY_2022_2_OR_NEWER
        TerrainEditor2D[] all = FindObjectsByType<TerrainEditor2D>(FindObjectsSortMode.None);
#else
        TerrainEditor2D[] all = FindObjectsOfType<TerrainEditor2D>();
#endif
        var others = all.Where(o => o != t).ToArray();

        MakeUnique(t.GetComponent<MeshFilter>(), others, o => o.GetComponent<MeshFilter>());
        if (t.CapObj != null)
            MakeUnique(t.CapObj.GetComponent<MeshFilter>(), others, o => o.CapObj != null ? o.CapObj.GetComponent<MeshFilter>() : null);
        if (t.Collider3DObj != null)
        {
            MeshFilter mf = t.Collider3DObj.GetComponent<MeshFilter>();
            if (MakeUnique(mf, others, o => o.Collider3DObj != null ? o.Collider3DObj.GetComponent<MeshFilter>() : null))
                t.Collider3DObj.GetComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
        }
    }

    static bool MakeUnique(MeshFilter mf, TerrainEditor2D[] others, System.Func<TerrainEditor2D, MeshFilter> getFilter)
    {
        if (mf == null || mf.sharedMesh == null)
            return false;

        bool shared = others.Any(o =>
        {
            MeshFilter f = getFilter(o);
            return f != null && f.sharedMesh == mf.sharedMesh;
        });
        if (!shared)
            return false;

        Mesh copy = Instantiate(mf.sharedMesh);
        copy.name = mf.sharedMesh.name.Replace("(Clone)", "") + "_copy";
        mf.sharedMesh = copy;
        return true;
    }

    /// <summary>Registers everything a terrain edit can touch so Ctrl+Z works.</summary>
    void RecordAll(string undoName)
    {
        var objs = new List<Object> { t, t.GetComponent<EdgeCollider2D>() };

        MeshFilter mf = t.GetComponent<MeshFilter>();
        if (mf.sharedMesh != null) objs.Add(mf.sharedMesh);

        if (t.CapObj != null)
        {
            MeshFilter cap = t.CapObj.GetComponent<MeshFilter>();
            if (cap != null && cap.sharedMesh != null) objs.Add(cap.sharedMesh);
        }

        if (t.Collider3DObj != null)
        {
            MeshFilter col = t.Collider3DObj.GetComponent<MeshFilter>();
            if (col != null && col.sharedMesh != null) objs.Add(col.sharedMesh);
        }

        Undo.RecordObjects(objs.Where(o => o != null).ToArray(), undoName);
    }

    void MarkDirty()
    {
        EditorUtility.SetDirty(t);
        if (!Application.isPlaying && t.gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(t.gameObject.scene);
        SceneView.RepaintAll();
    }

    static void SortingLayerField(string label, SerializedProperty nameProp, SerializedProperty idProp)
    {
        SortingLayer[] layers = SortingLayer.layers;
        string[] names = layers.Select(l => l.name).ToArray();

        int current = System.Array.IndexOf(names, nameProp.stringValue);
        int shown = Mathf.Max(0, current);
        int picked = EditorGUILayout.Popup(label, shown, names);

        if (picked != current)
        {
            nameProp.stringValue = names[picked];
            idProp.intValue = layers[picked].id;
            if (current >= 0) GUI.changed = true;
        }
    }

    static bool Section(string title, string key, bool defaultOpen = true)
    {
        string prefKey = "TerrainEditor2D_" + key;
        bool open = SessionState.GetBool(prefKey, defaultOpen);

        EditorGUILayout.Space(2);
        bool newOpen = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
        if (newOpen != open)
            SessionState.SetBool(prefKey, newOpen);

        if (newOpen)
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        return newOpen;
    }

    static void EndSection()
    {
        EditorGUILayout.EndVertical();
    }

    // ------------------------------------------------------------------ menu

    [MenuItem("GameObject/2D Object/Terrain 2D", false, 10)]
    static void CreateTerrainMenu(MenuCommand command)
    {
        GameObject go = TerrainEditor2D.InstantiateTerrain2D(Vector3.zero);
        go.name = "Terrain 2D";

        GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);

        if (command.context == null && SceneView.lastActiveSceneView != null)
        {
            TerrainEditor2D terrain = go.GetComponent<TerrainEditor2D>();
            Vector3 pivot = SceneView.lastActiveSceneView.pivot;
            go.transform.position = new Vector3(pivot.x - terrain.Width * 0.5f, pivot.y - terrain.Height * 0.5f, 0f);
        }

        Undo.RegisterCreatedObjectUndo(go, "Create Terrain 2D");
        Selection.activeGameObject = go;
    }
}*/