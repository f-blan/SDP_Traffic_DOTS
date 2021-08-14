using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;

// [AlwaysSynchronizeSystem] Unsure if this should be here
public class VehicleMovementSystem : SystemBase
{
    protected override void OnUpdate(){

        float dt = Time.DeltaTime;

        //Unsure about WithBurst, found on a tutorial https://www.youtube.com/watch?v=2IYa1jDGTFs

        Entities.WithBurst(synchronousCompilation:true).WithAll<CarPathBuffer>().ForEach((ref Translation translation, ref VehicleMovementData vehicleMovementData, ref DynamicBuffer<CarPathBuffer> carPathBuffer) => {
            float moveSpeed = 1f;

            int lastCarPathIndex = carPathBuffer.Length - 1; //Unsure if the path is inverted or not, needs a double check
            CarPathBuffer lastCarPathBuffer = carPathBuffer[lastCarPathIndex];
            vehicleMovementData.direction = lastCarPathBuffer.withDirection;

            // Moving into the appropriate direction, very weird code check for optimization
            if(lastCarPathBuffer.withDirection % 2 == 0){
                if(lastCarPathBuffer.withDirection == 0){
                    translation.Value.y += moveSpeed * dt;
                }
                else{
                    translation.Value.y -= moveSpeed * dt;
                }
            }
            else if(lastCarPathBuffer.withDirection == 1){
                translation.Value.x += moveSpeed * dt;
            }
            else{
                translation.Value.x -= moveSpeed * dt;
            }

            //If the car reaches the given node, then, remove the last path element from the buffer
            if(translation.Value.x >=lastCarPathBuffer.x && translation.Value.y >=lastCarPathBuffer.y){
                carPathBuffer.RemoveAt(lastCarPathIndex);
            }
        // Missing parallel scheduling
        }).Run();

        return;
    }
}
