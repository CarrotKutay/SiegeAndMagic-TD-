using Unity.Entities;
using Unity.Jobs;

public class LightningSystem : JobComponentSystem
{
    private EndSimulationEntityCommandBufferSystem commandBufferSystem;

    protected override void OnCreate()
    {
        commandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        JobHandle buildJob = Entities.ForEach((Entity entity,
                                                LightningBuildSystem uncompleteBuild,
                                                LightningArcEntity arc,
                                                LightningArcPosition position) =>
        {

        }).Schedule(inputDeps);

        return default;
    }

    /*  private void setupLightningArc(Entity lightningArc)
     {
         origin = transform.position;
         lineRenderer.positionCount = 1;
         lineRenderer.SetPosition(0, origin); // setting start position
         setRandomDestination();
         lineRenderer.material = lightningArcMaterial;
         lineRenderer.endWidth = 0;
         lineRenderer.startWidth = startWidth;
         lineRenderer.numCapVertices = 3;
     } */
}
