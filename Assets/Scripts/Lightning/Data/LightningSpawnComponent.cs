using Unity.Entities;
[GenerateAuthoringComponent]
public struct LightningSpawnComponent : IComponentData
{
    public float timeUntilNextSpawn;
    public Entity lightningArcPrefab;
    public bool isNoSubemitter;
}