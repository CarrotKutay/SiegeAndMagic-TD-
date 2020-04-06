using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public class GridSystem : SystemBase
{
    private EntityQuery entityQueryGroup;
    protected override void OnCreate()
    {

    }
    protected override void OnUpdate()
    {
        /* NativeArray<Node> nodes = entityQueryGroup.ToComponentDataArrayAsync<Node>(Allocator.TempJob, out JobHandle handleGetNodes);
        handleGetNodes.Complete();



        nodes.Dispose(); */
    }
}



public struct GetCellIndexFromWorldPositionJob : IJob
{
    public float3 Position;
    public int CellIndex;
    public float CellSize;
    public int GridWidth;

    public void Execute()
    {
        int x = (int)(math.floor(Position.x / CellSize));
        int y = (int)(math.floor(Position.z / CellSize));

        CellIndex = y * GridWidth + x;
    }
}
