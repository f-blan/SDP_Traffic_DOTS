using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;


public class CarSpawnerSystem : SystemBase
{
    private NativeArray<TileStruct> map;
    private EndSimulationEntityCommandBufferSystem ecb_s;
    private bool isMapValid;
    private Material[] carMaterialVariants;
    private int carVariants;

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

        carVariants = GameObject.Find("Map_Visual").GetComponent<Map_Visual>().differentTypeOfVehicles;
        Material carMaterial = GameObject.Find("Map_Visual").GetComponent<Map_Visual>().CarMaterial;

        carMaterialVariants = new Material[carVariants];

        for(int i = 0; i < carVariants; i++){
            carMaterialVariants[i] = new Material(carMaterial);
            carMaterialVariants[i].color = new Color(UnityEngine.Random.Range(0.0f, 1f),UnityEngine.Random.Range(0.0f, 1f),UnityEngine.Random.Range(0.0f, 1f),1f);
        }
    } 
        

    protected override void OnDestroy(){
        if(isMapValid){
            map.Dispose(); 
        }
    }
    
    // Update is called once per frame
    protected override void OnUpdate()
    {
        EntityCommandBuffer ecbNonParallel = ecb_s.CreateCommandBuffer();
        EntityCommandBuffer.ParallelWriter ecb = ecbNonParallel.AsParallelWriter();
        
        int2 mapSize = new int2(Map_Setup.Instance.CityMap.GetWidth(), Map_Setup.Instance.CityMap.GetHeight());        
        int2 graphSize = new int2(Map_Setup.Instance.CityGraph.GetWidth(),Map_Setup.Instance.CityGraph.GetHeight());
        int2 districtSize = new int2(Map_Setup.Instance.CityMap.GetDistrictWidth(), Map_Setup.Instance.CityMap.GetDistrictHeight());
        Vector3 originPosition = Map_Setup.Instance.CityMap.GetOriginPosition();
        
        NativeArray<TileStruct> localMapArray = map;

        float maxCarSpeed = Map_Spawner.instance.maxCarSpeed;

        float deltaTime = UnityEngine.Time.deltaTime;
        
        //spawning cars in districts with a certain delay (so that we do not overload the system with pathFind for all cars and start the app faster)
        Entities.WithReadOnly(localMapArray).ForEach((Entity e, int entityInQueryIndex, ref CarSpawnerComponent carSpawnerComponent) =>{
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
                        ecb.AddComponent(entityInQueryIndex, car, new ChangeColorTag{
                            newColor = carSpawnerComponent.color
                        }); 

                        SpawnerUtils.SetUpPathFind(carSpawnerComponent.d_x,carSpawnerComponent.d_y,r_x, r_y, car, graphSize,districtSize,mapSize,localMapArray,ecb,entityInQueryIndex,maxCarSpeed, r);
                        carSpawnerComponent.n_cars--;
                    }
                    
                }
            }
            
            ecb.RemoveComponent<CarSpawnerComponent>(entityInQueryIndex, e);
        }).ScheduleParallel(); 
        
        Entities.WithAll<ChangeColorTag>().ForEach((Entity e, in RenderMesh renderMesh, in ChangeColorTag changeColorTag) => {
            int index = UnityEngine.Random.Range((int) 0, (int) carVariants);
            ecbNonParallel.SetSharedComponent(e, new RenderMesh{
                mesh = renderMesh.mesh,
                material = carMaterialVariants[index],
                layer = 0
            });
            ecbNonParallel.RemoveComponent<ChangeColorTag>(e);
        }).WithoutBurst().Run();
        
        ecb_s.AddJobHandleForProducer(this.Dependency);
    }
}
