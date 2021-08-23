using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;

public class QuadrantSystem : SystemBase
{
    public enum VehicleTrafficLightType{
        VehicleType,
        TrafficLight
    };

    private static NativeMultiHashMap<int, QuadrantVehicleData> nativeMultiHashMapQuadrant;
    private const int quadrantYMultiplier = 1000; //Offset in Y
    private const int quadrantCellSize = 10; //Size of the quadrant
    private const float minimumDistance = 32.0f; //Minimum distance to be considered as close
    private const float epsilonDistance = 0.5f;
    private const float minStopDistance = 2.0f;

    private EntityQueryDesc entityQueryDesc = new EntityQueryDesc{
        Any = new ComponentType[]{
            ComponentType.ReadOnly<VehicleMovementData>(), 
            ComponentType.ReadOnly<TrafficLightComponent>()
        }
    };

    //Function that maps position into an index
    private static int GetPositionHashMapKey(float3 position){
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }
    //Struct passed with important information about the vehicle's situation

    public struct VehicleData{
        public int direction;
        public float2 unitaryVector;
    }

    public struct TrafficLightData{
        public bool isRed;
    }

    public struct QuadrantVehicleData{
        public float3 position;
        public Entity entity;
        public VehicleTrafficLightType type;
        
        public VehicleData vehicleData;
        public TrafficLightData trafficLightData;
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
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x ,quadrantVehicleData, ref closest, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x + (quadrantVehicleData.vehicleData.unitaryVector.y != 0.0f ? 1 : 0) + (quadrantVehicleData.vehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closest, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x - (quadrantVehicleData.vehicleData.unitaryVector.y != 0.0f ? 1 : 0) - (quadrantVehicleData.vehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closest, ref curMinDistance); 
        return closest;
    }
    //Checks whether there is a vehicle close in the quadrant
    public static void ComputeDistance(NativeMultiHashMap<int, QuadrantVehicleData> nativeMultiHashMap, int hashMapKey, QuadrantVehicleData inputQuadrantVehicleData, ref QuadrantVehicleData closest, ref float curMinDistance){

        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantVehicleData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                //Comprehensive check that, depending on the direction the vehicle currently is, it will check if there are vehicles directly in front of it 
                if((inputQuadrantVehicleData.vehicleData.direction % 2 == 0 ? (math.abs(inputQuadrantVehicleData.position.x - quadrantData.position.x) < epsilonDistance) && ((inputQuadrantVehicleData.vehicleData.unitaryVector.y)*quadrantData.position.y > (inputQuadrantVehicleData.vehicleData.unitaryVector.y)*inputQuadrantVehicleData.position.y) : math.abs(inputQuadrantVehicleData.position.y - quadrantData.position.y) < epsilonDistance && ((inputQuadrantVehicleData.vehicleData.unitaryVector.x)*quadrantData.position.x > (inputQuadrantVehicleData.vehicleData.unitaryVector.x)*inputQuadrantVehicleData.position.x)) && quadrantData.entity != inputQuadrantVehicleData.entity){
                    float currentComputedDistance = math.distancesq(inputQuadrantVehicleData.position, quadrantData.position);
                    //Stores the entity if there's no closest entity yet and if the distance to the following vehicle is smaller than the minimum distance
                    if(closest.entity == Entity.Null && currentComputedDistance < minimumDistance){
                        closest.entity = quadrantData.entity;
                        closest.position = quadrantData.position;
                        closest.type = quadrantData.type;
                        closest.vehicleData = quadrantData.vehicleData;
                        closest.trafficLightData = quadrantData.trafficLightData;
                        curMinDistance = currentComputedDistance;
                    }
                    //If the computed distance is smaller than the previously stored distance then store the new closest entity
                    else if(currentComputedDistance < curMinDistance){
                        closest.entity = quadrantData.entity;
                        closest.position = quadrantData.position;
                        closest.vehicleData = quadrantData.vehicleData;
                        closest.trafficLightData = quadrantData.trafficLightData;
                        closest.type = quadrantData.type;
                        curMinDistance = currentComputedDistance; 
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
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        //Deletes all elements currently inside of the hash map
        nativeMultiHashMapQuadrant.Clear();
        if(query.CalculateEntityCount() > nativeMultiHashMapQuadrant.Capacity){
            nativeMultiHashMapQuadrant.Capacity = query.CalculateEntityCount();
        }

        NativeMultiHashMap<int, QuadrantVehicleData>.ParallelWriter quadrantParallelWriter = nativeMultiHashMapQuadrant.AsParallelWriter(); 
        //Adds all elements with VehicleMovementData component to the hashmap 
        Entities.WithAny<VehicleMovementData, TrafficLightComponent>().ForEach((Entity entity, in Translation translation) => {
            if(HasComponent<VehicleMovementData>(entity)){
                VehicleMovementData vehicleMovementData = GetComponent<VehicleMovementData>(entity);
                quadrantParallelWriter.Add(GetPositionHashMapKey(translation.Value), new QuadrantVehicleData{
                    entity = entity,
                    position = translation.Value,
                    vehicleData = new VehicleData{
                        direction = vehicleMovementData.direction,
                        unitaryVector = math.sign(vehicleMovementData.offset)
                    },
                    type = VehicleTrafficLightType.VehicleType
                });
            }
            else if(HasComponent<TrafficLightComponent>(entity)){
                TrafficLightComponent trafficLightComponent = GetComponent<TrafficLightComponent>(entity);
                quadrantParallelWriter.Add(GetPositionHashMapKey(translation.Value), new QuadrantVehicleData{
                    entity = entity,
                    type = VehicleTrafficLightType.TrafficLight,
                    position = translation.Value,
                    trafficLightData = new TrafficLightData{
                        isRed = trafficLightComponent.isRed
                    }
                });
            }
        }).WithoutBurst().Run();

        NativeMultiHashMap<int, QuadrantVehicleData> localQuadrant = nativeMultiHashMapQuadrant;
        //Iterates through the VehicleMovementData having components and checks for a closer vehicle
        Entities.WithAll<VehicleMovementData>().ForEach((Entity entity, ref Translation translation, ref VehicleMovementData vehicleMovementData) => { 
            QuadrantVehicleData quadrantData = ComputeClosestInDirection(localQuadrant, 
                GetPositionHashMapKey(translation.Value),
                new QuadrantVehicleData(){
                    entity = entity,
                    position = translation.Value,
                    vehicleData = new VehicleData{
                        direction = vehicleMovementData.direction,
                        unitaryVector = math.sign(vehicleMovementData.offset)
                    }
                });
            if(quadrantData.entity != Entity.Null){
                Debug.DrawLine(translation.Value, quadrantData.position);
            }
            if(quadrantData.type == VehicleTrafficLightType.TrafficLight && quadrantData.trafficLightData.isRed == true && math.distancesq(translation.Value,quadrantData.position) <= minStopDistance){
                vehicleMovementData.stop = true; 
            }
            else if(quadrantData.type == VehicleTrafficLightType.VehicleType && math.distancesq(translation.Value, quadrantData.position) <= minStopDistance){
                vehicleMovementData.stop = true;
            }
            else{
                vehicleMovementData.stop = false;
            }
        }).WithReadOnly(localQuadrant).ScheduleParallel();

        // DebugDrawQuadrant(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        return;
    }
}
