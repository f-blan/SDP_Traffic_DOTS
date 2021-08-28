using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
public static class QuadrantUtils  
{
    private const int quadrantYMultiplier = 1000; //Offset in Y
    private const int quadrantCellSize = 5; //Size of the quadrant
    private const float minimumDistance = 8.0f; //Minimum distance to be considered as close
    private const float minimumStopDistance = 2.5f;
    private const float tileSize = 1f;

    private const float epsilonDistance = 0.5f;
    //checks if the position identified by carPosition has an entity of the given type to its relative direction specified by the parameter
    //if true it returns the exact translation component of the found entity inside of foundPosition
    //this function deals automatically with the quadrant conversion of the positions
    public static bool GetHasEntityToRelativeDirection(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, float3 carPosition, int carDirection, int relativeDirection, 
        QuadrantSystem.VehicleTrafficLightType vehicleTrafficLightType, out float3 foundPosition){

        //eg. car is facing down and relative direction is 1 (right) this becomes 3 (absolute left, relative right)
        int absoluteDirection = (carDirection + relativeDirection)%4; 
        
        float3 targetPosition = GetNearTranslationInRelativeDirection(carPosition, carDirection, relativeDirection, 1);


        int hashMapKey = GetPositionHashMapKey(targetPosition);

        /* I'm pretty sure that checking more than one quadrant is not useful, just check the one where targetPosition is
        NativeArray<int2> directionVector = new NativeArray<int2>(4, Allocator.Temp);


        directionVector[0] = new int2(0, 1);
        directionVector[1] = new int2(1, 0);
        directionVector[2] = new int2(0, -1);
        directionVector[3] = new int2(1, 0);

        int absoluteDirectionQuadrantKey = GetPositionHashMapKey(new float3(carPosition.x + directionVector[absoluteDirection].x*quadrantCellSize, carPosition.y + directionVector[absoluteDirection].y*quadrantCellSize, 0));

        if(IsEntityInTargetPosition(nativeMultiHashMap, hashMapKey, targetPosition, carDirection, vehicleTrafficLightType, out foundPosition))
            return true;
        */

        return IsEntityInTargetPosition(nativeMultiHashMap, hashMapKey, targetPosition, carDirection, vehicleTrafficLightType, out foundPosition);
    }
    
    //experiment for computing the stop variable in vehicleMovement data
    public static bool GetStop(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, float3 carPosition, int carDirection, int offset){
        float3 targetPosition = GetNearTranslationInRelativeDirection(carPosition, carDirection, 0, offset);


        int hashMapKey = GetPositionHashMapKey(targetPosition);

        return IsObstacleInTargetPosition(nativeMultiHashMap,hashMapKey,targetPosition, carDirection);

    }
    private static bool IsObstacleInTargetPosition(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, int hashMapKey, float3 targetPosition, int carDirection){
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantSystem.QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                bool expr = quadrantData.type == QuadrantSystem.VehicleTrafficLightType.VehicleType || (quadrantData.type == QuadrantSystem.VehicleTrafficLightType.TrafficLight && quadrantData.trafficLightData.isRed);
                //if this elemenet (a parkSpot since this is the parkSpot hashmap) is in the position to the relative right of the car, return true
                if(expr && isWithinTarget2(targetPosition, quadrantData.position, tileSize/2)){
                    
                    return true;
                }

            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        
        return false;
    }
    public static float3 GetNearTranslationInRelativeDirection(float3 pos, int direction, int relativeDirection, int offset){
        int absoluteDirection = (direction + relativeDirection)%4;
        switch(absoluteDirection){
            case 0:
                return new float3(pos.x +0, pos.y +tileSize*offset, pos.z);
            case 1:
                return new float3(pos.x +tileSize*offset, pos.y +0, pos.z);
            case 2:
                return new float3(pos.x +0, pos.y -tileSize*offset, pos.z);
            case 3:
                return new float3(pos.x -tileSize*offset, pos.y +0, pos.z);
            default:
                return new float3(0, 0, 0);
        }
    }
    private static int GetPositionHashMapKey(float3 position){
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }
    
    private static bool IsEntityInTargetPosition(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, int hashMapKey, 
                    float3 targetPosition, int carDirection, QuadrantSystem.VehicleTrafficLightType vehicleTrafficLightType, out float3 foundPosition){
    
    NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantSystem.QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{

                //if this elemenet (a parkSpot since this is the parkSpot hashmap) is in the position to the relative right of the car, return true
                if(quadrantData.type == vehicleTrafficLightType && isWithinTarget(targetPosition, quadrantData.position, carDirection)){
                    foundPosition = new float3(quadrantData.position.x, quadrantData.position.y, quadrantData.position.z);
                    return true;
                }

            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        foundPosition = new float3(0,0,0);
        return false;

    }
    private static bool isWithinTarget(float3 targetPosition, float3 checkedPosition, int direction){
        
        if(direction%2 == 0){
            if(math.abs(targetPosition.y - checkedPosition.y)<tileSize/2 && math.abs(targetPosition.x - checkedPosition.x) <= tileSize/2)
                return true;
        }else{
            if(math.abs(targetPosition.x - checkedPosition.x)<tileSize/2 && math.abs(targetPosition.y - checkedPosition.y) <= tileSize/2)
                return true;
        }
        return false;
    }
    public static bool isWithinTarget2(float3 targetPosition, float3 checkedPosition, float range){
        if(math.abs(targetPosition.y - checkedPosition.y)<range && math.abs(targetPosition.x - checkedPosition.x) <= range)
                return true;
        
        return false;
    }
}
