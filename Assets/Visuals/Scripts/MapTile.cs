using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapTile 
{
    public enum TileType{
        Road,
        Obstacle,
        ParkSpot,
        TrafficLight,
        Intersection,
        BusStop
    }

    private Map<MapTile> map;
    private int x;
    private int y;
    private TileType type;
    private GraphNode graphNode;

    public MapTile(Map<MapTile> map, int x, int y) {
        this.map = map;
        this.x = x;
        this.y = y;
        type = TileType.Road;
        graphNode = null;
    }

    public bool isObstacle(){
        return type == TileType.Obstacle;
    }
    public TileType GetTileType(){
        return type;
    }

    public void SetTileType(TileType t){
        this.type = t;
        
    }

    //only intersections have a grahNode
    public void SetGraphNode(GraphNode g){
        if(type == TileType.Intersection){
            this.graphNode = g;
        }else{
            return;
        }
    }
    public GraphNode GetGraphNode(){
        if(type == TileType.Intersection){
            return this.graphNode;
        }else{
            return null;
        }
    }
}
