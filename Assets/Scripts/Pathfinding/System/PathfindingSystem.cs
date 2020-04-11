using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;

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
        // get entityCommandBuffer.ToConcurrent()
        var entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent();
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // initialize grid globals values 
        var gridWidth = GridGlobals.getGlobalGridWidth();
        var gridHeight = GridGlobals.getGlobalGridHeight();
        var gridCellSize = GridGlobals.getGlobalGridCellSize();

        NativeArray<PathNode> nodeGrid = new NativeArray<PathNode>(gridWidth * gridHeight, Allocator.TempJob);

        // perform action on all Entities that still need initialization and contain GridData
        var gridCreationHandle = Entities
            .WithAll<InitializeGridTag>()
            .WithName("Grid_Initialization")
            .ForEach(
            (int entityInQueryIndex, in GridData data, in Translation Position, in Entity entity) =>
            {
                // get GridData and Position outside of For-Loops to save resources
                float3 gridPosition = Position.Value;

                // create Entities containing Nodes (node data)
                for (int y = 0; y < data.Height; y++)
                {
                    for (int x = 0; x < data.Width; x++)
                    {
                        var node = new PathNode
                        {
                            IndexOfParentNode = -1,
                            Position = gridPosition
                                            + new float3(0, 0, data.CellSize * y)
                                            + new float3(data.CellSize * x, 0, 0),
                            GCost = int.MaxValue,
                            HCost = int.MaxValue,
                            FCost = 0,
                            Walkable = true,
                            Index = y * data.Width + x
                        };
                        nodeGrid[node.Index] = node;
                    }
                }

                entityCommandBuffer.RemoveComponent<InitializeGridTag>(entityInQueryIndex, entity);

            }).ScheduleParallel(Dependency);
        gridCreationHandle.Complete();

        var addFinalPathBuffer = Entities
            .WithName("Adding_Buffer_FinalPath")
            .WithAll<PathfindingParameters>()
                .ForEach(
                    (int entityInQueryIndex, in Entity entity) =>
                    {
                        var buffer = entityCommandBuffer.AddBuffer<PathElement>(entityInQueryIndex, entity);
                        buffer.Capacity = (gridWidth * gridHeight);
                    }
                ).ScheduleParallel(Dependency);
        addFinalPathBuffer.Complete();

        var GetPathBufferFromEntity = GetBufferFromEntity<PathElement>();

        var pathfindingHandle = Entities.WithName("Start_Pathfinding_Jobs")
                .ForEach(
                    (int entityInQueryIndex, in PathfindingParameters parameters, in Entity entity) =>
                    {
                        var findingPathJobs = new FindPath
                        {
                            Pathfinder = entity,
                            Grid = nodeGrid,
                            GridCellSize = gridCellSize,
                            GridHeight = gridHeight,
                            GridWidth = gridWidth,
                            StartPosition = parameters.Start,
                            TargetPosition = parameters.Target,
                            GetPathBuffer = GetPathBufferFromEntity
                        };
                    }
                )
                .WithNativeDisableParallelForRestriction(GetPathBufferFromEntity)
                .ScheduleParallel(Dependency);

        pathfindingHandle.Complete();
        nodeGrid.Dispose();
    }


    public struct FindPath : IJob
    {
        public Entity Pathfinder;
        public NativeArray<PathNode> Grid;
        public int GridWidth, GridHeight;
        public float GridCellSize;
        public float3 StartPosition, TargetPosition;
        public BufferFromEntity<PathElement> GetPathBuffer;
        public DynamicBuffer<PathElement> FinalPath;

        public void Execute()
        {
            FinalPath = GetPathBuffer[Pathfinder];

            //lists to valuate nodes 
            var OpenList = new NativeList<int>(Allocator.Temp);
            var ClosedList = new NativeList<int>(Allocator.Temp);

            var startNode = Grid[GetNodeIndexFromWorldPosition(StartPosition, GridWidth, GridHeight, GridCellSize)];
            var targetNode = Grid[GetNodeIndexFromWorldPosition(TargetPosition, GridWidth, GridHeight, GridCellSize)];

            startNode.GCost = 0;
            startNode.HCost = startNode.CalculateDistanceCostTo(targetNode.Position);
            startNode.CalculateFCost();

            Grid[startNode.Index] = startNode;

            OpenList.Add(startNode.Index);

            /* while (OpenList.Length > 0)
            {
                int currentNodeIndex = GetNodeIndexWithLowestFCost(OpenList, Grid);
                PathNode currentNode = Grid[currentNodeIndex];

                if (targetNode.Index == currentNodeIndex)
                {
                    // reached target 
                    break;
                }

                // remove current node index from openList
                for (int i = 0; i < OpenList.Length; i++)
                {
                    if (OpenList[i] == currentNodeIndex)
                    {
                        OpenList.RemoveAtSwapBack(i);
                        break;
                    }
                }

                ClosedList.Add(currentNodeIndex);

                var yIndex = (int)(currentNodeIndex / GridWidth);
                var xIndex = currentNodeIndex - yIndex * GridWidth; // currentNodeIndex % gridWidth -> whatever is faster

                // get Neighboring nodes
                for (int y = yIndex - 1; y < yIndex + 2; y++)
                {
                    if (y < 0 || y >= GridHeight) { continue; } // continue for indices which would lay above or beyond grid
                    for (int x = xIndex - 1; x < xIndex + 2; x++)
                    {
                        if (x < 0 || x >= GridWidth) { continue; } // continue for indices which would lay outside of grid (left and right)

                        var neighboringNode = Grid[y * GridWidth + x];
                        var possibleNodeToEvaluate =
                                neighboringNode.Walkable && !ClosedList.Contains(neighboringNode.Index);

                        if (possibleNodeToEvaluate)
                        {
                            // evaluate node setting up G, H and FCost
                            var tentativeGCost = currentNode.GCost + neighboringNode.CalculateDistanceCostTo(StartPosition);
                            if (tentativeGCost < neighboringNode.GCost)
                            {
                                neighboringNode.IndexOfParentNode = currentNodeIndex;
                                neighboringNode.GCost = tentativeGCost;
                                neighboringNode.HCost = neighboringNode.CalculateDistanceCostTo(TargetPosition);
                                neighboringNode.CalculateFCost();
                            }
                        }
                    }
                }
            } */

            // create final path
            var iterator = targetNode;

            while (iterator.IndexOfParentNode >= 0)  // iterate through path
            {
                FinalPath.Add(new PathElement
                {
                    Position = iterator.Position
                });
                iterator = Grid[iterator.IndexOfParentNode];
            }

            OpenList.Dispose();
            ClosedList.Dispose();
        }

        public int GetNodeIndexWithLowestFCost(NativeList<int> list, NativeArray<PathNode> nodesArray)
        {
            PathNode nodeWithLowestFCost = nodesArray[0];
            for (int i = 1; i < list.Length; i++)
            {
                PathNode comparingNode = nodesArray[list[i]];
                if (comparingNode.FCost < nodeWithLowestFCost.FCost
                    || (comparingNode.FCost < nodeWithLowestFCost.FCost && comparingNode.GCost < nodeWithLowestFCost.GCost))
                {
                    nodeWithLowestFCost = comparingNode;
                }
            }
            return nodeWithLowestFCost.Index;
        }

        public int GetNodeIndexFromWorldPosition(float3 Position, int width, int height, float cellSize)
        {
            int x = math.abs((int)(math.floor((Position.x + width / 2) / cellSize)));
            int y = math.abs((int)(math.floor((Position.z + height / 2) / cellSize)));
            return y * width + x;
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