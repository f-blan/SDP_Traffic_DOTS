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
        [SerializeField] private Mesh mesh;

        [SerializeField] private Material carMaterial;

        public void SpawnCarEntities(Map<MapTile> CityMap, PathFindGraph CityGraph, List<MapTile> roadTiles, int n_entities){

            if(roadTiles.Count < n_entities){
                Debug.Log("Too many entities for the map to handle!");
                return;
            }        
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            EntityArchetype arch = em.CreateArchetype(typeof(Translation), typeof(RenderMesh), 
                                typeof(LocalToWorld), typeof(RenderBounds), typeof(CarPathParams));

            NativeArray<Entity> cars = new NativeArray<Entity>(n_entities, Allocator.Temp);

            em.CreateEntity(arch, cars);

            Debug.Log("available places: " + roadTiles.Count);

            Unity.Mathematics.Random r = new Unity.Mathematics.Random(0x6E624EB7u);
            for(int t=0; t<n_entities; ++t){
               
                int index = UnityEngine.Random.Range(0, roadTiles.Count);
                MapTile tile = roadTiles[index];
                Entity e = cars[t];
                roadTiles.RemoveAt(index);

                Vector3 wp = CityMap.GetWorldPosition(tile.GetX(), tile.GetY());

                em.SetComponentData(e, new Translation{Value = new float3(wp[0], wp[1], 0)});

                em.SetSharedComponentData(e, new RenderMesh{
                    mesh = mesh,
                    material = carMaterial,
                    layer = 1
                });
                SetUpPathFind(tile.GetX(), tile.GetY(), e, CityGraph.GetWidth(), CityGraph.GetHeight(), CityMap, em,r);
            }
            cars.Dispose();
        }
    
    private void SetUpPathFind(int x, int y, Entity entity, int graph_width, int graph_height, Map<MapTile> CityMap, EntityManager em, Unity.Mathematics.Random r){
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
            }while(CityMap.GetMapObject(pos.x, pos.y).GetTileType() != MapTile.TileType.Intersection);
            
            GraphNode g = CityMap.GetMapObject(pos.x, pos.y).GetGraphNode();
            
            int2 endPos = new int2(r.NextInt(0, graph_width), r.NextInt(0, graph_height));

            em.SetComponentData(entity, new CarPathParams{init_cost = cost, direction = direction, startPosition = new int2(g.GetX(), g.GetY()), endPosition = new int2(endPos.x, endPos.y)});
            DynamicBuffer<CarPathBuffer> b = em.AddBuffer<CarPathBuffer>(entity);
            
            walkOffset.Dispose();
    }

    
    
        
}
