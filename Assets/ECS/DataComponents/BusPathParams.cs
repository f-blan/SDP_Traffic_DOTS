using Unity.Entities;
using Unity.Mathematics;

public struct BusPathParams : IComponentData
{
    public int2 pos1;
    public int2 pos2;
    public int2 pos3; 
}

