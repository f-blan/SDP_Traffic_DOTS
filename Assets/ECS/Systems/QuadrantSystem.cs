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
        TrafficLight,
        ParkSpot,
        BusStop,
    };

    private static NativeMultiHashMap<int, QuadrantData> nativeMultiHashMapQuadrant;
    //franco: i'm creating another one since looking for a car spot is an operation relevant only to parking cars
    private static NativeMultiHashMap<int, QuadrantData> nativeMultiHashMapQuadrantParkSpots;
    private static NativeMultiHashMap<int, QuadrantData> nativeMultiHashMapQuadrantBusStops;
    private const int quadrantYMultiplier = 1000; //Offset in Y
    private const int quadrantCellSize = 5; //Size of the quadrant
    private const float minimumDistance = 4.0f; //Minimum distance to be considered as close
    private const float minimumStopDistance = 2.5f;

    private const float epsilonDistance = 0.5f;
    private const float tileSize = 1f;

    private bool isParkSpotMapValid;

    private EntityQueryDesc entityQueryDescVehicles = new EntityQueryDesc{
        Any = new ComponentType[]{
            ComponentType.ReadOnly<VehicleMovementData>()
        }
    };
    private EntityQueryDesc entityQueryDesc = new EntityQueryDesc{
        Any = new ComponentType[]{
            ComponentType.ReadOnly<VehicleMovementData>(), 
            ComponentType.ReadOnly<TrafficLightComponent>()
        }
    };

    private EntityQueryDesc entityQueryDescParkSpots = new EntityQueryDesc{
        Any = new ComponentType[]{
            ComponentType.ReadOnly<ParkSpotTag>()
        }
    };

    private EntityQueryDesc entityQueryDescBusStops = new EntityQueryDesc{
        Any = new ComponentType[]{
            ComponentType.ReadOnly<BusStopTag>()
        }
    };

    //Function that maps position into an index
    private static int GetPositionHashMapKey(float3 position){
        return (int) (math.floor(position.x / quadrantCellSize) + (quadrantYMultiplier * math.floor(position.y / quadrantCellSize)));
    }
    //Struct passed with important information about the vehicle's situation

    public struct VehicleData{
        public int direction;
        public int targetDirection;
        public float2 unitaryVector;
        public bool isBus;
        public bool stop;
        public int turnState;
    }

    public struct TrafficLightData{
        public bool isRed;
    }

    public struct QuadrantData{
        public float3 position;
        public Entity entity;
        public VehicleTrafficLightType type;
        
        public float distance;

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

    private static void ComputeClosestInDirection(NativeMultiHashMap<int, QuadrantData> nativeMultiHashMap, int hashMapKey, QuadrantData quadrantVehicleData, ref NativeArray<QuadrantData> closestEntitiesNativeArray){

        QuadrantData closest = new QuadrantData{
            entity = Entity.Null,
            position = float3.zero
        };

        closestEntitiesNativeArray[0] = closest;
        closestEntitiesNativeArray[1] = closest;
        
        float curMinDistance = -1.0f;
        
        ComputeDistance(nativeMultiHashMap, hashMapKey, quadrantVehicleData, ref closestEntitiesNativeArray, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x ,quadrantVehicleData, ref closestEntitiesNativeArray, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x + (quadrantVehicleData.vehicleData.unitaryVector.y != 0.0f ? 1 : 0) + (quadrantVehicleData.vehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closestEntitiesNativeArray, ref curMinDistance); 
        ComputeDistance(nativeMultiHashMap, hashMapKey + (int) quadrantVehicleData.vehicleData.unitaryVector.y * quadrantYMultiplier + (int) quadrantVehicleData.vehicleData.unitaryVector.x - (quadrantVehicleData.vehicleData.unitaryVector.y != 0.0f ? 1 : 0) - (quadrantVehicleData.vehicleData.unitaryVector.x != 0.0f ? quadrantYMultiplier : 0), quadrantVehicleData, ref closestEntitiesNativeArray, ref curMinDistance); 
        
        return;
    }
    //Checks whether there is a vehicle close in the quadrant
    public static void ComputeDistance(NativeMultiHashMap<int, QuadrantData> nativeMultiHashMap, int hashMapKey, QuadrantData inputQuadrantData, ref NativeArray<QuadrantData> closestEntitiesNativeArray, ref float curMinDistance){
        
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        QuadrantData quadrantData;
        //Iterate through all of the elements in the current bucket
        if(nativeMultiHashMap.TryGetFirstValue(hashMapKey, out quadrantData, out nativeMultiHashMapIterator)){
            do{
                //Comprehensive check that, depending on the direction the vehicle currently is, it will check if there are vehicles directly in front of it 
                if((inputQuadrantData.vehicleData.direction % 2 == 0 ? 
                    (math.abs(inputQuadrantData.position.x - quadrantData.position.x) < epsilonDistance) && ((inputQuadrantData.vehicleData.unitaryVector.y)*quadrantData.position.y > (inputQuadrantData.vehicleData.unitaryVector.y)*inputQuadrantData.position.y) : 
                    math.abs(inputQuadrantData.position.y - quadrantData.position.y) < epsilonDistance && ((inputQuadrantData.vehicleData.unitaryVector.x)*quadrantData.position.x > (inputQuadrantData.vehicleData.unitaryVector.x)*inputQuadrantData.position.x)) && quadrantData.entity != inputQuadrantData.entity){
                    float currentComputedDistance = math.distancesq(inputQuadrantData.position, quadrantData.position);
                    //Stores the entity if there's no closest entity yet and if the distance to the following vehicle is smaller than the minimum distance
                    if(closestEntitiesNativeArray[0].entity == Entity.Null && currentComputedDistance < minimumDistance){
                        quadrantData.distance = currentComputedDistance;
                        closestEntitiesNativeArray[0] = quadrantData;
                        curMinDistance = currentComputedDistance;
                    }
                    //If the computed distance is smaller than the previously stored distance then store the new closest entity
                    else if(currentComputedDistance < curMinDistance){
                        closestEntitiesNativeArray[1] = closestEntitiesNativeArray[0];
                        quadrantData.distance = currentComputedDistance;
                        closestEntitiesNativeArray[0] = quadrantData;
                        curMinDistance = currentComputedDistance; 
                    }
                } 
            }while(nativeMultiHashMap.TryGetNextValue(out quadrantData, ref nativeMultiHashMapIterator));
        }

        return;
    }

    //Creates the NativeMultiHashMap
    protected override void OnCreate(){
        nativeMultiHashMapQuadrant = new NativeMultiHashMap<int, QuadrantData>(0, Allocator.Persistent);
        
        return;
    }
    //Disposes
    protected override void OnDestroy(){
        nativeMultiHashMapQuadrant.Dispose();
        if(isParkSpotMapValid){ 
            nativeMultiHashMapQuadrantParkSpots.Dispose();
            nativeMultiHashMapQuadrantBusStops.Dispose();
        }
    }   

    protected override void OnUpdate(){ 
        
        //Query that gets all elements with a Translation
        EntityQuery query = GetEntityQuery(entityQueryDesc);
        //Deletes all elements currently inside of the hash map
        nativeMultiHashMapQuadrant.Clear();
        //franco: reset operation is not needed for parkSpot hashmap: They don't move and spawn all together. 
        
        
        if(query.CalculateEntityCount() > nativeMultiHashMapQuadrant.Capacity){
            EntityQuery queryVehicles = GetEntityQuery(entityQueryDescVehicles);
            nativeMultiHashMapQuadrant.Capacity = query.CalculateEntityCount();
            Map_Setup.Instance.runningEntities = queryVehicles.CalculateEntityCount();
        }

        if(isParkSpotMapValid == false){
            //couldn't do thin in OnCreate (race condition)
            nativeMultiHashMapQuadrantParkSpots = new NativeMultiHashMap<int, QuadrantData>(
                    GetEntityQuery(entityQueryDescParkSpots).CalculateEntityCount(), Allocator.Persistent);
            nativeMultiHashMapQuadrantBusStops = new NativeMultiHashMap<int, QuadrantData>(
                    GetEntityQuery(entityQueryDescBusStops).CalculateEntityCount(), Allocator.Persistent);
        }
        
        NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantParallelWriter = nativeMultiHashMapQuadrant.AsParallelWriter();
        NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantParallelWriterParkSpots = nativeMultiHashMapQuadrantParkSpots.AsParallelWriter();
        NativeMultiHashMap<int, QuadrantData>.ParallelWriter quadrantParallelWriterBusStops = nativeMultiHashMapQuadrantBusStops.AsParallelWriter();
        //Adds all elements with VehicleMovementData component to the hashmap 
        Entities.WithAny<VehicleMovementData, TrafficLightComponent>().ForEach((Entity entity, in Translation translation) => {
            if(HasComponent<VehicleMovementData>(entity)){
                
                VehicleMovementData vehicleMovementData = GetComponent<VehicleMovementData>(entity);
                bool isBus = false;
                if(HasComponent<BusPathComponent>(entity)){
                    isBus = true;
                }
                quadrantParallelWriter.Add(GetPositionHashMapKey(translation.Value), new QuadrantData{
                    entity = entity,
                    position = translation.Value,
                    vehicleData = new VehicleData{
                        direction = vehicleMovementData.direction,
                        unitaryVector = math.sign(vehicleMovementData.offset),
                        isBus = isBus,
                        targetDirection = vehicleMovementData.targetDirection,
                        turnState = vehicleMovementData.turningState,
                        stop = vehicleMovementData.stop,
                    },
                    type = VehicleTrafficLightType.VehicleType
                });
                //DebugDrawQuadrant(translation.Value);
            }
            else if(HasComponent<TrafficLightComponent>(entity)){
                TrafficLightComponent trafficLightComponent = GetComponent<TrafficLightComponent>(entity);
                quadrantParallelWriter.Add(GetPositionHashMapKey(translation.Value), new QuadrantData{
                    entity = entity,
                    type = VehicleTrafficLightType.TrafficLight,
                    position = translation.Value,
                    trafficLightData = new TrafficLightData{
                        isRed = trafficLightComponent.isRed
                    }
                });
            }
        }).ScheduleParallel();

        //franco: we do the same for parkSpots and busStops (only once)
        if(isParkSpotMapValid == false){
            isParkSpotMapValid=true;
            Entities.WithAll<ParkSpotTag, Translation>().ForEach((Entity entity, in Translation translation) => {
                quadrantParallelWriterParkSpots.Add(GetPositionHashMapKey(translation.Value), new QuadrantData{
                    entity = entity,
                    type = VehicleTrafficLightType.ParkSpot,
                    position = translation.Value
                });
            }).ScheduleParallel();
            Entities.WithAll<BusStopTag, Translation>().ForEach((Entity entity, in Translation translation) => {
                quadrantParallelWriterBusStops.Add(GetPositionHashMapKey(translation.Value), new QuadrantData{
                    entity = entity,
                    type = VehicleTrafficLightType.BusStop,
                    position = translation.Value
                });
            }).ScheduleParallel();
        }

        NativeMultiHashMap<int, QuadrantData> localQuadrant = nativeMultiHashMapQuadrant;
        NativeMultiHashMap<int, QuadrantData> localQuadrantParkSpots = nativeMultiHashMapQuadrantParkSpots;
        NativeMultiHashMap<int, QuadrantData> localQuadrantBusStops = nativeMultiHashMapQuadrantBusStops;
        //Iterates through the VehicleMovementData having components and checks for a closer vehicle
        Entities.WithAll<VehicleMovementData>().WithReadOnly(localQuadrantParkSpots).ForEach((Entity entity, ref Translation translation, ref VehicleMovementData vehicleMovementData) => { 
            

            //the car is parked, non need to compute the stop variable as it doesn't move
            if(vehicleMovementData.state==2) return;

            //the car/bus is parked and wants to get into the road: we just check if it can (road tile is free) and if so we change its translation component
            if(vehicleMovementData.state == 3){
                QuadrantData dummy;
  
                //if the parked car doesn't have a car on its left it can get into the road
                if(!QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrant, translation.Value, vehicleMovementData.direction, 3, VehicleTrafficLightType.VehicleType, out dummy,1, tileSize*7/10)){
                    //move the car on the roadTile
 
                    translation.Value = QuadrantUtils.GetNearTranslationInRelativeDirection(translation.Value, vehicleMovementData.direction, 3, 1);
                    vehicleMovementData.state = 4;
                }
                
                return;
            }
            /*
            NativeArray<QuadrantData> closestNativeArray = new NativeArray<QuadrantData>(2, Allocator.Temp);
            
            ComputeClosestInDirection(localQuadrant,
                GetPositionHashMapKey(translation.Value),
                new QuadrantData(){
                    entity = entity,
                    position = translation.Value,
                    distance = -1,
                    vehicleData = new VehicleData{
                        direction = vehicleMovementData.direction,
                        unitaryVector = math.sign(vehicleMovementData.offset),
                    }
                }, ref closestNativeArray);
            */
            
            // if(closestNativeArray[1].entity != Entity.Null && closestNativeArray[0].entity != Entity.Null){
            //     Debug.Log("Closest distance " + closestNativeArray[0].distance + "Second closest distance" + closestNativeArray[1].distance);
            // }

            // if(closestNativeArray[1].entity != Entity.Null){
            //     Debug.DrawLine(translation.Value, closestNativeArray[1].position);
            // }
            // if(closestNativeArray[0].entity != Entity.Null){
            //     Debug.DrawLine(translation.Value, closestNativeArray[0].position);
            // }

            //car is looking for a parkSpot, we also want to know if they have a park available to their right
            if(vehicleMovementData.state == 1){
                QuadrantData parkSpot;
                //no reason to check for parkSpot if our car has another car to its right (we're either at an intersection or the parkSpot is taken)
                if(!QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrant, translation.Value, vehicleMovementData.direction,1,VehicleTrafficLightType.VehicleType, out parkSpot,1, tileSize/2)){
                    
                    if(QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrantParkSpots, translation.Value, vehicleMovementData.direction,1, VehicleTrafficLightType.ParkSpot,out parkSpot,1, tileSize/2)){
                        //ParkSpot found: change the state and translation component of the car
                        vehicleMovementData.state = 2;
                        translation.Value = parkSpot.position;
                        vehicleMovementData.parkingTimer = 0;
                    }
                }
            }
            vehicleMovementData.stop = false;

            //state reserved to buses: the bus knows there's a busStop in the current node
            if(vehicleMovementData.state==5){
                float3 rightSquare= QuadrantUtils.GetNearTranslationInRelativeDirection(translation.Value, vehicleMovementData.direction,1,1f);
                float3 topRightSquare = QuadrantUtils.GetNearTranslationInRelativeDirection(rightSquare,vehicleMovementData.direction,0,1f);
                int hashMapKey = GetPositionHashMapKey(topRightSquare);
                QuadrantData qData;
                bool expr = QuadrantUtils.IsEntityInTargetPosition(localQuadrant, hashMapKey,topRightSquare,vehicleMovementData.direction, VehicleTrafficLightType.VehicleType, out qData, tileSize/2);
                
                
                if(expr && qData.vehicleData.isBus){
                    //if there's already a bus in the stop, you wait in a position where the stopped bus can still exit
                    vehicleMovementData.stop = true;
                    return;
                }
                else if(QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrantBusStops, translation.Value, vehicleMovementData.direction,1, VehicleTrafficLightType.BusStop,out qData,1, tileSize*3/5)){
                    //BusStop found: change the state and translation component of the car
                    vehicleMovementData.state = 6;
                    translation.Value = qData.position;
                    vehicleMovementData.parkingTimer = 0;
                    return;
                }
            }

            //vehicleMovementData.stop = QuadrantUtils.GetStop(localQuadrant, translation.Value, vehicleMovementData.direction,1);
            
            //--------------------------------- NEW COLLISION AVOIDANCE BEHAVIOR --------------------------
            if(vehicleMovementData.turningState != -1){
                vehicleMovementData.stop = QuadrantUtils.TurningHandler(localQuadrant,vehicleMovementData.turningState, vehicleMovementData, translation.Value);
                return;
            }
            /*
            bool tl;
            vehicleMovementData.stop = QuadrantUtils.GetStop(localQuadrant,translation.Value, vehicleMovementData.direction, 1f, vehicleMovementData, out tl);
            vehicleMovementData.trafficLightintersection = tl;
            */
            
            QuadrantData obstacle;
            
            if(QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrant, translation.Value, vehicleMovementData.direction,0,VehicleTrafficLightType.VehicleType,out obstacle,.9f,tileSize/2)){
                if(obstacle.vehicleData.direction == vehicleMovementData.direction){ 
                    vehicleMovementData.stop = true;
                    return;
                }
                /*float3 frontTile = QuadrantUtils.GetNearTranslationInRelativeDirection(translation.Value, vehicleMovementData.direction, 0, 1f);
                if((vehicleMovementData.direction+1)%4 == obstacle.vehicleData.direction && !QuadrantUtils.isWithinTarget2(frontTile, obstacle.position, tileSize*3/10)){
                    //Debug.Log("car stop");
                    vehicleMovementData.stop = false;
                }else{
                    vehicleMovementData.stop = true;
                    return;
                }*/
            }
            if(QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrant, translation.Value, vehicleMovementData.direction,0,VehicleTrafficLightType.TrafficLight,out obstacle,.9f,tileSize/2)){
                vehicleMovementData.trafficLightintersection = true;
                if(obstacle.trafficLightData.isRed){
                    //Debug.Log("tl stop");
                    vehicleMovementData.stop = true;
                }
            }
            //---------------------------------------------------------------------------------------
            /* OLD BEHAVIOR (should be compatible, uncomment this and the ComputeClosestInDirection method to use it)
            if(closestNativeArray[0].entity == Entity.Null){
                vehicleMovementData.stop = false;
            }
            else if(closestNativeArray[0].type == VehicleTrafficLightType.TrafficLight && closestNativeArray[0].trafficLightData.isRed && closestNativeArray[0].distance < minimumStopDistance){
                vehicleMovementData.stop = true; 
            }
            else if((closestNativeArray[0].type == VehicleTrafficLightType.VehicleType && closestNativeArray[0].vehicleData.stop) || (closestNativeArray[0].type == VehicleTrafficLightType.VehicleType &&  minimumStopDistance > closestNativeArray[0].distance) || (closestNativeArray[1].type == VehicleTrafficLightType.VehicleType && closestNativeArray[1].vehicleData.stop)){
                //vehicleMovementData.stop = true;
                //we only stop if the closest vehicle is not at our left: give precedence to car on the right
                float3 frontTile = QuadrantUtils.GetNearTranslationInRelativeDirection(translation.Value, vehicleMovementData.direction, 0,1);
                if((closestNativeArray[0].vehicleData.direction+1)%4 == vehicleMovementData.direction && !QuadrantUtils.isWithinTarget2(frontTile, closestNativeArray[0].position, tileSize*5/10)){
                    
                    vehicleMovementData.stop = false;
                }else{
                    vehicleMovementData.stop = true;
                }
            }
            /*else if(closestNativeArray[0].type == VehicleTrafficLightType.TrafficLight && !closestNativeArray[0].trafficLightData.isRed && closestNativeArray[0].distance < minimumStopDistance){
                vehicleMovementData.stop = false;
                //if you have a green traffic light in front, you also check if there are cars blocking your way inside. If yes you stay out of the intersection
                QuadrantData foundEntity;
                bool intrsectionBusy = QuadrantUtils.GetHasEntityToRelativeDirection(localQuadrant, closestNativeArray[0].position,vehicleMovementData.direction,0,VehicleTrafficLightType.VehicleType,out foundEntity,2, tileSize*8/10);
                
                if(intrsectionBusy){ //&& foundEntity.vehicleData.direction != vehicleMovementData.direction){
                    
                    vehicleMovementData.stop=true;
                } else{
                    vehicleMovementData.stop=false;
                }
            }
            else{
                vehicleMovementData.stop = false;
            }*/

            //closestNativeArray.Dispose();
        }).WithReadOnly(localQuadrant).WithReadOnly(localQuadrantBusStops).ScheduleParallel();//WithoutBurst().Run();//ScheduleParallel();
        // }).WithoutBurst().Run();

        // DebugDrawQuadrant(Camera.main.ScreenToWorldPoint(Input.mousePosition));

        return;
    }



}
