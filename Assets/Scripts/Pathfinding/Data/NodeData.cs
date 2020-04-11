using Unity.Entities;
using Unity.Transforms;

public struct Node : IComponentData
{
    public PathfindingSystem.PathNode Value;
}

public struct NodeElement : IBufferElementData
{
    public Entity Node;
}