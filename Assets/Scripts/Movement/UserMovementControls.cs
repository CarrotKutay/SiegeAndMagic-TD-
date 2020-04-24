using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public class UserMovementControls : MonoBehaviour
{
    public Event OnMovementOrderCreated;
    public struct MovementOrderEventComponent : IComponentData
    {
        public float3 MovementOrderPosition;
    }
    private bool MovementOrder;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EntityManager manager;
    public Entity User;

    private void Awake()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
    }

    private void Start()
    {
        // searching for user entity
        var entities = manager.GetAllEntities(Allocator.TempJob);
        foreach (var entity in entities)
        {
            if (manager.HasComponent<UserTag>(entity))
            {
                User = entity;
                break;
            }
        }
        entities.Dispose();
    }
    private void Update()
    {
        MovementOrder = Input.GetMouseButtonUp(0); // movement order keyed to left mouse button released -> hardcoded for now
        if (MovementOrder)
        {
            // get current mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity))
            {
                // Removing old movement
                if (manager.HasComponent<PerformingMovement>(User)) { manager.RemoveComponent<PerformingMovement>(User); }
                if (manager.HasComponent<PathElement>(User))
                {
                    var pathBuffer = manager.GetBuffer<PathElement>(User);
                    pathBuffer.Clear();
                }

                var Position = manager.GetComponentData<Translation>(User);

                if (manager.HasComponent<PathfindingParameters>(User))
                {
                    manager.SetComponentData<PathfindingParameters>(User,
                        new PathfindingParameters
                        {
                            Start = Position.Value,
                            Target = hitInfo.point
                        });
                }
                else
                {
                    manager.AddComponentData<PathfindingParameters>(User,
                    new PathfindingParameters
                    {
                        Start = Position.Value,
                        Target = hitInfo.point
                    });
                }
            }
        }


    }
}
