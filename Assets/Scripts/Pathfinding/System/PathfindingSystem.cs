using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public class PathfindingSystem : SystemBase
{
    public const int MOVE_COST_STRAIGHT = 10;
    public const int MOVE_COST_DIAGONAL = 14;
    public const int MOVE_COST_VERTICAL = 0;

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        // initialize grid globals values 
        var gridWidth = GridGlobals.getGlobalGridWidth();
        var gridHeight = GridGlobals.getGlobalGridHeight();
        var gridCellSize = GridGlobals.getGlobalGridCellSize();

        // initialize grid to calculate path
        NativeArray<Entity> nodes = GetEntityQuery(typeof(Node)).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getNodeEntities);
        NativeArray<Entity> pathfinders = GetEntityQuery(typeof(PathfindingParameters))
                                            .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getPathfinderEntities);
        JobHandle setup = JobHandle.CombineDependencies(getNodeEntities, getPathfinderEntities);

        FindPath findPathJob = new FindPath()
        {
            Pathfinders = pathfinders,
            Grid = nodes
        };

        var findPathJobHandle = findPathJob.Schedule(pathfinders.Length, 1, setup);


        Dependency = JobHandle.CombineDependencies(Dependency, findPathJobHandle);
        this.CompleteDependency();
    }

    public struct FindPath : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Grid;
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Pathfinders;
        public void Execute(int index)
        {
            //throw new System.NotImplementedException();
        }
    }

    public struct PathNode
    {
        ///<summary>
        /// Index of this node. Functions as identifier
        ///</summary>
        public int Index;
        ///<summary>
        /// WorldPosition of this Node
        ///</summary>
        public float3 Position;
        ///<summary>
        /// Distance to start position of the path to find between 2 points
        ///</summary>
        public int GCost;
        ///<summary>
        /// Distance to target position of the path to find between 2 points
        ///</summary>
        public int HCost;
        ///<summary>
        /// Combined MoveCost needed for this node (GCost + HCost)
        ///</summary>
        public int FCost;
        public bool Walkable;
        ///<summary>
        /// Cannot link directly between Value types (this node is a value type)
        /// Therefore, we will use the index of each Node to link to its parent
        ///</summary>
        public int IndexOfParentNode;

        public void CalculateFCost()
        {
            FCost = GCost + HCost;
        }
        ///<summary>
        /// Measures the direct Distance between this <see cref="PathNode"/> and the <paramref name="targetNodePosition"/>
        /// and calculates the Distance cost between them. Usefull to caluculate the
        /// <see cref="GCost"/> and <see cref="HCost"/> of this Node
        ///</summary>
        public int CalculateDistanceCostTo(float3 targetNodePosition)
        {
            var longlineCost = (int)math.abs(Position.x - targetNodePosition.x) * MOVE_COST_STRAIGHT;
            var diagonalCost = (int)math.abs(Position.z - targetNodePosition.z) * MOVE_COST_DIAGONAL;
            var verticalCost = (int)math.abs(Position.y - targetNodePosition.y) * MOVE_COST_VERTICAL;

            return longlineCost + diagonalCost + verticalCost;
        }

        public int GetNodeIndexFromWorldPosition(float3 Position, int width, int height, float cellSize)
        {
            int x = math.abs((int)(math.floor((Position.x + width / 2) / cellSize)));
            int y = math.abs((int)(math.floor((Position.z + height / 2) / cellSize)));
            return y * width + x;
        }

        ///<summary>
        /// Resets Movement related values of this node back to initialization values
        ///</summary>
        public void cleanNode()
        {
            GCost = int.MaxValue;
            HCost = int.MaxValue;
            FCost = 0;
            IndexOfParentNode = -1;
        }

    }
}