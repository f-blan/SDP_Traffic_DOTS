using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
public static class PathUtils
{
    public static NativeArray<PathNode> GetPathNodeArray(){
        PathFindGraph CityGraph = Map_Setup.Instance.CityGraph;
        int2 graphSize = new int2(CityGraph.GetWidth(), CityGraph.GetHeight());
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
    public static int CalculateIndex(int x, int y, int graphWidth) {
        return x + y * graphWidth;
    }

    public static int2 CalculateBusStopCoords(int d_x, int d_y, int2 districtSize, int2 busStopRelativeCoords){
        int x = (d_x * districtSize.x) + busStopRelativeCoords.x;
        int y = (d_y * districtSize.y) + busStopRelativeCoords.y; 
        
        return new int2(x, y);
    }
    public struct PathNode {
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
    public static int GetLowestCostFNodeIndex(NativeList<int> openlist,NativeArray<PathNode> pathNodeMap ){
        


        PathNode lowestCostPathNode = pathNodeMap[openlist[0]];
        for(int t=1; t<openlist.Length;++t){
            PathNode testNode = pathNodeMap[openlist[t]];
            if(testNode.fCost<lowestCostPathNode.fCost){
                lowestCostPathNode=testNode;
            }
        }
        return lowestCostPathNode.index;
    }

    public static int CalculateHCost(int2 aPosition, int2 bPosition, int move_x_cost, int move_y_cost) {
        int xDistance = math.abs(aPosition.x - bPosition.x);
        int yDistance = math.abs(aPosition.y - bPosition.y);
        
        return move_x_cost * xDistance + move_y_cost * yDistance;
    }

    public static NativeArray<PathNode> GetDistrictNodeArray(int move_x_cost, int move_y_cost){
        PathFindGraph CityGraph = Map_Setup.Instance.CityGraph;
        Map<MapTile> CityMap = Map_Setup.Instance.CityMap;
        int2 graphSize = new int2(CityMap.GetNDistrictsX(), CityMap.GetNDistrictsY());

        NativeArray<PathNode> DistrictNodeArray = new NativeArray<PathNode>(graphSize.x*graphSize.y, Allocator.Persistent);

        for(int x =0 ; x< graphSize.x; x++){
            for(int y =0 ; y < graphSize.y; ++y){
                PathNode pn = new PathNode();
                pn.x = x;
                pn.y = y;
                pn.index = CalculateIndex(x,y, graphSize.x);
                
                pn.goesTo = new int4(move_x_cost, move_y_cost, move_x_cost, move_y_cost);
                pn.gCost = int.MaxValue;

                pn.cameFromNodeIndex = -1;
                pn.reachedWithDirection = -1;
                pn.reachedWithCost = -1;
                //reassign since pn is a reference
                DistrictNodeArray[pn.index] = pn;

            }
        }
        return DistrictNodeArray;
    }

    public static void PathFind(NativeArray<PathUtils.PathNode> graphArray, int2 endPosition, int2 startPosition, int2 graphSize,  int2 Hcosts){
            //calculate heuristic cost for each node
            for(int t =0; t< graphArray.Length; ++t){
                PathUtils.PathNode p = graphArray[t];
                p.hCost = PathUtils.CalculateHCost(new int2(p.x, p.y), endPosition, Hcosts.x, Hcosts.y);
                p.cameFromNodeIndex=-1;

                //reassign since p is a reference
                graphArray[t] = p;
            }
                //array of neighbour offset to get nearby graphnodes
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[3] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[0] = new int2(0, +1); // Up
            neighbourOffsetArray[2] = new int2(0, -1); // Down
            int startNodeIndex = PathUtils.CalculateIndex(startPosition.x, startPosition.y, graphSize.x); 
            int endNodeIndex = PathUtils.CalculateIndex(endPosition.x, endPosition.y, graphSize.x);

            //set start's gCost to 0
            PathUtils.PathNode startNode = graphArray[startNodeIndex];
            startNode.gCost=0;
            startNode.CalculateFCost();
            graphArray[startNodeIndex] = startNode;

           //list of discovered (but unporcessed) indexNodes
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            //list of processed indexNodes
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            //as long as we have unprocessed reachable nodes
            while(openList.Length >0){
                int currentNodeIndex = PathUtils.GetLowestCostFNodeIndex(openList, graphArray);
                PathUtils.PathNode currentNode = graphArray[currentNodeIndex];

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
                    int neighBourNodeIndex = PathUtils.CalculateIndex(neighbourPosition.x, neighbourPosition.y, graphSize.x);

                    if(closedList.Contains(neighBourNodeIndex)){
                        //that node was already processed
                        continue;
                    }

                    PathUtils.PathNode neighbourNode = graphArray[neighBourNodeIndex];
                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tentativeGCost = currentNode.gCost + currentNode.goesTo[t];

                    if(tentativeGCost<neighbourNode.gCost){
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.reachedWithDirection = t;
                        neighbourNode.reachedWithCost = currentNode.goesTo[t];
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();

                        //reassign 
                        graphArray[neighBourNodeIndex] = neighbourNode;

                        if(!openList.Contains(neighbourNode.index)){
                            openList.Add(neighbourNode.index);
                        }
                    }

                }
            }

            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
            return;

    }

    //variant of function PathFind but meant to be used for paths with multiple destination points (eg. BusPathFind)
    public static void PathFindPartial(NativeArray<PathUtils.PathNode> graphArray, int2 endPosition, int2 startPosition, int2 graphSize,  int2 Hcosts, NativeList<int> openList, NativeList<int> closedList,NativeArray<int2> neighbourOffsetArray){
            
            //calculate heuristic cost for each node
            for(int t =0; t< graphArray.Length; ++t){
                PathUtils.PathNode p = graphArray[t];
                p.hCost = PathUtils.CalculateHCost(new int2(p.x, p.y), endPosition, Hcosts.x, Hcosts.y);
                

                //reassign since p is a reference
                graphArray[t] = p;
            }

            int startNodeIndex = PathUtils.CalculateIndex(startPosition.x, startPosition.y, graphSize.x); 
            int endNodeIndex = PathUtils.CalculateIndex(endPosition.x, endPosition.y, graphSize.x);

            //set start's gCost to 0
            PathUtils.PathNode startNode = graphArray[startNodeIndex];
            startNode.gCost=0;
            startNode.CalculateFCost();
            graphArray[startNodeIndex] = startNode;

          

            openList.Add(startNode.index);

            //as long as we have unprocessed reachable nodes
            while(openList.Length >0){
                int currentNodeIndex = PathUtils.GetLowestCostFNodeIndex(openList, graphArray);
                PathUtils.PathNode currentNode = graphArray[currentNodeIndex];

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
                    int neighBourNodeIndex = PathUtils.CalculateIndex(neighbourPosition.x, neighbourPosition.y, graphSize.x);

                    if(closedList.Contains(neighBourNodeIndex)){
                        //that node was already processed
                        continue;
                    }

                    PathUtils.PathNode neighbourNode = graphArray[neighBourNodeIndex];
                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tentativeGCost = currentNode.gCost + currentNode.goesTo[t];

                    if(tentativeGCost<neighbourNode.gCost){
                        neighbourNode.cameFromNodeIndex = currentNodeIndex;
                        neighbourNode.reachedWithDirection = t;
                        neighbourNode.reachedWithCost = currentNode.goesTo[t];
                        neighbourNode.gCost = tentativeGCost;
                        neighbourNode.CalculateFCost();

                        //reassign 
                        graphArray[neighBourNodeIndex] = neighbourNode;

                        if(!openList.Contains(neighbourNode.index)){
                            openList.Add(neighbourNode.index);
                        }
                    }

                }
            }
            return;

    }

