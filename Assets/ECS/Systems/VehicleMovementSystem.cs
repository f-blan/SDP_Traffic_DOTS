using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

// [AlwaysSynchronizeSystem] Unsure if this should be here
[UpdateAfter(typeof(CarPathSystem))]
public class VehicleMovementSystem : SystemBase
{
    // private Map<MapTile> map;
    // private float tileSize;
    // private float3 originPosition;
    protected override void OnStartRunning()
    {
        base.OnStartRunning();

        // map = Map_Setup.Instance.CityMap;
        // tileSize = map.GetTileSize();
        // originPosition = new float3(map.GetOriginPosition().x, map.GetOriginPosition().y, map.GetOriginPosition().z);
    }

    protected override void OnUpdate(){

        float dt = Time.DeltaTime;

        //Unsure about WithBurst, found on a tutorial https://www.youtube.com/watch?v=2IYa1jDGTFs
        //For debugging, add WithoutBurst() in the chain and do Debug.Log
        Entities.WithAll<CarPathBuffer>().ForEach((ref Translation translation, ref VehicleMovementData vehicleMovementData, ref DynamicBuffer<CarPathBuffer> carPathBuffer, ref Rotation rotation) => {

            CarPathBuffer lastCarPathBuffer; //Temporary variable for accessing the currently used carPathBuffer

            // //Debug
            // Camera.main.transform.position = new Vector3(translation.Value.x, translation.Value.y, Camera.main.transform.position.z);
            // //End Debug
            
            if(carPathBuffer.IsEmpty){
                //Check if the CarPathBuffer Component contains anything just for a safety measure
                return;
            }

            lastCarPathBuffer = carPathBuffer[carPathBuffer.Length - 1]; //Path list is inverted
            //If not yet initialized, then, initialize
            if(float.IsNaN(vehicleMovementData.initialPosition.x)){
                vehicleMovementData.initialPosition.x = translation.Value.x; //Set initial position to the starting position of the vehicle
                vehicleMovementData.initialPosition.y = translation.Value.y;

                vehicleMovementData.offset = CarUtils.ComputeOffset(lastCarPathBuffer.cost, vehicleMovementData.direction, -1, carPathBuffer[carPathBuffer.Length - 2].withDirection); //Compoutes offset
            }

            translation.Value.x += vehicleMovementData.velocity.x * dt;
            translation.Value.y += vehicleMovementData.velocity.y * dt;
            
            //Checks whether or not the vehicle has already reached its temporary destination
            bool checker = CarUtils.ComputeReachedDestination(vehicleMovementData.direction, vehicleMovementData.initialPosition, vehicleMovementData.offset, translation.Value);

            if(checker){
                //Removing last element from the buffer
                carPathBuffer.RemoveAt(carPathBuffer.Length - 1);

                if(carPathBuffer.IsEmpty){
                    // Final resting position
                    translation.Value.x = vehicleMovementData.initialPosition.x + vehicleMovementData.offset.x;
                    translation.Value.y = vehicleMovementData.initialPosition.y + vehicleMovementData.offset.y;
                    //Reseting values
                    vehicleMovementData.initialPosition.x = float.NaN;
                    vehicleMovementData.initialPosition.y = float.NaN;
                    vehicleMovementData.offset.x = float.NaN;
                    vehicleMovementData.offset.y = float.NaN;
                    //There's no need to remove the carPathBuffer as Unity removes it automatically once it's empty
                    return;
                }

                //U-turn logic, missing animations
                //ComputeUTurn return an offset that moves one space either x or y depending on where the u turn is being performed
                float2 uTurnOffset = CarUtils.ComputeUTurn(vehicleMovementData.direction, carPathBuffer[carPathBuffer.Length - 1].withDirection);

                // Debug 
                // Debug.Log("Initial Position: " + vehicleMovementData.initialPosition);
                // Debug.Log("U turn offset: " + uTurnOffset);
                // Debug.Log("Vehicle movement data offset: " + vehicleMovementData.offset);
                // End debug

                //Update initial position
                vehicleMovementData.initialPosition.x += vehicleMovementData.offset.x + uTurnOffset.x;
                vehicleMovementData.initialPosition.y += vehicleMovementData.offset.y + uTurnOffset.y;

                //Update translation position in case it goes out of bounds (Systems that have vehicles with very large speeds)
                translation.Value.x = vehicleMovementData.initialPosition.x;
                translation.Value.y = vehicleMovementData.initialPosition.y;

                //If there are enough CarPathBuffers remaining, then, compute new offset
                if(carPathBuffer.Length >= 3){
                    vehicleMovementData.offset = CarUtils.ComputeOffset(carPathBuffer[carPathBuffer.Length - 1].cost, carPathBuffer[carPathBuffer.Length - 1].withDirection, vehicleMovementData.direction, carPathBuffer[carPathBuffer.Length - 2].withDirection);
                }
                else{
                    vehicleMovementData.offset = CarUtils.ComputeOffset(carPathBuffer[carPathBuffer.Length - 1].cost, carPathBuffer[carPathBuffer.Length - 1].withDirection, vehicleMovementData.direction, -1);
                }

                lastCarPathBuffer = carPathBuffer[carPathBuffer.Length - 1];

                //Update velocity
                vehicleMovementData.velocity = CarUtils.ComputeVelocity(vehicleMovementData.speed, lastCarPathBuffer.withDirection);
                //Update direction
                vehicleMovementData.direction = lastCarPathBuffer.withDirection;
                //Update rotation
                rotation.Value = Quaternion.Euler(0f,0f,CarUtils.ComputeRotation(lastCarPathBuffer.withDirection));
            }
        }).ScheduleParallel();

        return;
    }
}