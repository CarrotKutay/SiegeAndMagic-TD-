/* using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

public class Pathfinding : SystemBase
{
    private EndSimulationEntityCommandBufferSystem esecbs;

    protected override void OnCreate()
    {
        esecbs = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var creationBuffer = esecbs.CreateCommandBuffer().ToConcurrent();
        var deletionBuffer = esecbs.CreateCommandBuffer().ToConcurrent();

        var getNodeBufferFromEntity = GetBufferFromEntity<NodeElement>(false);

        var gridWidth = GridGlobals.getGlobalGridWidth();
        var gridHeight = GridGlobals.getGlobalGridHeight();
        var gridLength = gridHeight * gridWidth;
        var gridCellSize = GridGlobals.getGlobalGridCellSize();

        var nodesEntityArray = GetEntityQuery(typeof(Node)).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getNodesJob);
        Dependency = JobHandle.CombineDependencies(this.Dependency, getNodesJob);
        CompleteDependency();

        // create data lists
        if (nodesEntityArray.Length > 0)
        {
            Entities.WithName("Setup_Pathfinding")
                .WithAll<SetupPathfindingTag>()
                .ForEach((int entityInQueryIndex, in Entity entity) =>
                {
                    deletionBuffer.RemoveComponent<SetupPathfindingTag>(entityInQueryIndex, entity);
                    Entity openListEntity = creationBuffer.CreateEntity(entityInQueryIndex);
                    Entity closedListEntity = creationBuffer.CreateEntity(entityInQueryIndex);

                    var openList = creationBuffer.AddBuffer<NodeElement>(entityInQueryIndex, openListEntity);
                    var closedList = creationBuffer.AddBuffer<NodeElement>(entityInQueryIndex, closedListEntity);

                    creationBuffer.SetComponent<PathfindingOpenList>(entityInQueryIndex, entity,
                        new PathfindingOpenList { Value = openListEntity }
                    );
                    creationBuffer.SetComponent<PathfindingClosedList>(entityInQueryIndex, entity,
                        new PathfindingClosedList { Value = closedListEntity }
                    );
                    var nodeBuffer = creationBuffer.AddBuffer<NodeElement>(entityInQueryIndex, entity);
                    nodeBuffer.ResizeUninitialized(gridLength);
                    for (int i = 0; i < nodesEntityArray.Length; i++)
                    {
                        nodeBuffer[i] = new NodeElement { Node = nodesEntityArray[i] };
                    }
                    creationBuffer.AddComponent<FindingPathTag>(entityInQueryIndex, entity, new FindingPathTag { Value = true });
                }).ScheduleParallel();
        }


        Entities.WithName("FindPath")
        .WithAll<FindingPathTag>()
        .ForEach(
            (int entityInQueryIndex, in Entity entity) =>
            {
                // retrieve nodeArray
                var nodeElements = getNodeBufferFromEntity[entity].ToNativeArray(Allocator.Temp);

                float3 startPosition = GetComponent<PathfindingStart>(entity).Value;
                float3 targetPosition = GetComponent<PathfindingTarget>(entity).Value;
                // retrieve start and target node (as entity) to find path between
                var startNodeEntity = nodeElements[GridGlobals.GetCellIndexFromWorldPosition(
                    startPosition, gridWidth, gridHeight, gridCellSize
                )].Node;
                var targetNodeEntity = nodeElements[GridGlobals.GetCellIndexFromWorldPosition(
                        targetPosition, gridWidth, gridHeight, gridCellSize
                    )].Node;

                var OpenList = getNodeBufferFromEntity[
                    GetComponent<PathfindingOpenList>(entity).Value
                ];
                var ClosedList = getNodeBufferFromEntity[
                    GetComponent<PathfindingClosedList>(entity).Value
                ];
                OpenList.Capacity = gridLength;
                ClosedList.Capacity = gridLength;

                // add startNodeEntity to OpenList to evaluate from as origin
                OpenList.Add(new NodeElement { Node = startNodeEntity });
                // setup startNodeEntity
                var startNode = GetComponent<Node>(startNodeEntity);
                startNode.GCost = 0;
                startNode.HCost = GridGlobals.GetDistanceBetween(
                    startPosition, targetPosition
                );
                startNode.FCost = startNode.GCost + startNode.HCost;

                while (OpenList.Length > 0)
                {
                    UnityEngine.Debug.Log(OpenList.Length.ToString());
                    var currentNodeEntity = OpenList[0].Node;
                    var currentNodeData = GetComponent<Node>(currentNodeEntity);
                    var index = 0;

                    // evaluate OpenList in search for the next best step for the final path towards the target
                    for (int i = 1; i < OpenList.Length; i++)
                    {
                        var nodeDataToEvaluateAgainst = GetComponent<Node>(OpenList[i].Node);

                        bool isNextStepToTargetNode = nodeDataToEvaluateAgainst.FCost < currentNodeData.FCost
                                                    || (nodeDataToEvaluateAgainst.FCost == currentNodeData.FCost
                                                    && nodeDataToEvaluateAgainst.GCost < currentNodeData.GCost);
                        if (isNextStepToTargetNode)
                        {
                            currentNodeEntity = OpenList[i].Node;
                            index = i;
                        }
                    }

                    if (currentNodeEntity.Equals(targetNodeEntity)) { break; }

                    var currentNodeIndex = GridGlobals.GetCellIndexFromWorldPosition(currentNodeData.Position.Value, gridWidth, gridHeight, gridCellSize);
                    var yIndex = (int)(currentNodeIndex / gridWidth);
                    var xIndex = currentNodeIndex - yIndex * gridWidth; // currentNodeIndex % gridWidth -> whatever is faster

                    // add current node entity to closed list as it can be ignored in all further evaluations
                    // as it has been evaluated now already                    
                    ClosedList.Add(new NodeElement
                    {
                        Node = currentNodeEntity
                    });
                    // remove current node from evaluations as it is included to be part of the final path
                    OpenList.RemoveAt(index);

                    // get Neighbouring node entities to current node entity
                    for (int y = yIndex - 1; y < yIndex + 2; y++)
                    {
                        if (y < 0 || y >= gridHeight) { continue; } // continue for indices which would lay above or beyond grid
                        for (int x = xIndex - 1; x < xIndex + 2; x++)
                        {
                            if (x < 0 || x >= gridWidth) { continue; } // continue for indices which would lay outside of grid (left and right)

                            var neighboringNodeEntity = nodeElements[y * gridWidth + x].Node;
                            var neighboringNodeData = GetComponent<Node>(neighboringNodeEntity);
                            bool possibleNodeToEvaluate = neighboringNodeData.Walkable;

                            if (possibleNodeToEvaluate)
                            {
                                // has this node already been evaluated before, if so we pass
                                // possibleNodeToEvaluate to false
                                // ? this seems costly is there a better way ?
                                foreach (var element in ClosedList)
                                {
                                    if (element.Node.Equals(neighboringNodeEntity))
                                    {
                                        possibleNodeToEvaluate = false;
                                        break;
                                    }
                                }
                            }
                            if (possibleNodeToEvaluate)
                            {
                                // evaluate node setting up G, H and FCost
                                var tentativeGCost = currentNodeData.GCost + GridGlobals.GetDistanceBetween(neighboringNodeData.Position.Value, startPosition);
                                if (tentativeGCost <= neighboringNodeData.GCost)
                                {
                                    neighboringNodeData.Parent = currentNodeEntity;
                                    neighboringNodeData.GCost = tentativeGCost;
                                    neighboringNodeData.HCost = GridGlobals.GetDistanceBetween(neighboringNodeData.Position.Value, targetPosition);
                                    neighboringNodeData.FCost = tentativeGCost + neighboringNodeData.HCost;
                                }
                            }
                        }
                    }
                } // end while -> all nodes in open list are evaluated //! no feature if there is no path possible yet

                // create final path
                var iterator = GetComponent<Node>(targetNodeEntity);
                var finalPath = creationBuffer.AddBuffer<NodeElement>(entityInQueryIndex, entity);
                if (iterator.Parent != null) // check if there is a final path found
                {
                    finalPath.Add(new NodeElement
                    {
                        Node = targetNodeEntity
                    });
                }
                while (iterator.Parent != null)  // iterate through path
                {
                    finalPath.Add(new NodeElement
                    {
                        Node = iterator.Parent
                    });
                    iterator = GetComponent<Node>(iterator.Parent);
                }

                deletionBuffer.RemoveComponent<FindingPathTag>(entityInQueryIndex, entity);
                nodeElements.Dispose();
            }
        )
        .WithNativeDisableParallelForRestriction(getNodeBufferFromEntity)
        .ScheduleParallel();

        esecbs.AddJobHandleForProducer(this.Dependency);
        this.CompleteDependency();
        nodesEntityArray.Dispose();
    }
} */