using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

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
        // get Buffer from Entity into a local variable
        var nodeBufferFromEntity = GetBufferFromEntity<NodeElement>(false);

        // perform action on all Entities that still need initialization and contain GridData
        Entities.WithAll<InitializeGridTag, GridData>().ForEach(
            (int entityInQueryIndex, in Entity gridEntity) =>
            {
                // remove Initialization tag
                ECBConcurrent.RemoveComponent<InitializeGridTag>(entityInQueryIndex, gridEntity);

                // create buffer containing nodes to be held by grid (!value type)
                var nodes = nodeBufferFromEntity[gridEntity];
                // get GridData and Position outside of For-Loops to save resources
                GridData data = GetComponent<GridData>(gridEntity);
                float3 gridPosition = GetComponent<Translation>(gridEntity).Value;

                // create Entities containing Nodes (node data)
                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        var entityNode = ECBConcurrent.CreateEntity(entityInQueryIndex);
                        ECBConcurrent.AddComponent<Node>(entityInQueryIndex, entityNode,
                            new Node()
                            {
                                Position = new Translation()
                                {
                                    Value = gridPosition
                                            + new float3(0, 0, data.CellSize) * y
                                            + new float3(data.CellSize * x, 0, 0)
                                },
                                Walkable = true
                            }
                        );

                        // assigning entities to buffer
                        nodes[y * data.Width + x] = new NodeElement
                        {
                            Node = entityNode
                        };
                    }
                }

                // reassigning buffer as new buffer of entity
                // has to be done as the buffer is a value type !?
                DynamicBuffer<NodeElement> buffer = ECBConcurrent.SetBuffer<NodeElement>(entityInQueryIndex, gridEntity);
                buffer.Clear();
                buffer.AddRange(nodes.AsNativeArray());

            })
            .WithNativeDisableParallelForRestriction(nodeBufferFromEntity)
            .ScheduleParallel();

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
