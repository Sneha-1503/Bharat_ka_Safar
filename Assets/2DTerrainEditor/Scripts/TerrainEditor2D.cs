using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class TerrainEditor2D : MonoBehaviour
{
    // --- Variables

    #region Main values
    public int InstanceId { get; private set; }

    /// <summary>
    /// 2D terrain width in units
    /// </summary>
    public int Width = 50;
    /// <summary>
    /// 2D terrain height in units
    /// </summary>
    public int Height = 50;
    /// <summary>
    /// 2D terrain segments per unit
    /// </summary>
    public int Resolution = 1;
    #endregion

    #region 2D Terrain settings values
    /// <summary>
    /// Main material of this 2D terrain with tiled texture
    /// </summary>
    public Material MainMaterial;
    /// <summary>
    /// Number of main texture tiles for whole 2D terrain 
    /// </summary>
    public int MainTextureSize = 15;
    /// <summary>
    /// Color of 2D terrain mesh. Works only if shader supports vertex colors.
    /// </summary>
    public Color MainColor = Color.white;
    public int SortingLayerId;
    public string SortingLayerName;
    public int OrderInLayer;
    #endregion

    #region Random values
    /// <summary>
    /// Average height of randomly generated 2D terrain. WARNING: Value can not be more than <see cref="Height"/>
    /// </summary>
    public float RndHeight = 25;
    /// <summary>
    /// Max hills number of randomly generated 2D terrain. WARNING: Value can not be more than (<see cref="Width"/> - 1) / 2
    /// </summary>
    public int RndHillsCount = 5;
    /// <summary>
    /// Average height of randomly generated 2D terrain. WARNING: Value can not be more than <see cref="Height"/> / 2
    /// </summary>
    public float RndAmplitude = 5;
    #endregion

    #region Cap settings values
    /// <summary>
    /// Is cap needs to be generated for this 2D terrain?
    /// </summary>
    public bool GenerateCap;
    /// <summary>
    /// Cap material of this 2D terrain with tiled texture
    /// </summary>
    public Material CapMaterial;
    /// <summary>
    /// Number of cap texture tiles for whole 2D terrain (does not affect to cap with <see cref="SmartCap"/> enabled)
    /// </summary>
    public int CapTextureTiling = 15;
    /// <summary>
    /// Color of cap mesh. Works only if shader supports vertex colors.
    /// </summary>
    public Color CapColor = Color.white;
    public string CapSortingLayerName;
    public int CapSortingLayerId;
    public int CapOrderInLayer;
    /// <summary>
    /// Cap height in units
    /// </summary>
    public float CapHeight = 1;
    /// <summary>
    /// Cap offset relative to 2D terrain tip
    /// </summary>
    public float CapOffset;
    /// <summary>
    /// Is Smart Cap mode enabled for 2D terrain cap?
    /// </summary>
    public bool SmartCap = true;
    /// <summary>
    /// Min height in units for splitting cap into different paths
    /// </summary>
    public float SmartCapCutHeight = 1;
    /// <summary>
    /// Width of side segments in units
    /// </summary>
    public float SmartCapSideSegmentsWidth = 1;
    /// <summary>
    /// Horizontal UV for smart cap texture. It's like separator which divides cap texture into 3 parts.
    /// </summary>
    public Vector2 SmartCapSegmentsUv = new Vector2(0.125f, 0.875f);
    #endregion

    #region Deform settings values
    /// <summary>
    /// Is this terrain 2D can deform in realtime?
    /// </summary>
    public bool RealtimeDeformEnabled = false;
    /// <summary>
    /// Is UV texture coordinates should be updated on each iteration of deformation?
    /// </summary>
    public bool RealtimeDeformUpdateUv = true;
    /// <summary>
    /// Is colliders should be updated on each iteration of deformation?
    /// </summary>
    public bool RealtimeDeformUpdateColliders = true;
    #endregion

    #region Additional settings values
    /// <summary>
    /// Is side end points of terrain 2D path needs to be fixed and protected from changes? 
    /// </summary>
    public bool FixSides;
    /// <summary>
    /// This value will NOT be changed after applying random terrain generation.WARNING: Value can not be more than <see cref="Height"/>
    /// </summary>
    public float LeftFixedPoint = 25;
    /// <summary>
    /// This value will be changed after applying random terrain generation. WARNING: Value can not be more than <see cref="Height"/>
    /// </summary>
    public float RightFixedPoint = 25;
    /// <summary>
    /// Is 3D collider mesh needs to be generated on top of this 2D terrain?
    /// </summary>
    public bool Generate3DCollider;
    /// <summary>
    /// Width of 3D collider by Z
    /// </summary>
    public float Collider3DWidth = 5;
    #endregion

    #region References

    public GameObject CapObj;
    public GameObject Collider3DObj;
    public List<SmartCapArea> SmartCapAreas; 
    #endregion

    // --- public methods

    /// <summary>
    /// Generates new flat terrain based on main values
    /// </summary>
    public void Create()
    {
        Mesh pathMesh;
        if (gameObject.GetComponent<MeshFilter>().sharedMesh == null)
        {
            pathMesh = new Mesh();
            pathMesh.name = "Terrain2D_mesh_" + InstanceId;
            gameObject.GetComponent<MeshFilter>().mesh = pathMesh;
        }
        else
        {
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            pathMesh.Clear();
        }

        #region Generate vertices
        Vector3[] vertices = new Vector3[((Width * 2) * Resolution) + 2];

        for (int i = 0; i < vertices.Length; i += 2)
        {
            float vertsInterval = (i * 0.5f) / Resolution;

            vertices[i] = new Vector3(vertsInterval, Height * 0.5f, 0);
            vertices[i + 1] = new Vector3(vertsInterval, 0, 0);
        }

        if (FixSides)
        {
            vertices[0].y = LeftFixedPoint;
            vertices[vertices.Length - 2].y = RightFixedPoint;
        }
        #endregion

        #region Generate triangles
        int[] tris = new int[(vertices.Length - 2) * 3];

        bool toSide = false;
        int curTrisIndex = 0;
        for (int i = 0; i < vertices.Length - 2; i++)
        {
            if (toSide)
            {
                tris[curTrisIndex] = i;
                tris[curTrisIndex + 1] = i + 1;
                tris[curTrisIndex + 2] = i + 2;
            }
            else
            {
                tris[curTrisIndex] = i + 2;
                tris[curTrisIndex + 1] = i + 1;
                tris[curTrisIndex + 2] = i;
            }
            toSide = !toSide;

            curTrisIndex += 3;
        }
        #endregion

        #region Generate UV
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i].y / Height) * ((float)Height / Width) * MainTextureSize);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i + 1].y / Height) * ((float)Height / Width) * MainTextureSize);
        }
        #endregion

        pathMesh.vertices = vertices;
        pathMesh.triangles = tris;
        pathMesh.normals = GetMeshNormals(vertices.Length);
        pathMesh.uv = uv;
        pathMesh.SetColors(GetMeshColors(MainColor, vertices.Length).ToList());
        pathMesh.RecalculateBounds();

        SetSortingLayerAndOrder();

        UpdateCollider2D();
        UpdateCap(true);
        UpdateCollider3D(true);
    }

    /// <summary>
    /// Generates new or updates 2D collider based on terrain path
    /// </summary>
    public void UpdateCollider2D()
    {
        #region Update Edge Collider 2D
        if (GetComponent<EdgeCollider2D>() != null)
        {
            Vector3[] path = GetPath(Space.Self);
            Vector2[] colliderPath = new Vector2[path.Length];

            for (int i = 0; i < path.Length; i++)
            {
                colliderPath[i] = path[i];
            }


            GetComponent<EdgeCollider2D>().points = colliderPath;
        }
        #endregion

        #region Update Polygon Collider 2D
        /*
        Vector3[] vertices = GetVertsPos();
        Vector2[] points = new Vector2[vertices.Length];

        int point = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            if (i >= vertices.Length - 1)
                break;

            points[point] = new Vector2(vertices[i].x, vertices[i].y);
            point++;
        }

        for (int i = vertices.Length - 1; i > 1; i -= 2)
        {
            if (i <= 0)
                break;

            points[point] = new Vector2(vertices[i].x, vertices[i].y);
            point++;
        }

        gameObject.GetComponent<PolygonCollider2D>().SetPath(0, points);
        */
        #endregion

    }

    /// <summary>
    /// Generates new or updates mesh for 3D collider based on terrain path
    /// </summary>
    /// <param name="initalGeneration">Is collider generates initially?</param>
    /// <param name="updateShared">Is shared mesh of collider should be updated too?</param>
    public void UpdateCollider3D(bool updateShared)
    {
        #region Check object & mesh
        if (!Generate3DCollider)
        {
            if (Collider3DObj != null)
                DestroyImmediate(Collider3DObj);

            return;
        }
            
        if (Collider3DObj == null)
        {
            Collider3DObj = new GameObject("Collider 3D");
            Collider3DObj.transform.parent = transform;
            Collider3DObj.transform.position = transform.position;
            Collider3DObj.AddComponent<MeshFilter>();
            Collider3DObj.AddComponent<MeshCollider>();
        }

        Mesh collider3DMesh;
        if (Collider3DObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            collider3DMesh = new Mesh();
            collider3DMesh.name = "Terrain2D_collider3D_mesh_" + InstanceId;
            Collider3DObj.GetComponent<MeshFilter>().mesh = collider3DMesh;
        }
        else
        {
            if (updateShared)
            {
                collider3DMesh = Collider3DObj.GetComponent<MeshFilter>().sharedMesh;
                collider3DMesh.Clear();
            }
            else collider3DMesh = Collider3DObj.GetComponent<MeshFilter>().mesh;
        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        bool initGeneration = collider3DMesh.vertexCount != vertices.Length;

        #region Generate vertices
        
        Vector3[] vertsPoints = new Vector3[vertices.Length];

        

        int point = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            vertsPoints[point] = new Vector3(vertices[i].x, vertices[i].y, -Collider3DWidth * 0.5f);
            vertsPoints[point + 1] = new Vector3(vertices[i].x, vertices[i].y, Collider3DWidth * 0.5f);
            point += 2;
        }

        collider3DMesh.vertices = vertsPoints;
        #endregion

        if (initGeneration)
        {
            #region Generate triangles
            int[] tris = new int[(vertsPoints.Length - 2) * 3];

            bool toSide = false;
            int curTrisIndex = 0;
            for (int i = 0; i < vertsPoints.Length - 2; i++)
            {
                if (toSide)
                {
                    tris[curTrisIndex] = i + 2;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i;
                }
                else
                {
                    tris[curTrisIndex] = i;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i + 2;
                }
                toSide = !toSide;

                curTrisIndex += 3;
            }

            collider3DMesh.triangles = tris;
            #endregion

            collider3DMesh.normals = GetMeshNormals(vertsPoints.Length);
        }

        Collider3DObj.GetComponent<MeshCollider>().enabled = false;
        Collider3DObj.GetComponent<MeshCollider>().enabled = true;

        Collider3DObj.GetComponent<MeshCollider>().sharedMesh = collider3DMesh;
    }

    /// <summary>
    /// Changes terrain geometry in realtime based on new path. Path can be obtained from <see cref="GetPath()"/> method
    /// </summary>
    /// <param name="newPath">Array of new terrain path points</param>
    public void ApplyDeform(Vector3[] newPath)
    {
        if (!RealtimeDeformEnabled)
            return;

        Mesh pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        if (FixSides)
        {
            newPath[0].y = LeftFixedPoint;
            newPath[newPath.Length - 1].y = RightFixedPoint;
        }

        #region Generate vertices
        Vector3[] vertices = GetVertsPos();
        int pathIndex = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            if (newPath[pathIndex].y < 0)
                newPath[pathIndex].y = 0;

            if (newPath[pathIndex].y > Height)
                newPath[pathIndex].y = Height;

            vertices[i] = newPath[pathIndex];
            pathIndex++;
        }
        
        pathMesh.vertices = vertices;
        #endregion 

        if (RealtimeDeformUpdateUv)
        {
            #region Generate UV
            Vector2[] uv = new Vector2[vertices.Length];

            for (int i = 0; i < uv.Length; i += 2)
            {
                uv[i] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i].y / Height) * ((float)Height / Width) * MainTextureSize);
                uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i + 1].y / Height) * ((float)Height / Width) * MainTextureSize);
            }

            pathMesh.uv = uv;
            #endregion
        }

        pathMesh.RecalculateBounds();

        UpdateCap(false);
        if (RealtimeDeformUpdateColliders)
        {
            UpdateCollider2D();
            UpdateCollider3D(false);
        }
    }

    /// <summary>
    /// Updates terrain path based on <see cref="RndAmplitude"/>, <see cref="RndHillsCount"/> and <see cref="RndHeight"/> values
    /// </summary>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void RandomizeTerrain(bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        #region Generate vertices
        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;

        int v = (Width * Resolution) / RndHillsCount;
        int step = 0;

        float rndHeightPoint = GetRandomHeightPoint();

        if (FixSides)
            vertices[0].y = LeftFixedPoint;
        else
        {
            vertices[0].y = rndHeightPoint;
            rndHeightPoint = GetRandomHeightPoint();
        }

        for (int i = 2; i < vertices.Length; i += 2)
        {
            if (step >= v)
            {
                rndHeightPoint = GetRandomHeightPoint();
                step = 0;
            }

            vertices[i].y = Mathf.Lerp(vertices[i - 2].y, rndHeightPoint, ((float)step / v) / Resolution);
            step++;
        }

        if (FixSides)
            RightFixedPoint = vertices[vertices.Length - 2].y;
        #endregion

        pathMesh.vertices = vertices;
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.RecalculateBounds();
        
        UpdateCap(updateShared);
        UpdateCollider2D();
        UpdateCollider3D(updateShared);
    }
    
    /// <summary>
    /// Updates terrain path based on <see cref="RndAmplitude"/>, <see cref="RndHillsCount"/> and <see cref="RndHeight"/> values and given left initial point
    /// </summary>
    /// <param name="leftInitialVertexPos">Vertex point in local space from which generation will start for this terrain mesh. Can be obtained from <see cref="GetLastVertexPos()"/> method</param>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void RandomizeTerrain(Vector3 leftInitialVertexPos, bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        #region Generate vertices
        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;

        int v = (Width * Resolution) / RndHillsCount;
        int step = 0;

        float a = GetRandomHeightPoint();
        vertices[0].y = leftInitialVertexPos.y;

        for (int i = 2; i < vertices.Length; i += 2)
        {
            if (step >= v)
            {
                a = GetRandomHeightPoint();
                step = 0;
            }

            if (!FixSides)
                vertices[i].y = Mathf.Lerp(vertices[i - 2].y, a, ((float)step / v) / Resolution);
            else
            {
                vertices[i].y = Mathf.Lerp(vertices[i - 2].y, a, ((float)step / v) / Resolution);
            }

            step++;
        }
        #endregion

        pathMesh.vertices = vertices;
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.RecalculateBounds();

        UpdateCap(updateShared);
        UpdateCollider2D();
        UpdateCollider3D(updateShared);
    }

    /// <summary>
    /// Sets sorting layer and order for terrain and cap
    /// </summary>
    public void SetSortingLayerAndOrder()
    {
        gameObject.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName;
        gameObject.GetComponent<MeshRenderer>().sortingOrder = OrderInLayer;

        if (CapObj != null)
        {
            CapObj.GetComponent<MeshRenderer>().sortingLayerName = CapSortingLayerName;
            CapObj.GetComponent<MeshRenderer>().sortingOrder = CapOrderInLayer;
        }
    }

    /// <summary>
    /// Returns last (upper right) vertex point position of terrain mesh. This value can be used as initial point in <see cref="RandomizeTerrain()"/> method for left-to-right random generation
    /// </summary>
    /// <returns>Last (upper right) vertex point position in local space</returns>
    public Vector3 GetLastVertexPos()
    {
        int vertsCount = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices.Length;

        return gameObject.GetComponent<MeshFilter>().sharedMesh.vertices[vertsCount - 2];
    }

    /// <summary>
    /// Returns all vertices points of this terrain mesh
    /// </summary>
    /// <returns>Array of all vertices points</returns>
    public Vector3[] GetVertsPos()
    {
        if (gameObject.GetComponent<MeshFilter>().sharedMesh == null)
            return null;
        
        return gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
    }

    /// <summary>
    /// Return path of this terrain
    /// </summary>
    /// <param name="relativeSpace">Relative to local or world space</param>
    /// <returns>Array of path points</returns>
    public Vector3[] GetPath(Space relativeSpace)
    {
        Mesh terrainMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

        if (terrainMesh == null)
            return null;

        Vector3[] path = new Vector3[terrainMesh.vertexCount / 2];
        int vertIndex = 0;
        if (relativeSpace == Space.Self)
        {
            for (int i = 0; i < terrainMesh.vertexCount; i += 2)
            {
                path[vertIndex] = terrainMesh.vertices[i];
                vertIndex++;
            }
        }
        else
        {
            for (int i = 0; i < terrainMesh.vertexCount; i += 2)
            {
                path[vertIndex] = terrainMesh.vertices[i] + transform.position;
                vertIndex++;
            }
        }

        return path;
    }

    /// <summary>
    /// Returns all vertices points of cap mesh
    /// </summary>
    /// <returns>Array of all vertices points</returns>
    public Vector3[] GetVertsPosCap()
    {
        if (!GenerateCap || CapObj == null)
            return null;

        if (CapObj.GetComponent<MeshFilter>() == null)
            return null;

        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
            return null;

        return CapObj.GetComponent<MeshFilter>().sharedMesh.vertices;
    }


    // --- Unity Editor methods

    /// <summary>
    /// Sets mesh ID for this terrain (used only in Unity Editor by custom inspector)
    /// </summary>
    /// <param name="instanceId">Component instance id</param>
    public void SetInstanceId(int instanceId)
    {
        InstanceId = instanceId;
    }

    /// <summary>
    /// Fully updates terrain based on given vertices array (used only in Unity Editor by custom inspector)
    /// </summary>
    /// <param name="newVertsPos">New vertices array</param>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void EditTerrain(Vector3[] newVertsPos, bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = newVertsPos;

        if (FixSides)
        {
            vertices[0].y = LeftFixedPoint;
            vertices[vertices.Length - 2].y = RightFixedPoint;
        }

        #region Generate UV
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i].y / Height) * ((float)Height / Width) * MainTextureSize);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i + 1].y / Height) * ((float)Height / Width) * MainTextureSize);
        }
        #endregion

        pathMesh.vertices = vertices;
        pathMesh.uv = uv;
        pathMesh.SetColors(GetMeshColors(MainColor, vertices.Length).ToList());
        pathMesh.RecalculateBounds();

        SetSortingLayerAndOrder();
        
        UpdateCap(updateShared);
        UpdateCollider3D(updateShared);
    }

    
    // --- private methods

    /// <summary>
    /// Creates new or updates cap
    /// </summary>
    /// <param name="initialGeneration">Is cap generates initially?</param>
    /// <param name="updateShared">Shared mesh of cap should be updated too?</param>
    void UpdateCap(bool updateShared)
    {
        #region Check object & mesh
        if (!GenerateCap)
        {
            if (CapObj != null)
                DestroyImmediate(CapObj);

            return;
        }

        if (CapObj == null)
        {
            CapObj = new GameObject("Cap");
            CapObj.transform.position = transform.position;
            CapObj.transform.parent = transform;
            CapObj.AddComponent<MeshFilter>();
            CapObj.AddComponent<MeshRenderer>();
        }

        if (SmartCap)
        {
            UpdateCapSmart(updateShared);
            return;
        }

        Mesh capMesh;
        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            capMesh = new Mesh();
            capMesh.name = "Terrain2D_cap_mesh_" + InstanceId;
            CapObj.GetComponent<MeshFilter>().mesh = capMesh;
        }
        else
        {
            if (updateShared)
            {
                capMesh = CapObj.GetComponent<MeshFilter>().sharedMesh;
                capMesh.Clear();
            }
            else capMesh = CapObj.GetComponent<MeshFilter>().mesh;
        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        bool initGeneration = vertices.Length != capMesh.vertexCount;

        #region Generate vertices
        

        for (int i = 0; i < vertices.Length; i += 2)
        {
            vertices[i] = new Vector3(vertices[i].x, vertices[i].y, -0.01f);
            vertices[i + 1] = new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight, -0.01f);
            vertices[i].y += CapOffset;
            vertices[i + 1].y += CapOffset;
        }

        capMesh.vertices = vertices;
        #endregion

        if (initGeneration)
        {
            #region Generate triangles
            int[] tris = new int[(vertices.Length - 2) * 3];

            bool toSide = false;
            int curTrisIndex = 0;
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                if (toSide)
                {
                    tris[curTrisIndex] = i;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i + 2;
                }
                else
                {
                    tris[curTrisIndex] = i + 2;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i;
                }
                toSide = !toSide;

                curTrisIndex += 3;
            }

            capMesh.triangles = tris;
            #endregion

            capMesh.normals = GetMeshNormals(vertices.Length);
        }

        #region Generate UV
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * CapTextureTiling, 1);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * CapTextureTiling, 0);
        }

        capMesh.uv = uv;
        #endregion

        capMesh.SetColors(GetMeshColors(CapColor, vertices.Length).ToList());
        capMesh.RecalculateBounds();

        CapObj.GetComponent<Renderer>().material = CapMaterial;
    }

    /// <summary>
    /// Updates smart cap
    /// </summary>
    /// <param name="updateShared">Shared mesh of cap should be updated too?</param>
    void UpdateCapSmart(bool updateShared)
    {
        #region Check mesh
        Mesh capMesh;
        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            capMesh = new Mesh();
            capMesh.name = "Terrain2D_cap_mesh_" + InstanceId;
            CapObj.GetComponent<MeshFilter>().mesh = capMesh;
        }
        else
        {
            if (updateShared)
            {
                capMesh = CapObj.GetComponent<MeshFilter>().sharedMesh;
                capMesh.Clear();
            }
            else capMesh = CapObj.GetComponent<MeshFilter>().mesh;

        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        SmartCapAreas = new List<SmartCapArea>();

        #region Create smart cap areas
        SmartCapArea curSmartCapArea = new SmartCapArea();
        bool nextSmartCapArea = false;
        int counterSign = 1;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            if (i == vertices.Length - 2)
                counterSign = -1;

            if (Mathf.Abs(vertices[i].y - vertices[i + (2 * counterSign)].y) < SmartCapCutHeight)
            {
                if (!nextSmartCapArea)
                {
                    SmartCapAreas.Add(new SmartCapArea());
                    curSmartCapArea = SmartCapAreas[SmartCapAreas.Count - 1];
                    nextSmartCapArea = true;
                }

                curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i].x, vertices[i].y + CapOffset, -0.01f));
                curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight + CapOffset, -0.01f));
            }
            else
            {
                if (nextSmartCapArea)
                {
                    curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i].x, vertices[i].y + CapOffset, -0.01f));
                    curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight + CapOffset, -0.01f));
                }
                nextSmartCapArea = false;
            }
        }

        #endregion

        #region Remove too small areas
        for (int i = SmartCapAreas.Count - 1; i >= 0; i--)
        {
            SmartCapAreas[i].MainSegmentsCount = (SmartCapAreas[i].VertsPoints.Count - 2) / 2;
            if (SmartCapAreas[i].MainSegmentsCount < Resolution)
                SmartCapAreas.RemoveAt(i);
        }
        #endregion

        #region Combine all vertices
        List<Vector3> totalCapVertices = new List<Vector3>();
        Vector3[] sideVertices = new Vector3[4];
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            curSmartCapArea = SmartCapAreas[i];

            //Add side segments vertices
            sideVertices[0] = curSmartCapArea.VertsPoints[0] - new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[1] = curSmartCapArea.VertsPoints[1] - new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[2] = curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 1] + new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[3] = curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 2] + new Vector3(SmartCapSideSegmentsWidth, 0, 0);

            sideVertices[0].y += (sideVertices[0].y - curSmartCapArea.VertsPoints[2].y);
            sideVertices[1].y += (sideVertices[1].y - curSmartCapArea.VertsPoints[3].y);
            sideVertices[2].y += (sideVertices[2].y - curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 3].y);
            sideVertices[3].y += (sideVertices[3].y - curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 4].y);

            curSmartCapArea.VertsPoints.Insert(0, sideVertices[1]);
            curSmartCapArea.VertsPoints.Insert(0, sideVertices[0]);
            curSmartCapArea.VertsPoints.Add(sideVertices[3]);
            curSmartCapArea.VertsPoints.Add(sideVertices[2]);

            for (int j = 0; j < curSmartCapArea.VertsPoints.Count; j += 2)
            {
                totalCapVertices.Add(curSmartCapArea.VertsPoints[j]);
                totalCapVertices.Add(curSmartCapArea.VertsPoints[j + 1]);

                if (j >= 2 && j <= curSmartCapArea.VertsPoints.Count - 3)
                {
                    totalCapVertices.Add(curSmartCapArea.VertsPoints[j]);
                    totalCapVertices.Add(curSmartCapArea.VertsPoints[j + 1]);
                }

            }
        }
        #endregion

        #region Generate UV
        Vector2[] uv = new Vector2[totalCapVertices.Count];
        float uvPerUnit = (SmartCapSegmentsUv.y - SmartCapSegmentsUv.x) / Resolution;
        int uvIndex = 0;
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            curSmartCapArea = SmartCapAreas[i];

            uv[uvIndex] = new Vector2(0, 1);
            uv[uvIndex + 1] = new Vector2(0, 0);
            uv[uvIndex + 2] = new Vector2(SmartCapSegmentsUv.x, 1);
            uv[uvIndex + 3] = new Vector2(SmartCapSegmentsUv.x, 0);

            uvIndex += 4;

            int unitCounter = 0;
            int uvTotalVerts = curSmartCapArea.VertsPoints.Count + (curSmartCapArea.VertsPoints.Count - 14);
            for (int j = 0; j < uvTotalVerts; j += 4)
            {
                uv[uvIndex] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * unitCounter), 1);
                uv[uvIndex + 1] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * unitCounter), 0);
                uv[uvIndex + 2] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * (unitCounter + 1)), 1);
                uv[uvIndex + 3] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * (unitCounter + 1)), 0);

                uvIndex += 4;

                unitCounter++;
                if (unitCounter >= Resolution)
                    unitCounter = 0;
            }

            if (uv[uvIndex - 1].x < SmartCapSegmentsUv.y)
            {
                SmartCapAreas[i].CorruptedTilesVertsPoints.Add(totalCapVertices[uvIndex - 1]);
                SmartCapAreas[i].CorruptedTilesVertsPoints.Add(totalCapVertices[uvIndex - 2]);
            }

            uv[uvIndex] = new Vector2(SmartCapSegmentsUv.y, 1);
            uv[uvIndex + 1] = new Vector2(SmartCapSegmentsUv.y, 0);
            uv[uvIndex + 2] = new Vector2(1, 1);
            uv[uvIndex + 3] = new Vector2(1, 0);

            uvIndex += 4;
        }

        #endregion

        #region Generate triangles

        int unusedVertsCount = 0;
        foreach (var smartCapArea in SmartCapAreas)
        {
            unusedVertsCount += smartCapArea.VertsPoints.Count - 4;
        }

        int[] totalCapTris = new int[((totalCapVertices.Count - unusedVertsCount) - 2) * 3];
        int curTrisIndex = 0;
        int vertCounter = 0;
        bool toSide = false;
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            SmartCapArea curArea = SmartCapAreas[i];

            int skipVertCounter = 0;
            for (int j = 2; j < curArea.VertsPoints.Count + (curArea.VertsPoints.Count - 4); j++)
            {
                if (skipVertCounter > 1)
                {
                    vertCounter += 2;
                    j += 2;
                    skipVertCounter = 0;
                }

                if (toSide)
                {
                    totalCapTris[curTrisIndex] = vertCounter;
                    totalCapTris[curTrisIndex + 1] = vertCounter + 1;
                    totalCapTris[curTrisIndex + 2] = vertCounter + 2;
                }
                else
                {
                    totalCapTris[curTrisIndex] = vertCounter + 2;
                    totalCapTris[curTrisIndex + 1] = vertCounter + 1;
                    totalCapTris[curTrisIndex + 2] = vertCounter;
                }
                toSide = !toSide;

                vertCounter++;
                curTrisIndex += 3;
                skipVertCounter++;
            }

            vertCounter += 2;

            toSide = false;
        }
        #endregion

        capMesh.SetVertices(totalCapVertices);
        capMesh.triangles = totalCapTris;
        capMesh.normals = GetMeshNormals(totalCapVertices.Count);
        capMesh.uv = uv;
        capMesh.SetColors(GetMeshColors(CapColor, totalCapVertices.Count).ToList());
        capMesh.RecalculateBounds();
    }
    
    /// <summary>
    /// Returns height point based on Rnd values
    /// </summary>
    /// <returns>Random height point</returns>
    float GetRandomHeightPoint() 
    {
        float a = RndHeight - RndAmplitude;
        float b = RndHeight + RndAmplitude;

        if (a < 0.1f)
            a = 0.1f;
        if (b > Height)
            b = Height;

        return Random.Range(a, b);
    }

    /// <summary>
    /// Returns normals of terrain mesh
    /// </summary>
    Vector3[] GetMeshNormals(int vertsCount)
    {
        Vector3[] normals = new Vector3[vertsCount];

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = -Vector3.forward;
        }

        return normals;
    }

    /// <summary>
    /// Returns UV coordinates of terrain mesh 
    /// </summary>
    /// <param name="vertices">Array of vertices</param>
    /// <returns>Array of UV coordinates</returns>
    Vector2[] GetMeshUv(Vector3[] vertices)
    {
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i].y / Height) * ((float)Height / Width) * MainTextureSize);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i + 1].y / Height) * ((float)Height / Width) * MainTextureSize);
        }

        return uv;
    }

    /// <summary>
    /// Returns array of colors for mesh
    /// </summary>
    /// <param name="col">General color</param>
    /// <param name="vertsCount">vertices count</param>
    /// <returns></returns>
    Color[] GetMeshColors(Color col, int vertsCount)
    {
        Color[] colors = new Color[vertsCount];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = col;
        }

        return colors;
    }


    // --- static methods
    
    /// <summary>
    /// Instantiates new 2D terrain
    /// </summary>
    /// <param name="position">Position in world space</param>
    /// <returns>2D terrain game object</returns>
    public static GameObject InstantiateTerrain2D(Vector3 position)
    {
        GameObject newTerrain = new GameObject("New Terrain 2D");
        newTerrain.transform.position = position;
        newTerrain.AddComponent<TerrainEditor2D>();

        newTerrain.GetComponent<TerrainEditor2D>().Create();

        return newTerrain;
    }
    
    /// <summary>
    /// Instantiates new 2D terrain
    /// </summary>
    /// <param name="position">Position in world space</param>
    /// <param name="width">Width in units</param>
    /// <param name="height">Height in units</param>
    /// <param name="resolution">Mesh esolution</param>
    /// <returns>2D terrain game object</returns>
    public static GameObject InstantiateTerrain2D(Vector3 position, int width, int height, int resolution)
    {
        GameObject newTerrain = new GameObject("New Terrain 2D");
        newTerrain.transform.position = position;
        newTerrain.AddComponent<TerrainEditor2D>();

        newTerrain.GetComponent<TerrainEditor2D>().Width = width;
        newTerrain.GetComponent<TerrainEditor2D>().Height = height;
        newTerrain.GetComponent<TerrainEditor2D>().Resolution = resolution;

        newTerrain.GetComponent<TerrainEditor2D>().Create();

        return newTerrain;
    }


    // --- internal classes

    [Serializable]
    public class SmartCapArea
    {
        public List<Vector3> VertsPoints = new List<Vector3>();
        public int MainSegmentsCount = 0;
        public List<Vector3> CorruptedTilesVertsPoints = new List<Vector3>(); 

    }
}


























