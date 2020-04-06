
using Unity.Mathematics;

public static class GridGlobals
{
    private static int WIDTH;
    private static int HEIGHT;
    private static float CELLSIZE;

    public static void UpdateGridGlobalWidth(int width)
    {
        WIDTH = width;
    }
    public static void UpdateGridGlobalHeight(int height)
    {
        HEIGHT = height;
    }
    public static void UpdateGridGlobalCellSize(float cellSize)
    {
        CELLSIZE = cellSize;
    }
    public static void UpdateGridGlobals(int width, int height, float cellSize)
    {
        UpdateGridGlobalHeight(height);
        UpdateGridGlobalWidth(width);
        UpdateGridGlobalCellSize(cellSize);
    }

    public static int getGlobalGridWidth()
    {
        return WIDTH;
    }
    public static int getGlobalGridHeight()
    {
        return HEIGHT;
    }
    public static float getGlobalGridCellSize()
    {
        return CELLSIZE;
    }

    public static int GetCellIndexFromWorldPosition(float3 Position)
    {
        int x = math.abs((int)(math.floor((Position.x + WIDTH / 2) / CELLSIZE)));
        int y = math.abs((int)(math.floor((Position.z + HEIGHT / 2) / CELLSIZE)));

        return y * WIDTH + x;
    }
}
