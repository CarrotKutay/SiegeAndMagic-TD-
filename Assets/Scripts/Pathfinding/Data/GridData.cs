using Unity.Entities;
using Unity.Collections;

public struct GridData : IComponentData
{
    public int Width, Height;
    public float CellSize;
}
