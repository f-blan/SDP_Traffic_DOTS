using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class BusMovementSystem : SystemBase
{
    private float globalMaxBusSpeed;
    private const float MAX_STOP_TIME = 2.0f;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        globalMaxBusSpeed = Map_Spawner.instance.maxBusSpeed;
    }

    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;
        float maxBusSpeed = globalMaxBusSpeed;


        Entities.WithAll<BusPathComponent>().ForEach((Entity entity, int entityInQueryIndex, ref BusPathComponent busPathComponent, ref Translation translation, ref VehicleMovementData vehicleMovementData, ref Rotation rotation) => {
            
            PathElement currentPathElement  = busPathComponent.pathArrayReference.Value.pathArray[busPathComponent.pathIndex]; 
            if(vehicleMovementData.state == 3){
                //this is handled by quadrantSystem
                return;
            }
            if(vehicleMovementData.state == 6){
                vehicleMovementData.parkingTimer+= dt;
                if(vehicleMovementData.parkingTimer >= MAX_STOP_TIME){
                    vehicleMovementData.parkingTimer = 0;
                    vehicleMovementData.state = 3;
                }
                return;
            }
            if(CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.intersectionOffset, translation.Value) && vehicleMovementData.turningState == -1){
                vehicleMovementData.turningState = CarUtils.ComputeTurnState(vehicleMovementData.direction, vehicleMovementData.targetDirection);
                //temporary solution: save the reference tile into intersectionOffset
                vehicleMovementData.intersectionOffset.x = translation.Value.x;
                vehicleMovementData.intersectionOffset.y = translation.Value.y;
                
            }
            if(vehicleMovementData.stop){
                return;
            }
            /*
            if(vehicleMovementData.stopTime < 0){
                vehicleMovementData.stopTime += dt;
                return;
            }*/

            if(float.IsNaN(vehicleMovementData.initialPosition.x)){
                vehicleMovementData.initialPosition.x = translation.Value.x; //Set initial position to the starting position of the vehicle
                vehicleMovementData.initialPosition.y = translation.Value.y;

                vehicleMovementData.speed = maxBusSpeed;
                vehicleMovementData.velocity = CarUtils.ComputeVelocity(maxBusSpeed, vehicleMovementData.direction); //Create a velocity vector with respect to the direction

                vehicleMovementData.offset = 
                    CarUtils.ComputeOffset(
                        currentPathElement.cost[busPathComponent.verse == -1 ? 0 : 1],
                        vehicleMovementData.direction, 
                        -1, 
                        busPathComponent.pathArrayReference.Value.pathArray[GetNextPathIndex(ref busPathComponent)].withDirection[busPathComponent.verse == -1 ? 0 : 1],
                        out vehicleMovementData.intersectionOffset
                    );
                vehicleMovementData.stop = false;
                PathElement nextPathElement = busPathComponent.pathArrayReference.Value.pathArray[GetNextPathIndex(ref busPathComponent)];
                vehicleMovementData.targetDirection = nextPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1];
                vehicleMovementData.turningState = -1;
                vehicleMovementData.stopTime = 0;
                vehicleMovementData.trafficLightintersection = false;
                vehicleMovementData.isSurpassable = false;
            }

            translation.Value.x += vehicleMovementData.velocity.x * dt;
            translation.Value.y += vehicleMovementData.velocity.y * dt;

            if(CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.offset, translation.Value)){
                UpdatePathElement(ref busPathComponent);
                currentPathElement = busPathComponent.pathArrayReference.Value.pathArray[busPathComponent.pathIndex];
                if(currentPathElement.costToStop[busPathComponent.verse == -1 ? 0 : 1] != -1) vehicleMovementData.state = 5; 

                vehicleMovementData.direction = currentPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1];
                UpdateVehicleMovementData(ref vehicleMovementData, ref busPathComponent, ref translation);
                rotation = new Rotation{Value = Quaternion.Euler(0, 0, CarUtils.ComputeRotation(currentPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1]))};

                PathElement nextPathElement = busPathComponent.pathArrayReference.Value.pathArray[GetNextPathIndex(ref busPathComponent)];
                vehicleMovementData.targetDirection = nextPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1];
                vehicleMovementData.turningState = -1;
                vehicleMovementData.stopTime = 0;
                vehicleMovementData.trafficLightintersection = false;
                vehicleMovementData.isSurpassable=false;
                //vehicleMovementData.stopTime = -MAX_STOP_TIME;

            }

            return;
        }).ScheduleParallel();

        return;
    }

    protected static void UpdatePathElement(ref BusPathComponent busPathComponent){

        busPathComponent.pathIndex = GetNextPathIndex(ref busPathComponent);

        return;
    }

    protected static int GetNextPathIndex(ref BusPathComponent busPathComponent){

        if(busPathComponent.verse == -1){
            return (busPathComponent.pathIndex + 1) % busPathComponent.pathLength;
        }
        else{
            return (busPathComponent.pathIndex + (busPathComponent.pathLength - 1)) % busPathComponent.pathLength;
        }
    }
    protected static int GetPrevPathIndex(ref BusPathComponent busPathComponent){

        if(busPathComponent.verse == 1){
            return (busPathComponent.pathIndex + 1) % busPathComponent.pathLength;
        }
        else{
            return (busPathComponent.pathIndex + (busPathComponent.pathLength - 1)) % busPathComponent.pathLength;
        }
    }

    protected static void UpdateVehicleMovementData(ref VehicleMovementData vehicleMovementData, ref BusPathComponent busPathComponent, ref Translation translation){
        
        PathElement currentPathElement = busPathComponent.pathArrayReference.Value.pathArray[busPathComponent.pathIndex];

        int curDirection = vehicleMovementData.direction;
        int nextDirection = busPathComponent.pathArrayReference.Value.pathArray[GetNextPathIndex(ref busPathComponent)].withDirection[busPathComponent.verse == -1 ? 0 : 1];
        int prevDirection = busPathComponent.pathArrayReference.Value.pathArray[GetPrevPathIndex(ref busPathComponent)].withDirection[busPathComponent.verse == -1 ? 0 : 1];
        
        //if(busPathComponent.verse==1)
        //Debug.Log(currentPathElement.x +"-" +currentPathElement.y + " "+currentPathElement.withDirection.y);
        //Debug.Log(curDirection);
        float2 uTurnOffset = CarUtils.ComputeUTurn(prevDirection, curDirection);

        vehicleMovementData.initialPosition.x += vehicleMovementData.offset.x + uTurnOffset.x;
        vehicleMovementData.initialPosition.y += vehicleMovementData.offset.y + uTurnOffset.y;

        translation.Value.x = vehicleMovementData.initialPosition.x;
        translation.Value.y = vehicleMovementData.initialPosition.y;

        vehicleMovementData.velocity = CarUtils.ComputeVelocity(vehicleMovementData.speed, vehicleMovementData.direction);
        vehicleMovementData.offset = CarUtils.ComputeOffset(currentPathElement.cost[busPathComponent.verse == -1 ? 0 : 1], 
        vehicleMovementData.direction, prevDirection, nextDirection, out vehicleMovementData.intersectionOffset);

        return;
    }
}
