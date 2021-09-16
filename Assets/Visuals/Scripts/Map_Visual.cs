using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;

public class Map_Visual : MonoBehaviour
{

    public static Map_Visual instance {private set; get;}
    private Map<MapTile> map;
    [SerializeField] public Mesh mesh;
    [SerializeField] private Material Road;
    [SerializeField] private Material Obstacle;
    [SerializeField] private Material ParkSpot;
    [SerializeField] private Material TrafficLight;
    [SerializeField] public Material trafficLightSpriteSheet;
    [SerializeField] public Material CarMaterial;
    [SerializeField] public int differentTypeOfVehicles; 
    [SerializeField] private Material BusStop;
    [SerializeField] private Material DistrictMaterial0;
    [SerializeField] private Material DistrictMaterial1;
    [SerializeField] private Material DistrictMaterial2;
    [SerializeField] private Material DistrictMaterial3;
    
    [SerializeField] public float delayAddition;

    private void Awake(){
        instance = this;
    }

    public void SetMap(Map<MapTile> map,PathFindGraph CityGraph,int n_cars){
        this.map = map;
        UpdateVisual2(CityGraph,n_cars);
    }

    
    // for big maps this does not work, need to improve this piece of code (i was thinking to make one mesh for each district by adding more game object)
    private void UpdateVisual(){
        //MeshUtils.CreateEmptyMeshArrays(map.GetWidth()* map.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds));

        NativeArray<Entity> tiles = new NativeArray<Entity>(map.GetWidth()*map.GetHeight(), Allocator.Temp);

        em.CreateEntity(arch, tiles);

        for(int x=0; x< map.GetWidth(); x++){
            for(int y =0; y< map.GetHeight(); y++){
            int index = x*map.GetHeight()+y;
            Entity e = tiles[index];
            Vector3 wp = map.GetWorldPosition(x, y);

            
            Material material;

            switch(map.GetMapObject(x,y).GetTileType()){
                case MapTile.TileType.Road:
                        material = Road;
                        break;
                case MapTile.TileType.Intersection:
                        material = Road;
                        break;
                    case MapTile.TileType.Obstacle:
                        material = Obstacle;
                        break;
                    case MapTile.TileType.TrafficLight:
                        material = TrafficLight;
                        break;
                    case MapTile.TileType.ParkSpot:
                        material = ParkSpot;
                        break;
                    default:
                        material = BusStop;
                        break;
            }

            em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 0)});

            em.SetSharedComponentData(e, new RenderMesh{
                mesh = mesh,
                material = material,
                layer = 0
            });
                 
            }
        }
        tiles.Dispose();
        
    }

    private void UpdateVisual2(PathFindGraph CityGraph, int n_cars){
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), typeof(NonUniformScale),
                        typeof(CarSpawnerComponent));

        NativeArray<Entity> districts = new NativeArray<Entity>(map.GetNDistrictsX()*map.GetNDistrictsY(), Allocator.Temp);
        EntityArchetype defaultCarArchetype = em.CreateArchetype( typeof(RenderMesh), 
                                typeof(LocalToWorld), typeof(RenderBounds), typeof(VehicleMovementData));
        Entity defaultCarEntity = em.CreateEntity(defaultCarArchetype);

        Mesh carMesh = MeshUtils.CreateMesh(0.47f, 1f);


        em.SetSharedComponentData(defaultCarEntity, new RenderMesh{
            mesh = carMesh,
            material = CarMaterial,
            layer = 1
        });

        em.CreateEntity(arch, districts);

        Material material;
        int left_cars = n_cars;
        int n_cars_each = n_cars/districts.Length;
        int leftover_cars = n_cars % districts.Length;
        if(map.GetSpawnTilesPerDistrict()< n_cars_each){
            Debug.Log("maximum number of cars for the given map size exceeded, only the maximum available number of cars will be spawned. Create a bigger map and retry");
        }
        if(n_cars_each==0 && n_cars!=0){
            n_cars_each=1;
        }
        float curDelay = delayAddition;
       

        for(int d_x=0; d_x<map.GetNDistrictsX(); ++d_x){
            for(int d_y = 0; d_y< map.GetNDistrictsY(); ++d_y){
                int districtIndex = CityGraph.GetDistrictType(d_x,d_y);
                switch(districtIndex){
                    case 0:
                        material = DistrictMaterial0;
                    break;
                    case 1:
                        material = DistrictMaterial1;
                    break;
                    case 2:
                        material = DistrictMaterial2;
                    break;
                    case 3:
                        material = DistrictMaterial3;
                    break;
                    
                    default:
                    material = DistrictMaterial0;
                    break;
                }
                int index = d_x*map.GetNDistrictsY()+d_y;
                
                Entity e = districts[index];
                Vector3 wp = map.GetDistrictWorldPosition(d_x, d_y);
                
                int carsToSpawn;
                if(left_cars>=n_cars_each){
                    carsToSpawn = n_cars_each;
                    left_cars-=n_cars_each;
                    if(UnityEngine.Random.Range(0,10)>4 && leftover_cars>0 && left_cars>0){
                        carsToSpawn+=2;
                        leftover_cars-=2;
                    }
                }else{
                    carsToSpawn = left_cars;
                    left_cars-=carsToSpawn;
                }
                //left_cars-= carsToSpawn;
                
                em.SetName(e, "district" + d_x + "-" + d_y);
                em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 1)});
                em.SetComponentData(e, new CarSpawnerComponent{d_x = d_x, d_y = d_y, timer = 0, delay = curDelay,n_cars = carsToSpawn, entityToSpawn = defaultCarEntity, dType=districtIndex });
                em.SetComponentData(e, new NonUniformScale{ Value = {
                    x = map.GetDistrictWidth(),
                    y = map.GetDistrictHeight(),
                    z = 0}
                });

                em.SetSharedComponentData(e, new RenderMesh{
                    mesh = mesh,
                    material = material,
                    layer = 0
                });
                curDelay+=delayAddition;
                
            }
        }
        districts.Dispose();
    }
}
