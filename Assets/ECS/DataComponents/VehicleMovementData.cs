using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct VehicleMovementData : IComponentData
{
    public float speed;
    public float2 velocity; //Execution speed oriented implementation
    public int direction;

    public float2 initialPosition;
    public float2 offset;

    public bool stop;

    //franco: parameters needed for parking system
    public int state; //0 = following path, 1 = looking for parkSpot, 2 = parked, 3 = trying to get into the road, 4 = ready to start
    //public bool hasParkSpotToTheRight;
    public int startGraphIndex;
    public int curGraphIndex;
    public int targetDirection;
    public float parkingTimer;
}
