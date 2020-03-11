using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;

public class LightningArcSpawnSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityArchetype lightningEntityArchtype;
    /* private Entity lightningMaterial = GameObjectConversionUtility. */

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        lightningEntityArchtype = EntityManager.CreateArchetype(
            typeof(Translation),
            typeof(LightningArcEntity),
            typeof(LightningArcTipComponent),
            typeof(DeathComponent)
        );
    }

    private Unity.Mathematics.Random randomIntGenerator = new Unity.Mathematics.Random();
    private int minSpawnWait = 0; // inclusive
    private int maxSpawnWait = 6; // exclusive

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        EntityCommandBuffer entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer();
        EntityCommandBuffer.Concurrent entityCommandBufferConcurrent = entityCommandBuffer.ToConcurrent();


        float rndWaitTime = (float)Time.ElapsedTime + randomIntGenerator.NextInt(minSpawnWait, maxSpawnWait); //will be same for all current entities
        int rndNumberOfSpawns = randomIntGenerator.NextInt(0, 4); //will be same for all current entities
        float deltaTime = Time.DeltaTime;

        JobHandle handle = Entities.ForEach((int entityInQueryIndex, ref LightningSpawnComponent spawnComponent, in Translation position) =>
        {
            if (spawnComponent.timeUntilNextSpawn <= Time.ElapsedTime)
            {
                spawnComponent.timeUntilNextSpawn = rndWaitTime;
                Entity lightningEntity = entityCommandBufferConcurrent.CreateEntity(entityInQueryIndex, lightningEntityArchtype);
                entityCommandBufferConcurrent.SetComponent(entityInQueryIndex, lightningEntity,
                    new Translation
                    {
                        Value = new float3(position.Value)
                    }
                );
                entityCommandBufferConcurrent.SetComponent(entityInQueryIndex, lightningEntity,
                    new LightningArcEntity
                    {
                        lineRenderer = new UnityEngine.LineRenderer()
                    }
                );
            }
            else
            {
                spawnComponent.timeUntilNextSpawn -= deltaTime;
            }
        }).Schedule(inputDeps);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);

        return handle;
    }
}
