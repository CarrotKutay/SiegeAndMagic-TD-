using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct LightningPhysics : IJobParallelFor
{
    public NativeList<LightningData>.ParallelWriter dataList;
    public int numberOfPositions;
    public int peak; // peak middle point between all positions of the Lightning Arc
    public float gravityFloatingMultiplier; // how much will the position change
    public Vector3 gravityFloatingDirection; // to which direction will the position change

    public void Execute(int index)
    {
        if (index != 0 && index != numberOfPositions - 1)
        {
            int indexMultiplier = index < peak ? index : peak - (index - peak);

            // test for correct indexing, will we need a cast to uint? 
            if (indexMultiplier < 0) Debug.Log("indexMuliplier is negative ("
                                                + indexMultiplier
                                                + ") for position at index: "
                                                + index);

            Vector3 direction = gravityFloatingMultiplier * indexMultiplier / peak * gravityFloatingDirection;

            dataList.AddNoResize(
                new LightningData()
                {
                    direction = direction,
                    index = index
                }
            );
        }
    }
}

public struct LightningData
{
    public Vector3 direction;
    public int index;
    public bool RunTestOnData(int limit)
    {
        bool test = true;
        test = index == 0 ? false : true;
        test = index == limit ? false : true;
        if (!test) Debug.Log("index: " + index);
        return test;
    }
}

// ? not sure it is possible to find a way to move positions outside of mai thread via IJobPrallelFor 
// ? while not using ECS (which is not possible as using LineRenderer is not supported in ECS yet)

/* public struct MoveLightningPositions : IJobParallelFor
{
    public NativeArray<Translation> oldPositions; // positions to move from
    public NativeArray<Translation> newPositions; // positions to move to

    public float deltaTime; // time each frame takes
    public float duration; // duration time of the movement
    public void Execute(int index)
    {
        while (duration > 0)
        {
            oldPositions[index].Value
        }
    }
} */
