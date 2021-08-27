using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;


public class CarSpawnerSystem : SystemBase
{
    
    private NativeArray<TileStruct> map;
    private EndSimulationEntityCommandBufferSystem ecb_s;
    private bool isMapValid;

    protected override void OnCreate(){
        base.OnCreate();
        // Find the ECB system once and store it for later usage
        ecb_s = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        
        isMapValid=false;
    }

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        //get the graph in a format usable by jobs (a native map of struct PathNode, and do it only once per runtime: Allocator.Persistent used)
        //Debug.Log("creating map");
        map = SpawnerUtils.GetMapArray();
        isMapValid=true;
        
    }

    protected override void OnDestroy(){

        if(isMapValid){
            map.Dispose();
            
        }
    }
    
    // Update is called once per frame
    protected override void OnUpdate()
    {
        EntityCommandBuffer.ParallelWriter ecb = ecb_s.CreateCommandBuffer().AsParallelWriter();
        
        int2 mapSize = new int2(Map_Setup.Instance.CityMap.GetWidth(), Map_Setup.Instance.CityMap.GetHeight());        
        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(),Map_Setup.Instance.CityGraph.GetHeight());
        int2 districtSize = new int2(Map_Setup.Instance.CityMap.GetDistrictWidth(), Map_Setup.Instance.CityMap.GetDistrictHeight());
        Vector3 originPosition = Map_Setup.Instance.CityMap.GetOriginPosition();
        
        NativeArray<TileStruct> localMapArray = map;
        float maxCarSpeed = 1f;

        float deltaTime = UnityEngine.Time.deltaTime;
        
        //spawning cars in districts with a certain delay (so that we do not overload the system with pathFind for all cars and start the app faster)
        Entities.WithReadOnly(localMapArray).ForEach((Entity e,int entityInQueryIndex, ref CarSpawnerComponent carSpawnerComponent) =>{
            carSpawnerComponent.timer+=deltaTime;
            if(carSpawnerComponent.timer < carSpawnerComponent.delay){
                return;
            }

            
            
            //the 2 and minus 2 is to avoid spawning a car close to the end of its district, which may lead to cars spawning in already busy tiles
            for(int r_x = 2; r_x < districtSize.x-2; ++r_x){
                for(int r_y = 2; r_y< districtSize.y-2; ++r_y){
                    
                    if(carSpawnerComponent.n_cars <= 0){
                        //we spawned all cars we had to spawn
                        break;
                    }
                    int index = SpawnerUtils.CalculateIndex(carSpawnerComponent.d_x, carSpawnerComponent.d_y, r_x, r_y, districtSize,mapSize);
                    
                    if(localMapArray[index].type == 0){
                        Vector3 wp = SpawnerUtils.GetWorldPosition(localMapArray[index].x, localMapArray[index].y, originPosition);
                        
                        Entity car = ecb.Instantiate(entityInQueryIndex, carSpawnerComponent.entityToSpawn);
                        int seed = entityInQueryIndex + index;
                        Unity.Mathematics.Random r = new Unity.Mathematics.Random((uint) seed);
                        ecb.AddComponent(entityInQueryIndex,car, new Translation{Value = new float3(wp[0], wp[1], -1)});

                        SpawnerUtils.SetUpPathFind(carSpawnerComponent.d_x,carSpawnerComponent.d_y,r_x, r_y, car, graphSize,districtSize,mapSize,localMapArray,ecb,entityInQueryIndex,maxCarSpeed, r);
                        carSpawnerComponent.n_cars--;
                    }
                    
                }
            }
            
            ecb.RemoveComponent<CarSpawnerComponent>(entityInQueryIndex, e);
        }).ScheduleParallel(); 
        
        
        ecb_s.AddJobHandleForProducer(this.Dependency);
    }
}
