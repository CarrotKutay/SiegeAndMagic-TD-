using Unity.Entities;
[GenerateAuthoringComponent]
public struct DeathComponent : IComponentData
{
    public float timeUntilDeath;
    public bool destroy;
}
