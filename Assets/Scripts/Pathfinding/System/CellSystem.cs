using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;

public class CellSystem : SystemBase
{
    public struct DebugJob : IJobParallelFor
    {
        public NativeArray<NodeElement> nodeElements;
        public void Execute(int index)
        {

        }

    }
    private EntityManager eManager;
    private EndSimulationEntityCommandBufferSystem EndSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        eManager = World.EntityManager;
        EndSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        /*  var ECB = EndSimulationEntityCommandBufferSystem.CreateCommandBuffer();

         Entities.WithoutBurst().ForEach( // allocating a native array -> can't use burst ?!
             (in DynamicBuffer<NodeElement> buffer) =>
                 {
                     NativeArray<NodeElement> nodeElements = buffer.ToNativeArray(Allocator.TempJob);

                     DebugJob job = new DebugJob()
                     {
                         nodeElements = nodeElements
                     };
                     JobHandle handle = job.Schedule(nodeElements.Length, 1);
                     handle.Complete();

                     var nodeEntity = ECB.CreateEntity();
                     //ECB.AddComponent(nodeEntity, node);
                     //ECB.SetComponent<Node>(nodeEntity, node);

                     nodeElements.Dispose();

                 }).Run(); */
    }

    /* protected override void OnUpdate()
    {
        NativeArray<CellData> cells;

        Entities.ForEach(
            (ref DynamicBuffer<Node> buffer) =>
                {
                    cells = new NativeArray<Node>(buffer.AsNativeArray(), Allocator.TempJob);
                    DebugJob job = new DebugJob();
                    job.Cells = cells;

                    JobHandle debugJobHandle = job.Schedule(cells.Length, 1);
                    debugJobHandle.Complete(); 
                    cells.Dispose();
                }).Run();
    } */
}