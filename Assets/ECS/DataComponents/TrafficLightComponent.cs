using Unity.Entities;
using Unity.Mathematics;

public struct TrafficLightComponent : IComponentData
{
    public bool isRed;
    public bool isVertical;

    public float greenLightDuration;
}
