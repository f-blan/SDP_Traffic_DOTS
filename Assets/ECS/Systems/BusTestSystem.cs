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
    
    protected override void OnUpdate(){
        /*
        
        Entities.ForEach((Entity e, int entityInQueryIndex, ref BusPathComponent busPathComponent)=>{
            
            
            //BusPathBlobArray a = busPathComponent.pathArrayReference.Value;
            //Debug.Log("attempt2");
            ref BlobArray<PathElement> v = ref busPathComponent.pathArrayReference.Value.pathArray;
            

            if(busPathComponent.verse == 1){
            for(int t=0; t<busPathComponent.pathLength; ++t){
                
                Debug.Log("------------COORDS-------------");
                Debug.Log(v[t].x);
                Debug.Log(v[t].y);
                
                Debug.Log("costs");
                Debug.Log(v[t].cost.x);
                Debug.Log(v[t].cost.y);

                Debug.Log("directions");
                Debug.Log(v[t].withDirection.x);
                Debug.Log(v[t].withDirection.y);
                
                Debug.Log("stopCost");
                Debug.Log(v[t].costToStop.x);
                Debug.Log(v[t].costToStop.y);
                  
                
                
            }   
            }
            Debug.Log("----------------------END---------------------------------------");
            //ecb.DestroyEntity(entityInQueryIndex, e);
        }).ScheduleParallel();*/
    }
}
