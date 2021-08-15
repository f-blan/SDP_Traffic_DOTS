using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Burst;
using CodeMonkey.Utils;

public class CarPathSystem : SystemBase
{
     private EndSimulationEntityCommandBufferSystem ecb_s;
     //this used to be a native hashmap, hence the name, but i later changed it to nativearray
     private NativeArray<PathNode> PathNodeMap;

     private bool isGraphValid;

    
    private const int MOVE_X_COST = 8;
    private const int MOVE_Y_COST = 7;
    
     protected override void OnCreate(){
        base.OnCreate();
        // Find the ECB system once and store it for later usage
        ecb_s = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        isGraphValid=false;
    }
    
     protected override void OnUpdate(){
        EntityCommandBuffer.ParallelWriter ecb = ecb_s.CreateCommandBuffer().AsParallelWriter();

        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(), Map_Setup.Instance.CityGraph.GetHeight());        
    
        //get the graph in a format usable by jobs (a native map of local struct PathNode, and do it only once per runtime: Allocator.Persistent used)
        if(isGraphValid==false){
            PathNodeMap = GetPathNodeMap(graphSize);
            isGraphValid=true;
        }

        NativeArray<PathNode> localPathNodeMap; 
        localPathNodeMap= PathNodeMap; //because if it's not local the compiler complains

