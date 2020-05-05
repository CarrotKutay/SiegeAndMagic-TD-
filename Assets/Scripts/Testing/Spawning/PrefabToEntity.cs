using Unity.Entities;
[GenerateAuthoringComponent]
public struct PrefabToEntity : IComponentData
{
    public Entity Prefab;

}