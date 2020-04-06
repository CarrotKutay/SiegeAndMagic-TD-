using Unity.Collections;
using Unity.Entities;

public struct GridData : IComponentData
{
    public int Width, Height;
    public float CellSize;
}


