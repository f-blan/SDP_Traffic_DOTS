using Unity.Entities;
using Unity.Mathematics;

public struct TrafficLightComponent : IComponentData
{
    public bool isRed;
    public bool isVertical;
    public float3 baseTranslation;
    public float greenLightDuration;
    public float timer;
    public int state;
}
