using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

public struct Node : IComponentData
{
    public Translation Position;
    public MoveDataJob MoveData;
    public bool Walkable;
    public Entity Parent;
}

public struct NodeElement : IBufferElementData
{
    public Entity Node;
}

public struct MoveDataJob : IJob
{
    public float HCost, GCost, FCost;
    public void Execute()
    {
        FCost = HCost + GCost;
    }
}
