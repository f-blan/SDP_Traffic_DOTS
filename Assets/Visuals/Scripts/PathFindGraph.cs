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
    private GraphNode[,] GraphArray;
    //coordinate of the bus stops in CityGraph wrt to their district
    private int2 busStopRelativeCoords;
    //position in CityMap of reference tile of bus stop
    private int2 busStopRelativePosition;

    public PathFindGraph(int n_districts_x, int n_districts_y, int d_width, int d_height){
        this.n_districts_x=n_districts_x;
        this.n_districts_y=n_districts_y;
        this.district_width=d_width;
        this.district_height=d_height;

        this.width = district_width*n_districts_x;
        this.height = district_height*n_districts_y;
        

        GraphArray = new GraphNode[height, width];

        int index = 0;
        for(int y=0; y<height; ++y){
            for(int x=0; x< width; ++x){
                GraphArray[y,x] = new GraphNode(index, x,y);
            }
        }
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
    public void SetBusStopRelativeCoords(int x, int y){
        busStopRelativeCoords.x = x;
        busStopRelativeCoords.y = y;
    }
    public int2 GetBusStopRelativeCoords(){
        return busStopRelativeCoords;
    }
    public void SetBusStopRelativePosition(int x, int y){
        busStopRelativePosition.x = x;
        busStopRelativePosition.y = y;
    }
    public int2 GetBusStopRelativePosition(){
        return busStopRelativePosition;
    }

    public int2 GetDistrictSize(){
        return new int2(district_width, district_height);
    }
}
