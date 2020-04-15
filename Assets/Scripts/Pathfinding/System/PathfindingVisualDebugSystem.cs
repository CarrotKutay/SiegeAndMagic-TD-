using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Transforms;

public class PathfindingVisualDebugSystem : SystemBase
{
    private EntityQueryDesc desc;
    private EntityManager manager;
    private DebugPathView VisualDebugObj;
    private float TimeForNextUpdate = 0;

    protected override void OnCreate()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        desc = new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(PerformingMovement), typeof(PathElement) }
        };
    }

    protected override void OnUpdate()
    {
        if (Time.ElapsedTime > TimeForNextUpdate)
        {
            TimeForNextUpdate += Time.DeltaTime;
            var pathParams = GetEntityQuery(desc)
                    .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getPathParams);
            getPathParams.Complete();

            foreach (var pathEntity in pathParams)
            {
                createPathDebugObjects(pathEntity);
            }

            pathParams.Dispose();
        }
    }

    private void createPathDebugObjects(Entity pathObj)
    {
        var getPathFromEntity = GetBufferFromEntity<PathElement>();
        var currentPathIndex = GetComponent<CurrentPathNodeIndex>(pathObj).Value;

        if (currentPathIndex > 0)
        {
            var Position = GetComponent<Translation>(pathObj);
            var pathBuffer = getPathFromEntity[pathObj];
            var float3Buffer = pathBuffer.Reinterpret<float3>();

            VisualDebugObj = new DebugPathView
            {
                Start = Position.Value
            };
            var tempList = new List<float3>(float3Buffer.ToNativeArray(Allocator.Temp).ToArray());
            VisualDebugObj.Path = tempList.GetRange(0, currentPathIndex);
            VisualDebugObj.Target = VisualDebugObj.Path[0];

            DebugDrawPathData(
                VisualDebugObj.Start,
                VisualDebugObj.Target,
                VisualDebugObj.Path
            );
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
}

public struct DebugPathView
{
    public float3 Start, Target;
    public List<float3> Path;
}
