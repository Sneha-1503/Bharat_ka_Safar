using UnityEngine;
using System.Collections.Generic;
using System;
public class GS2DRope : MonoBehaviour {
    [HideInInspector]
    public SpriteRenderer[] SegmentsPrefabs;
    [HideInInspector]
    public SegmentSelectionMode SegmentsMode;
    [HideInInspector]
    public LineOverflowMode OverflowMode;
    [HideInInspector]
    public int bendLimit = 45;
    [HideInInspector]
    public bool HangFirstSegment = false, HangLastSegment = false,
        useBendLimit = true, WithPhysics = true, BreakableJoints = false,
        createSeemlessRope, springy;
    [HideInInspector]
    public Vector2 FirstSegmentConnectionAnchor, LastSegmentConnectionAnchor;
    [HideInInspector]
    public float BreakForce = 100, overlapFactor, ropeWidth = 0.25f;
    [HideInInspector]
    public List<Vector3> nodes = new List<Vector3>(new Vector3[] {new Vector3(-3,0,0),new Vector3(3,0,0) });
    [HideInInspector]
    public Material ropeMaterial;
}
public enum SegmentSelectionMode
{
    RoundRobin = 0,
    Random = 1
}
public enum LineOverflowMode
{
    Round = 0,
    Shrink = 1,
    Extend = 2
}