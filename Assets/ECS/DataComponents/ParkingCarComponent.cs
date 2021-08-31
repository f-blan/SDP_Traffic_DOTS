using Unity.Entities;

public struct ParkingCarComponent : IComponentData 
{
    int target_x;
    int target_y;
    int cur_x;
    int cur_y;
}
