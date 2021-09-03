using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System;

public class Map_Setup : MonoBehaviour
{
    public static Map_Setup Instance {private set; get; }

    private Map_Visual map_Visual;
    private Map_Spawner map_Spawner;
    
    //map is a sequence of copy-pasted "districts"
    public Map<MapTile> CityMap;
    public PathFindGraph CityGraph;
    public int runningEntities;

    //dimensions (in MapTiles) of each district
    private int District_width;
    private int District_height;
    private int spawnTilesPerDistrict;
    
    //dimensions (in GraphNodes) of each district
    private int GraphDistrict_width;
    private int GraphDistrict_height;

    //this number defines the two above properties as well as the shape of the district

    //number of district in the map along the x and y axis
    [SerializeField] private int map_n_districts_x;
    [SerializeField] int map_n_districts_y;

    [SerializeField] public int n_entities;
    [SerializeField] int n_bus_lines;
    //[SerializeField] int districtTypeIndex;
    [SerializeField] int Frequency_District_0;
    [SerializeField] int Frequency_District_1;
    [SerializeField] int Frequency_District_2;
    [SerializeField] int Frequency_District_3;

    private void Awake(){
        Instance = this;
        map_Visual = GameObject.Find("Map_Visual").GetComponent<Map_Visual> ();
        map_Spawner = GameObject.Find("Map_Spawner").GetComponent<Map_Spawner>();

        int[] districtData = MapUtils.GetDistrictParams(1);
        District_width = districtData[0];
        District_height = districtData[1];
        
        GraphDistrict_width = districtData[2];
        GraphDistrict_height=districtData[3];
        spawnTilesPerDistrict = districtData[4];

        runningEntities=0;
    }

    // Start is called before the first frame update
    void Start()
    {
        Material HorizontalMaterial = Resources.Load<Material>("TrafficLightHorizontal");
        Material VerticalMaterial = Resources.Load<Material>("TrafficLightVertical");
        
        
        

        int width,height;
        width = District_width * map_n_districts_x;
        height = District_height * map_n_districts_y;

        float tileSize = 1f;
        Vector3 originPosition = new Vector3((-width/2) +0.5f, (-height/2) +0.5f, 0);

        CityMap = new Map<MapTile>(map_n_districts_x, map_n_districts_y,  District_width, District_height,tileSize,originPosition, (Map<MapTile> map, int x, int y, Vector3 wp) => new MapTile(map,x,y, wp),spawnTilesPerDistrict);
        CityGraph = new PathFindGraph( map_n_districts_x, map_n_districts_y,  GraphDistrict_width, GraphDistrict_height);

        //initialize both CityMap and CityGraph according to parameters
        List<GraphNode> busStopNodes;
        List<Tuple<bool, MapTile>> trafficLightTiles;
        List<MapTile> parkSpotTiles;
        List<MapTile> busStopTiles;
        int4 freqs = new int4(Frequency_District_0,Frequency_District_1,Frequency_District_2,Frequency_District_3);
        MapUtils.InitializeMap(CityMap, CityGraph, freqs, map_n_districts_x, map_n_districts_y,out busStopNodes,
            out trafficLightTiles, out parkSpotTiles, out busStopTiles);
        

        map_Visual.SetMap(CityMap,CityGraph, n_entities);

        //map_Spawner.SpawnCarEntities(CityMap,CityGraph, roadTiles, n_entities); Spawning of cars is dealt by a system (initialized in SetMap)
        
        map_Spawner.SpawnBusLine(CityMap,CityGraph,busStopNodes, n_bus_lines);
        map_Spawner.SpawnTrafficLights(CityMap, trafficLightTiles);
        map_Spawner.SpawnParkSpots(CityMap, parkSpotTiles);
        map_Spawner.SpawnBusStops(CityMap, busStopTiles);
        
    }
}
