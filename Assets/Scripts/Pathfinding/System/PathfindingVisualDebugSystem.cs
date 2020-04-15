using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;

public class PathfindingVisualDebugSystem : SystemBase
{
    private EntityManager manager;
    private List<Entity> OpenPaths;

    protected override void OnCreate()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;

        OpenPaths = new List<Entity>();
    }

    protected override void OnUpdate()
    {
        var pathParams = GetEntityQuery(typeof(VisualDebugData))
                .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getPathParams);
        getPathParams.Complete();

        foreach (var pathEntity in pathParams)
        {
            if (!OpenPaths.Contains(pathEntity))
            {
                OpenPaths.Add(pathEntity);
            }
        }
        createPathDebugObjects();

        var GetPathElementBuffer = GetBufferFromEntity<PathElement>();
        var openPaths = GetEntityQuery(typeof(PathElement))
                .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getFinalPaths);
        getFinalPaths.Complete();

        openPaths.Dispose();
        pathParams.Dispose();
    }

    private void createPathDebugObjects()
    {
        var getPathFromEntity = GetBufferFromEntity<PathElement>();
        for (int i = 0; i < OpenPaths.Count; i++)
        {
            var data = GetComponent<VisualDebugData>(OpenPaths[i]);
            var pathBuffer = getPathFromEntity[OpenPaths[i]];
            var float3Buffer = pathBuffer.Reinterpret<float3>();

            var visualDebugObejct = new GameObject("Visual Debug Path").AddComponent<VisualDebugMono>();
            visualDebugObejct.StartPosition = data.StartPosition;
            visualDebugObejct.TargetPosition = data.TargetPosition;
            visualDebugObejct.Path = new List<float3>();
            visualDebugObejct.Path.AddRange(float3Buffer.ToNativeArray(Allocator.Temp).ToArray());

            manager.DestroyEntity(OpenPaths[i]);
            OpenPaths.RemoveAt(i);
        }
    }

    public class VisualDebugMono : MonoBehaviour
    {
        public float3 StartPosition, TargetPosition;
        public List<float3> Path;

        private void Start()
        {
            Destroy(gameObject, 3f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawCube(StartPosition, Vector3.one);
            Gizmos.color = Color.red;
            Gizmos.DrawCube(TargetPosition, Vector3.one);
            Gizmos.color = Color.magenta;
            for (int i = 0; i < Path.Count - 1; i++)
            {
                Gizmos.DrawLine(Path[i], Path[i + 1]);
            }
        }
    }
}
