/* using Unity.Entities;
using Unity.Jobs;

public class SpawnEventListenerSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        EntityCommandBuffer.Concurrent entityCommandBufferConcurrent = entityCommandBuffer.ToConcurrent();

        JobHandle jobHandle = Entities.ForEach((int entityInQueryIndex, ref SpawnEventComponent spawnEvent) =>
        {
            Entity spawn = entityCommandBufferConcurrent.Instantiate(entityInQueryIndex, spawnEvent.prefabToSpawn);
        }).Schedule(inputDeps);

        return jobHandle;
    }
}
 */