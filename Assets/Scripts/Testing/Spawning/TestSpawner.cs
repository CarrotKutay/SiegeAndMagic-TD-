using UnityEngine;
using Unity.Jobs;
using Unity.Entities;
using Unity.Collections;

public class TestSpawner : MonoBehaviour
{
    [SerializeField] private bool SpawnObject = false;
    private EntitySpawnJob spawnJob;
    [SerializeField] private GameObject GameObjectPrefab;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityManager manager;

    private void Start()
    {
        endSimulationEntityCommandBufferSystem = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;
    }

    // Update is called once per frame
    void Update()
    {

        if (SpawnObject)
        {
            SpawnObject = !SpawnObject;

            /* var spawnBuffer = endSimulationEntityCommandBufferSystem
                    .CreateCommandBuffer().ToConcurrent();

            var entities = manager.GetAllEntities(Allocator.TempJob);

            foreach (var entity in entities)
            {
                if (manager.HasComponent<PaladinUnit>(entity))
                {
                    spawnJob = new EntitySpawnJob
                    {
                        ECS_Concurrent = spawnBuffer,
                        EntityPrefab = entity,
                    };
                    var handle = spawnJob.Schedule();
                    handle.Complete();
                    break;
                }
            } 
            entities.Dispose();
            */

            Instantiate(GameObjectPrefab);
        }
    }
}

