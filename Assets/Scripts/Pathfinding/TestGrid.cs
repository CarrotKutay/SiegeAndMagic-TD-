using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class TestGrid : MonoBehaviour
{
    [SerializeField]
    private int testWidth = 50;
    [SerializeField]
    private int testHeight = 50;
    private EntityManager manager;
    private EntityArchetype nodeArchetype;

    // Start is called before the first frame update
    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        nodeArchetype = manager.CreateArchetype(
            typeof(Node)
        );

        Entity grid = manager.CreateEntity();
        manager.SetName(grid, "GridTest");
        manager.AddComponent(grid, typeof(Translation));
        manager.AddComponent(grid, typeof(GridData));
        manager.SetComponentData(grid,
            new Translation()
            {
                Value = new float3(0, 0, 0)
            }
        );
        manager.SetComponentData(grid,
            new GridData()
            {
                Width = testWidth,
                Height = testHeight,
                CellSize = 1
            }
        );

        // initialize tags
        manager.AddComponent(grid,
            typeof(InitializeGridTag)
        );
        manager.SetComponentData(grid,
            new InitializeGridTag() { Value = true }
        );

        // add buffer for nodes
        manager.AddBuffer<NodeElement>(grid).ResizeUninitialized(testHeight * testWidth);
    }
}
