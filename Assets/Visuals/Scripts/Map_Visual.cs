using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Map_Visual : MonoBehaviour
{

    private Map<MapTile> map;
    private Mesh mesh;
    private bool updateMesh;

    private void Awake(){
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void SetMap(Map<MapTile> map){
        this.map = map;
        UpdateVisual();

        
    }

    
    // for big maps this does not work, need to improve this piece of code (i was thinking to make one mesh for each district by adding more game object)
    private void UpdateVisual(){
        MeshUtils.CreateEmptyMeshArrays(map.GetWidth()* map.GetHeight(), out Vector3[] vertices, out Vector2[] uv, out int[] triangles);
        Transform parent = this.transform;
        for(int x=0; x< map.GetWidth(); x++){
            for(int y =0; y< map.GetHeight(); y++){

                /* 
                GameObject district = new GameObject();
                district.transform.SetParent(parent);
                */
                

                int index = x *map.GetHeight()+y;
                Vector3 quadSize = new Vector3(1,1)*map.GetTileSize();

                MapTile mapTile = map.GetMapObject(x,y);

                Vector2 uv00 = new Vector2(.5f, .5f);
                Vector2 uv11 = new Vector2(1f,1f);;
                
               

                switch (mapTile.GetTileType())
                {
                    case MapTile.TileType.Road:
                        uv00 = new Vector2(.5f, .5f);
                        uv11 = new Vector2(1f,1f);
                        break;
                    case MapTile.TileType.Obstacle:
                        uv00 = new Vector2(0f, .5f);
                        uv11 = new Vector2(.5f,1f);
                        break;
                    case MapTile.TileType.TrafficLight:
                        uv00 = new Vector2(.5f, 0f);
                        uv11 = new Vector2(1f,.5f);
                        break;
                    case MapTile.TileType.ParkSpot:
                        uv00 = new Vector2(0f, 0f);
                        uv11 = new Vector2(.5f,.5f);
                        break;
                    default:
                        uv00 = new Vector2(.5f, .5f);
                        uv11 = new Vector2(1f,1f);
                        break;
                }

                

                MeshUtils.AddToMeshArrays(vertices, uv, triangles, index, map.GetWorldPosition(x, y) + quadSize * .0f, 0f, quadSize, uv00, uv11);
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
    }
}
