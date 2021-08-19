using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Burst;

public class Map_Spawner : MonoBehaviour
{
        [SerializeField] private Material carMaterial;
        [SerializeField] private Material busMaterial;
        [SerializeField] private float maxCarSpeed;
        

        //used by BusPathSystem
        public static void SpawnBusEntities(){
            //todo
        }
        public void SpawnBusLine(Map<MapTile> CityMap, PathFindGraph CityGraph, List<GraphNode> busStopNodes, int n_buses){
            
            if(busStopNodes.Count < n_buses){
                Debug.Log("Too many buses for the map to handle!");
                return;   
            }
            if(CityMap.GetNDistrictsX()<=1 && CityMap.GetNDistrictsY()<=1){
                Debug.Log("map is too small to allow buses to run!");
                return;
            }
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype arch = em.CreateArchetype( typeof(BusPathParams));

             NativeArray<Entity> busStops = new NativeArray<Entity>(n_buses, Allocator.Temp);
            
            Mesh busMesh = CreateMesh(0.47f, 1f);
            em.CreateEntity(arch, busStops);
                 
             Unity.Mathematics.Random r = new Unity.Mathematics.Random(0x6E624EB7u);

             for(int t=0; t< n_buses; ++t){
                 int index = UnityEngine.Random.Range(0, busStopNodes.Count);

                 GraphNode node = busStopNodes[index];
                 //one bus stop per district, i find the district coords
                 int d_x = (node.GetX()-CityGraph.GetBusStopRelativeCoords().x)/CityMap.GetNDistrictsX();
                 int d_y = (node.GetY()-CityGraph.GetBusStopRelativeCoords().y)/CityMap.GetNDistrictsY();
                 Entity e = busStops[t];
                 busStopNodes.RemoveAt(index);

                 Vector3 wp = CityMap.GetWorldPosition(node.GetX(), node.GetY());

                em.SetName(e, "BusStop "+t);
                SetUpBusPathFind(d_x, d_y, e, CityMap.GetNDistrictsX(), CityMap.GetNDistrictsY(), CityMap, em,r);
             }
        }
        private void SetUpBusPathFind(int d_x, int d_y, Entity entity, int n_district_x, int n_district_y, Map<MapTile> CityMap, EntityManager em, Unity.Mathematics.Random r){
            
            
            NativeArray<int2> walkOffset = new NativeArray<int2>(4, Allocator.Temp);
            walkOffset[0] = new int2(0,1);
            walkOffset[1] = new int2(1,0);
            walkOffset[2] = new int2(0, -1);
            walkOffset[3] = new int2(-1,0);
        
            int2 pos1 = new int2(d_x, d_y);
            
            int2 pos2 = new int2(-1, -1);

            //select a district different from the current one ( each district has only one busStop node)
            do{
                pos2.x =  r.NextInt(0, n_district_x);
                pos2.y = r.NextInt(0, n_district_y);
            }while(pos2.x == pos1.x && pos2.y == pos1.y);

            int2 pos3 = new int2(-1,-1);

            //select a third one
            do{
                pos3.x =  r.NextInt(0, n_district_x);
                pos3.y = r.NextInt(0, n_district_y);
            }while((pos3.x == pos1.x && pos3.y == pos1.y)||(pos3.x == pos2.x && pos3.y == pos2.y));
            
            em.SetComponentData(entity, new BusPathParams{pos1 = pos1, pos2 = pos2, pos3 = pos3});
            

            walkOffset.Dispose();
    }
        public void SpawnCarEntities(Map<MapTile> CityMap, PathFindGraph CityGraph, List<MapTile> roadTiles, int n_entities){

            if(roadTiles.Count < n_entities){
                Debug.Log("Too many entities for the map to handle!");
                return;
            }        
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), 
                                typeof(LocalToWorld), typeof(RenderBounds), typeof(CarPathParams), typeof(Rotation), typeof(VehicleMovementData));

            NativeArray<Entity> cars = new NativeArray<Entity>(n_entities, Allocator.Temp);

            em.CreateEntity(arch, cars);

            Mesh carMesh = CreateMesh(0.47f, 1f);

            //Unity.Mathematics.Random r = new Unity.Mathematics.Random(0x6E624EB7u);
            for(int t=0; t<n_entities; ++t){
               
                int index = UnityEngine.Random.Range(0, roadTiles.Count);
                
                MapTile tile = roadTiles[index];

                Entity e = cars[t];
                roadTiles.RemoveAt(index);

                Vector3 wp = CityMap.GetWorldPosition(tile.GetX(), tile.GetY());

                em.SetName(e, "Vehicle "+t);

                em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], -1)});

                em.SetSharedComponentData(e, new RenderMesh{
                    mesh = carMesh,
                    material = carMaterial,
                    layer = 1
                });

                SetUpPathFind(tile.GetX(), tile.GetY(), e, CityGraph.GetWidth(), CityGraph.GetHeight(), CityMap, em);
            }
            cars.Dispose();
        }
    
    private void SetUpPathFind(int x, int y, Entity entity, int graph_width, int graph_height, Map<MapTile> CityMap, EntityManager em){
            int direction=0;
            
            int verse=0;
            bool[] walkableDirections = new bool[]{CityMap.GetMapObject(x, y+1).IsWalkable(), CityMap.GetMapObject(x+1, y).IsWalkable(), 
                                                    CityMap.GetMapObject(x, y-1).IsWalkable(), CityMap.GetMapObject(x-1, y).IsWalkable()};
            
            //majority voting for verse
            for(int t=0; t<4; ++t){
                if(t%2 == 0 && walkableDirections[t]){
                    verse++;
                }else if(t%2 == 1 && walkableDirections[t]){
                    verse--;
                }
            }

            if(verse >0){
                verse=0;
            }else{
                verse = 1;
            }
            
            //in the correct direction you have an unwalkable tile (park, obstacle or busStop) to your right and walkable in front of you
            for(int t= verse; t<4; t= t+2){
                int relative_right = (t + 1)%4;
                
                if(walkableDirections[relative_right] == false && walkableDirections[t] == true){
                    
                    direction = t;
                    break;
                }
            }

            //now get the graphnode x and y
            NativeArray<int2> walkOffset = new NativeArray<int2>(4, Allocator.Temp);
            walkOffset[0] = new int2(0,1);
            walkOffset[1] = new int2(1,0);
            walkOffset[2] = new int2(0, -1);
            walkOffset[3] = new int2(-1,0);
            
            int2 pos = new int2(x, y);
            int cost = 0;
            
            do{
                pos.x += walkOffset[direction].x;
                pos.y += walkOffset[direction].y;
                cost++;
                
                CityMap.GetMapObject(pos.x, pos.y).GetTileType();
            }while(CityMap.GetMapObject(pos.x, pos.y).GetTileType() != MapTile.TileType.Intersection);
            
            GraphNode g = CityMap.GetMapObject(pos.x, pos.y).GetGraphNode();
            int2 endPos;
            do{
                endPos = new int2(UnityEngine.Random.Range(0, graph_width), UnityEngine.Random.Range(0, graph_height));
                
            }while(endPos.x == g.GetX() && endPos.y == g.GetY());
            em.SetComponentData(entity, new CarPathParams{init_cost = cost, direction = direction, startPosition = new int2(g.GetX(), g.GetY()), endPosition = new int2(endPos.x, endPos.y)});
            //Debug.Log(endPos.x + " " + endPos.y);
            InitializeCarData(em, entity, direction);

            walkOffset.Dispose();
    }
    private Mesh CreateMesh(float width, float height){

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        float halfWidth = width/2f;
        float halfHeight = height/2f;

        vertices[0] = new Vector3(-halfWidth, -halfHeight);
        vertices[1] = new Vector3(-halfWidth, +halfHeight);
        vertices[2] = new Vector3(+halfWidth, +halfHeight);
        vertices[3] = new Vector3(+halfWidth, -halfHeight);

        uv[0] = new Vector2(0,0);
        uv[1] = new Vector2(0,1);
        uv[2] = new Vector2(1,1);
        uv[3] = new Vector2(1,0);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;

        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        mesh.name = "Car Mesh";

        return mesh;
    }

    private void InitializeCarData(EntityManager entityManager, Entity entity, int direction){

        entityManager.SetComponentData(entity, new Rotation{Value = Quaternion.Euler(0f, 0f, CarUtils.ComputeRotation(direction))});

        entityManager.SetComponentData(entity, new VehicleMovementData{
            speed = maxCarSpeed,
            direction = direction,
            velocity = CarUtils.ComputeVelocity(maxCarSpeed, direction),
            initialPosition = new float2(float.NaN, float.NaN),
            offset = new float2(float.NaN, float.NaN)
        });
    }
}
