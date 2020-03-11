using Unity.Entities;
using Unity.Mathematics;
[GenerateAuthoringComponent]
public struct SpawnEventComponent : IComponentData
{
    public float3 position;
    public int numberOfSpawns;
    public Entity prefabToSpawn;
}