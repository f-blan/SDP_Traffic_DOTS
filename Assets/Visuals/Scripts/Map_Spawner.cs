using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using System;

public class Map_Spawner : MonoBehaviour
{
        public static Map_Spawner instance {private set; get; }
        [SerializeField] private Material busMaterial;
        [SerializeField] public float maxCarSpeed;
        [SerializeField] public float maxBusSpeed;

        [SerializeField] public float minTrafficLightTime;
        [SerializeField] public float maxTrafficLightTime;
        [SerializeField] private Mesh Quad;
        [SerializeField] private Material CircleMaterial;

        //used by BusPathSystem

        private void Awake(){
            if(instance == null){
                instance = this;
            }
        }
        public static void SpawnBusEntities(NativeList<PathElement> pathList, Vector3 referenceWorldPosition,
            EntityCommandBuffer.ParallelWriter ecb, int eqi, Entity entityToSpawn){ 
            
            BlobAssetReference<BusPathBlobArray> pathReference;
           
            
            //create the blob array 
            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp)){
                ref BusPathBlobArray blobAsset = ref blobBuilder.ConstructRoot<BusPathBlobArray>();
                BlobBuilderArray<PathElement> blobPathArray = blobBuilder.Allocate<PathElement>(ref blobAsset.pathArray, pathList.Length);

                for(int t=0; t<pathList.Length; ++t){
                    PathElement p = pathList[t];
                    blobPathArray[t] = new PathElement{x = p.x, y = p.y, cost = new int2(p.cost.x, p.cost.y), 
                        withDirection = new int2(p.withDirection.x, p.withDirection.y), costToStop = new int2(p.costToStop.x,p.costToStop.y)};
                }
                pathReference = blobBuilder.CreateBlobAssetReference<BusPathBlobArray>(Allocator.Persistent);
            }
            ref BlobArray<PathElement> v = ref pathReference.Value.pathArray;
            
            
            
            //for now i spawn only two buses
            Entity busVerseA = ecb.Instantiate(eqi, entityToSpawn);
            Entity busVerseB = ecb.Instantiate(eqi, entityToSpawn);
            
            
            //typeof(Translation), typeof(RenderMesh), typeof(LocalToWorld), typeof(RenderBounds), typeof(BusPathComponent)
            //ecb.AddComponent(eqi, busVerseA, busArchetype);
            
            ecb.AddComponent<BusPathComponent>(eqi, busVerseA,  
                new BusPathComponent{pathIndex = 1, 
                pathLength = pathList.Length,
                verse = -1,
                pathArrayReference = pathReference});
            
            ecb.AddComponent<BusPathComponent>(eqi, busVerseB,  
                new BusPathComponent{pathIndex = pathList.Length-1, 
                pathLength = pathList.Length, 
                verse = 1, 
                pathArrayReference = pathReference});

            ecb.AddComponent<VehicleMovementData>(eqi, busVerseA, 
                new VehicleMovementData{direction = pathList[1].withDirection.x,
                initialPosition = new float2(float.NaN,float.NaN)});

            ecb.AddComponent<VehicleMovementData>(eqi, busVerseB, 
                new VehicleMovementData{direction = pathList[pathList.Length-1].withDirection.y,
                initialPosition = new float2(float.NaN,float.NaN)});

            //ecb.SetComponent<Translation>(eqi, busVerseA, new Translation{Value = new float3(referenceWorldPosition[0], referenceWorldPosition[1], referenceWorldPosition[2])});
            //ecb.SetComponent<Translation>(eqi, busVerseB, new Translation{Value = new float3(referenceWorldPosition[0]-1f, referenceWorldPosition[1]+1f, referenceWorldPosition[2])});

            ecb.AddComponent<Translation>(eqi, busVerseA, new Translation{Value = SpawnerUtils.ComputeBusInitialPosition(referenceWorldPosition, pathList[1].withDirection.x)});
            ecb.AddComponent<Translation>(eqi, busVerseB, new Translation{Value = SpawnerUtils.ComputeBusInitialPosition(referenceWorldPosition, pathList[pathList.Length-1].withDirection.y)});
            
            ecb.AddComponent<Rotation>(eqi, busVerseA, new Rotation{Value = Quaternion.Euler(0,0,CarUtils.ComputeRotation(pathList[1].withDirection.x))});
            ecb.AddComponent<Rotation>(eqi, busVerseB, new Rotation{Value = Quaternion.Euler(0,0,CarUtils.ComputeRotation(pathList[pathList.Length-1].withDirection.y))});

        }
        public void SpawnBusLine(Map<MapTile> CityMap, PathFindGraph CityGraph, List<GraphNode> busStopNodes, int n_buses_attempt){
            int n_buses;
            if(busStopNodes.Count < n_buses_attempt){
                Debug.Log("Too many bus lines for the map to handle! Max number will be spawned instead");
                n_buses = busStopNodes.Count;   
            }else{
                n_buses=n_buses_attempt;
            }
            if(CityMap.GetNDistrictsX()<=1 || CityMap.GetNDistrictsY()<=1){
                Debug.Log("map is too small to allow buses to run!");
                return;
            }
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype arch = em.CreateArchetype( typeof(BusPathParams));
            EntityArchetype busArchetype = em.CreateArchetype( typeof(RenderMesh), 
                                typeof(LocalToWorld), typeof(RenderBounds));

            NativeArray<Entity> busStops = new NativeArray<Entity>(n_buses, Allocator.Temp);
            
            Mesh busMesh = CreateMesh(0.47f, 1f);
            Entity defaultBusEntity = em.CreateEntity(busArchetype);
            //em.SetComponentData(defaultBusEntity, new BusPathComponent{pathIndex = 10000});
            em.SetSharedComponentData(defaultBusEntity, new RenderMesh{
                mesh = busMesh,
                material = busMaterial,
                layer = 1
            });
            float delayAddition = Map_Visual.instance.delayAddition;
            em.CreateEntity(arch, busStops);
                 
             Unity.Mathematics.Random r = new Unity.Mathematics.Random(0x6E624EB7u);

             for(int t=0; t< n_buses; ++t){
                 int index = UnityEngine.Random.Range(0, busStopNodes.Count);
                 float delay = delayAddition + delayAddition*index;

                 GraphNode node = busStopNodes[index];
                 int districtType = CityGraph.GetDistrictTypeFromNodeCoords(node.GetX(), node.GetY());
                 //one bus stop per district, i find the district coords
                 int d_x = (node.GetX()-CityGraph.GetBusStopRelativeCoords(districtType).x)/CityGraph.GetDistrictSize().x;
                 int d_y = (node.GetY()-CityGraph.GetBusStopRelativeCoords(districtType).y)/CityGraph.GetDistrictSize().y;
                 Entity e = busStops[t];
                 busStopNodes.RemoveAt(index);

                 Vector3 wp = CityMap.GetWorldPosition(node.GetX(), node.GetY());

                em.SetName(e, "BusStop "+t);
                SetUpBusPathFind(d_x, d_y, e, CityMap.GetNDistrictsX(), CityMap.GetNDistrictsY(), CityMap, em,r, defaultBusEntity, districtType, delay);
             }
        }
        private void SetUpBusPathFind(int d_x, int d_y, Entity entity, int n_district_x, int n_district_y, Map<MapTile> CityMap, EntityManager em, Unity.Mathematics.Random r, Entity defaultBusEntity, int pos1DistrictType, float delay){
            
            

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
            
            em.SetComponentData(entity, new BusPathParams{pos1 = pos1, pos2 = pos2, pos3 = pos3, entityToSpawn = defaultBusEntity, pos1DistrictType = pos1DistrictType, timer =0, delay = delay});

        }
        // public void SpawnCarEntities(Map<MapTile> CityMap, PathFindGraph CityGraph, List<MapTile> roadTiles, int n_entities){

        //     if(roadTiles.Count < n_entities){
        //         Debug.Log("Too many entities for the map to handle!");
        //         return;
        //     }        
        //     EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        //     EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), 
        //                         typeof(LocalToWorld), typeof(RenderBounds), typeof(CarPathParams), typeof(Rotation), typeof(VehicleMovementData));

        //     NativeArray<Entity> cars = new NativeArray<Entity>(n_entities, Allocator.Temp);

        //     em.CreateEntity(arch, cars);

        //     Mesh carMesh = CreateMesh(0.47f, 1f);

        //     //Unity.Mathematics.Random r = new Unity.Mathematics.Random(0x6E624EB7u);
        //     for(int t=0; t<n_entities; ++t){
               
        //         int index = UnityEngine.Random.Range(0, roadTiles.Count);
                
        //         MapTile tile = roadTiles[index];

        //         Entity e = cars[t];
        //         roadTiles.RemoveAt(index);

        //         Vector3 wp = CityMap.GetWorldPosition(tile.GetX(), tile.GetY());

        //         em.SetName(e, "Vehicle "+t);

        //         em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], -1)});

        //         em.SetSharedComponentData(e, new RenderMesh{
        //             mesh = carMesh,
        //             material = carMaterial,
        //             layer = 1
        //         });

        //         SetUpPathFind(tile.GetX(), tile.GetY(), e, CityGraph.GetWidth(), CityGraph.GetHeight(), CityMap, em);
        //     }
        //     cars.Dispose();
        // }
    
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
    public Mesh CreateMesh(float width, float height){

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
    public void BlobTest(){
        BlobAssetReference<BusPathBlobArray> pathReference;
            Debug.Log("blobbing");
            
            //create the blob array 
            using (BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp)){
                ref BusPathBlobArray blobAsset = ref blobBuilder.ConstructRoot<BusPathBlobArray>();
                BlobBuilderArray<PathElement> blobPathArray = blobBuilder.Allocate<PathElement>(ref blobAsset.pathArray, 3);

                
                    blobPathArray[0] = new PathElement{x = 1, y = 1, cost = new int2(1, 1), 
                        withDirection = new int2(1, 1), costToStop = new int2(1,1)};
                
                pathReference = blobBuilder.CreateBlobAssetReference<BusPathBlobArray>(Allocator.Persistent);
            }

        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        Entity e = em.CreateEntity(typeof(BusPathComponent));
        em.SetName(e, "asdasd ");
        em.AddComponentData(e, new BusPathComponent{pathArrayReference = pathReference});
        
    }

    public void SpawnTrafficLights(Map<MapTile> CityMap, List<Tuple<bool,MapTile>> trafficLightTiles){
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(TrafficLightComponent),
            typeof(LocalToWorld), typeof(RenderMesh), typeof(RenderBounds), typeof(NonUniformScale));

        NativeArray<Entity> tiles = new NativeArray<Entity>(trafficLightTiles.Count, Allocator.Temp);

        em.CreateEntity(arch, tiles);
        /*
        for(int t=0; t<trafficLightTiles.Count; ++t){
            Material material;
            bool isVertical;
            if(trafficLightTiles[t].Item1){
                material = VerticalTrafficLightMaterial;
                isVertical=true;
            }else{
                material = HorizontalTrafficLightMaterial;
                isVertical = false;
            }

            MapTile curTile = trafficLightTiles[t].Item2; 
            Entity e = tiles[t];
            Vector3 wp = CityMap.GetWorldPosition(curTile.GetX(), curTile.GetY());

            em.SetName(e, "Traffic Light " + t);
            em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 0)});

            em.SetComponentData(e, new TrafficLightComponent{isRed = trafficLightTiles[t].Item1, isVertical = isVertical});

            em.SetComponentData(e, new SpriteSheetAnimationComponent{
                currentFrame = 0,
                frameCount = 3,
                frameTimer = 0f,
                frameTimerMax = UnityEngine.Random.Range(0f, 0.5f)
            });
        }*/

        float randomTime = .5f;

        for(int t=0; t<trafficLightTiles.Count; ++t){

            if(t % 4 == 0){
                randomTime = UnityEngine.Random.Range(minTrafficLightTime, maxTrafficLightTime);
            }

            MapTile curTile = trafficLightTiles[t].Item2; 
            Entity e = tiles[t];
            Vector3 wp = CityMap.GetWorldPosition(curTile.GetX(), curTile.GetY());

            em.SetName(e, "Traffic Light " + t);
            float offset = -0.3f;
            int state = 2;
            if(trafficLightTiles[t].Item1){
                state = 0;
                offset = -offset;
            }
            em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1]+offset, 0)});

            em.SetComponentData(e, new TrafficLightComponent{isRed = trafficLightTiles[t].Item1, isVertical = trafficLightTiles[t].Item1, 
                greenLightDuration = randomTime, baseTranslation=new float3(wp[0], wp[1], 0), timer = 0f, state = state,
            });
            em.SetSharedComponentData(e, new RenderMesh{
                mesh = Quad,
                material = CircleMaterial,
                layer = 1
            });

            em.SetComponentData(e, new NonUniformScale{ Value = {
                x = 0.4f,
                y = 0.4f,
                z = 0}
            });
        }



        tiles.Dispose();
    }

    public void SpawnParkSpots(Map<MapTile> CityMap, List<MapTile> parkSpotTiles){
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(ParkSpotTag));

        NativeArray<Entity> tiles = new NativeArray<Entity>(parkSpotTiles.Count, Allocator.Temp);

        em.CreateEntity(arch, tiles);
        
        for(int t=0; t<parkSpotTiles.Count; ++t){
            MapTile curTile = parkSpotTiles[t]; 
            Entity e = tiles[t];
            Vector3 wp = CityMap.GetWorldPosition(curTile.GetX(), curTile.GetY());

            em.SetName(e, "ParkSpot " + t);
            em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 0)});

            
        }
        tiles.Dispose();
    }
    public void SpawnBusStops(Map<MapTile> CityMap, List<MapTile> busStopTiles){
        EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
        EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(BusStopTag));

        NativeArray<Entity> tiles = new NativeArray<Entity>(busStopTiles.Count, Allocator.Temp);

        em.CreateEntity(arch, tiles);
        
        for(int t=0; t<busStopTiles.Count; ++t){
            MapTile curTile = busStopTiles[t]; 
            Entity e = tiles[t];
            Vector3 wp = CityMap.GetWorldPosition(curTile.GetX(), curTile.GetY());

            em.SetName(e, "BusStop " + t);
            em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 0)});

            
        }
        tiles.Dispose();
    }
}

