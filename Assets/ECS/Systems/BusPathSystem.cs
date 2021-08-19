using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


public class BusPathSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecb_s;
     //this is a compact representation of the districts (each one represents a busStop). In BusPathParams i have three districts coordinates
     //what i will do is compute a circular path (of busStops) in the district map, and then a path of intersection nodes to link each
     //busStop of this previous path. This might be expensive (shortest path light greediness should help), 
     //but is only run once for all selected bus stops at the first frame
     private NativeArray<PathUtils.PathNode> DistrictNodeArray;
     private NativeArray<PathUtils.PathNode> PathNodeArray;

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
        isGraphValid=true;
    }

    protected override void OnDestroy(){
        if(isGraphValid){
            PathNodeArray.Dispose();
            DistrictNodeArray.Dispose();
        }
     }


    protected override void OnUpdate(){
        EntityCommandBuffer.ParallelWriter ecb = ecb_s.CreateCommandBuffer().AsParallelWriter();

        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(), Map_Setup.Instance.CityGraph.GetHeight());  
        int2 graphSizeDistrict = new int2(Map_Setup.Instance.CityMap.GetNDistrictsX(),Map_Setup.Instance.CityMap.GetNDistrictsY());    
        int2 busStopRelativeCoords = Map_Setup.Instance.CityGraph.GetBusStopRelativeCoords();
        int2 districtSize = new int2(Map_Setup.Instance.CityMap.GetDistrictWidth(), Map_Setup.Instance.CityMap.GetDistrictHeight());

        NativeArray<PathUtils.PathNode> localPathNodeArray; 
        localPathNodeArray= PathNodeArray; //because if it's not local the compiler complains
        NativeArray<PathUtils.PathNode> localDistrictNodeArray; 
        localDistrictNodeArray= DistrictNodeArray;

        //compute path
        Entities.ForEach(( Entity entity, int entityInQueryIndex, ref BusPathParams busPathParams)=>{

            NativeArray<int2> DistrictPathIndexes = ComputeDistrictPath(localDistrictNodeArray, graphSizeDistrict, busPathParams.pos1, busPathParams.pos2, busPathParams.pos3);
            NativeArray<PathUtils.PathNode> tmpPathNodeArray=new NativeArray<PathUtils.PathNode>(localPathNodeArray, Allocator.Temp);

            ComputeNodePath(DistrictPathIndexes, tmpPathNodeArray, graphSize, districtSize, busStopRelativeCoords);

            Map_Spawner.SpawnBusEntities();
            
            DistrictPathIndexes.Dispose();
            tmpPathNodeArray.Dispose();
        }).ScheduleParallel();
        //magical lifesaver line (probably delays and schedules all actions performed by ecb inside the forEach)
        ecb_s.AddJobHandleForProducer(this.Dependency);
    }
    private static NativeList<int2> ComputeDistrictPath(NativeArray<PathUtils.PathNode> districtNodeArray,int2 graphSizeDistrict, int2 pos1, int2 pos2, int2 pos3){
        //if bottleneck this can be done as jobs, but it's done only in the first frame for the whole duration so it's not critical for framerate
        //also this whole system is already executed in parallel between the entities, so using a job might not bring to an improvement
        
        
        NativeArray<PathUtils.PathNode> graphArray = new NativeArray<PathUtils.PathNode>(districtNodeArray, Allocator.Temp);
        NativeList<int> openList;
        NativeList<int> closedList;
        NativeArray<int2> neighbourOffsetArray;
        int2 moveCosts = new int2(D_MOVE_X_COST, D_MOVE_Y_COST);

        PathUtils.InitPartialData(graphArray,out neighbourOffsetArray, out openList, out closedList);

        //i compute the circular path on three points separately (this should be rather fast since district graph is very small compared to actual graph)
        PathUtils.PathFindPartial(graphArray, pos2, pos1, graphSizeDistrict, moveCosts, openList,closedList, neighbourOffsetArray);
        PathUtils.PathFindPartial(graphArray, pos3, pos2, graphSizeDistrict, moveCosts, openList,closedList, neighbourOffsetArray);
        PathUtils.PathFindPartial(graphArray, pos1, pos3, graphSizeDistrict, moveCosts, openList,closedList, neighbourOffsetArray);

        NativeList<int2> toInt2 = PathUtils.turnPathToInt2List(graphArray, graphSizeDistrict, pos1);

        graphArray.Dispose();
        neighbourOffsetArray.Dispose();
        openList.Dispose();
        closedList.Dispose();

        return toInt2;
    }
    
    private static void ComputeNodePath(NativeArray<int2> districtPath, NativeArray<PathUtils.PathNode> PathNodeArray,int2 graphSize, int2 districtSize, int2 busStopRelativeCoords){
        //if bottleneck this can be done as jobs, but it's done only in the first frame for the whole duration so it's not critical for framerate
        //also this whole system is already executed in parallel between the entities, so using a job might not bring to an improvement
        
        
        NativeList<int> openList;
        NativeList<int> closedList;
        NativeArray<int2> neighbourOffsetArray;
        int2 moveCosts = new int2(N_MOVE_X_COST, N_MOVE_Y_COST);

        PathUtils.InitPartialData(PathNodeArray, out neighbourOffsetArray, out openList, out closedList);


        int2 curNodePos = PathUtils.CalculateBusStopCoords(districtPath[0].x, districtPath[0].y, districtSize, busStopRelativeCoords );
        int2 nextNodePos;
        for(int t=1; t<districtPath.Length; ++t){
            nextNodePos = PathUtils.CalculateBusStopCoords(districtPath[t].x, districtPath[t].y, districtSize, busStopRelativeCoords );    

            PathUtils.PathFindPartial(PathNodeArray, nextNodePos, curNodePos,graphSize, new int2(N_MOVE_X_COST,N_MOVE_Y_COST), openList,closedList,neighbourOffsetArray );

            curNodePos = nextNodePos;
        }


        return;
    }

    
}
