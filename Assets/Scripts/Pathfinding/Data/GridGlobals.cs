
using Unity.Mathematics;

public static class GridGlobals
{
    private static int Width;
    private static int Height;
    private static float CellSize;

    public static void UpdateGridGlobalWidth(int width)
    {
        Width = width;
    }
    public static void UpdateGridGlobalHeight(int height)
    {
        Height = height;
    }
    public static void UpdateGridGlobalCellSize(float cellSize)
    {
        CellSize = cellSize;
    }
    public static void UpdateGridGlobals(int width, int height, float cellSize)
    {
        UpdateGridGlobalHeight(height);
        UpdateGridGlobalWidth(width);
        UpdateGridGlobalCellSize(cellSize);
    }

    public static int getGlobalGridWidth()
    {
        return Width;
    }
    public static int getGlobalGridHeight()
    {
        return Height;
    }
    public static float getGlobalGridCellSize()
    {
        return CellSize;
    }
}
