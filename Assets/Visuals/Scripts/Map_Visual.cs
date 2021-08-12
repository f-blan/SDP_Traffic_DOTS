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

    private Map<MapTile> map;
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material Road;
    [SerializeField] private Material Obstacle;
    [SerializeField] private Material ParkSpot;
    [SerializeField] private Material TrafficLight;
    
    
    
    

    public void SetMap(Map<MapTile> map){
        this.map = map;
        UpdateVisual();

        
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
                        material = Road;
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
}
