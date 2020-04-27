using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;

[DisableAutoCreation]
public class PathfindingVisualDebugSystem : SystemBase
{
    private EntityQueryDesc desc;
    private EntityManager manager;
    private float TimeForNextUpdate = 0;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(PerformingMovement), typeof(PathElement) },
            None = new ComponentType[] { typeof(PathfindingParameters) }
        };
    }

    protected override void OnUpdate()
    {
        if (Time.ElapsedTime > TimeForNextUpdate)
        {
            TimeForNextUpdate += Time.DeltaTime;
            var Paths = GetEntityQuery(desc)
                    .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getPathParams);

            getPathParams.Complete();


            for (int index = 0; index < Paths.Length; index++)
            {
                var currentPathIndex = GetComponent<CurrentPathNodeIndex>(Paths[index]).Value;
                if (currentPathIndex > 0)
                {
                    var Position = GetComponent<Translation>(Paths[index]);
                    var pathBuffer = manager.GetBuffer<PathElement>(Paths[index]);
                    var float3Buffer = pathBuffer.Reinterpret<float3>();
                    if (float3Buffer.Length > 0)
                    {
                        var debugObj = new DebugPathView
                        {
                            Start = Position.Value
                        };
                        var tempList = new List<float3>(float3Buffer.ToNativeArray(Allocator.Temp).ToArray());
                        debugObj.Path = tempList.GetRange(0, currentPathIndex);
                        debugObj.Target = debugObj.Path[0];

                        DebugDrawPathData(
                            debugObj.Start,
                            debugObj.Target,
                            debugObj.Path
                        );
                    }
                }
            }

            Paths.Dispose();
        }
    }

    private void DebugDrawPathData(float3 Start, float3 Target, List<float3> Path)
    {
        DebugCubeDraw(Start, Color.green, Time.DeltaTime);
        DebugCubeDraw(Target, Color.red, Time.DeltaTime);
        for (int i = 0; i < Path.Count - 1; i++)
        {
            Debug.DrawLine(Path[i], Path[i + 1], Color.magenta, .1f);
        }
    }

    private void DebugCubeDraw(float3 Center, Color color, float Duration, float SideLength = 2)
    {
        var halfSideLength = SideLength / 2;
        // half side length directional vector
        var half_SL_Direction = new float3(halfSideLength, halfSideLength, halfSideLength);
        //bottom left corner origin (front)
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(halfSideLength, -halfSideLength, -halfSideLength), color, Duration);
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(-halfSideLength, -halfSideLength, halfSideLength), color, Duration);
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(-halfSideLength, halfSideLength, -halfSideLength), color, Duration);
        //top right corner (back)
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(halfSideLength, -halfSideLength, halfSideLength), color, Duration);
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(-halfSideLength, halfSideLength, halfSideLength), color, Duration);
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(halfSideLength, halfSideLength, -halfSideLength), color, Duration);

        half_SL_Direction = new float3(halfSideLength, -halfSideLength, halfSideLength);
        //bottom right corner (back)
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(-halfSideLength, -halfSideLength, halfSideLength), color, Duration);
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(halfSideLength, -halfSideLength, -halfSideLength), color, Duration);

        //top left corner (front)
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(halfSideLength, halfSideLength, -halfSideLength), color, Duration);
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(-halfSideLength, halfSideLength, halfSideLength), color, Duration);

        half_SL_Direction = new float3(halfSideLength, halfSideLength, -halfSideLength);
        //top right corner (front)
        Debug.DrawLine(Center + half_SL_Direction, Center + new float3(halfSideLength, -halfSideLength, -halfSideLength), color, Duration);

        //bottom left corner (back)
        Debug.DrawLine(Center - half_SL_Direction, Center + new float3(-halfSideLength, halfSideLength, halfSideLength), color, Duration);

    }

    public struct DebugPathJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Paths;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathElement> GetPathBuffer;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<CurrentPathNodeIndex> GetPathIndex;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<Translation> GetPosition;
        public float DeltaTime;

        public void Execute()
        {
            for (int index = 0; index < Paths.Length; index++)
            {
                var currentPathIndex = GetPathIndex[Paths[index]].Value;
                if (currentPathIndex > 0)
                {
                    var Position = GetPosition[Paths[index]];
                    var pathBuffer = GetPathBuffer[Paths[index]];
                    var float3Buffer = pathBuffer.Reinterpret<float3>();

                    var debugObj = new DebugPathView
                    {
                        Start = Position.Value
                    };
                    var tempList = new List<float3>(float3Buffer.ToNativeArray(Allocator.Temp).ToArray());
                    debugObj.Path = tempList.GetRange(0, currentPathIndex);
                    debugObj.Target = debugObj.Path[0];

                    /* DebugDrawPathData(
                        debugObj.Start,
                        debugObj.Target,
                        debugObj.Path
                    ); */
                }
            }
        }


    }

    public void shutdown()
    {
        this.EntityManager.CompleteAllJobs();
    }

    protected override void OnDestroy()
    {
        shutdown();
    }
}

public struct DebugPathView
{
    public float3 Start, Target;
    public List<float3> Path;
}