    public static void InitPartialData(NativeArray<PathUtils.PathNode> graphArray,out NativeArray<int2> neighbourOffsetArray,out NativeList<int> openList, out NativeList<int> closedList){
            
            neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[3] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[0] = new int2(0, +1); // Up
            neighbourOffsetArray[2] = new int2(0, -1); // Down

            //list of discovered (but unporcessed) indexNodes
            openList = new NativeList<int>(Allocator.Temp);
            //list of processed indexNodes
            closedList = new NativeList<int>(Allocator.Temp);
    }

    public static NativeList<int2> turnPathToInt2List(NativeArray<PathNode> graphArray, int2 graphSize, int2 pos){
        int index = CalculateIndex(pos.x, pos.y, graphSize.x);
        NativeList<int2> pathList = new NativeList<int2>(Allocator.Temp);
        

        int2 curPos = new int2(pos.x, pos.y);
        int curIndex = CalculateIndex(curPos.x,curPos.y, graphSize.x);
        PathNode curNode = graphArray[curIndex];
        do{
            pathList.Add(new int2(curPos.x, curPos.y));
            curNode = graphArray[curNode.cameFromNodeIndex];
            curPos.x = curNode.x;
            curPos.y = curNode.y;

        }while(curPos.x != pos.x && curPos.y != pos.y);

        return pathList;
    }

    
}
