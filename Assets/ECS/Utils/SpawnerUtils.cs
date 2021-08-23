using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
public static class SpawnerUtils 
{
    public static NativeArray<TileStruct> GetMapArray(){

        Map<MapTile> CityMap = Map_Setup.Instance.CityMap;
        int2 mapSize = new int2 (CityMap.GetWidth(), CityMap.GetHeight());
        int2 districtSize = new int2(CityMap.GetDistrictWidth(),CityMap.GetDistrictHeight());

        NativeArray<TileStruct> mapArray = new NativeArray<TileStruct>(mapSize.x * mapSize.y, Allocator.Persistent);
        int i;
        for(int d_x=0; d_x<CityMap.GetNDistrictsX(); ++d_x){
            for(int d_y =0; d_y<CityMap.GetNDistrictsY(); ++d_y){
                for(int r_x =0; r_x< CityMap.GetDistrictWidth(); ++r_x){
                    for(int r_y=0; r_y < CityMap.GetDistrictHeight(); ++r_y){
                        TileStruct ts = new TileStruct();
                        MapTile mt = CityMap.GetMapObject(d_x,d_y,r_x, r_y);
                        i = CalculateIndex(d_x,d_y,r_x,r_y, districtSize, mapSize);
                        ts.x = mt.GetX();
                        ts.y= mt.GetY();
                        ts.IsWalkable = mt.IsWalkable();
                        switch(mt.GetTileType()){
                            case MapTile.TileType.Road:
                                ts.type = 0;
                                ts.g_x = -1;
                                ts.g_y = -1;
                            break;
                            case MapTile.TileType.Intersection:
                                ts.type = 1;
                                ts.g_x = mt.GetGraphNode().GetX();
                                ts.g_y = mt.GetGraphNode().GetY();
                            break;
                            default:
                                ts.type = 2;
                                ts.g_x = -1;
                                ts.g_y = -1;
                            break;
                        }
                        mapArray[i] = ts;
                        i++;
                    }
                }
                
            }
        }
        
        return mapArray;
    }

    public static int CalculateIndex(int d_x, int d_y, int r_x, int r_y, int2 districtSize, int2 mapSize){
        return (d_x*districtSize.x+r_x)+(d_y*districtSize.y+r_y)*mapSize.x;
    }

    

    public static Vector3 GetWorldPosition(int x, int y, Vector3 originPosition) {
        return new Vector3(x, y, 0) * 1f + originPosition;
    }
    public static void SetUpPathFind(int d_x, int d_y, int r_x, int r_y, Entity entity, int2 graphSize, int2 districSize, int2 mapSize,
        NativeArray<TileStruct> mapArray, EntityCommandBuffer.ParallelWriter ecb, int eqi, float maxCarSpeed, Unity.Mathematics.Random r){
        int direction=0;
        
        int verse=0;
        NativeArray<bool> walkableDirections = new NativeArray<bool>(4, Allocator.Temp);
        walkableDirections[0] = mapArray[CalculateIndex(d_x,d_y,r_x,r_y+1, districSize, mapSize)].IsWalkable;
        walkableDirections[1] = mapArray[CalculateIndex(d_x,d_y,r_x+1,r_y, districSize, mapSize)].IsWalkable;
        walkableDirections[2] = mapArray[CalculateIndex(d_x,d_y,r_x,r_y-1, districSize, mapSize)].IsWalkable;
        walkableDirections[3] = mapArray[CalculateIndex(d_x,d_y,r_x-1,r_y, districSize, mapSize)].IsWalkable;
        

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
        
        int2 pos = new int2(r_x, r_y);
        int cost = 0;
        int type = 0;
        do{
            pos.x += walkOffset[direction].x;
            pos.y += walkOffset[direction].y;
            cost++;
            
            type=mapArray[CalculateIndex(d_x,d_y,pos.x,pos.y, districSize, mapSize)].type;
        }while(type != 1);
        
        int2 startPos = new int2(mapArray[CalculateIndex(d_x,d_y,pos.x,pos.y, districSize, mapSize)].g_x, mapArray[CalculateIndex(d_x,d_y,pos.x,pos.y, districSize, mapSize)].g_y);
        int2 endPos;
        do{
            endPos = new int2(r.NextInt(0, graphSize.x), r.NextInt(0, graphSize.y));
            
        }while(endPos.x == startPos.x && endPos.y == startPos.y);
        ecb.AddComponent(eqi,entity, new CarPathParams{init_cost = cost, direction = direction, startPosition = startPos, endPosition = endPos});
        //Debug.Log(endPos.x + " " + endPos.y);
        InitializeCarData(ecb, entity, direction, eqi, maxCarSpeed);

        walkOffset.Dispose();
        walkableDirections.Dispose();
    }
    private static void InitializeCarData(EntityCommandBuffer.ParallelWriter entityManager, Entity entity, int direction,int eqi, float maxCarSpeed){

        entityManager.AddComponent(eqi,entity, new Rotation{Value = Quaternion.Euler(0f, 0f, CarUtils.ComputeRotation(direction))});

        entityManager.SetComponent(eqi,entity, new VehicleMovementData{
            speed = maxCarSpeed,
            direction = direction,
            velocity = CarUtils.ComputeVelocity(maxCarSpeed, direction),
            initialPosition = new float2(float.NaN, float.NaN),
            offset = new float2(float.NaN, float.NaN)
        });
    }
}

public struct TileStruct{
    public int x;
    public int y;
    public int type; //0 =road, 1 = intersection, 2 = other
    public int g_x;
    public int g_y;
    public bool IsWalkable;
}
