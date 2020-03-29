using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

public class TestGrid : MonoBehaviour
{
    [SerializeField]
    private int testWidth = 50;
    [SerializeField]
    private int testHeight = 50;
    private EntityManager manager;
    private EntityArchetype gridArchetype;

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
        manager.AddBuffer<CellData>(grid);

        OnGridEntityCreated(grid);
    }

    private void OnGridEntityCreated(Entity grid)
    {
        for (int y = 0; y < testHeight; y++)
        {
            for (int x = 0; x < testWidth; x++)
            {
                manager.GetBuffer<CellData>(grid).Add(new CellData()
                {
                    Walkable = false
                });
            }
        }
    }
}
