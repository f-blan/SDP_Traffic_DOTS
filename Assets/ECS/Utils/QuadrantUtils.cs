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

    private const float lookingTime = 1f;
    private const float epsilonDistance = 0.5f;

    

    

    //checks if the position identified by carPosition has an entity of the given type to its relative direction specified by the parameter
    //if true it returns the exact translation component of the found entity inside of foundPosition
    //this function deals automatically with the quadrant conversion of the positions
    public static bool GetHasEntityToRelativeDirection(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, float3 carPosition, int carDirection, int relativeDirection, 
        QuadrantSystem.VehicleTrafficLightType vehicleTrafficLightType, out QuadrantSystem.QuadrantData foundEntity, float offset, float range){

        //eg. car is facing down and relative direction is 1 (right) this becomes 3 (absolute left, relative right)
        int absoluteDirection = (carDirection + relativeDirection)%4; 
        
        float3 targetPosition = GetNearTranslationInRelativeDirection(carPosition, carDirection, relativeDirection, offset);
        


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

        return IsEntityInTargetPosition(nativeMultiHashMap, hashMapKey, targetPosition, carDirection, vehicleTrafficLightType, out foundEntity, range, false, -1);
    }
    
    //experiment for computing the stop variable in vehicleMovement data
    public static bool GetStop(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, float3 carPosition, int carDirection, int offset, out bool tl){
        float3 targetPosition = GetNearTranslationInRelativeDirection(carPosition, carDirection, 0, offset);


        int hashMapKey = GetPositionHashMapKey(targetPosition);

        return IsObstacleInTargetPosition(nativeMultiHashMap,hashMapKey,targetPosition, carDirection, out tl);

    }
    private static bool IsObstacleInTargetPosition(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, int hashMapKey, float3 targetPosition, int carDirection, out bool tl){
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantSystem.QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                bool expr = quadrantData.type == QuadrantSystem.VehicleTrafficLightType.VehicleType || (quadrantData.type == QuadrantSystem.VehicleTrafficLightType.TrafficLight && quadrantData.trafficLightData.isRed);
                //if this elemenet (a parkSpot since this is the parkSpot hashmap) is in the position to the relative right of the car, return true
                if(expr && isWithinTarget2(targetPosition, quadrantData.position, tileSize/2)){
                    tl = true;
                    return true;
                }

            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        tl = false;
        return false;
    }
    public static float3 GetNearTranslationInRelativeDirection(float3 pos, int direction, int relativeDirection, float offset){
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
    
    public static bool IsEntityInTargetPosition(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, int hashMapKey, 
                    float3 targetPosition, int carDirection, QuadrantSystem.VehicleTrafficLightType vehicleTrafficLightType, out QuadrantSystem.QuadrantData foundEntity, float range, bool isSurpassable, int turnState){
    
    NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantSystem.QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{

                //if this elemenet (a parkSpot since this is the parkSpot hashmap) is in the position to the relative right of the car, return true
                if(quadrantData.type == vehicleTrafficLightType && isWithinTarget2(targetPosition, quadrantData.position, range)){
                    if(!quadrantData.vehicleData.isSurpassable && !isSurpassable){
                        foundEntity = quadrantData;
                        return true;
                    }else if(quadrantData.vehicleData.isSurpassable != isSurpassable && quadrantData.vehicleData.direction !=carDirection){
                        foundEntity = quadrantData;
                        return true;
                    }else if(quadrantData.vehicleData.isSurpassable && isSurpassable && turnState == quadrantData.vehicleData.turnState){
                        foundEntity = quadrantData;
                        return true;
                    }
                }

            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }
        foundEntity = new QuadrantSystem.QuadrantData{};
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

    public static bool TurningHandler(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap,int turnState,ref VehicleMovementData vehicleMovementData, float3 curPosition, float dt, float MAX_STARVATION_TIMER){
        float3 reference = new float3(vehicleMovementData.intersectionOffset.x, vehicleMovementData.intersectionOffset.y, 0);
        float3 tile1, tile2, tile3, tile4, tile5;
        int hashMapKey;
        QuadrantSystem.QuadrantData dummy;
        vehicleMovementData.stopTime += dt;
        vehicleMovementData.StarvationTimer += dt;
        
        if(vehicleMovementData.StarvationTimer >= MAX_STARVATION_TIMER){
            return false;
        }
        if(vehicleMovementData.stopTime <= lookingTime){
            return true;
        }
        switch(turnState){
            case 0:
                //give precedence to cars coming from your right and don't get into intersection if there's no space for you
                tile1= QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0,2f);
                tile2 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,1,1f);
                tile4 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,1,2f);
                tile3 = QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0, 3f);
                hashMapKey = GetPositionHashMapKey(tile2);
                
                //precedence rule is valid only if intersection is not regulated by semamphores
                if(!vehicleMovementData.trafficLightintersection){
                    if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile2,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2, vehicleMovementData.isSurpassable, turnState)){ 
                        //we don't stop if we're already in front of the car that we're supposed to give precedence to
                        tile2 = GetNearTranslationInRelativeDirection(dummy.position, dummy.vehicleData.direction,0,1.2f);

                        if(!dummy.vehicleData.stop) return !isWithinTarget2(curPosition, tile2, 0.5f);
                        if(dummy.vehicleData.stop && dummy.vehicleData.turnState == 1) return !isWithinTarget2(curPosition,tile2,0.5f);
                    }
                    hashMapKey = GetPositionHashMapKey(tile4);
                    if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile4,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){ 
                        //we don't stop if we're already in front of the car that we're supposed to give precedence to
                        tile4 = GetNearTranslationInRelativeDirection(dummy.position, dummy.vehicleData.direction,0,1.2f);
                        if(!dummy.vehicleData.stop) return !isWithinTarget2(curPosition, tile4, 0.5f);
                    }
                }
                hashMapKey = GetPositionHashMapKey(tile1);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile1,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                    if(!isWithinTarget2(tile1, curPosition, tileSize*3/5)){
                        return true;
                    }
                }
                hashMapKey = GetPositionHashMapKey(tile3);
                return IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState);
            case 1:
                //don't get into the intersection if there's no space for you after turning (ez)
                tile1= QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0,1f);
                tile2 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,1,1f);
                tile3 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,1,2f);
                hashMapKey = GetPositionHashMapKey(tile3);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)) return true;
                hashMapKey = GetPositionHashMapKey(tile2);
                return IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile2,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize*3/5,vehicleMovementData.isSurpassable, turnState);
            case 3:
                //cars turning left can create some problem, it's better to make them wait for a while after they check the road
                //Debug.Log(dt);
                
                //give precedence to cars on the right and coming in front, get into the intersection only if you can turn left immediately and there's space after turning
                tile1= QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0,2f);
                tile2 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,3,1f);
                tile3 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,1,1f);
                tile4 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,3,2f);
                tile5 = QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0,1f);
                
                hashMapKey = GetPositionHashMapKey(tile1);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile1,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                    if(!isWithinTarget2(curPosition, tile1, 0.5f)){
                        return true;
                    }
                }
                /*
                hashMapKey = GetPositionHashMapKey(tile5);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile5,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize*3/6)){
                    if(!isWithinTarget2(curPosition, tile5, 0.5f)){
                        return true;
                    }
                }*/

                hashMapKey = GetPositionHashMapKey(tile2);
                /*
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile2,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize*3/5)){
                    return dummy.vehicleData.targetDirection != vehicleMovementData.direction;
                }*/
                
                hashMapKey = GetPositionHashMapKey(tile2);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile2,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                    if(dummy.vehicleData.direction == (vehicleMovementData.direction+3)%4 && dummy.vehicleData.stop){
                        //traffic congestion tradeoff (we stop only if a car is in the square, is in stop mode and is not going the same way as our car)
                        return true;
                    }
                };
                
                
                if(!vehicleMovementData.trafficLightintersection) {
                    
                    
                    hashMapKey = GetPositionHashMapKey(tile3);
                    
                    if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                        
                        
                        tile3 = GetNearTranslationInRelativeDirection(dummy.position, dummy.vehicleData.direction,0,1f);
                        if(!dummy.vehicleData.stop && !isWithinTarget2(curPosition, tile3, 1.25f)){
                            return true;
                        }
                    }
                    tile3 = GetNearTranslationInRelativeDirection(tile1, vehicleMovementData.direction,1,2f);
                    
                    hashMapKey = GetPositionHashMapKey(tile3);
                    if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                        
                        tile3 = GetNearTranslationInRelativeDirection(dummy.position, dummy.vehicleData.direction,0,1f);
                        if(!dummy.vehicleData.stop && !isWithinTarget2(curPosition, tile3, 1.25f)){
                            return true;
                        }
                    }
                }
                tile3 = GetNearTranslationInRelativeDirection(tile2, vehicleMovementData.direction,0,1f);
                hashMapKey = GetPositionHashMapKey(tile3);
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState)){
                    if(dummy.vehicleData.direction == (vehicleMovementData.direction+3)%4 && !isWithinTarget2(tile3, curPosition, tileSize/2)){
                        return true;
                    }
                }
                
                hashMapKey = GetPositionHashMapKey(tile4);
                return IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile4,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2,vehicleMovementData.isSurpassable, turnState);
            case 2:
                //perform U turn only if there's space for you
                tile1= QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,0,1f);
                tile2 = QuadrantUtils.GetNearTranslationInRelativeDirection(tile1,vehicleMovementData.direction,3,1f);
                tile3 = QuadrantUtils.GetNearTranslationInRelativeDirection(reference, vehicleMovementData.direction,3,1f);

                hashMapKey = GetPositionHashMapKey(tile2);
                
                if(IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile2,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize*4/10,vehicleMovementData.isSurpassable, turnState)){
                    if(dummy.vehicleData.direction == (vehicleMovementData.direction+2)%4 && dummy.vehicleData.stop){
                        return true;
                    }
                }
                return false;
                /*hashMapKey = GetPositionHashMapKey(tile3);
                return IsEntityInTargetPosition(nativeMultiHashMap,hashMapKey,tile3,vehicleMovementData.direction,QuadrantSystem.VehicleTrafficLightType.VehicleType, out dummy, tileSize/2);
            */default:
                return false;
        }   
    }
}
