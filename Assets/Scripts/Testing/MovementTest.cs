using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Physics;
using UnityEngine;

public class MovementTest : MonoBehaviour
{
    private EntityManager manager;
    private EntityArchetype unitArchetype;
    [SerializeField]
    private float3 startPosition;
    [SerializeField]
    private float3 targetPosition;
    [SerializeField]
    private UnityEngine.Mesh testMesh;
    [SerializeField]
    private UnityEngine.Material testMaterial;

    // Start is called before the first frame update
    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        unitArchetype = manager.CreateArchetype(
            typeof(Translation),
            typeof(Rotation),
            typeof(RenderMesh),
            typeof(RenderBounds),
            typeof(LocalToWorld),
            typeof(PathfindingParameters),
            typeof(CurrentPathNodeIndex),
            typeof(UserTag)
        );

        createTestUnit();
    }

    private void createTestUnit()
    {
        var testUnit = manager.CreateEntity(unitArchetype);
        manager.SetName(testUnit, "TestUnit");
        manager.SetComponentData(testUnit,
            new PathfindingParameters()
            {
                Start = startPosition,
                Target = targetPosition
            }
        );
        manager.SetComponentData(testUnit,
            new CurrentPathNodeIndex()
            {
                Value = 0
            }
        );
        manager.SetSharedComponentData(testUnit,
            new RenderMesh
            {
                mesh = testMesh,
                material = testMaterial
            }
        );
        manager.SetComponentData(testUnit,
            new Translation
            {
                Value = startPosition
            }
        );
        manager.SetComponentData(testUnit,
            new UserTag
            {
                Value = true
            });
    }
}
