using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
[GenerateAuthoringComponent]
public struct LightningArcEntity : IComponentData
{
    public LineRenderer lineRenderer;
    public Entity lightningArcMaterialPrefab;
    ///<summary>
    /// Maximum distance from origin for any arc that a lightning arc is able to reach
    ///</summary>
    public float maxRadius;
    public float startWidth;
    public int nextSegmentCounter;
    public bool isSubemitter;
}