using Unity.Entities;
using Unity.Transforms;
[GenerateAuthoringComponent]
public struct LightningArcTipComponent : IComponentData
{
    public Translation position;

    // TODO public SphereCollider tipCollider; not yet implemented to use colliders in ECS will need workaround
}