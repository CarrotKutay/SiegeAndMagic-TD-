using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System;

public class FollowPathOrderSystem : SystemBase
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
        var ecb_Concurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var getBuffer = GetBufferFromEntity<PathElement>();
        var getPosition = GetComponentDataFromEntity<Translation>();
        var getPathIndex = GetComponentDataFromEntity<CurrentPathNodeIndex>();
        //var DeltaTime = new NativeArray<float>(1, Allocator.TempJob);

        var entities = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] {
                typeof(PerformingMovement),
                typeof(PathElement),
                typeof(Translation) }
        }).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getEntitiesHandle);

        /* NativeArray<float> floatArray;
        var setFloatArray = new MemsetNativeArray<float>
        {
            Source = floatArray = new NativeArray<float>(entities.Length, Allocator.TempJob),
            Value = float.MaxValue
        }.Schedule(entities.Length, 1, getEntitiesHandle); */

        var job = new MoveJob
        {
            DeltaTime = Time.DeltaTime,
            ecb_Concurrent = ecb_Concurrent,
            MovingObjects = entities,
            GetCurrentPathIndex = getPathIndex,
            GetPathBuffer = getBuffer,
            GetPosition = getPosition,
        };
        //var finalMoveDeps = JobHandle.CombineDependencies(getEntitiesHandle, setFloatArray);
        var handle = job.Schedule(entities.Length, 1, getEntitiesHandle);

        handle.Complete();
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);
    }

    public struct MoveJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> MovingObjects;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> GetPosition;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<CurrentPathNodeIndex> GetCurrentPathIndex;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathElement> GetPathBuffer;
        [NativeDisableParallelForRestriction]
        public EntityCommandBuffer.Concurrent ecb_Concurrent;
        public float DeltaTime;



        public void Execute(int index)
        {
            var entity = MovingObjects[index];
            var Position = GetPosition[entity];
            var PathIndex = GetCurrentPathIndex[entity];
            var Buffer = GetPathBuffer[entity];

            if (Buffer.Length > 1)
            {
                var distanceToCurrentStep = math.distance(Position.Value,
                Buffer[PathIndex.Value].Position + new float3(0, Position.Value.y, 0)); //! ignoring vertical distance for now between target and moving object

                var nextStep = Buffer[PathIndex.Value].Position;
                var currentStep = Buffer[PathIndex.Value + 1].Position;
                var direction = math.normalizesafe(nextStep - currentStep);
                direction.y = 0; // ! negating any vertical movement for now between target steps
                Position.Value += direction * DeltaTime * 5; // 5 -> hardcoded movementspeed for testing
                ecb_Concurrent.SetComponent<Translation>(index, entity, Position);

                var distanceNextFrame = math.distance(Position.Value,
                    Buffer[PathIndex.Value].Position + new float3(0, Position.Value.y, 0));

                var reachedTarget = distanceNextFrame > distanceToCurrentStep;

                if (reachedTarget)
                {
                    PathIndex.Value--;
                    ecb_Concurrent.SetComponent<CurrentPathNodeIndex>(index, entity, PathIndex);
                };
                if (PathIndex.Value < 0) { ecb_Concurrent.RemoveComponent<PerformingMovement>(index, entity); }
            }
        }
    }

}
