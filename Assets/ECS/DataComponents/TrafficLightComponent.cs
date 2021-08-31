using Unity.Entities;

public struct TrafficLightComponent : IComponentData
{
    public bool isRed;
    public bool isVertical;
}
