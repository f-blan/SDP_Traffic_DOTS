using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

public class Map<TMapObject>
{
    

    private int width;
    private int height;
    private int n_districts_x;
    private int n_districts_y;
    private int district_width;
    private int district_height;
    private float tileSize;
    private Vector3 originPosition;
    private TMapObject[,] mapArray;
    private int spawnTilesPerDistrict;
    private int roadTilesPerDistrict;
    

    //constructor
    public Map(int n_districts_x, int n_districts_y, int d_width, int d_height, float tileSize, Vector3 originPosition,Func<Map<TMapObject>, int, int, TMapObject> createMapObject, int spawnTilesPerDistrict){
        this.n_districts_x=n_districts_x;
        this.n_districts_y=n_districts_y;
        this.district_width=d_width;
        this.district_height=d_height;
        this.spawnTilesPerDistrict = spawnTilesPerDistrict;
        this.width = district_width*n_districts_x;
        this.height = district_height*n_districts_y;
        this.tileSize = tileSize;
        this.originPosition = originPosition;

        mapArray = new TMapObject[height, width];
        

        for (int y = 0; y < mapArray.GetLength(0); y++) {
            for (int x = 0; x < mapArray.GetLength(1); x++) {
                mapArray[y, x] = createMapObject(this, x, y);
                
            }
        }
    }

    public int GetSpawnTilesPerDistrict(){
        return this.spawnTilesPerDistrict;
    }

    public Vector3 GetOriginPosition(){
        return originPosition;
    }

    public int GetWidth() {
        return width;
    }

    public int GetHeight() {
        return height;
    }

    public float GetTileSize() {
        return tileSize;
    }

    public Vector3 GetWorldPosition(int x, int y) {
        return new Vector3(x, y, 0) * tileSize + originPosition;
    }
    public Vector3 GetDistrictWorldPosition(int d_x, int d_y){
        int r_x = d_x*district_width;
        int r_y = d_y * district_height;
        float offset_x = district_width/2f;
        float offset_y = district_height/2f;
        return new Vector3(r_x + offset_x - .5f, r_y+ offset_y - .5f, 0) *tileSize + originPosition;
    }
    //given the world position (x,y,z) it returns the x and y of the corresponding tile in the map
    public void GetXY(Vector3 worldPosition, out int x, out int y) {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / tileSize);
        y = Mathf.FloorToInt((worldPosition - originPosition).y / tileSize);
    }

    //setter when you know x and y coords in the map
    public void SetMapObject(int x, int y, TMapObject value){
        if(x >= 0 && y>=0 && x< width && y<height){
            mapArray[y,x] = value;
            
        }
    }

    
    //setter when you only know the world position
    public void SetMapObject(Vector3 worldPosition, TMapObject value){
        int x, y;
        GetXY(worldPosition, out x, out y);
        SetMapObject(x,y,value);
    }

    public TMapObject GetMapObject(int x, int y){
        if (x >= 0 && y >= 0 && x < width && y < height) {
            return mapArray[y, x];
        } else {
            return default(TMapObject);
        }
    }
    public TMapObject GetMapObject(Vector3 worldPosition) {
        int x, y;
        GetXY(worldPosition, out x, out y);
        return GetMapObject(x, y);
    }

    public TMapObject GetMapObject(int d_x, int d_y, int r_x, int r_y){
        return GetMapObject(r_x + d_x*district_width, r_y + d_y*district_height);
    }

    public int GetNDistrictsX(){
        return n_districts_x;
    }
    public int GetNDistrictsY(){
        return n_districts_y;
    }
    public int GetDistrictWidth(){
        return district_width;
    }
    public int GetDistrictHeight(){
        return district_height;
    }
    
}
