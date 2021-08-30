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
}
