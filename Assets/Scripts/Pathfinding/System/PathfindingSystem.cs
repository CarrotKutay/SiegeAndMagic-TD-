using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Transforms;
using System.Collections.Generic;
[UpdateInGroup(typeof(SimulationSystemGroup))]
public class PathfindingSystem : SystemBase
{
    public const int MOVE_COST_STRAIGHT = 10;
    public const int MOVE_COST_DIAGONAL = 14;
    public const int MOVE_COST_VERTICAL = 0;
    public PathNode[] Nodes;

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
        var GetPathElementBuffer = GetBufferFromEntity<PathElement>();
        var GetPathIndexComponent = GetComponentDataFromEntity<CurrentPathNodeIndex>();

        // initialize grid values
        var gridSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<GridSystem>();
        var gridWidth = gridSystem.GridWidth;
        var gridHeight = gridSystem.GridHeight;
        var gridCellSize = gridSystem.GridCellSize;


        if (Nodes != null)
        {

            UnityEngine.Debug.Log("Performing Pathfinding");
            var getParameters = GetComponentDataFromEntity<PathfindingParameters>();
            var NodeGrid = new NativeArray<PathNode>(Nodes, Allocator.TempJob);

            NativeArray<Entity> pathfindingObjects = GetEntityQuery(typeof(PathfindingParameters))
                .ToEntityArrayAsync(Allocator.TempJob, out JobHandle getPathfinders);

            var findingPathJob = new FindPath
            {
                Pathfinders = pathfindingObjects,
                Grid = NodeGrid,
                GridCellSize = gridCellSize,
                GridHeight = gridHeight,
                GridWidth = gridWidth,
                GetPathParams = getParameters,
                GetBuffer = GetPathElementBuffer,
                GetPathIndex = GetPathIndexComponent,
                ecb_Concurrent = entityCommandBuffer
            };

            var job = findingPathJob.Schedule(pathfindingObjects.Length, 1, getPathfinders);
            endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(job);

        }
    }

    public struct FindPath : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Pathfinders;
        [DeallocateOnJobCompletion]
        [NativeDisableParallelForRestriction]
        public NativeArray<PathNode> Grid;
        public int GridWidth, GridHeight;
        public float GridCellSize;
        //public float3 StartPosition, TargetPosition;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<PathfindingParameters> GetPathParams;
        [NativeDisableParallelForRestriction]
        public BufferFromEntity<PathElement> GetBuffer;
        [NativeDisableParallelForRestriction]
        public ComponentDataFromEntity<CurrentPathNodeIndex> GetPathIndex;
        public EntityCommandBuffer.Concurrent ecb_Concurrent;

        public void Execute(int index)
        {
            //lists to valuate nodes 
            var OpenList = new NativeList<int>(Allocator.Temp);
            var ClosedList = new NativeList<int>(Allocator.Temp);

            var parameters = GetPathParams[Pathfinders[index]];

            var startNode = Grid[GetNodeIndexFromWorldPosition(parameters.Start, GridWidth, GridHeight, GridCellSize)];
            var targetNode = Grid[GetNodeIndexFromWorldPosition(parameters.Target, GridWidth, GridHeight, GridCellSize)];

            startNode.GCost = 0;
            startNode.HCost = startNode.CalculateDistanceCostTo(targetNode.Position);
            startNode.CalculateFCost();

            Grid[startNode.Index] = startNode;

            OpenList.Add(startNode.Index);

            while (OpenList.Length > 0)
            {
                //UnityEngine.Debug.Log("jobs running");
                int currentNodeIndex = GetNodeIndexWithLowestFCost(OpenList, Grid);
                PathNode currentNode = Grid[currentNodeIndex];

                if (targetNode.Index == currentNodeIndex)
                {
                    //Found Path to target node!
                    break;
                }

                // remove current node index from openList
                if (OpenList.Contains(currentNodeIndex))
                {
                    OpenList.RemoveAtSwapBack(
                        OpenList.IndexOf(currentNodeIndex)
                    );
                }

                ClosedList.Add(currentNodeIndex);

                var yIndex = (int)(currentNodeIndex / GridWidth);
                var xIndex = currentNodeIndex - yIndex * GridWidth; // currentNodeIndex % gridWidth -> whatever is faster

                //UnityEngine.Debug.Log("x: " + xIndex + ", y: " + yIndex);

                // get Neighboring nodes
                for (int y = yIndex - 1; y < yIndex + 2; y++)
                {
                    if (y < 0 || y >= GridHeight) { continue; } // continue for indices which would lay above or beyond grid
                    for (int x = xIndex - 1; x < xIndex + 2; x++)
                    {
                        // continue for indices which would lay outside of grid (left and right)
                        // continue if the index matches the current nodes index
                        if (x < 0 || x >= GridWidth || (y == yIndex && x == xIndex)) { continue; }
                        //UnityEngine.Debug.Log("Checking neighbor out");

                        var neighboringNode = Grid[y * GridWidth + x];
                        var possibleNodeToEvaluate =
                                neighboringNode.Walkable && !ClosedList.Contains(neighboringNode.Index);

                        if (possibleNodeToEvaluate)
                        {
                            // evaluate node setting up G, H and FCost
                            var tentativeGCost = currentNode.GCost + neighboringNode.CalculateDistanceCostTo(parameters.Start);
                            if (tentativeGCost < neighboringNode.GCost)
                            {
                                neighboringNode.IndexOfParentNode = currentNodeIndex;
                                neighboringNode.GCost = tentativeGCost;
                                neighboringNode.HCost = neighboringNode.CalculateDistanceCostTo(parameters.Target);
                                neighboringNode.CalculateFCost();

                                Grid.ReinterpretStore(neighboringNode.Index, neighboringNode);

                                // add neighbor to OpenList for evaluation
                                OpenList.Add(neighboringNode.Index);
                            }
                        }
                    }
                }
            }

            var FinalPath = ecb_Concurrent.AddBuffer<PathElement>(index, Pathfinders[index]);
            var pathIndex = GetPathIndex[Pathfinders[index]];
            // create final path
            var iteratingNode = Grid[targetNode.Index];

            while (iteratingNode.IndexOfParentNode != -1)  // iterate through path
            {
                FinalPath.Add(new PathElement
                {
                    Position = iteratingNode.Position
                });
                iteratingNode = Grid[iteratingNode.IndexOfParentNode];
            }
            // add current Position as last node to start path from
            FinalPath.Add(new PathElement
            {
                Position = parameters.Start
            });
            //* The next node this obj needs to move towards is at length - 2
            //* length - 1 is the starting position this obj are already standing on
            pathIndex.Value = FinalPath.Length - 2;
            ecb_Concurrent.SetComponent<CurrentPathNodeIndex>(index, Pathfinders[index], pathIndex);
            ecb_Concurrent.RemoveComponent<PathfindingParameters>(index, Pathfinders[index]);
            ecb_Concurrent.AddComponent<PerformingMovement>(index, Pathfinders[index],
                new PerformingMovement { Value = true }
            );

            OpenList.Dispose();
            ClosedList.Dispose();
        }

        public int GetNodeIndexWithLowestFCost(NativeList<int> list, NativeArray<PathNode> nodesArray)
        {
            PathNode nodeWithLowestFCost = nodesArray[list[0]];
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
            var new_pos = Position + new float3(width / 2, 0, height / 2);
            int x = (int)(math.floor(new_pos.x / cellSize));
            int y = (int)(math.floor(new_pos.z / cellSize));
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
    }
}