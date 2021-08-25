using Unity.Collections;
using Unity.Mathematics;

public static class QuadrantUtils  
{
    private const int quadrantYMultiplier = 1000; //Offset in Y
    private const int quadrantCellSize = 10; //Size of the quadrant
    private const float minimumDistance = 8.0f; //Minimum distance to be considered as close
    private const float minimumStopDistance = 2.5f;
    private const float tileSize = 1f;

    private const float epsilonDistance = 0.5f;
    // Start is called before the first frame update
    public static bool GetHasParkSpotToTheRight(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMapParkSpots, float3 carPosition, int carDirection){
        int hashMapKey = GetPositionHashMapKey(carPosition);

        NativeArray<float2> directionVector = new NativeArray<float2>(4, Allocator.Temp);

        directionVector[0] = new float2(0, quadrantCellSize);
        directionVector[1] = new float2(quadrantCellSize, 0);
        directionVector[2] = new float2(0, -quadrantCellSize);
        directionVector[3] = new float2(-quadrantCellSize, 0);

        int relativeRight = (carDirection + 1)%4; 
        
        float2 targetPosition = new float2(carPosition.x + directionVector[relativeRight].x*tileSize, carPosition.y + directionVector[relativeRight].y*tileSize);

        int relativeRightQuadrantKey = GetPositionHashMapKey(new float3(carPosition.x + directionVector[relativeRight].x*quadrantCellSize, carPosition.y + directionVector[relativeRight].y*quadrantCellSize, 0));

        

        if(GetHasParkSpotToTheRightInQuadrant(nativeMultiHashMapParkSpots, GetPositionHashMapKey(carPosition),targetPosition, carDirection)){
            directionVector.Dispose();
            return true;
        }

        if(GetHasParkSpotToTheRightInQuadrant(nativeMultiHashMapParkSpots,relativeRightQuadrantKey, targetPosition,carDirection)){
            directionVector.Dispose();
            return true;
        }

        directionVector.Dispose();
        return false;
        
    }
    private static int GetPositionHashMapKey(float3 position){
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }
    private static bool GetHasParkSpotToTheRightInQuadrant(NativeMultiHashMap<int, QuadrantSystem.QuadrantData> nativeMultiHashMap, int hashMapKey, 
                    float2 targetPosition, int carDirection){
                        
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantSystem.QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                //if this elemenet (a parkSpot since this is the parkSpot hashmap) is in the position to the relative right of the car, return true
                if(isWithinTarget(targetPosition, quadrantData.position, carDirection))
                    return true;

            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }

        return false;
    }

    private static bool isWithinTarget(float2 targetPosition, float3 checkedPosition, int direction){

        if(direction%2 == 0){
            if(math.abs(targetPosition.y - checkedPosition.y)<tileSize/2 && math.abs(targetPosition.x - checkedPosition.x) <= tileSize)
                return true;
        }else{
            if(math.abs(targetPosition.x - checkedPosition.x)<tileSize/2 && math.abs(targetPosition.y - checkedPosition.y) <= tileSize)
                return true;
        }
        return false;
    }
}
