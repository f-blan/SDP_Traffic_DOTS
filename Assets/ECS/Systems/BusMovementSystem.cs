using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.Mathematics;

public class BusMovementSystem : SystemBase
{
    private const float MAX_BUS_SPEED = 1.0f;
    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;

        Entities.WithAll<BusPathComponent>().ForEach((ref BusPathComponent busPathComponent, ref Translation translation, ref VehicleMovementData vehicleMovementData, ref Rotation rotation) => {
            
            PathElement currentPathElement  = busPathComponent.pathArrayReference.Value.pathArray[busPathComponent.pathIndex]; 

            if(float.IsNaN(vehicleMovementData.initialPosition.x)){
                vehicleMovementData.initialPosition.x = translation.Value.x; //Set initial position to the starting position of the vehicle
                vehicleMovementData.initialPosition.y = translation.Value.y;

                vehicleMovementData.speed = MAX_BUS_SPEED;
                vehicleMovementData.velocity = CarUtils.ComputeVelocity(MAX_BUS_SPEED, vehicleMovementData.direction); //Create a velocity vector with respect to the direction

                vehicleMovementData.offset = 
                    CarUtils.ComputeOffset(
                        currentPathElement.cost[busPathComponent.verse == -1 ? 0 : 1],
                        vehicleMovementData.direction, 
                        busPathComponent.pathArrayReference.Value.pathArray[GetPrevPathIndex(ref busPathComponent)].withDirection[busPathComponent.verse == -1 ? 0 : 1], 
                        busPathComponent.pathArrayReference.Value.pathArray[GetNextPathIndex(ref busPathComponent)].withDirection[busPathComponent.verse == -1 ? 0 : 1]);
                vehicleMovementData.stop = false;
            }

            translation.Value.x += vehicleMovementData.velocity.x * dt;
            translation.Value.y += vehicleMovementData.velocity.y * dt;

            if(CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.offset, translation.Value)){

                UpdatePathElement(ref busPathComponent);
                currentPathElement = busPathComponent.pathArrayReference.Value.pathArray[busPathComponent.pathIndex]; 

                vehicleMovementData.direction = currentPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1];
                UpdateVehicleMovementData(ref vehicleMovementData, ref busPathComponent, ref translation);
                rotation = new Rotation{Value = Quaternion.Euler(0, 0, CarUtils.ComputeRotation(currentPathElement.withDirection[busPathComponent.verse == -1 ? 0 : 1]))};
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
        
        Debug.Log(prevDirection);
        Debug.Log(curDirection);
        float2 uTurnOffset = CarUtils.ComputeUTurn(prevDirection, curDirection);

        vehicleMovementData.initialPosition.x += vehicleMovementData.offset.x + uTurnOffset.x;
        vehicleMovementData.initialPosition.y += vehicleMovementData.offset.y + uTurnOffset.y;

        translation.Value.x = vehicleMovementData.initialPosition.x;
        translation.Value.y = vehicleMovementData.initialPosition.y;

        vehicleMovementData.velocity = CarUtils.ComputeVelocity(vehicleMovementData.speed, vehicleMovementData.direction);
        vehicleMovementData.offset = CarUtils.ComputeOffset(currentPathElement.cost[busPathComponent.verse == -1 ? 0 : 1], 
        vehicleMovementData.direction, prevDirection, nextDirection);

        return;
    }
}
