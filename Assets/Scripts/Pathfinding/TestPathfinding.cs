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
            typeof(GridData),
            typeof(PathfindingStart),
            typeof(PathfindingTarget),
            typeof(FindingPathTag)
        );

        Entity findPathEntity = entityManager.CreateEntity(findPathArchetype);
        entityManager.SetComponentData(findPathEntity,
            new GridData
            {
                CellSize = GridGlobals.getGlobalGridCellSize(),
                Width = GridGlobals.getGlobalGridWidth(),
                Height = GridGlobals.getGlobalGridHeight()
            }
        );
        entityManager.SetComponentData(findPathEntity,
            new PathfindingStart
            {
                Value = new float3(startPosition)
            }
        );
        entityManager.SetComponentData(findPathEntity,
            new PathfindingTarget
            {
                Value = new float3(targetPosition)
            }
        );
        entityManager.SetComponentData(findPathEntity,
            new FindingPathTag
            {
                Value = true
            }
        );
    }
}
