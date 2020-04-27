using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;

[UpdateInGroup(typeof(InitializationSystemGroup))]
public class GridSystem : SystemBase
{
    private int gridHeight;
    private float gridCellSize;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private int gridWidth;

    public float GridCellSize { get => gridCellSize; set => gridCellSize = value; }
    public int GridWidth { get => gridWidth; set => gridWidth = value; }
    public int GridHeight { get => gridHeight; set => gridHeight = value; }

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
        GridWidth = GridGlobals.getGlobalGridWidth();
        GridHeight = GridGlobals.getGlobalGridHeight();
        GridCellSize = GridGlobals.getGlobalGridCellSize();

        NativeArray<PathfindingSystem.PathNode> nodeGrid = new NativeArray<PathfindingSystem.PathNode>(GridWidth * GridHeight, Allocator.TempJob);

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

        var pathfinding = World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<PathfindingSystem>();
        pathfinding.Nodes = new PathfindingSystem.PathNode[GridWidth * GridHeight];
        nodeGrid.ToArray().CopyTo(pathfinding.Nodes, 0);


        nodeGrid.Dispose();
    }
}
