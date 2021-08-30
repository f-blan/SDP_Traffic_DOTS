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
    bool printed = false; 

    
    protected override void OnUpdate(){
        
        Vector3 originPosition = Map_Setup.Instance.CityMap.GetOriginPosition();
        int2 busStopRelativeCoords = Map_Setup.Instance.CityGraph.GetBusStopRelativeCoords();
        int2 busStopRelativePosition = Map_Setup.Instance.CityGraph.GetBusStopRelativePosition(); 

        Entities.ForEach((Entity e, int entityInQueryIndex, ref BusPathComponent busPathComponent)=>{

            ref BlobArray<PathElement> v = ref busPathComponent.pathArrayReference.Value.pathArray;

            for(int t=0; t<busPathComponent.pathLength - 1; t++){
                Debug.DrawLine(new float3(v[t].x, v[t].y, 0), new float3(v[t+1].x, v[t+1].y, 0));
            }
        }).ScheduleParallel(); 
        
        Entities.ForEach((Entity e, int entityInQueryIndex, ref BusPathComponent busPathComponent)=>{
            
            
            //BusPathBlobArray a = busPathComponent.pathArrayReference.Value;
            //Debug.Log("attempt2");
            ref BlobArray<PathElement> v = ref busPathComponent.pathArrayReference.Value.pathArray;
            
            if(busPathComponent.verse == 1 && !printed){
                Debug.Log("Verse: 1");
                Debug.Log("Coordinate list: ");
                string tmp = "";
                for(int t=0; t<busPathComponent.pathLength; ++t){
                    tmp += "Coordinates: " + v[t].x + " " + v[t].y + "\n"; 
                    tmp += "Cost: " + v[t].cost.x + " " + v[t].cost.y + "\n";
                    tmp += "Direction: " + v[t].withDirection.x + " " + v[t].withDirection.y + "\n";
                    // Debug.Log("------------COORDS-------------");
                    // Debug.Log(v[t].x);
                    // Debug.Log(v[t].y);
                    
                    // Debug.Log("costs");
                    // Debug.Log(v[t].cost.x);
                    // Debug.Log(v[t].cost.y);

                    // Debug.Log("directions");
                    // Debug.Log(v[t].withDirection.x);
                    // Debug.Log(v[t].withDirection.y);
                    
                    // Debug.Log("stopCost");
                    // Debug.Log(v[t].costToStop.x);
                    // Debug.Log(v[t].costToStop.y);

                }   
                Debug.Log(tmp);
                printed = true;
                Debug.Log("----------------------END---------------------------------------");
            }
            //ecb.DestroyEntity(entityInQueryIndex, e);
        }).WithoutBurst().Run();
    }
}