/*
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
}










/*
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class TerrainEditor2D : MonoBehaviour
{
    // --- Variables

    #region Main values
    public int InstanceId { get; private set; }

    [Tooltip("2D terrain width in units")]
    [Min(2)] public int Width = 50;

    [Tooltip("2D terrain height in units")]
    [Min(1)] public int Height = 50;

    [Tooltip("2D terrain segments per unit. Higher = smoother terrain, more vertices")]
    [Range(1, 10)] public int Resolution = 1;
    #endregion

    #region 2D Terrain settings values
    [Tooltip("Main material of this 2D terrain with tiled texture")]
    public Material MainMaterial;

    [Tooltip("Number of main texture tiles for whole 2D terrain")]
    [Min(1)] public int MainTextureSize = 15;

    [Tooltip("Color of 2D terrain mesh. Works only if shader supports vertex colors")]
    public Color MainColor = Color.white;

    public int SortingLayerId;
    public string SortingLayerName = "Default";
    public int OrderInLayer;
    #endregion

    #region Random values
    [Tooltip("Average height of randomly generated terrain (0 - Height)")]
    public float RndHeight = 25;

    [Tooltip("Max hills number of randomly generated terrain")]
    [Min(1)] public int RndHillsCount = 5;

    [Tooltip("How far hills can go above / below the average height (0 - Height/2)")]
    public float RndAmplitude = 5;

    [Tooltip("Use a fixed seed so Randomize produces the same terrain every time")]
    public bool UseRandomSeed;

    [Tooltip("Seed used when 'Use Random Seed' is enabled")]
    public int RandomSeed = 12345;
    #endregion

    #region Cap settings values
    [Tooltip("Generate a cap (grass / snow strip) on top of the terrain")]
    public bool GenerateCap;

    [Tooltip("Cap material of this 2D terrain with tiled texture")]
    public Material CapMaterial;

    [Tooltip("Number of cap texture tiles for whole terrain (ignored when Smart Cap is enabled)")]
    [Min(1)] public int CapTextureTiling = 15;

    [Tooltip("Color of cap mesh. Works only if shader supports vertex colors")]
    public Color CapColor = Color.white;

    public string CapSortingLayerName = "Default";
    public int CapSortingLayerId;
    public int CapOrderInLayer = 1;

    [Tooltip("Cap height in units")]
    [Min(0)] public float CapHeight = 1;

    [Tooltip("Cap offset relative to terrain top edge")]
    public float CapOffset;

    [Tooltip("Split the cap into separate pieces on steep slopes, with textured end segments")]
    public bool SmartCap = true;

    [Tooltip("Min height difference in units between neighbour points that splits the cap")]
    [Min(0.01f)] public float SmartCapCutHeight = 1;

    [Tooltip("Width of the cap end segments in units")]
    [Min(0)] public float SmartCapSideSegmentsWidth = 1;

    [Tooltip("Horizontal UV separators that divide the cap texture into left end / middle / right end")]
    public Vector2 SmartCapSegmentsUv = new Vector2(0.125f, 0.875f);
    #endregion

    #region Deform settings values
    [Tooltip("Can this terrain be deformed at runtime with ApplyDeform()?")]
    public bool RealtimeDeformEnabled = false;

    [Tooltip("Update UVs on every deformation")]
    public bool RealtimeDeformUpdateUv = true;

    [Tooltip("Update colliders on every deformation")]
    public bool RealtimeDeformUpdateColliders = true;
    #endregion

    #region Additional settings values
    [Tooltip("Lock the left and right end heights of the terrain")]
    public bool FixSides;

    [Tooltip("Locked height of the left end (0 - Height)")]
    public float LeftFixedPoint = 25;

    [Tooltip("Locked height of the right end (0 - Height). Updated after random generation")]
    public float RightFixedPoint = 25;

    [Tooltip("Generate a 3D MeshCollider along the terrain surface")]
    public bool Generate3DCollider;

    [Tooltip("Depth (Z) of the 3D collider")]
    [Min(0.01f)] public float Collider3DWidth = 5;
    #endregion

    #region References
    public GameObject CapObj;
    public GameObject Collider3DObj;
    public List<SmartCapArea> SmartCapAreas;
    #endregion

    // --- Unity callbacks

    /// <summary>
    /// Keeps inspector values inside valid ranges
    /// </summary>
    void OnValidate()
    {
        Width = Mathf.Max(2, Width);
        Height = Mathf.Max(1, Height);
        Resolution = Mathf.Clamp(Resolution, 1, 10);

        RndHeight = Mathf.Clamp(RndHeight, 0, Height);
        RndAmplitude = Mathf.Clamp(RndAmplitude, 0, Height * 0.5f);
        RndHillsCount = Mathf.Clamp(RndHillsCount, 1, Mathf.Max(1, (Width * Resolution) / 2));

        LeftFixedPoint = Mathf.Clamp(LeftFixedPoint, 0, Height);
        RightFixedPoint = Mathf.Clamp(RightFixedPoint, 0, Height);

        SmartCapSegmentsUv.x = Mathf.Clamp01(SmartCapSegmentsUv.x);
        SmartCapSegmentsUv.y = Mathf.Clamp(SmartCapSegmentsUv.y, SmartCapSegmentsUv.x, 1f);
    }

    // --- public methods

    /// <summary>
    /// Generates new flat terrain based on main values
    /// </summary>
    public void Create()
    {
        Mesh pathMesh;
        if (gameObject.GetComponent<MeshFilter>().sharedMesh == null)
        {
            pathMesh = new Mesh();
            pathMesh.name = "Terrain2D_mesh_" + InstanceId;
            gameObject.GetComponent<MeshFilter>().sharedMesh = pathMesh;
        }
        else
        {
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            pathMesh.Clear();
        }

        #region Generate vertices
        Vector3[] vertices = new Vector3[((Width * 2) * Resolution) + 2];

        for (int i = 0; i < vertices.Length; i += 2)
        {
            float vertsInterval = (i * 0.5f) / Resolution;

            vertices[i] = new Vector3(vertsInterval, Height * 0.5f, 0);
            vertices[i + 1] = new Vector3(vertsInterval, 0, 0);
        }

        if (FixSides)
        {
            vertices[0].y = LeftFixedPoint;
            vertices[vertices.Length - 2].y = RightFixedPoint;
        }
        #endregion

        #region Generate triangles
        int[] tris = new int[(vertices.Length - 2) * 3];

        bool toSide = false;
        int curTrisIndex = 0;
        for (int i = 0; i < vertices.Length - 2; i++)
        {
            if (toSide)
            {
                tris[curTrisIndex] = i;
                tris[curTrisIndex + 1] = i + 1;
                tris[curTrisIndex + 2] = i + 2;
            }
            else
            {
                tris[curTrisIndex] = i + 2;
                tris[curTrisIndex + 1] = i + 1;
                tris[curTrisIndex + 2] = i;
            }
            toSide = !toSide;

            curTrisIndex += 3;
        }
        #endregion

        pathMesh.vertices = vertices;
        pathMesh.triangles = tris;
        pathMesh.normals = GetMeshNormals(vertices.Length);
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.SetColors(GetMeshColors(MainColor, vertices.Length).ToList());
        pathMesh.RecalculateBounds();

        ApplyMaterials();
        SetSortingLayerAndOrder();

        UpdateCollider2D();
        UpdateCap(true);
        UpdateCollider3D(true);
    }

    /// <summary>
    /// Assigns main and cap materials to their renderers
    /// </summary>
    public void ApplyMaterials()
    {
        GetComponent<MeshRenderer>().sharedMaterial = MainMaterial;

        if (CapObj != null && CapObj.GetComponent<MeshRenderer>() != null)
            CapObj.GetComponent<MeshRenderer>().sharedMaterial = CapMaterial;
    }

    /// <summary>
    /// Generates new or updates 2D collider based on terrain path
    /// </summary>
    public void UpdateCollider2D()
    {
        EdgeCollider2D edge = GetComponent<EdgeCollider2D>();
        if (edge == null)
            return;

        Vector3[] path = GetPath(Space.Self);
        if (path == null)
            return;

        Vector2[] colliderPath = new Vector2[path.Length];
        for (int i = 0; i < path.Length; i++)
            colliderPath[i] = path[i];

        edge.points = colliderPath;
    }

    /// <summary>
    /// Generates new or updates mesh for 3D collider based on terrain path
    /// </summary>
    /// <param name="updateShared">Is shared mesh of collider should be updated too?</param>
    public void UpdateCollider3D(bool updateShared)
    {
        #region Check object & mesh
        if (!Generate3DCollider)
        {
            if (Collider3DObj != null)
                DestroyImmediate(Collider3DObj);

            return;
        }

        if (Collider3DObj == null)
        {
            Collider3DObj = new GameObject("Collider 3D");
            Collider3DObj.transform.parent = transform;
            Collider3DObj.transform.localPosition = Vector3.zero;
            Collider3DObj.transform.localRotation = Quaternion.identity;
            Collider3DObj.transform.localScale = Vector3.one;
            Collider3DObj.AddComponent<MeshFilter>();
            Collider3DObj.AddComponent<MeshCollider>();
        }

        Mesh collider3DMesh;
        if (Collider3DObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            collider3DMesh = new Mesh();
            collider3DMesh.name = "Terrain2D_collider3D_mesh_" + InstanceId;
            Collider3DObj.GetComponent<MeshFilter>().sharedMesh = collider3DMesh;
        }
        else
        {
            if (updateShared)
            {
                collider3DMesh = Collider3DObj.GetComponent<MeshFilter>().sharedMesh;
                collider3DMesh.Clear();
            }
            else collider3DMesh = Collider3DObj.GetComponent<MeshFilter>().mesh;
        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        bool initGeneration = collider3DMesh.vertexCount != vertices.Length;

        #region Generate vertices
        Vector3[] vertsPoints = new Vector3[vertices.Length];

        int point = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            vertsPoints[point] = new Vector3(vertices[i].x, vertices[i].y, -Collider3DWidth * 0.5f);
            vertsPoints[point + 1] = new Vector3(vertices[i].x, vertices[i].y, Collider3DWidth * 0.5f);
            point += 2;
        }

        collider3DMesh.vertices = vertsPoints;
        #endregion

        if (initGeneration)
        {
            #region Generate triangles
            int[] tris = new int[(vertsPoints.Length - 2) * 3];

            bool toSide = false;
            int curTrisIndex = 0;
            for (int i = 0; i < vertsPoints.Length - 2; i++)
            {
                if (toSide)
                {
                    tris[curTrisIndex] = i + 2;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i;
                }
                else
                {
                    tris[curTrisIndex] = i;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i + 2;
                }
                toSide = !toSide;

                curTrisIndex += 3;
            }

            collider3DMesh.triangles = tris;
            #endregion

            collider3DMesh.normals = GetMeshNormals(vertsPoints.Length);
        }

        collider3DMesh.RecalculateBounds();

        MeshCollider meshCollider = Collider3DObj.GetComponent<MeshCollider>();
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = collider3DMesh;
    }

    /// <summary>
    /// Changes terrain geometry in realtime based on new path. Path can be obtained from <see cref="GetPath"/> method
    /// </summary>
    /// <param name="newPath">Array of new terrain path points</param>
    public void ApplyDeform(Vector3[] newPath)
    {
        if (!RealtimeDeformEnabled)
            return;

        Mesh pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        if (FixSides)
        {
            newPath[0].y = LeftFixedPoint;
            newPath[newPath.Length - 1].y = RightFixedPoint;
        }

        #region Generate vertices
        Vector3[] vertices = GetVertsPos();
        int pathIndex = 0;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            newPath[pathIndex].y = Mathf.Clamp(newPath[pathIndex].y, 0, Height);
            vertices[i] = newPath[pathIndex];
            pathIndex++;
        }

        pathMesh.vertices = vertices;
        #endregion

        if (RealtimeDeformUpdateUv)
            pathMesh.uv = GetMeshUv(vertices);

        pathMesh.RecalculateBounds();

        UpdateCap(false);
        if (RealtimeDeformUpdateColliders)
        {
            UpdateCollider2D();
            UpdateCollider3D(false);
        }
    }

    /// <summary>
    /// Updates terrain path based on <see cref="RndAmplitude"/>, <see cref="RndHillsCount"/> and <see cref="RndHeight"/> values
    /// </summary>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void RandomizeTerrain(bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        Random.State prevState = Random.state;
        if (UseRandomSeed)
            Random.InitState(RandomSeed);

        #region Generate vertices
        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;

        int v = Mathf.Max(1, (Width * Resolution) / Mathf.Max(1, RndHillsCount));
        int step = 0;

        float rndHeightPoint = GetRandomHeightPoint();

        if (FixSides)
            vertices[0].y = LeftFixedPoint;
        else
        {
            vertices[0].y = rndHeightPoint;
            rndHeightPoint = GetRandomHeightPoint();
        }

        for (int i = 2; i < vertices.Length; i += 2)
        {
            if (step >= v)
            {
                rndHeightPoint = GetRandomHeightPoint();
                step = 0;
            }

            vertices[i].y = Mathf.Lerp(vertices[i - 2].y, rndHeightPoint, ((float)step / v) / Resolution);
            step++;
        }

        if (FixSides)
            RightFixedPoint = vertices[vertices.Length - 2].y;
        #endregion

        if (UseRandomSeed)
            Random.state = prevState;

        pathMesh.vertices = vertices;
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.RecalculateBounds();

        UpdateCap(updateShared);
        UpdateCollider2D();
        UpdateCollider3D(updateShared);
    }

    /// <summary>
    /// Updates terrain path based on <see cref="RndAmplitude"/>, <see cref="RndHillsCount"/> and <see cref="RndHeight"/> values and given left initial point
    /// </summary>
    /// <param name="leftInitialVertexPos">Vertex point in local space from which generation will start for this terrain mesh. Can be obtained from <see cref="GetLastVertexPos()"/> method</param>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void RandomizeTerrain(Vector3 leftInitialVertexPos, bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        Random.State prevState = Random.state;
        if (UseRandomSeed)
            Random.InitState(RandomSeed);

        #region Generate vertices
        Vector3[] vertices = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;

        int v = Mathf.Max(1, (Width * Resolution) / Mathf.Max(1, RndHillsCount));
        int step = 0;

        float a = GetRandomHeightPoint();
        vertices[0].y = leftInitialVertexPos.y;

        for (int i = 2; i < vertices.Length; i += 2)
        {
            if (step >= v)
            {
                a = GetRandomHeightPoint();
                step = 0;
            }

            vertices[i].y = Mathf.Lerp(vertices[i - 2].y, a, ((float)step / v) / Resolution);
            step++;
        }
        #endregion

        if (UseRandomSeed)
            Random.state = prevState;

        pathMesh.vertices = vertices;
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.RecalculateBounds();

        UpdateCap(updateShared);
        UpdateCollider2D();
        UpdateCollider3D(updateShared);
    }

    /// <summary>
    /// Sets sorting layer and order for terrain and cap
    /// </summary>
    public void SetSortingLayerAndOrder()
    {
        gameObject.GetComponent<MeshRenderer>().sortingLayerName = SortingLayerName;
        gameObject.GetComponent<MeshRenderer>().sortingOrder = OrderInLayer;

        if (CapObj != null)
        {
            CapObj.GetComponent<MeshRenderer>().sortingLayerName = CapSortingLayerName;
            CapObj.GetComponent<MeshRenderer>().sortingOrder = CapOrderInLayer;
        }
    }

    /// <summary>
    /// Returns last (upper right) vertex point position of terrain mesh. This value can be used as initial point in <see cref="RandomizeTerrain(Vector3, bool)"/> method for left-to-right random generation
    /// </summary>
    /// <returns>Last (upper right) vertex point position in local space</returns>
    public Vector3 GetLastVertexPos()
    {
        Vector3[] verts = gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
        return verts[verts.Length - 2];
    }

    /// <summary>
    /// Returns all vertices points of this terrain mesh
    /// </summary>
    /// <returns>Array of all vertices points</returns>
    public Vector3[] GetVertsPos()
    {
        if (gameObject.GetComponent<MeshFilter>().sharedMesh == null)
            return null;

        return gameObject.GetComponent<MeshFilter>().sharedMesh.vertices;
    }

    /// <summary>
    /// Return path of this terrain
    /// </summary>
    /// <param name="relativeSpace">Relative to local or world space</param>
    /// <returns>Array of path points</returns>
    public Vector3[] GetPath(Space relativeSpace)
    {
        Mesh terrainMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;

        if (terrainMesh == null)
            return null;

        Vector3[] verts = terrainMesh.vertices; // cached: mesh.vertices allocates a copy on every call
        Vector3[] path = new Vector3[verts.Length / 2];
        int vertIndex = 0;

        for (int i = 0; i < verts.Length; i += 2)
        {
            path[vertIndex] = relativeSpace == Space.Self ? verts[i] : verts[i] + transform.position;
            vertIndex++;
        }

        return path;
    }

    /// <summary>
    /// Returns all vertices points of cap mesh
    /// </summary>
    /// <returns>Array of all vertices points</returns>
    public Vector3[] GetVertsPosCap()
    {
        if (!GenerateCap || CapObj == null)
            return null;

        if (CapObj.GetComponent<MeshFilter>() == null)
            return null;

        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
            return null;

        return CapObj.GetComponent<MeshFilter>().sharedMesh.vertices;
    }


    // --- Unity Editor methods

    /// <summary>
    /// Sets mesh ID for this terrain (used only in Unity Editor by custom inspector)
    /// </summary>
    /// <param name="instanceId">Component instance id</param>
    public void SetInstanceId(int instanceId)
    {
        InstanceId = instanceId;
    }

    /// <summary>
    /// Fully updates terrain based on given vertices array (used by the custom inspector)
    /// </summary>
    /// <param name="newVertsPos">New vertices array</param>
    /// <param name="updateShared">Shared mesh of terrain should be updated too?</param>
    public void EditTerrain(Vector3[] newVertsPos, bool updateShared)
    {
        Mesh pathMesh;
        if (updateShared)
            pathMesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        else pathMesh = gameObject.GetComponent<MeshFilter>().mesh;

        Vector3[] vertices = newVertsPos;

        if (FixSides)
        {
            vertices[0].y = LeftFixedPoint;
            vertices[vertices.Length - 2].y = RightFixedPoint;
        }

        pathMesh.vertices = vertices;
        pathMesh.uv = GetMeshUv(vertices);
        pathMesh.SetColors(GetMeshColors(MainColor, vertices.Length).ToList());
        pathMesh.RecalculateBounds();

        UpdateCap(updateShared);
        ApplyMaterials();
        SetSortingLayerAndOrder();

        UpdateCollider2D();
        UpdateCollider3D(updateShared);
    }


    // --- private methods

    /// <summary>
    /// Creates new or updates cap
    /// </summary>
    /// <param name="updateShared">Shared mesh of cap should be updated too?</param>
    void UpdateCap(bool updateShared)
    {
        #region Check object & mesh
        if (!GenerateCap)
        {
            if (CapObj != null)
                DestroyImmediate(CapObj);

            return;
        }

        if (CapObj == null)
        {
            CapObj = new GameObject("Cap");
            CapObj.transform.parent = transform;
            CapObj.transform.localPosition = Vector3.zero;
            CapObj.transform.localRotation = Quaternion.identity;
            CapObj.transform.localScale = Vector3.one;
            CapObj.AddComponent<MeshFilter>();
            CapObj.AddComponent<MeshRenderer>();
        }

        CapObj.GetComponent<MeshRenderer>().sharedMaterial = CapMaterial;

        if (SmartCap)
        {
            UpdateCapSmart(updateShared);
            return;
        }

        Mesh capMesh;
        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            capMesh = new Mesh();
            capMesh.name = "Terrain2D_cap_mesh_" + InstanceId;
            CapObj.GetComponent<MeshFilter>().sharedMesh = capMesh;
        }
        else
        {
            if (updateShared)
            {
                capMesh = CapObj.GetComponent<MeshFilter>().sharedMesh;
                capMesh.Clear();
            }
            else capMesh = CapObj.GetComponent<MeshFilter>().mesh;
        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        bool initGeneration = vertices.Length != capMesh.vertexCount;

        #region Generate vertices
        for (int i = 0; i < vertices.Length; i += 2)
        {
            vertices[i] = new Vector3(vertices[i].x, vertices[i].y, -0.01f);
            vertices[i + 1] = new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight, -0.01f);
            vertices[i].y += CapOffset;
            vertices[i + 1].y += CapOffset;
        }

        capMesh.vertices = vertices;
        #endregion

        if (initGeneration)
        {
            #region Generate triangles
            int[] tris = new int[(vertices.Length - 2) * 3];

            bool toSide = false;
            int curTrisIndex = 0;
            for (int i = 0; i < vertices.Length - 2; i++)
            {
                if (toSide)
                {
                    tris[curTrisIndex] = i;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i + 2;
                }
                else
                {
                    tris[curTrisIndex] = i + 2;
                    tris[curTrisIndex + 1] = i + 1;
                    tris[curTrisIndex + 2] = i;
                }
                toSide = !toSide;

                curTrisIndex += 3;
            }

            capMesh.triangles = tris;
            #endregion

            capMesh.normals = GetMeshNormals(vertices.Length);
        }

        #region Generate UV
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * CapTextureTiling, 1);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * CapTextureTiling, 0);
        }

        capMesh.uv = uv;
        #endregion

        capMesh.SetColors(GetMeshColors(CapColor, vertices.Length).ToList());
        capMesh.RecalculateBounds();
    }

    /// <summary>
    /// Updates smart cap
    /// </summary>
    /// <param name="updateShared">Shared mesh of cap should be updated too?</param>
    void UpdateCapSmart(bool updateShared)
    {
        #region Check mesh
        Mesh capMesh;
        if (CapObj.GetComponent<MeshFilter>().sharedMesh == null)
        {
            capMesh = new Mesh();
            capMesh.name = "Terrain2D_cap_mesh_" + InstanceId;
            CapObj.GetComponent<MeshFilter>().sharedMesh = capMesh;
        }
        else
        {
            if (updateShared)
            {
                capMesh = CapObj.GetComponent<MeshFilter>().sharedMesh;
                capMesh.Clear();
            }
            else capMesh = CapObj.GetComponent<MeshFilter>().mesh;
        }
        #endregion

        Vector3[] vertices = GetVertsPos();
        SmartCapAreas = new List<SmartCapArea>();

        #region Create smart cap areas
        SmartCapArea curSmartCapArea = new SmartCapArea();
        bool nextSmartCapArea = false;
        int counterSign = 1;
        for (int i = 0; i < vertices.Length; i += 2)
        {
            if (i == vertices.Length - 2)
                counterSign = -1;

            if (Mathf.Abs(vertices[i].y - vertices[i + (2 * counterSign)].y) < SmartCapCutHeight)
            {
                if (!nextSmartCapArea)
                {
                    SmartCapAreas.Add(new SmartCapArea());
                    curSmartCapArea = SmartCapAreas[SmartCapAreas.Count - 1];
                    nextSmartCapArea = true;
                }

                curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i].x, vertices[i].y + CapOffset, -0.01f));
                curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight + CapOffset, -0.01f));
            }
            else
            {
                if (nextSmartCapArea)
                {
                    curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i].x, vertices[i].y + CapOffset, -0.01f));
                    curSmartCapArea.VertsPoints.Add(new Vector3(vertices[i + 1].x, vertices[i].y - CapHeight + CapOffset, -0.01f));
                }
                nextSmartCapArea = false;
            }
        }
        #endregion

        #region Remove too small areas
        for (int i = SmartCapAreas.Count - 1; i >= 0; i--)
        {
            SmartCapAreas[i].MainSegmentsCount = (SmartCapAreas[i].VertsPoints.Count - 2) / 2;
            if (SmartCapAreas[i].MainSegmentsCount < Resolution)
                SmartCapAreas.RemoveAt(i);
        }
        #endregion

        // Terrain too steep everywhere: nothing to cap (avoids a negative array size below)
        if (SmartCapAreas.Count == 0)
        {
            capMesh.Clear();
            return;
        }

        #region Combine all vertices
        List<Vector3> totalCapVertices = new List<Vector3>();
        Vector3[] sideVertices = new Vector3[4];
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            curSmartCapArea = SmartCapAreas[i];

            //Add side segments vertices
            sideVertices[0] = curSmartCapArea.VertsPoints[0] - new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[1] = curSmartCapArea.VertsPoints[1] - new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[2] = curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 1] + new Vector3(SmartCapSideSegmentsWidth, 0, 0);
            sideVertices[3] = curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 2] + new Vector3(SmartCapSideSegmentsWidth, 0, 0);

            sideVertices[0].y += (sideVertices[0].y - curSmartCapArea.VertsPoints[2].y);
            sideVertices[1].y += (sideVertices[1].y - curSmartCapArea.VertsPoints[3].y);
            sideVertices[2].y += (sideVertices[2].y - curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 3].y);
            sideVertices[3].y += (sideVertices[3].y - curSmartCapArea.VertsPoints[curSmartCapArea.VertsPoints.Count - 4].y);

            curSmartCapArea.VertsPoints.Insert(0, sideVertices[1]);
            curSmartCapArea.VertsPoints.Insert(0, sideVertices[0]);
            curSmartCapArea.VertsPoints.Add(sideVertices[3]);
            curSmartCapArea.VertsPoints.Add(sideVertices[2]);

            for (int j = 0; j < curSmartCapArea.VertsPoints.Count; j += 2)
            {
                totalCapVertices.Add(curSmartCapArea.VertsPoints[j]);
                totalCapVertices.Add(curSmartCapArea.VertsPoints[j + 1]);

                if (j >= 2 && j <= curSmartCapArea.VertsPoints.Count - 3)
                {
                    totalCapVertices.Add(curSmartCapArea.VertsPoints[j]);
                    totalCapVertices.Add(curSmartCapArea.VertsPoints[j + 1]);
                }
            }
        }
        #endregion

        #region Generate UV
        Vector2[] uv = new Vector2[totalCapVertices.Count];
        float uvPerUnit = (SmartCapSegmentsUv.y - SmartCapSegmentsUv.x) / Resolution;
        int uvIndex = 0;
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            curSmartCapArea = SmartCapAreas[i];

            uv[uvIndex] = new Vector2(0, 1);
            uv[uvIndex + 1] = new Vector2(0, 0);
            uv[uvIndex + 2] = new Vector2(SmartCapSegmentsUv.x, 1);
            uv[uvIndex + 3] = new Vector2(SmartCapSegmentsUv.x, 0);

            uvIndex += 4;

            int unitCounter = 0;
            int uvTotalVerts = curSmartCapArea.VertsPoints.Count + (curSmartCapArea.VertsPoints.Count - 14);
            for (int j = 0; j < uvTotalVerts; j += 4)
            {
                uv[uvIndex] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * unitCounter), 1);
                uv[uvIndex + 1] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * unitCounter), 0);
                uv[uvIndex + 2] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * (unitCounter + 1)), 1);
                uv[uvIndex + 3] = new Vector2(SmartCapSegmentsUv.x + (uvPerUnit * (unitCounter + 1)), 0);

                uvIndex += 4;

                unitCounter++;
                if (unitCounter >= Resolution)
                    unitCounter = 0;
            }

            if (uv[uvIndex - 1].x < SmartCapSegmentsUv.y)
            {
                SmartCapAreas[i].CorruptedTilesVertsPoints.Add(totalCapVertices[uvIndex - 1]);
                SmartCapAreas[i].CorruptedTilesVertsPoints.Add(totalCapVertices[uvIndex - 2]);
            }

            uv[uvIndex] = new Vector2(SmartCapSegmentsUv.y, 1);
            uv[uvIndex + 1] = new Vector2(SmartCapSegmentsUv.y, 0);
            uv[uvIndex + 2] = new Vector2(1, 1);
            uv[uvIndex + 3] = new Vector2(1, 0);

            uvIndex += 4;
        }
        #endregion

        #region Generate triangles
        int unusedVertsCount = 0;
        foreach (var smartCapArea in SmartCapAreas)
            unusedVertsCount += smartCapArea.VertsPoints.Count - 4;

        int[] totalCapTris = new int[((totalCapVertices.Count - unusedVertsCount) - 2) * 3];
        int curTrisIndex = 0;
        int vertCounter = 0;
        bool toSide = false;
        for (int i = 0; i < SmartCapAreas.Count; i++)
        {
            SmartCapArea curArea = SmartCapAreas[i];

            int skipVertCounter = 0;
            for (int j = 2; j < curArea.VertsPoints.Count + (curArea.VertsPoints.Count - 4); j++)
            {
                if (skipVertCounter > 1)
                {
                    vertCounter += 2;
                    j += 2;
                    skipVertCounter = 0;
                }

                if (toSide)
                {
                    totalCapTris[curTrisIndex] = vertCounter;
                    totalCapTris[curTrisIndex + 1] = vertCounter + 1;
                    totalCapTris[curTrisIndex + 2] = vertCounter + 2;
                }
                else
                {
                    totalCapTris[curTrisIndex] = vertCounter + 2;
                    totalCapTris[curTrisIndex + 1] = vertCounter + 1;
                    totalCapTris[curTrisIndex + 2] = vertCounter;
                }
                toSide = !toSide;

                vertCounter++;
                curTrisIndex += 3;
                skipVertCounter++;
            }

            vertCounter += 2;

            toSide = false;
        }
        #endregion

        capMesh.SetVertices(totalCapVertices);
        capMesh.triangles = totalCapTris;
        capMesh.normals = GetMeshNormals(totalCapVertices.Count);
        capMesh.uv = uv;
        capMesh.SetColors(GetMeshColors(CapColor, totalCapVertices.Count).ToList());
        capMesh.RecalculateBounds();
    }

    /// <summary>
    /// Returns height point based on Rnd values
    /// </summary>
    /// <returns>Random height point</returns>
    float GetRandomHeightPoint()
    {
        float a = RndHeight - RndAmplitude;
        float b = RndHeight + RndAmplitude;

        if (a < 0.1f)
            a = 0.1f;
        if (b > Height)
            b = Height;

        return Random.Range(a, b);
    }

    /// <summary>
    /// Returns normals of terrain mesh
    /// </summary>
    Vector3[] GetMeshNormals(int vertsCount)
    {
        Vector3[] normals = new Vector3[vertsCount];

        for (int i = 0; i < normals.Length; i++)
            normals[i] = -Vector3.forward;

        return normals;
    }

    /// <summary>
    /// Returns UV coordinates of terrain mesh
    /// </summary>
    /// <param name="vertices">Array of vertices</param>
    /// <returns>Array of UV coordinates</returns>
    Vector2[] GetMeshUv(Vector3[] vertices)
    {
        Vector2[] uv = new Vector2[vertices.Length];

        for (int i = 0; i < uv.Length; i += 2)
        {
            uv[i] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i].y / Height) * ((float)Height / Width) * MainTextureSize);
            uv[i + 1] = new Vector2((float)i / (uv.Length - 2) * MainTextureSize, (vertices[i + 1].y / Height) * ((float)Height / Width) * MainTextureSize);
        }

        return uv;
    }

    /// <summary>
    /// Returns array of colors for mesh
    /// </summary>
    /// <param name="col">General color</param>
    /// <param name="vertsCount">vertices count</param>
    Color[] GetMeshColors(Color col, int vertsCount)
    {
        Color[] colors = new Color[vertsCount];
        for (int i = 0; i < colors.Length; i++)
            colors[i] = col;

        return colors;
    }


    // --- static methods

    /// <summary>
    /// Instantiates new 2D terrain
    /// </summary>
    /// <param name="position">Position in world space</param>
    /// <returns>2D terrain game object</returns>
    public static GameObject InstantiateTerrain2D(Vector3 position)
    {
        GameObject newTerrain = new GameObject("New Terrain 2D");
        newTerrain.transform.position = position;
        newTerrain.AddComponent<TerrainEditor2D>();

        newTerrain.GetComponent<TerrainEditor2D>().Create();

        return newTerrain;
    }

    /// <summary>
    /// Instantiates new 2D terrain
    /// </summary>
    /// <param name="position">Position in world space</param>
    /// <param name="width">Width in units</param>
    /// <param name="height">Height in units</param>
    /// <param name="resolution">Mesh resolution</param>
    /// <returns>2D terrain game object</returns>
    public static GameObject InstantiateTerrain2D(Vector3 position, int width, int height, int resolution)
    {
        GameObject newTerrain = new GameObject("New Terrain 2D");
        newTerrain.transform.position = position;
        TerrainEditor2D terrain = newTerrain.AddComponent<TerrainEditor2D>();

        terrain.Width = width;
        terrain.Height = height;
        terrain.Resolution = resolution;

        terrain.Create();

        return newTerrain;
    }


    // --- internal classes

    [Serializable]
    public class SmartCapArea
    {
        public List<Vector3> VertsPoints = new List<Vector3>();
        public int MainSegmentsCount = 0;
        public List<Vector3> CorruptedTilesVertsPoints = new List<Vector3>();
    }
}

// =====================================================================
// Custom Inspector (editor only - stripped from builds automatically)
// =====================================================================
#if UNITY_EDITOR
/// <summary>
/// Custom inspector for TerrainEditor2D.
/// Every setting updates the terrain live, and the path can be sculpted directly in the Scene view.
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
        var objs = new List<UnityEngine.Object> { t, t.GetComponent<EdgeCollider2D>() };

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
}
#endif*/