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
    private NativeArray<PathUtils.PathNode> PathNodeMap;

    private bool isGraphValid;
    
    private const int MOVE_X_COST = 8;
    private const int MOVE_Y_COST = 7;

    protected override void OnCreate(){
        base.OnCreate();
        // Find the ECB system once and store it for later usage
        ecb_s = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        
        
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        //get the graph in a format usable by jobs (a native map of struct PathNode, and do it only once per runtime: Allocator.Persistent used)
        if(isGraphValid!=true){
            PathNodeMap = PathUtils.GetPathNodeArray();
            isGraphValid=true;
        }
    }

    protected override void OnDestroy(){
        if(isGraphValid){
           
            PathNodeMap.Dispose();
            
            
        }
     }
    
    protected override void OnUpdate(){
        EntityCommandBuffer.ParallelWriter ecb = ecb_s.CreateCommandBuffer().AsParallelWriter();
        
        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(), Map_Setup.Instance.CityGraph.GetHeight());        

        NativeArray<PathUtils.PathNode> localPathNodeMap; 
        localPathNodeMap= PathNodeMap; //because if it's not local the compiler complains

        //compute path
        Entities.WithAll<CarPathBuffer>().ForEach(( Entity entity, int entityInQueryIndex, ref CarPathParams carPathParams, ref DynamicBuffer<CarPathBuffer> carPathBuffer)=>{
            //make a copy of the graph hashmap exclusive to the job
            NativeArray<PathUtils.PathNode> tmpPathNodeMap = new NativeArray<PathUtils.PathNode>(localPathNodeMap, Allocator.Temp);
            
            PathUtils.PathFind(tmpPathNodeMap, carPathParams.endPosition,carPathParams.startPosition,graphSize,
                             new int2(MOVE_X_COST, MOVE_Y_COST));
            int endNodeIndex = PathUtils.CalculateIndex(carPathParams.endPosition.x, carPathParams.endPosition.y, graphSize.x);

            AssignPath(entity,tmpPathNodeMap,endNodeIndex,ecb, carPathParams.direction, carPathParams.init_cost, entityInQueryIndex, ref carPathBuffer);
            tmpPathNodeMap.Dispose();
        }).ScheduleParallel();
        //magical lifesaver line (probably delays and schedules all actions performed by ecb inside the forEach)
        ecb_s.AddJobHandleForProducer(this.Dependency);
     }


    private static void AssignPath(Entity entity, NativeArray<PathUtils.PathNode> pathNodeMap,int endNodeIndex, EntityCommandBuffer.ParallelWriter ecb, int first_direction, int first_cost, int eqi,
        ref DynamicBuffer<CarPathBuffer> buf){
        
        
        //DynamicBuffer<CarPathBuffer> buf = ecb.AddBuffer<CarPathBuffer>(eqi,entity);

        
        
        PathUtils.PathNode endNode = pathNodeMap[endNodeIndex];
        
        
        buf[0]=new CarPathBuffer{x = endNode.x, y = endNode.y, cost = endNode.reachedWithCost, withDirection = endNode.reachedWithDirection};
        
        //add intermediate nodes
        PathUtils.PathNode cameFromNode = pathNodeMap[endNode.cameFromNodeIndex];
        PathUtils.PathNode currentNode = endNode;
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
    
}


