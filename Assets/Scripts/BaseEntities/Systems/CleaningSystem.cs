using Unity.Entities;
using Unity.Jobs;

public class CleaningSystem : JobComponentSystem
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

        float deltaTime = Time.DeltaTime;

        JobHandle jobHandle = Entities.ForEach((int entityInQueryIndex, Entity entity, ref DeathComponent deathComponent) =>
        {
            if (deathComponent.destroy && deathComponent.timeUntilDeath <= 0)
            {
                entityCommandBufferConcurrent.DestroyEntity(entityInQueryIndex, entity);
            }
            else if (deathComponent.destroy)
            {
                deathComponent.timeUntilDeath -= deltaTime;
            }
        }).Schedule(inputDeps);

        return jobHandle;
    }
}
