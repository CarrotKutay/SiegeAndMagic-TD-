using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

public class FollowPathOrderSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityQueryDesc desc;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(PerformingMovement), typeof(PathElement) },
            None = new ComponentType[] { typeof(PathfindingParameters) }
        };
    }

    protected override void OnUpdate()
    {
        var commandBufferConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var deltaTime = Time.DeltaTime;

        /* Entities.WithName("Movement")
            .WithNone<VisualDebugData, PathfindingParameters>()
            .WithAll<PerformingMovement>()
            .ForEach(
                (int entityInQueryIndex, ref Translation Position, in DynamicBuffer<PathElement> Buffer, in Entity entity) =>
                {
                    var Index = GetComponent<CurrentPathNodeIndex>(entity);
                    var reachedCurrentStep = math.distance(Position.Value,
                            Buffer[Index.Value].Position + new float3(0, Position.Value.y, 0)) < .2f; //! ignoring vertical distance for now
                    if (reachedCurrentStep)
                    {
                        Index.Value -= 1;
                        UnityEngine.Debug.Log(Index.Value.ToString());
                    };
                    if (reachedCurrentStep && Index.Value < 0)
                    {
                        commandBufferConcurrent.RemoveComponent<PerformingMovement>(entityInQueryIndex, entity);
                    }
                    else
                    {
                        var nextStep = Buffer[Index.Value].Position;
                        var direction = math.normalizesafe(nextStep - Position.Value);
                        direction.y = 0; // ! negating any vertical movement for now
                        Position.Value += direction * deltaTime * 5; // 5 -> hardcoded movementspeed for testing
                    }
                }
            ).ScheduleParallel(); */

        var movingObjects = GetEntityQuery(desc).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getMovingObjects);
        getMovingObjects.Complete();

        var GetPathElementBuffer = GetBufferFromEntity<PathElement>();
        var GetPosition = GetComponentDataFromEntity<Translation>();
        var GetPathIndex = GetComponentDataFromEntity<CurrentPathNodeIndex>();

        var moveJob = new MoveJob()
        {
            Entities = movingObjects,
            ecb_Concurrent = commandBufferConcurrent,
            GetPosition = GetPosition,
            GetPathIndex = GetPathIndex,
            GetBuffer = GetPathElementBuffer,
            DeltaTime = deltaTime
        };

        var handel = moveJob.Schedule(movingObjects.Length, 1, Dependency);
        handel.Complete();
    }

    struct MoveJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities;
        public EntityCommandBuffer.Concurrent ecb_Concurrent;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> GetPosition;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<CurrentPathNodeIndex> GetPathIndex;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathElement> GetBuffer;
        public float DeltaTime;
        public void Execute(int index)
        {
            var movingEntity = Entities[index];
            var Position = GetPosition[movingEntity];
            var PathIndex = GetPathIndex[movingEntity];
            var Buffer = GetBuffer[movingEntity];

            var reachedCurrentStep = math.distance(Position.Value,
                            Buffer[PathIndex.Value].Position + new float3(0, Position.Value.y, 0)) < .1f; //! ignoring vertical distance for now
            if (reachedCurrentStep)
            {
                PathIndex.Value--;
                ecb_Concurrent.SetComponent<CurrentPathNodeIndex>(index, movingEntity, PathIndex);
            };
            if (reachedCurrentStep && PathIndex.Value < 0)
            {
                ecb_Concurrent.RemoveComponent<PerformingMovement>(index, movingEntity);
            }
            else
            {
                var nextStep = Buffer[PathIndex.Value].Position;
                var currentStep = Buffer[PathIndex.Value + 1].Position;
                var direction = math.normalizesafe(nextStep - currentStep);
                direction.y = 0; // ! negating any vertical movement for now
                Position.Value += direction * DeltaTime * 5; // 5 -> hardcoded movementspeed for testing
                ecb_Concurrent.SetComponent<Translation>(index, movingEntity, Position);
            }
        }
    }
}
