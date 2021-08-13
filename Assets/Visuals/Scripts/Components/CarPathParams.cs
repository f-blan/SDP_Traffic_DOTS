using Unity.Entities;
using Unity.Mathematics;

public struct CarPathParams : IComponentData
{
    public int direction;
    public int init_cost;
    public int2 startPosition;
    public int2 endPosition;
}
