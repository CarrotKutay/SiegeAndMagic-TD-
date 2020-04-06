using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;


public struct PathfindingTarget : IComponentData
{
    public float3 Value;
}
public struct PathfindingStart : IComponentData
{
    public float3 Value;
}
