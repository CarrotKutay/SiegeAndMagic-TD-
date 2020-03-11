using Unity.Entities;
using Unity.Mathematics;
[GenerateAuthoringComponent]
public struct LightningArcPosition : IComponentData
{
    ///<summary>
    /// <see cref="origin"/> is the starting point of the lightning arc to be created. By default set to (0, 0, 0)
    ///</summary>
    public float3 origin;
    ///<summary>
    /// <see cref="destination"/> is the end-point of the arc to be created
    ///</summary>
    public float3 destination;
    ///<summary>
    /// The direction from the current position of the <see cref="LightningArc"/> to the <see cref="destination"/>
    ///</summary>
    /* public float3 dirToDest ; -> need to calculate on the fly */
    // TODO -> public UnityEngine.Plane constraintPlane; to spawn lightning arcs with constraints
}
