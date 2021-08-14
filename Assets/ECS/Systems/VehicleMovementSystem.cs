using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;

[AlwaysSynchronizeSystem]
public class VehicleMovementSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps){

        float dt = Time.DeltaTime;

        Entities.WithAll<CarPathParams>().ForEach((ref Translation translation, in CarPathParams carPathParams) => {
            float moveSpeed = 1f;

            if(carPathParams.direction % 2 == 0){
                if(carPathParams.direction == 0){
                    translation.Value.y += moveSpeed * dt;
                }
                else{
                    translation.Value.y -= moveSpeed * dt;
                }
            }
            else{
                if(carPathParams.direction == 1){
                    translation.Value.x += moveSpeed * dt;
                }
                else{
                    translation.Value.x -= moveSpeed * dt;
                }
                
            }
            
            
        }).Run();
        return default;
    }
}
