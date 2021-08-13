using Unity.Entities;
using Unity.Transforms;
using Unity.Jobs;

[AlwaysSynchronizeSystem]
public class VehicleMovementSystem : JobComponentSystem
{
    protected override JobHandle OnUpdate(JobHandle inputDeps){

        // float dt = Time.DeltaTime;

        // Entities.ForEach((ref Translation translation) => {
        //     float moveSpeed = 1f;
        //     translation.Value.x += moveSpeed * dt;
        // }).Run();
        return default;
    }
}
