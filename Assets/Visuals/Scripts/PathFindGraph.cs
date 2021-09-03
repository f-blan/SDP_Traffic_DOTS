using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
public class PathFindGraph 
{
    
    private int height;
    private int width;
    private int n_districts_x;
    private int n_districts_y;
    private int district_width;
    private int district_height;

    private int[,] districtTypes;
    private GraphNode[,] GraphArray;
    //coordinate of the bus stops in CityGraph wrt to their district
    private int2[] busStopRelativeCoords;
    //position in CityMap of reference tile of bus stop
    private int2[] busStopRelativePosition;

    public PathFindGraph(int n_districts_x, int n_districts_y, int d_width, int d_height){
        this.n_districts_x=n_districts_x;
        this.n_districts_y=n_districts_y;
        districtTypes = new int[n_districts_y,n_districts_x];
        this.district_width=d_width;
        this.district_height=d_height;

        this.width = district_width*n_districts_x;
        this.height = district_height*n_districts_y;
        
        busStopRelativeCoords = new int2[4];
        busStopRelativePosition = new int2[4];
        GraphArray = new GraphNode[height, width];

        int index = 0;
        for(int y=0; y<height; ++y){
            for(int x=0; x< width; ++x){
                GraphArray[y,x] = new GraphNode(index, x,y);
            }
        }
    }

    public void SetDistrictType(int d_x, int d_y, int type){
        districtTypes[d_y,d_x] = type;
    }

    public int GetDistrictType(int d_x, int d_y){
        return districtTypes[d_y, d_x];
    }
    
    public int GetWidth(){
        return width;
    }

    public int GetHeight(){
        return height;
    }
    public void SetGraphNode(int x, int y){

    }

    public GraphNode GetGraphNode(int x, int y){
        return GraphArray[y,x];
    }

    public GraphNode GetGraphNode(int index){
        int x = index % width;
        int y = index / width;

        return GraphArray[y,x];
    }

    public GraphNode GetGraphNode(int d_x, int d_y, int r_x, int r_y){
        return GetGraphNode(r_x + d_x*district_width, r_y + d_y*district_height);
    }
    public void SetBusStopRelativeCoords(int x, int y, int dType){
        busStopRelativeCoords[dType] = new int2(x,y);
    }
    public int2 GetBusStopRelativeCoords(int dType){
        return busStopRelativeCoords[dType];
    }
    

    
    public void SetBusStopRelativePosition(int x, int y, int dType){
        busStopRelativePosition[dType] = new int2(x,y);
    }
    public int2 GetBusStopRelativePosition(int dType){
        return busStopRelativePosition[dType];
    }

    public int2 GetDistrictSize(){
        return new int2(district_width, district_height);
    }

    public int GetDistrictTypeFromNodeCoords(int x, int y){
        int d_x= x/district_width;
        int d_y = y/district_height;
        return districtTypes[d_y, d_x];
    }
}
