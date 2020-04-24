using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

// running player controls over a system might be stupid as there will always only be one player be controller by one person playing
// therefore new approach through monobehaviour might be better
// also as monobehaviour will always be performing on main thread, controls will always be more responsive
public class MovementOrderEventSytem : SystemBase
{
    public Event OnMovementOrderCreated;
    public struct MovementOrderEventComponent : IComponentData
    {
        public float3 MovementOrderPosition;
    }
    public bool MovementOrder;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
                    .DefaultGameObjectInjectionWorld
                    .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        MovementOrder = Input.GetMouseButtonUp(0); // movement order keyed to left mouse button released -> hardcoded for now

        EntityCommandBuffer.Concurrent commandBufferConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();

        if (MovementOrder)
        {
            // get current mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
            {
                var manager = World.DefaultGameObjectInjectionWorld.EntityManager;

                // shutdown debug + movement system before initializing new movement as they are reading from old movement
                /* var debugSystem = World.GetOrCreateSystem<PathfindingVisualDebugSystem>();
                var movingSystem = World.GetOrCreateSystem<FollowPathOrderSystem>();
                var system = World.GetOrCreateSystem<EndFrameTRSToLocalToWorldSystem>();
                system.EntityManager.CompleteAllJobs();
                movingSystem.shutdown();
                debugSystem.shutdown(); */

                // add new movement
                JobHandle handle = Entities.WithName("UserMovementControl")
                    .WithAll<UserTag>()
                    .ForEach(
                    (int entityInQueryIndex, ref DynamicBuffer<PathElement> path, in Entity entity, in Translation Position) =>
                        {
                            // Removing old movement
                            commandBufferConcurrent.RemoveComponent<PerformingMovement>(entityInQueryIndex, entity);
                            path.Clear();

                            if (HasComponent<PathfindingParameters>(entity))
                            {
                                commandBufferConcurrent.SetComponent<PathfindingParameters>(entityInQueryIndex, entity,
                                    new PathfindingParameters
                                    {
                                        Start = Position.Value,
                                        Target = hitInfo.point
                                    });
                            }
                            else
                            {
                                commandBufferConcurrent.AddComponent<PathfindingParameters>(
                                    entityInQueryIndex, entity, new PathfindingParameters
                                    {
                                        Start = Position.Value,
                                        Target = hitInfo.point
                                    }
                                );
                            }
                        }
                ).Schedule(Dependency);

                endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);
            }
        }


    }


}
