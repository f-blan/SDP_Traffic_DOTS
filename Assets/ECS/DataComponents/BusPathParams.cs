using Unity.Entities;
using Unity.Mathematics;

public struct BusPathParams : IComponentData
{
    public int2 pos1;
    public int2 pos2;
    public int2 pos3; 

    //the district type of the starting node
    public int pos1DistrictType;
    public Entity entityToSpawn;
}

