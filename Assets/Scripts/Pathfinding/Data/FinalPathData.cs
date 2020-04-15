using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;


public struct PathfindingParameters : IComponentData
{
    public float3 Start;
    public float3 Target;
}
public struct CurrentPathNodeIndex : IComponentData
{
    public int Value;
}
