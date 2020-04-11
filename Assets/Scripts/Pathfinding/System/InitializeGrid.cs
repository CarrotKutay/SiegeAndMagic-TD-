using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;

public class InitializeGrid : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // create entityCommandBuffer as Concurrent to make parallel working possible
        var ECBConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        // perform action on all Entities that still need initialization and contain GridData
        Entities.WithAll<InitializeGridTag>().ForEach(
            (int entityInQueryIndex, in GridData data, in Translation Position, in Entity entity) =>
            {
                // get GridData and Position outside of For-Loops to save resources
                float3 gridPosition = Position.Value;

                // create Entities containing Nodes (node data)
                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        var entityNode = ECBConcurrent.CreateEntity(entityInQueryIndex);
                        ECBConcurrent.AddComponent<Node>(entityInQueryIndex, entityNode,
                            new Node()
                            {
                                Value = new PathfindingSystem.PathNode()
                                {
                                    IndexOfParentNode = -1,
                                    Position = gridPosition
                                            + new float3(0, 0, data.CellSize * y)
                                            + new float3(data.CellSize * x, 0, 0),
                                    GCost = int.MaxValue,
                                    HCost = int.MaxValue,
                                    FCost = 0,
                                    Walkable = true,
                                    Index = y * data.Width + x
                                }
                            }
                        );
                    }
                }

                ECBConcurrent.RemoveComponent<InitializeGridTag>(entityInQueryIndex, entity);

            }).ScheduleParallel();

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
