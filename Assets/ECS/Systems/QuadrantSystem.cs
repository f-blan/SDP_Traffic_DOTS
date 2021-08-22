using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

public class QuadrantSystem : SystemBase
{
    private static NativeMultiHashMap<int, QuadrantVehicleData> nativeMultiHashMapQuadrant;
    private const int quadrantYMultiplier = 1000; //Offset in Y
    private const int quadrantCellSize = 10; //Size of the quadrant
    private const float minimumDistance = 1.0f; //Minimum distance to be considered as close
    //Function that maps position into an index
    private static int GetPositionHashMapKey(float3 position){
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }
    //Struct passed with important information about the vehicle's situation
    public struct QuadrantVehicleData{
        public float3 position;
        public Entity entity;
        public int direction;
        public float2 unitaryVector;
    };
    //Debug function unused on final product
    private static void DebugDrawQuadrant(float3 position){
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantCellSize)*quadrantCellSize, (quadrantCellSize * math.floor(position.y / quadrantCellSize)));
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(1, 0)*quadrantCellSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(0, 1)*quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(0, 1)*quadrantCellSize, lowerLeft + new Vector3(1, 1)*quadrantCellSize);
        Debug.DrawLine(lowerLeft + new Vector3(1, 0)*quadrantCellSize, lowerLeft + new Vector3(1, 1)*quadrantCellSize);
    }
    //Test code unused in final code 
    private static int GetEntityCountInHashMap(NativeMultiHashMap<int,Entity> nativeMultiHashMap, int hashMapKey){
        Entity entity;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out entity, out nativeMultiHashMapIterator)){
            do{
                count++;
            }while(nativeMultiHashMap.TryGetNextValue(out entity, ref nativeMultiHashMapIterator));
        }

        return count;
    }

    private static QuadrantVehicleData ComputeClosestInDirection(NativeMultiHashMap<int, QuadrantVehicleData> nativeMultiHashMap, int hashMapKey, QuadrantVehicleData quadrantVehicleData){

        QuadrantVehicleData closest = new QuadrantVehicleData{
            entity = Entity.Null,
            position = float3.zero
        };

        float curMinDistance = -1.0f;
        //Checks if there are vehicles close in the neighbouring quadrants
        ComputeDistance(nativeMultiHashMap, hashMapKey, quadrantVehicleData, ref closest, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.unitaryVector.x ,quadrantVehicleData, ref closest, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.unitaryVector.x + (quadrantVehicleData.unitaryVector.y != 0.0f ? 1 : 0) + (quadrantVehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closest, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.unitaryVector.x - (quadrantVehicleData.unitaryVector.y != 0.0f ? 1 : 0) - (quadrantVehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closest, ref curMinDistance); 
        return closest;
    }
    //Checks whether there is a vehicle close in the quadrant
    public static void ComputeDistance(NativeMultiHashMap<int, QuadrantVehicleData> nativeMultiHashMap, int hashMapKey, QuadrantVehicleData inputQuadrantVehicleData, ref QuadrantVehicleData closest, ref float curMinDistance){

        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantVehicleData quadrantData;
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                // Debug.Log(inputQuadrantVehicleData.entity);
                // Debug.Log(inputQuadrantVehicleData.position.x + "," +inputQuadrantVehicleData.position.y);
                // Debug.Log(inputQuadrantVehicleData.direction);
                //TODO: Missing check in front of the vehicle
                if((inputQuadrantVehicleData.direction % 2 == 0 ? inputQuadrantVehicleData.position.x == quadrantData.position.x : inputQuadrantVehicleData.position.y == quadrantData.position.y)
                    && quadrantData.entity != inputQuadrantVehicleData.entity){
                    if(closest.entity == Entity.Null){
                        closest.entity = quadrantData.entity;
                        closest.position = quadrantData.position;
                        closest.direction = quadrantData.direction;
                        closest.unitaryVector = quadrantData.unitaryVector;
                        curMinDistance = math.distancesq(inputQuadrantVehicleData.position, quadrantData.position);
                    }
                    else if(math.distancesq(inputQuadrantVehicleData.position, quadrantData.position) < curMinDistance){
                        closest.entity = quadrantData.entity;
                        closest.position = quadrantData.position;
                        closest.direction = quadrantData.direction;
                        closest.unitaryVector = quadrantData.unitaryVector;
                        curMinDistance = math.distancesq(inputQuadrantVehicleData.position, quadrantData.position);
                    }
                } 
            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }

        return;
    }
    //Creates the NativeMultiHashMap
    protected override void OnCreate(){
        nativeMultiHashMapQuadrant = new NativeMultiHashMap<int, QuadrantVehicleData>(0, Allocator.Persistent);
        return;
    }
    //Disposes
    protected override void OnDestroy(){
        nativeMultiHashMapQuadrant.Dispose();
    }

    protected override void OnUpdate(){ 
        //Query that gets all elements with a Translation
        EntityQuery query = GetEntityQuery(typeof(VehicleMovementData)); 
        //Deletes all elements currently inside of the hash map
        nativeMultiHashMapQuadrant.Clear();
        if(query.CalculateEntityCount() > nativeMultiHashMapQuadrant.Capacity){
            nativeMultiHashMapQuadrant.Capacity = query.CalculateEntityCount();
        }

        NativeMultiHashMap<int, QuadrantVehicleData>.ParallelWriter quadrantParallelWriter = nativeMultiHashMapQuadrant.AsParallelWriter(); 
        //Adds all elements with VehicleMovementData component to the hashmap 
        Entities.WithAll<VehicleMovementData>().ForEach((Entity entity, in Translation translation, in VehicleMovementData vehicleMovementData) => {
            quadrantParallelWriter.Add(GetPositionHashMapKey(translation.Value), new QuadrantVehicleData{
                entity = entity,
                position = translation.Value,
                direction = vehicleMovementData.direction,
                unitaryVector = math.sign(vehicleMovementData.offset)
            });
        }).ScheduleParallel();

        NativeMultiHashMap<int, QuadrantVehicleData> localQuadrant = nativeMultiHashMapQuadrant;
        //Iterates through the VehicleMovementData having components and checks for a closer vehicle
        Entities.WithAll<VehicleMovementData>().ForEach((Entity entity, ref Translation translation, in VehicleMovementData vehicleMovementData) => { 
            QuadrantVehicleData quadrantData = ComputeClosestInDirection(localQuadrant, 
                GetPositionHashMapKey(translation.Value),
                new QuadrantVehicleData(){
                    entity = entity,
                    position = translation.Value,
                    direction = vehicleMovementData.direction,
                    unitaryVector = math.sign(vehicleMovementData.offset)
                });
            if(quadrantData.entity != Entity.Null){
                Debug.DrawLine(translation.Value, quadrantData.position);
            }
        }).WithReadOnly(localQuadrant).ScheduleParallel();

        // DebugDrawQuadrant(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        return;
    }
}
