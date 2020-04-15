using Unity.Entities;
using Unity.Mathematics;

public struct Node : IComponentData
{
    public PathfindingSystem.PathNode Value;
}

public struct PathElement : IBufferElementData
{
    public float3 Position;
}