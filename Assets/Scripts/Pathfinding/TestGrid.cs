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
    [SerializeField]
    private float testCellSize = 1;
    private EntityManager manager;

    private void Awake()
    {
        GridGlobals.UpdateGridGlobals(testWidth, testHeight, testCellSize);
    }

    // Start is called before the first frame update
    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity grid = manager.CreateEntity();
        manager.SetName(grid, "GridTest");
        manager.AddComponent(grid, typeof(Translation));
        manager.AddComponent(grid, typeof(GridData));
        manager.SetComponentData(grid,
            new Translation()
            {
                Value = new float3(
                    transform.position.x - GridGlobals.getGlobalGridWidth() / 2,
                    transform.position.y,
                    transform.position.z - GridGlobals.getGlobalGridHeight() / 2
                )
            }
        );
        manager.SetComponentData(grid,
            new GridData()
            {
                Width = GridGlobals.getGlobalGridWidth(),
                Height = GridGlobals.getGlobalGridHeight(),
                CellSize = GridGlobals.getGlobalGridCellSize()
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
        manager.AddBuffer<NodeElement>(grid).ResizeUninitialized(GridGlobals.getGlobalGridHeight() * GridGlobals.getGlobalGridWidth());
    }
}
