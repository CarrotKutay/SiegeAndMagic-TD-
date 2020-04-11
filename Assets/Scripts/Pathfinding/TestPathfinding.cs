using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class TestPathfinding : MonoBehaviour
{
    private EntityManager entityManager;
    [SerializeField]
    private float3 startPosition;
    [SerializeField]
    private float3 targetPosition;

    private EntityArchetype findPathArchetype;

    // Start is called before the first frame update
    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        findPathArchetype = entityManager.CreateArchetype(
            typeof(PathfindingParameters),
            typeof(CurrentPathNodeIndex)
        );

        Entity findPathEntity = entityManager.CreateEntity(findPathArchetype);
        entityManager.SetName(findPathEntity, "PathfindingEntity");
        entityManager.SetComponentData(findPathEntity,
            new PathfindingParameters()
            {
                Start = startPosition,
                Target = targetPosition
            }
        );
        entityManager.SetComponentData(findPathEntity,
            new CurrentPathNodeIndex()
            {
                Value = -1
            }
        );
    }
}
