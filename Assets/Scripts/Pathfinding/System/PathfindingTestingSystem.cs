using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

public struct TestGrid_NodeIndex : IJobParallelFor
{
    public NativeArray<PathfindingSystem.PathNode> Nodes;
    public void Execute(int index)
    {
        //node index test

        if (Nodes[index].Index != index)
        {
            Debug.Log("Node index of node at " + Nodes[index].Position.ToString() + "is incorrect"
                                    + "\nShould be '" + index + "' but it is '" + Nodes[index].Index + "'");
        }

    }
}

public struct TestGrid_ParentNodeIndex : IJobParallelFor
{
    public NativeArray<PathfindingSystem.PathNode> Nodes;

    public void Execute(int index)
    {
        if (Nodes[index].IndexOfParentNode != -1)
        {
            Debug.Log("Error at Parent Node Index" +
                    "\nCurrent Node Index: " + index +
                    " -> with Parent: " + Nodes[index].IndexOfParentNode
            );
        }
    }
}