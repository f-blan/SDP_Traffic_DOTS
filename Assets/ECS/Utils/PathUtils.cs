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
                pn.lastIteration=0;

                pn.isBusStop=false;
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
    public static Vector3 GetWorldPosition(int x, int y, Vector3 originPosition) {
        return new Vector3(x, y, 0) * 1f + originPosition;
    }

    public static Vector3 CalculateStopWorldPosition(int d_x, int d_y, int2 districtSizeTiles, int2 busStopRelativeCoords, int2 busStopRelativePosition, Vector3 originPosition){
        int2 busStopCoords = CalculateBusStopCoords(d_x,d_y,districtSizeTiles,busStopRelativePosition);
        

        return GetWorldPosition(busStopCoords.x, busStopCoords.y, originPosition);
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
        
        //needed to tell how i have to consider this node during the current partial pathfind iteration (blank or traversed) 
        public int lastIteration;

        public int4 goesTo;

        //public bool isWalkable;
        public bool isBusStop;

        public int cameFromNodeIndex;
        public int reachedWithDirection;
        public int reachedWithCost;
        
        public void CalculateFCost() {
            fCost = gCost + hCost;
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

    public static int GetLowestCostFNodeIndex(NativeList<int> openlist,NativeHashMap<int,PathNode> pathNodeMap ){
        



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
                pn.lastIteration=0;
                pn.goesTo = new int4(move_x_cost, move_y_cost, move_x_cost, move_y_cost);
                pn.gCost = int.MaxValue;

                if(x == 0){
                    pn.goesTo[3]=-1;
                }
                if(x == graphSize.x-1){
                    pn.goesTo[1]=-1;
                }
                if(y == 0){
                    pn.goesTo[2]=-1;
                }
                if(y == graphSize.y-1){
                    pn.goesTo[0]=-1;
                }

                pn.isBusStop=false;
                pn.cameFromNodeIndex = -1;
                pn.reachedWithDirection = -1;
                pn.reachedWithCost = -1;
                //reassign since pn is a reference
                DistrictNodeArray[pn.index] = pn;

            }
        }

        

        

        
        return DistrictNodeArray;
    }

    public static void PathFind(NativeArray<PathUtils.PathNode> graphArray,NativeHashMap<int, PathUtils.PathNode> graphMap, int2 endPosition, int2 startPosition, int2 graphSize,  int2 Hcosts){
            /*//calculate heuristic cost for each node
            for(int t =0; t< graphArray.Length; ++t){
                PathUtils.PathNode p = graphArray[t];
                p.hCost = PathUtils.CalculateHCost(new int2(p.x, p.y), endPosition, Hcosts.x, Hcosts.y);
                p.cameFromNodeIndex=-1;
               
                p.reachedWithCost=-1;
                p.reachedWithDirection=-1;
            
                p.gCost = int.MaxValue;
                //reassign since p is a reference
                graphArray[t] = p;
            }*/
                //array of neighbour offset to get nearby graphnodes
            NativeArray<int2> neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[3] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[0] = new int2(0, +1); // Up
            neighbourOffsetArray[2] = new int2(0, -1); // Down
            int startNodeIndex = PathUtils.CalculateIndex(startPosition.x, startPosition.y, graphSize.x); 
            int endNodeIndex = PathUtils.CalculateIndex(endPosition.x, endPosition.y, graphSize.x);

            //set start's gCost to 0
            PathUtils.PathNode startNode = CopyPathNode(graphArray[startNodeIndex]);
            startNode.hCost = CalculateHCost(new int2(startNode.x,startNode.y), endPosition,Hcosts.x, Hcosts.y );
            startNode.gCost=0;
            startNode.CalculateFCost();
            AddNodeToMap(graphMap, startNode);

           //list of discovered (but unporcessed) indexNodes
            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            //list of processed indexNodes
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);

            openList.Add(startNode.index);

            //as long as we have unprocessed reachable nodes
            while(openList.Length >0){
                int currentNodeIndex = PathUtils.GetLowestCostFNodeIndex(openList, graphMap);
                PathUtils.PathNode currentNode = graphMap[currentNodeIndex];

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
                    PathUtils.PathNode neighBourNode;
                    if(!openList.Contains(neighBourNodeIndex)){
                        neighBourNode = CopyPathNode(graphArray[neighBourNodeIndex]);
                        neighBourNode.hCost = CalculateHCost(new int2(neighBourNode.x, neighBourNode.y), endPosition, Hcosts.x, Hcosts.y );
                    }else{
                        neighBourNode = graphMap[neighBourNodeIndex];
                    }

                    
                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tentativeGCost = currentNode.gCost + currentNode.goesTo[t];

                    if(tentativeGCost<neighBourNode.gCost){
                        neighBourNode.cameFromNodeIndex = currentNodeIndex;
                        neighBourNode.reachedWithDirection = t;
                        neighBourNode.reachedWithCost = currentNode.goesTo[t];
                        neighBourNode.gCost = tentativeGCost;
                        neighBourNode.CalculateFCost();

                        //reassign 
                        //graphArray[neighBourNodeIndex] = neighbourNode;

                        if(!openList.Contains(neighBourNode.index)){
                            openList.Add(neighBourNode.index);
                            AddNodeToMap(graphMap, neighBourNode);
                        }else{
                            UpdateNodeToMap(graphMap, neighBourNode);
                        }
                    }

                }
            }
            
            //Debug.Log(graphMap[endNodeIndex].cameFromNodeIndex);
            neighbourOffsetArray.Dispose();
            openList.Dispose();
            closedList.Dispose();
            return;

    }

    //variant of function PathFind but meant to be used for paths with multiple destination points (eg. BusPathFind)
    public static int PathFindPartial(NativeArray<PathUtils.PathNode> graphArray,NativeHashMap<int, PathNode> graphMap, int2 endPosition, int2 startPosition, int2 graphSize,  int2 Hcosts, NativeArray<int2> neighbourOffsetArray,
        int lastDirection, int firstDirection, int firstNodeIndex){
            /*
            //reset the map
            for(int t =0; t< graphArray.Length; ++t){
                PathUtils.PathNode p = graphArray[t];
                p.hCost = PathUtils.CalculateHCost(new int2(p.x, p.y), endPosition, Hcosts.x, Hcosts.y);
                p.reachedWithCost=-1;
                p.reachedWithDirection=-1;
                p.cameFromNodeIndex=-1;
                p.gCost = int.MaxValue;

                //reassign since p is a reference
                graphArray[t] = p;
            }*/
            graphMap.Clear();

            NativeList<int> openList = new NativeList<int>(Allocator.Temp);
            NativeList<int> closedList = new NativeList<int>(Allocator.Temp);
            int startNodeIndex = PathUtils.CalculateIndex(startPosition.x, startPosition.y, graphSize.x); 
            int endNodeIndex = PathUtils.CalculateIndex(endPosition.x, endPosition.y, graphSize.x);
            //int firstStartIndex = PathUtils.CalculateIndex(firstStartPosition.x, firstStartPosition.y, graphSize.x);

            //set start's gCost to 0
            PathUtils.PathNode startNode = CopyPathNode(graphArray[startNodeIndex]);
            startNode.hCost = CalculateHCost(new int2(startNode.x,startNode.y), endPosition,Hcosts.x, Hcosts.y );
            startNode.gCost=0;
            startNode.CalculateFCost();
            AddNodeToMap(graphMap, startNode);

          

            openList.Add(startNode.index);
            int i=0;
            //as long as we have unprocessed reachable nodes
            while(openList.Length >0){
                int currentNodeIndex = PathUtils.GetLowestCostFNodeIndex(openList, graphMap);
                PathUtils.PathNode currentNode = graphMap[currentNodeIndex];

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
                    if(i==0 && (lastDirection+2)%4 == t){
                        //for buses we avoid U turns after reaching an edge node between the several computed paths 
                        
                        continue;
                    }
                    //get the neighboring node
                    int2 neighbourPosition = new int2(currentNode.x + neighbourOffsetArray[t].x, + currentNode.y + neighbourOffsetArray[t].y);
                    int neighBourNodeIndex = PathUtils.CalculateIndex(neighbourPosition.x, neighbourPosition.y, graphSize.x);

                    if(neighBourNodeIndex == firstNodeIndex && t == (firstDirection+2)%4){
                        //we avoid having the same direction for the very first node in the path
                        continue;
                    }

                    if(closedList.Contains(neighBourNodeIndex) && !(endNodeIndex == neighBourNodeIndex)){
                        //that node was already processed
                        //Debug.Log("continued");
                        
                        
                        
                        continue;
                    }

                    PathUtils.PathNode neighBourNode;
                     if(!openList.Contains(neighBourNodeIndex)){
                        neighBourNode = CopyPathNode(graphArray[neighBourNodeIndex]);
                        neighBourNode.hCost = CalculateHCost(new int2(neighBourNode.x, neighBourNode.y), endPosition, Hcosts.x, Hcosts.y );
                    }else{
                        neighBourNode = graphMap[neighBourNodeIndex];
                    }

                    int2 currentNodePosition = new int2(currentNode.x, currentNode.y);
                    int tentativeGCost = currentNode.gCost + currentNode.goesTo[t];

                    if(tentativeGCost<neighBourNode.gCost){
                        neighBourNode.cameFromNodeIndex = currentNodeIndex;
                        neighBourNode.reachedWithDirection = t;
                        neighBourNode.reachedWithCost = currentNode.goesTo[t];
                        neighBourNode.gCost = tentativeGCost;
                        neighBourNode.CalculateFCost();

                        //reassign 
                        //graphArray[neighBourNodeIndex] = neighbourNode;

                        if(!openList.Contains(neighBourNode.index)){
                            openList.Add(neighBourNode.index);
                            AddNodeToMap(graphMap, neighBourNode);
                        }else{
                            UpdateNodeToMap(graphMap, neighBourNode);
                        }
                    }

                }
                i++;
            }
            openList.Dispose();
            closedList.Dispose();
            if(graphMap[endNodeIndex].cameFromNodeIndex==-1){
                Debug.Log("ALERT PATH NOT FOUND. THIS SHOULD NOT BE POSSIBLE");
            }

            return graphMap[endNodeIndex].reachedWithDirection;

    }


    public static void InitPartialData(out NativeArray<int2> neighbourOffsetArray){
            
            neighbourOffsetArray = new NativeArray<int2>(4, Allocator.Temp);
            neighbourOffsetArray[3] = new int2(-1, 0); // Left
            neighbourOffsetArray[1] = new int2(+1, 0); // Right
            neighbourOffsetArray[0] = new int2(0, +1); // Up
            neighbourOffsetArray[2] = new int2(0, -1); // Down

            
    }

    public static void addPathToInt2List(NativeList<int2> pathList,NativeHashMap<int,PathNode> graphMap, int2 graphSize, int2 endPos){
        
        
        /*
        Debug.Log("array");
        Debug.Log(graphArray[0].cameFromNodeIndex);
        Debug.Log(graphArray[1].cameFromNodeIndex);
        Debug.Log(graphArray[2].cameFromNodeIndex);
        Debug.Log(graphArray[3].cameFromNodeIndex);
        */
       
        NativeList<int2> supportList = new NativeList<int2>(Allocator.Temp);

        int curIndex = CalculateIndex(endPos.x,endPos.y, graphSize.x);
        
        PathNode curNode = graphMap[curIndex];
        

        //curNode = graphArray[curNode.cameFromNodeIDndex];
        while(curNode.cameFromNodeIndex != -1){
            curNode = graphMap[curNode.cameFromNodeIndex];
            supportList.Add(new int2(curNode.x, curNode.y));
        }

        //supportList.Add(new int2(cameFromNode.x, cameFromNode.y));
        
        for(int t=supportList.Length-1; t>=0; --t){
            pathList.Add(supportList[t]);
        }

        supportList.Dispose();
    }

    private static PathNode CopyPathNode(PathNode p){
        return new PathNode{x = p.x, y = p.y, index = p.index, 
            gCost = p.gCost, hCost = p.hCost, fCost = p.fCost, 
            goesTo = new int4(p.goesTo[0],p.goesTo[1],p.goesTo[2],p.goesTo[3]),
            cameFromNodeIndex = p.cameFromNodeIndex, reachedWithCost = p.reachedWithCost, reachedWithDirection = p.reachedWithDirection,
            isBusStop = p.isBusStop};
    }

    private static void AddNodeToMap(NativeHashMap<int, PathUtils.PathNode> graphMap, PathNode p){
        if(graphMap.Count() == graphMap.Capacity){
            graphMap.Capacity = graphMap.Capacity*2;
        }

        graphMap.TryAdd(p.index, p);
    }

    private static void UpdateNodeToMap(NativeHashMap<int, PathUtils.PathNode> graphMap, PathNode p){
        graphMap.Remove(p.index);
        graphMap.TryAdd(p.index, p);
    }

    public static int CalculatePrevNodeIndex(int x, int y, int curDirection, int graphWidth){
        switch(curDirection){
            case 0:
                return CalculateIndex(x, y-1, graphWidth);
            case 1:
                return CalculateIndex(x-1,y,graphWidth);
            case 2:
                return CalculateIndex(x,y+1,graphWidth);
            case 3:
                return CalculateIndex(x+1,y,graphWidth);
            default:
                return -1;
        }
    }
}
