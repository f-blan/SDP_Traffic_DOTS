using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

public class BusPathSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecb_s;
     //this is a compact representation of the districts (each one represents a busStop). In BusPathParams i have three districts coordinates
     //what i will do is compute a circular path (of busStops) in the district map, and then a path of intersection nodes to link each
     //busStop of this previous path. This might be expensive (shortest path light greediness should help), 
     //but is only run once for all selected bus stops at the first frame
     private NativeArray<PathUtils.PathNode> DistrictNodeArray;
     private NativeArray<PathUtils.PathNode> PathNodeArray;
     private NativeArray<int2> globalBusStopRelativeCoords;
     private NativeArray<int2> globalBusStopRelativePosition;

     private bool isGraphValid; 

    private const int D_MOVE_X_COST = 30;
    private const int D_MOVE_Y_COST = 20;

    private const int N_MOVE_X_COST = 8;
    private const int N_MOVE_Y_COST = 7;

    protected override void OnCreate(){
        base.OnCreate();

        ecb_s = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        isGraphValid=false;
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        DistrictNodeArray = PathUtils.GetDistrictNodeArray(D_MOVE_X_COST, D_MOVE_Y_COST);
        PathNodeArray = PathUtils.GetPathNodeArray();
        globalBusStopRelativeCoords = PathUtils.initRelativeCoords(Map_Setup.Instance.CityGraph);
        globalBusStopRelativePosition = PathUtils.initRelativePosition(Map_Setup.Instance.CityGraph);
        isGraphValid=true;
    }

    protected override void OnDestroy(){
        if(isGraphValid){
            PathNodeArray.Dispose();
            DistrictNodeArray.Dispose();
            globalBusStopRelativeCoords.Dispose();
            globalBusStopRelativePosition.Dispose();
        }
     }

    

    protected override void OnUpdate(){
        
        EntityCommandBuffer.ParallelWriter ecb = ecb_s.CreateCommandBuffer().AsParallelWriter();

        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(), Map_Setup.Instance.CityGraph.GetHeight());  
        int2 graphSizeDistrict = new int2(Map_Setup.Instance.CityMap.GetNDistrictsX(),Map_Setup.Instance.CityMap.GetNDistrictsY());
        NativeArray<int2> busStopRelativeCoords = globalBusStopRelativeCoords;  
        
        NativeArray<int2> busStopRelativePosition = globalBusStopRelativePosition;
        int2 districtSizeTiles = new int2(Map_Setup.Instance.CityMap.GetDistrictWidth(), Map_Setup.Instance.CityMap.GetDistrictHeight());
        int2 districtSizeNodes = Map_Setup.Instance.CityGraph.GetDistrictSize();
        Vector3 originPosition = Map_Setup.Instance.CityMap.GetOriginPosition();

        NativeArray<PathUtils.PathNode> localPathNodeArray; 
        localPathNodeArray= PathNodeArray; //because if it's not local the compiler complains
        NativeArray<PathUtils.PathNode> localDistrictNodeArray; 
        localDistrictNodeArray= DistrictNodeArray;
        float deltaTime = UnityEngine.Time.deltaTime;
        //Debug.Log(busStopRelativeCoords.x + " coords " + busStopRelativeCoords.y + " " + busStopRelativePosition.x + " pos " + busStopRelativePosition.y);
        //compute path
        Entities.WithReadOnly(localDistrictNodeArray).ForEach(( Entity entity, int entityInQueryIndex, ref BusPathParams busPathParams)=>{
            busPathParams.timer+=deltaTime;
            if(busPathParams.timer<= busPathParams.delay){
                return;
            }
            //compute the busStop path
            NativeList<int3> DistrictPathIndexes = ComputeDistrictPath(localDistrictNodeArray, graphSizeDistrict, busPathParams.pos1, busPathParams.pos2, busPathParams.pos3);
            NativeHashMap<int, PathUtils.PathNode> tmpPathNodeMap=new NativeHashMap<int, PathUtils.PathNode>(10, Allocator.Temp);
            
            //compute the actual node path and transform into an array of PathElements
            NativeList<PathElement> pathList = ComputeNodePath(DistrictPathIndexes, localPathNodeArray, tmpPathNodeMap, graphSize, districtSizeNodes, busStopRelativeCoords);

            int dtype = busPathParams.pos1DistrictType;
            //get a reference world position and spawn the bus with their path as a blob array 
            Vector3 referenceWorldPosition = PathUtils.CalculateStopWorldPosition(busPathParams.pos1.x, busPathParams.pos1.y, districtSizeTiles,  busStopRelativeCoords[dtype],  busStopRelativePosition[dtype], originPosition);
            Map_Spawner.SpawnBusEntities(pathList, referenceWorldPosition, ecb, entityInQueryIndex, busPathParams.entityToSpawn);
            

            //get rid of support structures and destroy the busLine entity
            DistrictPathIndexes.Dispose();
            tmpPathNodeMap.Dispose();
            ecb.DestroyEntity(entityInQueryIndex, entity);
        }).WithReadOnly(localPathNodeArray).WithReadOnly(busStopRelativePosition).WithReadOnly(busStopRelativeCoords).ScheduleParallel();
        //magical lifesaver line (probably delays and schedules all actions performed by ecb inside the forEach)
        ecb_s.AddJobHandleForProducer(this.Dependency);
    }
    private static NativeList<int3> ComputeDistrictPath(NativeArray<PathUtils.PathNode> districtNodeArray,int2 graphSizeDistrict, int2 pos1, int2 pos2, int2 pos3){
        //if bottleneck this can be done as jobs, but it's done only in the first frame for the whole duration so it's not critical for framerate
        //also this whole system is already executed in parallel between the entities, so using a job might not bring to an improvement
        
        
        NativeHashMap<int,PathUtils.PathNode> graphMap = new NativeHashMap<int, PathUtils.PathNode>(10, Allocator.Temp);
        
        NativeArray<int2> neighbourOffsetArray;
        int2 moveCosts = new int2(D_MOVE_X_COST, D_MOVE_Y_COST);
        //this is a compact structure to store the nodes and the district type of the districts they represent
        NativeList<int3> toInt3 = new NativeList<int3>(Allocator.Temp);

        PathUtils.InitPartialData(out neighbourOffsetArray);
        
        //i compute the circular path on three points separately (this should be rather fast since district graph is very small compared to actual graph)
        int lastDirection = -1;
    
        //Debug.Log("first district");
        lastDirection =PathUtils.PathFindPartial(districtNodeArray, graphMap, pos2, pos1, graphSizeDistrict, moveCosts,  neighbourOffsetArray, lastDirection, -1,-1);
        PathUtils.addPathToInt3List(toInt3, graphMap, graphSizeDistrict, pos2);
        //Debug.Log("second district");
        lastDirection =PathUtils.PathFindPartial(districtNodeArray,graphMap, pos3, pos2, graphSizeDistrict, moveCosts, neighbourOffsetArray, lastDirection, -1,-1);
        PathUtils.addPathToInt3List(toInt3,graphMap, graphSizeDistrict, pos3);
        //Debug.Log("third district");
        lastDirection =PathUtils.PathFindPartial(districtNodeArray,graphMap,  pos1, pos3, graphSizeDistrict, moveCosts,  neighbourOffsetArray,lastDirection, -1,-1);
        PathUtils.addPathToInt3List(toInt3, graphMap, graphSizeDistrict, pos1);
       

        graphMap.Dispose();
        neighbourOffsetArray.Dispose();
        

        return toInt3;
    }
    
    private static NativeList<PathElement> ComputeNodePath(NativeList<int3> districtPath, NativeArray<PathUtils.PathNode> graphArray,NativeHashMap<int, PathUtils.PathNode> graphMap,int2 graphSize, int2 districtSizeNodes, NativeArray<int2> busStopRelativeCoords){
        //if bottleneck this can be done as jobs, but it's done only in the first frame for the whole duration so it's not critical for framerate
        //also this whole system is already executed in parallel between the entities, so using a job might not bring to an improvement
        
        
        
        NativeArray<int2> neighbourOffsetArray;
        int2 moveCosts = new int2(N_MOVE_X_COST, N_MOVE_Y_COST);

        PathUtils.InitPartialData( out neighbourOffsetArray);

        NativeList<PathElement> pathList = new NativeList<PathElement>(Allocator.Temp);
        //Debug.Log(districtPath[0].z);
        int lastDirection =-1;
        int2 firstNodePos = PathUtils.CalculateBusStopCoords(districtPath[0].x, districtPath[0].y, districtSizeNodes, busStopRelativeCoords[districtPath[0].z] );
        int2 curNodePos = firstNodePos;
        int2 nextNodePos;
        int2 firstData = new int2(-1,-1);
        for(int t=1; t<districtPath.Length; ++t){
            nextNodePos = PathUtils.CalculateBusStopCoords(districtPath[t].x, districtPath[t].y, districtSizeNodes, busStopRelativeCoords[districtPath[t].z] );    
            
            lastDirection=PathUtils.PathFindPartial(graphArray, graphMap,nextNodePos, curNodePos,graphSize, new int2(N_MOVE_X_COST,N_MOVE_Y_COST), neighbourOffsetArray,lastDirection,firstData.x, firstData.y);
         
            //Debug.Log(t);
            if(t==1){
                firstData = addPathToElementList(pathList, graphMap, nextNodePos, graphSize);
            }
            else{
                addPathToElementList(pathList, graphMap, nextNodePos, graphSize);
            }
            curNodePos = nextNodePos;
        }
        //Debug.Log("almost done");
        //last iteration: link path end with path beginning 
        nextNodePos = nextNodePos = PathUtils.CalculateBusStopCoords(districtPath[0].x, districtPath[0].y, districtSizeNodes, busStopRelativeCoords[districtPath[0].z] );
        lastDirection =PathUtils.PathFindPartial(graphArray,graphMap, nextNodePos, curNodePos,graphSize, new int2(N_MOVE_X_COST,N_MOVE_Y_COST), neighbourOffsetArray, lastDirection, firstData.x,firstData.y);
            
        addPathToElementList(pathList, graphMap, nextNodePos, graphSize);

        //add .x values also for the very first element (a bus stop)
        PathElement startElement = pathList[0];
        PathElement lastElement = pathList[pathList.Length-1];
        startElement.cost.x = lastElement.cost.y;
        startElement.withDirection.x = (lastElement.withDirection.y+2)%4;
        startElement.costToStop.x = startElement.cost.x-4;

        pathList[0] = startElement;
        //Debug.Log("Done");
        neighbourOffsetArray.Dispose();
        
        return pathList;
    }

    private static int2 addPathToElementList(NativeList<PathElement> pathList,
                                            NativeHashMap<int,PathUtils.PathNode> graphMap, int2 endNodePos, int2 graphSize){
        //NativeList<PathElement> pathList = new NativeList<PathElement>(Allocator.Temp);
        //Debug.Log("attempting addition to pathList");

        PathElement curElement = new PathElement();
        

        int initialListLength = pathList.Length;
        

        int endNodeIndex = PathUtils.CalculateIndex(endNodePos.x, endNodePos.y, graphSize.x);
        NativeList<int> supportList = new NativeList<int>(Allocator.Temp);

        PathUtils.PathNode curNode = graphMap[endNodeIndex];
        PathUtils.PathNode nextNode;
        while(curNode.cameFromNodeIndex != -1){
            curNode = graphMap[curNode.cameFromNodeIndex];
            supportList.Add(curNode.index);
        }

        

        for(int t= supportList.Length-1; t>=0; --t){
            curNode = graphMap[supportList[t]];

            if(t==0){
                nextNode = graphMap[endNodeIndex];
            }else{
                nextNode = graphMap[supportList[t-1]];
            }
            curElement = new PathElement();
            curElement.x = curNode.x;
            curElement.y = curNode.y;

            //if this is not the very first node and is the first node we're adding
            if(initialListLength > 0 && t == supportList.Length-1){
                //we have no knowledge of .x values in PathNodeArray so we get them from previous pathlist element
                curElement.cost.x = pathList[initialListLength-1].cost.y;
                curElement.withDirection.x = (pathList[initialListLength-1].withDirection.y+2)%4;
            }else{
                curElement.cost.x = curNode.reachedWithCost;
                curElement.withDirection.x = curNode.reachedWithDirection;
            }
            curElement.cost.y = nextNode.reachedWithCost;
            curElement.withDirection.y = (nextNode.reachedWithDirection+2)%4;

            if(curNode.isBusStop){
                curElement.costToStop.x=curNode.reachedWithCost - 3;
                curElement.costToStop.y=nextNode.reachedWithCost - 3;
            }else{
                curElement.costToStop.x = -1;
                curElement.costToStop.y = -1;
            }

            pathList.Add(curElement);
            
        }
        
        supportList.Dispose();

        return new int2(pathList[1].withDirection.x, PathUtils.CalculateIndex(pathList[0].x, pathList[0].y, graphSize.x));
    }
    
}
