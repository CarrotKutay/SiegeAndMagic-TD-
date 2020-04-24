using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;

public class GridSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        // initialize grid globals values 
        var gridWidth = GridGlobals.getGlobalGridWidth();
        var gridHeight = GridGlobals.getGlobalGridHeight();

        NativeArray<PathfindingSystem.PathNode> nodeGrid = new NativeArray<PathfindingSystem.PathNode>(gridWidth * gridHeight, Allocator.TempJob);

        // perform action on all Entities that still need initialization and contain GridData
        var gridCreationHandle = Entities
            .WithAll<InitializeGridTag>()
            .WithName("Grid_Initialization")
            .ForEach(
            (int entityInQueryIndex, in GridData data, in Translation Position, in Entity entity) =>
            {
                // get GridData and Position outside of For-Loops to save resources
                float3 gridPosition = Position.Value;

                // create Entities containing Nodes (node data)
                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        var node = new PathfindingSystem.PathNode
                        {
                            IndexOfParentNode = -1,
                            Position = gridPosition
                                            + new float3(0, 0, data.CellSize * y)
                                            + new float3(data.CellSize * x, 0, 0),
                            GCost = int.MaxValue,
                            HCost = 0,
                            FCost = 0,
                            Walkable = true,
                            Index = y * data.Width + x
                        };
                        nodeGrid.ReinterpretStore(node.Index, node);
                    }
                }
                entityCommandBuffer.RemoveComponent<InitializeGridTag>(entityInQueryIndex, entity);

            }).ScheduleParallel(Dependency);
        gridCreationHandle.Complete();

        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PathfindingSystem>().Nodes = new PathfindingSystem.PathNode[gridWidth * gridHeight];
        nodeGrid.ToArray().CopyTo(World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PathfindingSystem>().Nodes, 0);
        nodeGrid.Dispose();
    }
}
