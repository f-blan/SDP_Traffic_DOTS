using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(BusPathSystem))]
public class BusTestSystem : SystemBase
{
    
    private EndSimulationEntityCommandBufferSystem ecb_s;
    protected override void OnCreate(){
        base.OnCreate();

        ecb_s = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        
    }
    protected override void OnUpdate(){
        /*
        EntityCommandBuffer ecb = ecb_s.CreateCommandBuffer();
        Entities.ForEach((Entity e, ref BusPathComponent busPathComponent)=>{
            
            
            //BusPathBlobArray a = busPathComponent.pathArrayReference.Value;
            //Debug.Log("attempt2");
            ref BlobArray<PathElement> v = ref busPathComponent.pathArrayReference.Value.pathArray;
            

            if(busPathComponent.verse == 1){
            for(int t=0; t<busPathComponent.pathLength; ++t){
                Debug.Log(v[t].x + "-" +v[t].y + " costs: " + v[t].cost.x + "-" + v[t].cost.y + " directions: " + v[t].withDirection.x + "-" + v[t].withDirection.y
                    + " stopCost: " + v[t].costToStop.x + "-" + v[t].costToStop.y);
            }   
            }
            Debug.Log("----------------------END---------------------------------------");
            ecb.DestroyEntity( e);
        }).WithoutBurst().Run();*/
    }
}
