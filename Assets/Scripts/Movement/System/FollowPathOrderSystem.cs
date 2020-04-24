using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;

public class FollowPathOrderSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityQueryDesc desc;
    private NativeList<JobHandle> MoveJobs;

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
        MoveJobs = new NativeList<JobHandle>(Allocator.Persistent);
    }

    protected override void OnDestroy()
    {
        MoveJobs.Dispose();
    }

    protected override void OnUpdate()
    {
        var ecb_Concurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var getBuffer = GetBufferFromEntity<PathElement>();
        var DeltaTime = Time.DeltaTime;

        Entities.WithName("MovingObjects")
            .WithAll<PerformingMovement, PathElement>()
            .WithNone<PathfindingParameters>()
            .ForEach(
                (int entityInQueryIndex, ref Entity entity) =>
                {
                    var Position = GetComponent<Translation>(entity);
                    var PathIndex = GetComponent<CurrentPathNodeIndex>(entity);
                    var Buffer = getBuffer[entity];

                    var reachedCurrentStep = math.distance(Position.Value,
                        Buffer[PathIndex.Value].Position + new float3(0, Position.Value.y, 0)) < .1f; //! ignoring vertical distance for now
                    if (reachedCurrentStep)
                    {
                        PathIndex.Value--;
                        ecb_Concurrent.SetComponent<CurrentPathNodeIndex>(entityInQueryIndex, entity, PathIndex);
                    };
                    if (PathIndex.Value < 0) { ecb_Concurrent.RemoveComponent<PerformingMovement>(entityInQueryIndex, entity); }
                    else
                    {
                        var nextStep = Buffer[PathIndex.Value].Position;
                        var currentStep = Buffer[PathIndex.Value + 1].Position;
                        var direction = math.normalizesafe(nextStep - currentStep);
                        direction.y = 0; // ! negating any vertical movement for now
                        Position.Value += direction * DeltaTime * 5; // 5 -> hardcoded movementspeed for testing
                        ecb_Concurrent.SetComponent<Translation>(entityInQueryIndex, entity, Position);
                    }
                }
            )
            .WithNativeDisableParallelForRestriction(getBuffer)
            .Schedule();
    }

    public void shutdown()
    {
        this.EntityManager.CompleteAllJobs();
        this.CompleteDependency();
        this.Enabled = false;
    }

    /* protected override void OnUpdate()
    {
        var commandBufferConcurrent = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var deltaTime = Time.DeltaTime;

        var movingObjects = GetEntityQuery(desc).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getMovingObjects);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(getMovingObjects);
        getMovingObjects.Complete();

        for (int i = 0; i < MoveJobs.Length; i++)
        {
            if (MoveJobs[i].IsCompleted) MoveJobs.RemoveAtSwapBack(i);
        }

        var GetPathElementBuffer = GetBufferFromEntity<PathElement>();
        var GetPosition = GetComponentDataFromEntity<Translation>();
        var GetPathIndex = GetComponentDataFromEntity<CurrentPathNodeIndex>();

        for (int i = 0; i < movingObjects.Length; i++)
        {
            var moveJob = new MoveJob()
            {
                movingEntity = movingObjects[i],
                ecb_Concurrent = commandBufferConcurrent,
                GetPosition = GetPosition,
                GetPathIndex = GetPathIndex,
                GetBuffer = GetPathElementBuffer,
                DeltaTime = deltaTime
            };

            JobHandle.CompleteAll(MoveJobs);
            var moveJobDeps = JobHandle.CombineDependencies(MoveJobs);
            var handel = moveJob.Schedule(moveJobDeps);
            MoveJobs.Add(handel);
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handel);
        }
    }

    struct MoveJob : IJob
    {
        [DeallocateOnJobCompletion]
        public Entity movingEntity;
        public EntityCommandBuffer.Concurrent ecb_Concurrent;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> GetPosition;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<CurrentPathNodeIndex> GetPathIndex;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathElement> GetBuffer;
        public float DeltaTime;
        public void Execute()
        {
            //var movingEntity = Entities[index];
            var Position = GetPosition[movingEntity];
            var PathIndex = GetPathIndex[movingEntity];
            var Buffer = GetBuffer[movingEntity];

            var reachedCurrentStep = math.distance(Position.Value,
                Buffer[PathIndex.Value].Position + new float3(0, Position.Value.y, 0)) < .1f; //! ignoring vertical distance for now
            if (reachedCurrentStep)
            {
                PathIndex.Value--;
                ecb_Concurrent.SetComponent<CurrentPathNodeIndex>(0, movingEntity, PathIndex);
            };
            if (PathIndex.Value < 0) { ecb_Concurrent.RemoveComponent<PerformingMovement>(1, movingEntity); }
            else
            {
                var nextStep = Buffer[PathIndex.Value].Position;
                var currentStep = Buffer[PathIndex.Value + 1].Position;
                var direction = math.normalizesafe(nextStep - currentStep);
                direction.y = 0; // ! negating any vertical movement for now
                Position.Value += direction * DeltaTime * 5; // 5 -> hardcoded movementspeed for testing
                ecb_Concurrent.SetComponent<Translation>(2, movingEntity, Position);
            }

        }
    } */
}
