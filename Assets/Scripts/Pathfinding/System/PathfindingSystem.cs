using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public class PathfindingSystem : SystemBase
{
    private EntityQueryDesc entityQueryDesc;
    private EntityQuery entityQuery;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        entityQueryDesc = new EntityQueryDesc();
        entityQueryDesc.All = new ComponentType[] { typeof(PathfindingStart), typeof(PathfindingTarget), typeof(FindingPathTag) };

        entityQuery = GetEntityQuery(entityQueryDesc);
    }

    protected override void OnUpdate()
    {
        var ECBConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        var nodesArray = GetEntityQuery(typeof(Node)).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getNodesJob);
        getNodesJob.Complete();

        Entities.WithAll<PathfindingTarget, PathfindingStart, FindingPathTag>()
                .ForEach((int entityInQueryIndex, in Entity entity) =>
                {
                    if (nodesArray.Length == GridGlobals.getGlobalGridHeight() * GridGlobals.getGlobalGridWidth())
                    {
                        float3 startPosition = GetComponent<PathfindingStart>(entity).Value;
                        float3 targetPosition = GetComponent<PathfindingTarget>(entity).Value;
                        GridData gridData = GetComponent<GridData>(entity);

                        Node startNode = GetComponent<Node>(
                            nodesArray[GridGlobals.GetCellIndexFromWorldPosition(
                                startPosition
                            )]);
                        Node targetNode = GetComponent<Node>(
                            nodesArray[GridGlobals.GetCellIndexFromWorldPosition(
                                targetPosition
                            )]);

                        /* UnityEngine.Debug.Log("start: " + startNode.Position.Value.ToString());
                        UnityEngine.Debug.Log("target: " + targetNode.Position.Value.ToString()); */

                        ECBConcurrent.DestroyEntity(entityInQueryIndex, entity);
                    }

                })
                .WithDeallocateOnJobCompletion(nodesArray)
                .ScheduleParallel();

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(this.Dependency);
    }
}
