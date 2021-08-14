using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct VehicleMovementData : IComponentData
{
    public float2 speed;
    public int2 direction;

    public int size;
}