        //compute path
        Entities.ForEach(( Entity entity, int entityInQueryIndex, ref CarPathParams carPathParams)=>{
            //make a copy of the graph hashmap exclusive to the job
            NativeArray<PathNode> tmpPathNodeMap = new NativeArray<PathNode>(localPathNodeMap, Allocator.Temp);
            PathFindFunction(tmpPathNodeMap, carPathParams.endPosition, carPathParams.startPosition, graphSize, entity, ecb, carPathParams.direction, carPathParams.init_cost, entityInQueryIndex);
            
            tmpPathNodeMap.Dispose();
        }).ScheduleParallel();
        //magical lifesaver line (probably delays and schedules all actions performed by ecb inside the forEach)
        ecb_s.AddJobHandleForProducer(this.Dependency);
        
     }

     protected override void OnDestroy(){
         //dispose PathNodeMap
         PathNodeMap.Dispose();
     }

    
     private NativeArray<PathNode> GetPathNodeMap(int2 graphSize){
        PathFindGraph CityGraph = Map_Setup.Instance.CityGraph;

        NativeArray<PathNode> pathNodeMap = new NativeArray<PathNode>(graphSize.x*graphSize.y, Allocator.Persistent);

        for(int x =0 ; x< graphSize.x; x++){
            for(int y =0 ; y < graphSize.y; ++y){
                PathNode pn = new PathNode();
                pn.x = x;
                pn.y = y;
                pn.index = CalculateIndex(x,y, graphSize.x);
                int[] goesTo = CityGraph.GetGraphNode(x,y).GetGoesTo();
                pn.goesTo = new int4(goesTo[0], goesTo[1], goesTo[2], goesTo[3]);
                pn.gCost = int.MaxValue;

                pn.cameFromNodeIndex = -1;
                pn.reachedWithDirection = -1;
                pn.reachedWithCost = -1;
                //reassign since pn is a reference
                pathNodeMap[pn.index] = pn;
            }
        }
        return pathNodeMap;
     }

    private static int CalculateIndex(int x, int y, int graphWidth) {
        return x + y * graphWidth;
    }
     private struct PathNode {
        public int x;
        public int y;

        public int index;

        //cost to reach
        public int gCost;
        //euristic cost to reach end point from here
        public int hCost;
        //sum of the two
        public int fCost;

        public int4 goesTo;

        public bool isWalkable;

        public int cameFromNodeIndex;
        public int reachedWithDirection;
        public int reachedWithCost;
        
        public void CalculateFCost() {
            fCost = gCost + hCost;
        }

        public void SetIsWalkable(bool isWalkable) {
            this.isWalkable = isWalkable;
        }
    }

    private static void PathFindFunction(NativeArray<PathNode> PathNodeMap, int2 endPosition, int2 startPosition, int2 graphSize, Entity entity, EntityCommandBuffer.ParallelWriter ecb, int direction, int init_cost, int eqi){
        //calculate heuristic cost for each node
            for(int t =0; t<PathNodeMap.Length; ++t){
                PathNode p = PathNodeMap[t];
                p.hCost = CalculateHCost(new int2(p.x, p.y), endPosition);
                p.cameFromNodeIndex=-1;

                //reassign since p is a reference
                PathNodeMap[t] = p;
            }
                //array of neighbour offset to get nearby graphnodes
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[3] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[0] = new int2(0, +1); // Up
            neighbourOffsetArray[2] = new int2(0, -1); // Down
            int startNodeIndex = CalculateIndex(startPosition.x, startPosition.y, graphSize.x); 
            int endNodeIndex = CalculateIndex(endPosition.x, endPosition.y, graphSize.x);

            //set start's gCost to 0
            PathNode startNode = PathNodeMap[startNodeIndex];
            startNode.gCost=0;
            startNode.CalculateFCost();
            PathNodeMap[startNodeIndex] = startNode;

           //list of discovered (but unporcessed) indexNodes
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            //list of processed indexNodes
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            //as long as we have unprocessed reachable nodes
            while(openList.Length >0){
                int currentNodeIndex = GetLowestCostFNodeIndex(openList, PathNodeMap);
                PathNode currentNode = PathNodeMap[currentNodeIndex];

                if(currentNodeIndex==endNodeIndex){
                    //pathfinding completed!
                    break;
                }
                //remove current node from open list and add to closed one
                for(int t=0; t<openList.Length;t++){
                    if(openList[t] == currentNodeIndex){
                        openList.RemoveAtSwapBack(t);
                        break;
                    }
                }
                closedList.Add(currentNodeIndex);

                for(int t =0; t<neighbourOffsetArray.Length; ++t){
                    //if neighbour is unreachable from current node (intersection)
                    if(currentNode.goesTo[t] == -1){
                        continue;
                    }
                    //get the neighboring node
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffsetArray[t].x, + currentNode.y + neighbourOffsetArray[t].y);
                    int neighBourNodeIndex = CalculateIndex(neighbourPosition.x, neighbourPosition.y, graphSize.x);

                    if(closedList.Contains(neighBourNodeIndex)){
                        //that node was already processed
                        continue;
                    }

                    PathNode neighbourNode = PathNodeMap[neighBourNodeIndex];
                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tentativeGCost = currentNode.gCost + currentNode.goesTo[t];

                    if(tentativeGCost<neighbourNode.gCost){
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.reachedWithDirection = t;
                        neighbourNode.reachedWithCost = currentNode.goesTo[t];
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();

                        //reassign 
                        PathNodeMap[neighBourNodeIndex] = neighbourNode;

                        if(!openList.Contains(neighbourNode.index)){
                            openList.Add(neighbourNode.index);
                        }
                    }

                }
            }

            PathNode endNode = PathNodeMap[endNodeIndex];
            AssignPath(entity, PathNodeMap, endNode, ecb, direction, init_cost,  eqi);
            
            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
            
    }


    private static void AssignPath(Entity entity, NativeArray<PathNode> pathNodeMap,PathNode endNode, EntityCommandBuffer.ParallelWriter ecb, int first_direction, int first_cost, int eqi){
        DynamicBuffer<CarPathBuffer> buf = ecb.AddBuffer<CarPathBuffer>(eqi,entity);
        //add end node to buffer
        buf.Add(new CarPathBuffer{x = endNode.x, y = endNode.y, cost = endNode.reachedWithCost, withDirection = endNode.reachedWithDirection});

        //add intermediate nodes
        PathNode cameFromNode = pathNodeMap[endNode.cameFromNodeIndex];
        PathNode currentNode = endNode;
        int nodes = 1;
        
        while(cameFromNode.cameFromNodeIndex!=-1){
            nodes++;
            currentNode = cameFromNode;
            buf.Add(new CarPathBuffer{x = currentNode.x, y = currentNode.y, cost = currentNode.reachedWithCost, withDirection = currentNode.reachedWithDirection});
            
            
            cameFromNode = pathNodeMap[currentNode.cameFromNodeIndex];
        }
        nodes++;
        //add start node
        buf.Add(new CarPathBuffer{x = cameFromNode.x, y = cameFromNode.y, cost = first_cost, withDirection = first_direction});
        
        //remove CarPathParams and add CarPathComponent
        ecb.RemoveComponent<CarPathParams>(eqi, entity);
        ecb.AddComponent<CarPathComponent>(eqi, entity);

        ecb.SetComponent<CarPathComponent>(eqi, entity, new CarPathComponent{pathIndex = nodes-1, direction = first_direction});
    }
    private static int GetLowestCostFNodeIndex(NativeList<int> openlist,NativeArray<PathNode> pathNodeMap ){
        PathNode lowestCostPathNode = pathNodeMap[openlist[0]];
        for(int t=1; t<openlist.Length;++t){
            PathNode testNode = pathNodeMap[openlist[t]];
            if(testNode.fCost<lowestCostPathNode.fCost){
                lowestCostPathNode=testNode;
            }
        }
        return lowestCostPathNode.index;
    }

    private static int CalculateHCost(int2 aPosition, int2 bPosition) {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        
        return MOVE_X_COST * xDistance + MOVE_Y_COST * yDistance;
    }
}


