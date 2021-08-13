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

    private bool isWalkable;

    public MapTile(Map<MapTile> map, int x, int y) {
        this.map = map;
        this.x = x;
        this.y = y;
        type = TileType.Road;
        graphNode = null;
        isWalkable = true;
    }

    public bool IsWalkable(){
        return isWalkable;
    }

    public void SetIsWalkable(bool val){
        isWalkable= val;
    }

    public int GetX(){
        return x;
    }
    public int GetY(){
        return y;
    }

    public bool isObstacle(){
        return type == TileType.Obstacle;
    }
    public TileType GetTileType(){
        return type;
    }

    public void SetTileType(TileType t){
        this.type = t;
        switch(t){
            case TileType.BusStop:
                isWalkable = false;
            break;
            case TileType.ParkSpot:
                isWalkable = false;
            break;
            case TileType.Obstacle:
                isWalkable = false;
            break;
            default:
                isWalkable = true;
            break; 
        }
        
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
