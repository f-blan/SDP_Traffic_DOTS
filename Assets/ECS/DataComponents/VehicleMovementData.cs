using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class VehicleMovementData : IComponentData
{
    public float2 speed;
    public int2 direction;
}
