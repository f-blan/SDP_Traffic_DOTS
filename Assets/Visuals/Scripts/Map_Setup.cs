using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;
using CodeMonkey;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;

public class Map_Setup : MonoBehaviour
{
    public static Map_Setup Instance {private set; get; }

    private Map_Visual map_Visual;
    private Map_Spawner map_Spawner;
    
    //map is a sequence of copy-pasted "districts"
    public Map<MapTile> CityMap;
    public PathFindGraph CityGraph;

    //dimensions (in MapTiles) of each district
    private int District_width;
    private int District_height;
    
    //dimensions (in GraphNodes) of each district
    private int GraphDistrict_width;
    private int GraphDistrict_height;

    //this number defines the two above properties as well as the shape of the district
    [SerializeField] private int districtTypeIndex;

    //number of district in the map along the x and y axis
    [SerializeField] private int map_n_districts_x;
    [SerializeField] int map_n_districts_y;

    [SerializeField] int n_entities;
    [SerializeField] int n_bus_lines;

    private void Awake(){
        Instance = this;
        map_Visual = GameObject.Find("Map_Visual").GetComponent<Map_Visual> ();
        map_Spawner = GameObject.Find("Map_Spawner").GetComponent<Map_Spawner>();

        int[] districtData = MapUtils.GetDistrictParams(districtTypeIndex);
        District_width = districtData[0];
        District_height = districtData[1];
        
        GraphDistrict_width = districtData[2];
        GraphDistrict_height=districtData[3];
    }

    // Start is called before the first frame update
    void Start()
    {
        int width,height;
        width = District_width * map_n_districts_x;
        height = District_height * map_n_districts_y;

        float tileSize = 1f;
        Vector3 originPosition = new Vector3(-width/2, -height/2, 0);

        CityMap = new Map<MapTile>(map_n_districts_x, map_n_districts_y,  District_width, District_height,tileSize,originPosition, (Map<MapTile> map, int x, int y) => new MapTile(map,x,y));
        CityGraph = new PathFindGraph( map_n_districts_x, map_n_districts_y,  GraphDistrict_width, GraphDistrict_height);

        //initialize both CityMap and CityGraph according to parameters
        List<MapTile> roadTiles;
        List<GraphNode> busStopNodes;
        MapUtils.InitializeMap(CityMap, CityGraph, districtTypeIndex, map_n_districts_x, map_n_districts_y, out roadTiles,out busStopNodes);
        

        map_Visual.SetMap(CityMap);
        map_Spawner.SpawnCarEntities(CityMap,CityGraph, roadTiles, n_entities);
    }
}
